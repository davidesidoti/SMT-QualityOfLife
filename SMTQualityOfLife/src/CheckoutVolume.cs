using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SMTQualityOfLife
{
    public class CheckoutVolume
    {
        // === GUI STUFF
        private readonly GUIUtilities _guiUtilities;
        private Rect _windowRect = new Rect(0, 0, Mathf.Min(Screen.width, 650), Screen.height < 560 ? Screen.height : Screen.height - 100);
        private bool _showWindow;
        private Vector2 _scrollPosition;

        // === MOD STUFF
        public static bool CheckoutVolumeState;

        // === CONFIG STUFF
        public static ConfigEntry<float> BeepVolume;

        // === CLASS REFERENCES
        private readonly MainManager _manager;

        // === OTHER STUFF
        public static ManualLogSource Logger;

        public CheckoutVolume(ConfigFile config, ManualLogSource logger, MainManager manager, GUIUtilities guiUtilities)
        {
            Logger = logger;
            _guiUtilities = guiUtilities;
            _manager = manager;

            BeepVolume = config.Bind(
                "CheckoutVolumeMod",
                "BeepVolume",
                1f,
                "Scanner beep volume at checkouts (0.0 = mute, 1.0 = full volume)");
        }

        public void SetWindowVisibility(bool visible)
        {
            if (visible && !_showWindow)
            {
                _windowRect.x = (Screen.width - _windowRect.width) / 2;
                _windowRect.y = (Screen.height - _windowRect.height) / 2 + 30;
            }
            _showWindow = visible;
        }

        public void DrawWindow()
        {
            if (_showWindow)
            {
                _windowRect = GUILayout.Window(3, _windowRect, DrawWindowContent, "SMTQualityOfLife - Checkout Volume");
            }
        }

        private void DrawWindowContent(int windowID)
        {
            if (_guiUtilities.HeaderStyle == null)
                _guiUtilities.InitializeStyles();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            if (GUI.Button(new Rect(10, 25, 60, 25), "< Back"))
            {
                OnBackButtonClicked();
            }

            GUILayout.Space(50);

            if (_manager.CheckoutVolumeEnabled.Value)
            {
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(630), GUILayout.Height(420));

                GUILayout.Label("Checkout Volume Settings", _guiUtilities.HeaderStyle);
                GUILayout.Space(5);
                GUILayout.Label(
                    "Control the volume of the beep sound when products are scanned at checkout registers (both regular and self-checkout).",
                    _guiUtilities.DescriptionStyle);

                GUILayout.Space(15);
                _guiUtilities.DrawHorizontalLine();
                GUILayout.Space(15);

                GUILayout.Label("Scanner Beep Volume", _guiUtilities.HeaderStyle);
                GUILayout.Space(5);
                GUILayout.Label(
                    $"Current volume: {Mathf.RoundToInt(BeepVolume.Value * 100)}%",
                    _guiUtilities.LabelStyle);

                GUILayout.Space(10);
                BeepVolume.Value = GUILayout.HorizontalSlider(BeepVolume.Value, 0f, 1f, GUILayout.Width(600));
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Mute", GUILayout.Width(70)))
                    BeepVolume.Value = 0f;
                if (GUILayout.Button("25%", GUILayout.Width(70)))
                    BeepVolume.Value = 0.25f;
                if (GUILayout.Button("50%", GUILayout.Width(70)))
                    BeepVolume.Value = 0.5f;
                if (GUILayout.Button("75%", GUILayout.Width(70)))
                    BeepVolume.Value = 0.75f;
                if (GUILayout.Button("100%", GUILayout.Width(70)))
                    BeepVolume.Value = 1f;
                GUILayout.EndHorizontal();

                GUILayout.EndScrollView();
            }
            else
            {
                _guiUtilities.DrawModDisabledContent("Checkout Volume");
            }
        }

        private void OnBackButtonClicked()
        {
            Plugin.Instance.IsCheckoutVolumeWindowEnabled.Value = false;
            Plugin.Instance.IsMainWindowEnabled.Value = true;
        }
    }
}

namespace SMTQualityOfLife.Patches
{
    using SMTQualityOfLife;

    [HarmonyPatch(typeof(Data_Container))]
    internal class CheckoutVolumePatch
    {
        [HarmonyPatch("UserCode_RpcAddItemToCheckout__Single__GameObject")]
        [HarmonyPrefix]
        public static void BeepPrefix(Data_Container __instance)
        {
            if (!CheckoutVolume.CheckoutVolumeState) return;
            var audio = __instance.GetComponent<AudioSource>();
            if (audio != null)
                audio.volume = CheckoutVolume.BeepVolume.Value;
        }

        [HarmonyPatch("UserCode_SelfCheckoutActivateBag")]
        [HarmonyPrefix]
        public static void SelfCheckoutPrefix(Data_Container __instance)
        {
            if (!CheckoutVolume.CheckoutVolumeState) return;
            var audio = __instance.GetComponent<AudioSource>();
            if (audio != null)
                audio.volume = CheckoutVolume.BeepVolume.Value;
        }
    }
}
