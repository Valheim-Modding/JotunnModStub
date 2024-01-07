using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static QuickStackStore.QSSConfig;

namespace QuickStackStore
{
    internal class ControllerButtonHintHelper
    {
        internal static void AddControllerTooltipToTrashCan(Button button, Transform parent)
        {
            if (button.gameObject.GetComponent<UIGamePad>())
            {
                return;
            }

            var takeAllControllerHint = InventoryGui.instance.m_takeAllButton.GetComponent<UIGamePad>();

            var controllerHint = Object.Instantiate(takeAllControllerHint.m_hint, parent);
            var uiGamePadNew = button.gameObject.AddComponent<UIGamePad>();
            uiGamePadNew.m_hint = controllerHint;

            InventoryGui.instance.StartCoroutine(WaitAFrameToSetupControllerHint(uiGamePadNew));
        }

        private static IEnumerator WaitAFrameToSetupControllerHint(UIGamePad uiGamePad)
        {
            yield return null;

            SetupControllerHint(uiGamePad, KeybindChecker.joyTrash);
        }

        internal static IEnumerator WaitAFrameToSetupControllerHint(Button button, string joyHint)
        {
            yield return null;

            if (button && button.TryGetComponent<UIGamePad>(out var uiGamePad))
            {
                SetupControllerHint(uiGamePad, joyHint);
            }
        }

        internal static void SetupControllerHint(UIGamePad uiGamePad, string joyHint)
        {
            if (!uiGamePad || !uiGamePad.m_hint)
            {
                return;
            }

            uiGamePad.m_zinputKey = null;

            if (ZInput.IsGamepadActive() && ControllerConfig.UseHardcodedControllerSupport.Value)
            {
                uiGamePad.m_hint.gameObject.SetActive(true);

                var text = uiGamePad.m_hint.GetComponentInChildren<TextMeshProUGUI>(true);

                if (text)
                {
                    text.text = Localization.instance.Translate(KeybindChecker.joyTranslationPrefix + joyHint);
                }
            }
            else
            {
                uiGamePad.enabled = false;
                uiGamePad.m_hint.gameObject.SetActive(false);
            }
        }

        internal static void FixTakeAllButtonControllerHint(InventoryGui instance)
        {
            var takeAllUIGamePad = instance.m_takeAllButton.GetComponent<UIGamePad>();

            if (!takeAllUIGamePad)
            {
                return;
            }

            var takeAllControllerKeyHint = takeAllUIGamePad.m_hint.gameObject;

            if (ControllerConfig.RemoveControllerButtonHintFromTakeAllButton.Value)
            {
                takeAllUIGamePad.enabled = false;
                takeAllControllerKeyHint.SetActive(false);
            }
            else
            {
                takeAllUIGamePad.enabled = true;
                takeAllControllerKeyHint.SetActive(true);

                if (!takeAllControllerKeyHint.GetComponent<TakeAllHintFixer>())
                {
                    takeAllControllerKeyHint.AddComponent<TakeAllHintFixer>();
                }
            }
        }
    }

    internal class TakeAllHintFixer : MonoBehaviour
    {
        private Canvas canvas;

        private GraphicRaycaster graphicRaycaster;

        private bool fixedCanvas;

        protected void Start()
        {
            if (transform.parent.name != "TakeAll")
            {
                Destroy(this);
            }

            fixedCanvas = false;

            canvas = gameObject.GetComponent<Canvas>();
            graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>();

            if (!canvas)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            if (!graphicRaycaster)
            {
                graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        protected void Update()
        {
            if (!fixedCanvas && canvas && canvas.isActiveAndEnabled)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 1;
                fixedCanvas = true;
                Destroy(this);
            }
        }
    }
}