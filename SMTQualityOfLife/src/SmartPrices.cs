using System;
using System.Globalization;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace SMTQualityOfLife
{
    public class SmartPrices
    {
        // === GUI STUFF
        private readonly GUIUtilities _guiUtilities;
        private Rect _windowRect = new Rect(0, 0, Mathf.Min(Screen.width, 650), Screen.height < 560 ? Screen.height : Screen.height - 100);
        private bool _showWindow;
        private Vector2 _scrollPosition;

        // === MOD STUFF
        public static bool SmartPricesState;

        // === CONFIG STUFF
        public static ConfigEntry<float> PriceMarkupPercent;

        // === CLASS REFERENCES
        private readonly MainManager _manager;

        // === OTHER STUFF
        public static ManualLogSource Logger;

        public SmartPrices(ConfigFile config, ManualLogSource logger, MainManager manager, GUIUtilities guiUtilities)
        {
            Logger = logger;
            _guiUtilities = guiUtilities;
            _manager = manager;

            PriceMarkupPercent = config.Bind(
                "SmartPricesMod",
                "PriceMarkupPercent",
                100f,
                "Markup percentage above the base inflated price. Formula: basePrice * tierInflation * (1 + markup/100)");
        }

        public void SetWindowVisibility(bool visible)
        {
            if (visible && !_showWindow)
            {
                // Set initial window position when it becomes visible
                _windowRect.x = (Screen.width - _windowRect.width) / 2;
                _windowRect.y = (Screen.height - _windowRect.height) / 2 + 30;
            }
            _showWindow = visible;
        }

        public void DrawWindow()
        {
            if (_showWindow)
            {
                _windowRect = GUILayout.Window(2, _windowRect, DrawWindowContent, "SMTQualityOfLife - SmartPrices Mod");
            }
        }

        private void DrawWindowContent(int windowID)
        {
            if (_guiUtilities.HeaderStyle == null)
                _guiUtilities.InitializeStyles();

            // Allow the window to be draggable
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            // 'Back' Button at the top left
            if (GUI.Button(new Rect(10, 25, 60, 25), "< Back"))
            {
                OnBackButtonClicked();
            }

            GUILayout.Space(50);

            // Check if mod is enabled
            if (_manager.SmartPricesEnabled.Value)
            {
                // Start a scroll view in case content overflows
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(630), GUILayout.Height(420));

                GUILayout.Label("SmartPrices Settings", _guiUtilities.HeaderStyle);
                GUILayout.Space(5);
                GUILayout.Label(
                    "When you use the pricing gun on a product, the price will automatically be set " +
                    "to the optimal value based on the markup percentage below.\n\n" +
                    "Formula: base price x tier inflation x (1 + markup / 100)",
                    _guiUtilities.DescriptionStyle);

                GUILayout.Space(15);
                _guiUtilities.DrawHorizontalLine();
                GUILayout.Space(15);

                // Markup percentage controls
                GUILayout.Label("Price Markup", _guiUtilities.HeaderStyle);
                GUILayout.Space(5);
                GUILayout.Label(
                    $"Current markup: {PriceMarkupPercent.Value:F0}%",
                    _guiUtilities.LabelStyle);
                GUILayout.Label(
                    "Adjust the markup percentage above the base inflated price. " +
                    "Higher values = more profit but risk angering customers.",
                    _guiUtilities.DescriptionStyle);

                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("-5%", GUILayout.Width(70)))
                    PriceMarkupPercent.Value = Mathf.Max(0f, PriceMarkupPercent.Value - 5f);
                if (GUILayout.Button("-1%", GUILayout.Width(70)))
                    PriceMarkupPercent.Value = Mathf.Max(0f, PriceMarkupPercent.Value - 1f);
                if (GUILayout.Button("+1%", GUILayout.Width(70)))
                    PriceMarkupPercent.Value += 1f;
                if (GUILayout.Button("+5%", GUILayout.Width(70)))
                    PriceMarkupPercent.Value += 5f;
                GUILayout.EndHorizontal();

                GUILayout.EndScrollView();
            }
            else
            {
                _guiUtilities.DrawModDisabledContent("SmartPrices");
            }
        }

        private void OnBackButtonClicked()
        {
            Plugin.Instance.IsSmartPricesWindowEnabled.Value = false;
            Plugin.Instance.IsMainWindowEnabled.Value = true;
        }
    }
}

namespace SMTQualityOfLife.Patches
{
    using SMTQualityOfLife;

    [HarmonyPatch(typeof(PlayerNetwork))]
    internal class SmartPricesPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void Postfix(ref float ___pPrice, TextMeshProUGUI ___marketPriceTMP, ref TextMeshProUGUI ___yourPriceTMP)
        {
            if (!SmartPrices.SmartPricesState) return;
            if (___marketPriceTMP == null) return;

            string marketText = ___marketPriceTMP.text;
            if (string.IsNullOrEmpty(marketText) || marketText.Length < 2) return;

            // Strip leading "$" and parse the market price (already = basePricePerUnit * tierInflation)
            if (!float.TryParse(
                    marketText.Substring(1).Replace(',', '.'),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float marketPrice))
                return;

            float markup = SmartPrices.PriceMarkupPercent.Value;
            float smartPrice = marketPrice * (1f + markup / 100f);
            smartPrice = Mathf.Floor(smartPrice * 20f) / 20f;

            ___pPrice = smartPrice;
            ___yourPriceTMP.text = "$" + smartPrice.ToString("F2", CultureInfo.InvariantCulture);
        }
    }
}
