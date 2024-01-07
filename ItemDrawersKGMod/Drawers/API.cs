using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ItemDrawersKGMod;

namespace ItemDrawersKG_API;

// class to copy. keep in mind that AllDrawers should not be cached in any case since Prefab and Amount are applied once so it will be outdated,
// also there is no IsValid() check so next frame Drawer can be destroyed and you will get null reference exception
// best usage: call AllDrawers() every time you need it and process it in the same frame
public static class ItemDrawers_API
{
    private static readonly bool _IsInstalled;
    private static readonly MethodInfo MI_GetAllDrawers;
 
    public class Drawer(ZNetView znv)
    {
        public string Prefab = znv.m_zdo.GetString("Prefab");
        public int Amount = znv.m_zdo.GetInt("Amount");
        public void Remove(int amount) { znv.ClaimOwnership(); znv.InvokeRPC("ForceRemove", amount); }
        public void Withdraw(int amount) => znv.InvokeRPC("WithdrawItem_Request", amount);
        public void Add(int amount) => znv.InvokeRPC("AddItem_Request", Prefab, amount);
    }

    public static List<Drawer> AllDrawers => _IsInstalled ? 
        ((List<ZNetView>)MI_GetAllDrawers.Invoke(null, null)).Select(znv => new Drawer(znv)).ToList() 
        : new();
    
    static ItemDrawers_API()
    {
        if (Type.GetType("API.ClientSide, kg_ItemDrawers") is not { } drawersAPI)
        {
            _IsInstalled = false;
            return;
        }

        _IsInstalled = true;
        MI_GetAllDrawers = drawersAPI.GetMethod("AllDrawers", BindingFlags.Public | BindingFlags.Static);
    }
} 

//do not copy
public static class ClientSide
{
    public static List<ZNetView> AllDrawers() => DrawerComponent.AllDrawers.Where(d => d._znv.IsValid()).Select(d => d._znv).ToList();
}
