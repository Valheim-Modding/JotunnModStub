using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using ItemDrawersKG;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace ItemDrawersKGMod;

public class DrawerComponent : Container, Interactable, Hoverable
{
    public static readonly List<DrawerComponent> AllDrawers = [];
    private static Sprite _defaultSprite;
    public ZNetView _znv { private set; get; }
    private Image _image;
    private TMP_Text _text; 
    
    //UI
    private static bool ShowUI;
    private static DrawerOptions CurrentOptions;
    //

    public string CurrentPrefab
    {
        get => _znv.m_zdo.GetString("Prefab");
        set => _znv.m_zdo.Set("Prefab", value);
    }

    public int CurrentAmount
    {
        get => _znv.m_zdo.GetInt("Amount");
        set => _znv.m_zdo.Set("Amount", value);
    }

    public int PickupRange
    {
        get => _znv.m_zdo.GetInt("PickupRange", ItemDrawersKG.DrawerPickupRange.Value);
        set => _znv.m_zdo.Set("PickupRange", value);
    }

    private Color CurrentColor 
    {
        get => global::Utils.Vec3ToColor(_znv.m_zdo.GetVec3("Color", ItemDrawersKG.DefaultColor.Value));
        set => _znv.m_zdo.Set("Color", global::Utils.ColorToVec3(value));
    }

    public bool ItemValid => !string.IsNullOrEmpty(CurrentPrefab) && ObjectDB.instance.m_itemByHash.ContainsKey(CurrentPrefab.GetStableHashCode());
    private int ItemMaxStack => ObjectDB.instance.m_itemByHash[CurrentPrefab.GetStableHashCode()].GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize;
    private string LocalizedName => ObjectDB.instance.m_itemByHash[CurrentPrefab.GetStableHashCode()].GetComponent<ItemDrop>().m_itemData.m_shared.m_name.Localize();

    private struct DrawerOptions : ISerializableParameter
    {
        public DrawerComponent drawer;
        public Color32 color;
        public int pickupRange;

        public void Serialize(ref ZPackage pkg)
        {
            pkg.Write(global::Utils.ColorToVec3(color));
            pkg.Write(pickupRange);
        }

        public void Deserialize(ref ZPackage pkg)
        {
            color = global::Utils.Vec3ToColor(pkg.ReadVector3());
            pickupRange = pkg.ReadInt();
        }
    }

    private void OnDestroy() => AllDrawers.Remove(this);
    private void Awake()
    {
        _znv = GetComponent<ZNetView>();
        if (!_znv.IsValid()) return;
        AllDrawers.Add(this);
        _image = transform.Find("Cube/Canvas/Image").GetComponent<Image>();
        _defaultSprite ??= _image.sprite;
        _text = transform.Find("Cube/Canvas/Text").GetComponent<TMP_Text>();
        _text.color = CurrentColor;
        _znv.Register<string, int>("AddItem_Request", RPC_AddItem);
        _znv.Register<string, int>("AddItem_Player", RPC_AddItem_Player);
        _znv.Register<int>("WithdrawItem_Request", RPC_WithdrawItem_Request);
        _znv.Register<string, int>("UpdateIcon", RPC_UpdateIcon);
        _znv.Register<int>("ForceRemove", RPC_ForceRemove);
        _znv.Register<DrawerOptions>("ApplyOptions", RPC_ApplyOptions);
        RPC_UpdateIcon(0, CurrentPrefab, CurrentAmount);
        float randomTime = Random.Range(2.5f, 3f);
        InvokeRepeating(nameof(Repeat), randomTime, randomTime);
    }

    private void RPC_ApplyOptions(long sender, DrawerOptions options)
    {
        if (_znv.IsOwner())
        {
            CurrentColor = options.color;
            PickupRange = Mathf.Min(ItemDrawersKG.MaxDrawerPickupRange.Value, options.pickupRange);
        }
        _text.color = options.color;
    }

    private void RPC_ForceRemove(long sender, int amount)
    {
        amount = Mathf.Min(amount, CurrentAmount);
        CurrentAmount -= amount;
        _znv.InvokeRPC(ZNetView.Everybody, "UpdateIcon", CurrentPrefab, CurrentAmount);
    }

    private void RPC_WithdrawItem_Request(long sender, int amount)
    {
        if (CurrentAmount <= 0 || !ItemValid)
        {
            CurrentPrefab = "";
            CurrentAmount = 0;
            _znv.InvokeRPC(ZNetView.Everybody, "UpdateIcon", "", 0);
            return;
        }

        if (amount <= 0) return;
        amount = Mathf.Min(amount, CurrentAmount);
        CurrentAmount -= amount;
        _znv.InvokeRPC(sender, "AddItem_Player", CurrentPrefab, amount);
        _znv.InvokeRPC(ZNetView.Everybody, "UpdateIcon", CurrentPrefab, CurrentAmount);
    }

    private void RPC_AddItem_Player(long _, string prefab, int amount) => UtilsKG.InstantiateItem(ZNetScene.instance.GetPrefab(prefab), amount, 1);

    private void RPC_UpdateIcon(long _, string prefab, int amount)
    {
        if (!ItemValid)
        {
            _image.sprite = _defaultSprite;
            _text.gameObject.SetActive(false);
            return;
        }

        _image.sprite = ObjectDB.instance.GetItemPrefab(prefab).GetComponent<ItemDrop>().m_itemData.GetIcon();
        _text.text = amount.ToString();
        _text.gameObject.SetActive(true);
    }

    private void RPC_AddItem(long sender, string prefab, int amount)
    {
        if (!_znv.IsOwner()) return;
        if (amount <= 0) return;
        if (ItemValid && CurrentPrefab != prefab)
        {
            UtilsKG.InstantiateAtPos(ZNetScene.instance.GetPrefab(prefab), amount, 1, transform.position + Vector3.up * 1.5f);
            return;
        }

        int newAmount = ItemValid ? (CurrentAmount + amount) : amount;
        CurrentAmount = newAmount;
        if (CurrentPrefab != prefab) CurrentPrefab = prefab;
        _znv.InvokeRPC(ZNetView.Everybody, "UpdateIcon", prefab, newAmount);
    }

    private bool DoRepeat => Player.m_localPlayer && ItemValid && PickupRange > 0;
    private void Repeat()
    {
        if (!_znv.IsOwner()) return;
        if (!DoRepeat) return;

        Vector3 vector = transform.position + Vector3.up;
        foreach (ItemDrop component in ItemDrop.s_instances.Where(drop => Vector3.Distance(drop.transform.position, vector) < PickupRange))
        {
            string goName = global::Utils.GetPrefabName(component.gameObject);
            if (goName != CurrentPrefab) continue;
            if (!component.CanPickup(false))
            {
                component.RequestOwn();
                continue;
            }

            Instantiate(ItemDrawersKG.Explosion, component.transform.position, Quaternion.identity);
            int amount = component.m_itemData.m_stack;
            component.m_nview.ClaimOwnership();
            ZNetScene.instance.Destroy(component.gameObject);
            CurrentAmount += amount;
            _znv.InvokeRPC(ZNetView.Everybody, "UpdateIcon", CurrentPrefab, CurrentAmount);
        }
    }

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (!ItemValid) return false;

        if (user.IsCrouching())
        {
            CurrentOptions.drawer = this;
            CurrentOptions.color = CurrentColor;
            CurrentOptions.pickupRange = PickupRange;
            ShowUI = true;
            return true;
        }

        if (Input.GetKey(KeyCode.LeftAlt))
        {
            _znv.InvokeRPC("WithdrawItem_Request", 1);
            return true;
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            int amount = UtilsKG.CustomCountItems(CurrentPrefab, 1);
            if (amount <= 0) return true;
            UtilsKG.CustomRemoveItems(CurrentPrefab, amount, 1);
            _znv.InvokeRPC("AddItem_Request", CurrentPrefab, amount);
            return true;
        }

        _znv.InvokeRPC("WithdrawItem_Request", ItemMaxStack);
        return true;
    }


    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        string dropPrefab = item.m_dropPrefab?.name;
        if (string.IsNullOrEmpty(dropPrefab)) return false;

        if ((item.IsEquipable() || item.m_shared.m_maxStackSize <= 1) && !ItemDrawersKG.IncludeSet.Contains(dropPrefab)) return false;

        if (!string.IsNullOrEmpty(CurrentPrefab) && CurrentPrefab != dropPrefab) return false;

        int amount = item.m_stack;
        if (amount <= 0) return false;
        user.m_inventory.RemoveItem(item);
        _znv.InvokeRPC("AddItem_Request", dropPrefab, amount);
        return true;
    }

    public string GetHoverText()
    {
        StringBuilder sb = new StringBuilder();
        if (!ItemValid)
        {
            sb.AppendLine("<color=yellow><b>Use Hotbar to add item</b></color>");
            return sb.ToString().Localize();
        }

        if (Player.m_localPlayer.IsCrouching())
        {
            sb.AppendLine($"[<color=yellow><b>$KEY_Use</b></color>] open settings");
            return sb.ToString().Localize();
        }

        sb.AppendLine($"<color=yellow><b>{LocalizedName}</b></color> ({CurrentAmount})");
        sb.AppendLine("<color=yellow><b>Use Hotbar to add item</b></color>\n");
        if (CurrentAmount <= 0)
        {
            sb.AppendLine($"[<color=yellow><b>$KEY_Use</b></color>] or [<color=yellow><b>Left Alt + $KEY_Use</b></color>] to clear");
            sb.AppendLine($"[<color=yellow><b>Left Shift + $KEY_Use</b></color>] to deposit all <color=yellow><b>{LocalizedName}</b></color> ({UtilsKG.CustomCountItems(CurrentPrefab, 1)})");
            return sb.ToString().Localize();
        }

        sb.AppendLine($"[<color=yellow><b>$KEY_Use</b></color>] to withdraw stack ({ItemMaxStack})");
        sb.AppendLine($"[<color=yellow><b>Left Alt + $KEY_Use</b></color>] to withdraw single item");
        sb.AppendLine($"[<color=yellow><b>Left Shift + $KEY_Use</b></color>] to deposit all <color=yellow><b>{LocalizedName}</b></color> ({UtilsKG.CustomCountItems(CurrentPrefab, 1)})");
        return sb.ToString().Localize();
    }

    public string GetHoverName()
    {
        return "Item Drawer";
    }
    
    private const int windowWidth = 300;
    private const int windowHeight = 300;
    private const int halfWindowWidth = windowWidth / 2;
    private const int halfWindowHeight = windowHeight / 2;
    public static void ProcessInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && ShowUI)
        {
            ShowUI = false;
            Menu.instance.OnClose();
        }
    }
    public static void ProcessGUI()
    {
        if (!ShowUI) return;
        GUI.backgroundColor = Color.white;
        Rect centerOfScreen = new(Screen.width / 2f - halfWindowWidth, Screen.height / 2f - halfWindowHeight, windowWidth, windowHeight);
        GUI.Window(218102318, centerOfScreen, Window, "Item Drawer Options");
    }
    private static void Window(int id)
    {
        if (CurrentOptions.drawer == null || !CurrentOptions.drawer._znv.IsValid())
        {
            ShowUI = false;
            return;
        }
        GUILayout.Label($"Current Drawer: <color=yellow><b>{CurrentOptions.drawer.LocalizedName}</b></color> ({CurrentOptions.drawer.CurrentAmount})");
        byte r = CurrentOptions.color.r;
        byte g = CurrentOptions.color.g;
        byte b = CurrentOptions.color.b;
        GUILayout.Label($"Text Color: <color=#{r:X2}{g:X2}{b:X2}><b>0123456789</b></color>");
        GUILayout.Label($"R: {r}");
        r = (byte)GUILayout.HorizontalSlider(r, 0, 255);
        GUILayout.Label($"G: {g}");
        g = (byte)GUILayout.HorizontalSlider(g, 0, 255);
        GUILayout.Label($"B: {b}");
        b = (byte)GUILayout.HorizontalSlider(b, 0, 255);
        CurrentOptions.color = new Color32(r, g, b, 255);
        int pickupRange = CurrentOptions.pickupRange;
        GUILayout.Space(16f);
        GUILayout.Label($"Pickup Range: <color={(pickupRange > 0 ? "lime" : "red")}><b>{pickupRange}</b></color>"); 
        pickupRange = (int)GUILayout.HorizontalSlider(pickupRange, 0, ItemDrawersKG.MaxDrawerPickupRange.Value);
        CurrentOptions.pickupRange = pickupRange;
        GUILayout.Space(16f);
        if (GUILayout.Button("<color=lime>Apply</color>"))
        {
            CurrentOptions.drawer._znv.InvokeRPC(ZNetView.Everybody, "ApplyOptions", CurrentOptions);
            ShowUI = false;
        }
    }

    [HarmonyPatch]
    private static class IsVisible
    {
        [HarmonyTargetMethods, UsedImplicitly]
        private static IEnumerable<MethodInfo> Methods()
        {
            yield return AccessTools.Method(typeof(TextInput), nameof(TextInput.IsVisible));
            yield return AccessTools.Method(typeof(StoreGui), nameof(StoreGui.IsVisible));
        }
        
        [HarmonyPostfix, UsedImplicitly]
        private static void SetTrue(ref bool __result) => __result |= ShowUI;
    }
}

[HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
public static class Piece_OnDestroy_Patch
{
    [UsedImplicitly]
    private static void Postfix(Piece __instance)
    {
        if (__instance.gameObject.GetComponent<DrawerComponent>() is { } drawer)
        {
            if (drawer.ItemValid && drawer.CurrentAmount > 0)
            {
                UtilsKG.InstantiateAtPos(ZNetScene.instance.GetPrefab(drawer.CurrentPrefab), drawer.CurrentAmount, 1, __instance.transform.position + Vector3.up);
            }
        }
    }
}

