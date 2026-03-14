using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;

namespace SMTQualityOfLife
{
    public class MainManager
    {
        // === GUI STUFF
        private readonly GUIUtilities _guiUtilities = new GUIUtilities();
        private Rect _windowRect = new Rect(0, 0, Mathf.Min(Screen.width, 650), Screen.height < 560 ? Screen.height : Screen.height - 100);
        private bool _showWindow;
        private Vector2 _scrollPosition;

        private bool _tempTwentyCentsToggle;
        private bool _tempLowProductCountToggle;
        private bool _tempSmartPricesToggle;
        private bool _tempCheckoutVolumeToggle;
        
        // === CONFIG STUFF
        private readonly ConfigEntry<bool> _twentyCentsModEnabled;
        public readonly ConfigEntry<bool> LowProductCountEnabled;
        public readonly ConfigEntry<bool> SmartPricesEnabled;
        public readonly ConfigEntry<bool> CheckoutVolumeEnabled;
        
        // === OTHER STUFF
        private readonly ManualLogSource _logger;

        public MainManager(ConfigFile config, ManualLogSource logger)
        {
            _logger = logger;
            _twentyCentsModEnabled = config.Bind(
                "TwentyCentsMod",
                "TwentyCents Mod",
                false,
                "TwentyCents Mod enabled");
            
            LowProductCountEnabled = config.Bind(
                "LowCountProductsMod",
                "LowCountProducts Mod",
                false,
                "LowCountProducts Mod enabled");
            
            SmartPricesEnabled = config.Bind(
                "SmartPricesMod",
                "SmartPrices Mod",
                false,
                "SmartPrices Mod enabled");

            CheckoutVolumeEnabled = config.Bind(
                "CheckoutVolumeMod",
                "CheckoutVolume Mod",
                false,
                "Checkout Volume Mod enabled");

            _tempTwentyCentsToggle = _twentyCentsModEnabled.Value;
            _tempLowProductCountToggle = LowProductCountEnabled.Value;
            _tempSmartPricesToggle = SmartPricesEnabled.Value;
            _tempCheckoutVolumeToggle = CheckoutVolumeEnabled.Value;
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
                _windowRect = GUILayout.Window(0, _windowRect, DrawWindowContent, "SMTQualityOfLife - General");
            }
        }

        private void DrawWindowContent(int windowID)
        {
            if (_guiUtilities.HeaderStyle == null)
                _guiUtilities.InitializeStyles();

            // Allow the window to be draggable
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            // Start a scroll view in case content overflows
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(630), GUILayout.Height(420));

            // _guiUtilities.DrawModSection(
            //     "TwentyCents Mod",
            //     "When enabled, the 25 cents at the checkout will be replaced with 20 cents",
            //     ref _tempTwentyCentsToggle,
            //     null);
            
            _guiUtilities.DrawModSection(
                "LowCountProducts Mod",
                "This mod allows you to add all your low count products to the shopping cart at the manager blackboard by simply clicking a button.",
                ref _tempLowProductCountToggle,
                OpenLowCountProductsSettings);
            
            _guiUtilities.DrawModSection(
                "SmartPrices Mod",
                "This mod allows you to maximize your income by modifying the price set by the pricing machine to the highest price possible.",
                ref _tempSmartPricesToggle,
                OpenSmartPricesSettings);

            _guiUtilities.DrawModSection(
                "Checkout Volume",
                "Control the volume of the scanner beep sound at checkout registers (regular and self-checkout).",
                ref _tempCheckoutVolumeToggle,
                OpenCheckoutVolumeSettings);

            GUILayout.EndScrollView();
            
            // Update the config value based on the toggle state
            if (_twentyCentsModEnabled.Value != _tempTwentyCentsToggle)
            {
                _twentyCentsModEnabled.Value = _tempTwentyCentsToggle;
                _logger.LogInfo($"TwentyCents Mod enabled: {_tempTwentyCentsToggle}");
            }
            
            if (LowProductCountEnabled.Value != _tempLowProductCountToggle)
            {
                LowProductCountEnabled.Value = _tempLowProductCountToggle;
                LowCountProducts.LowCountProductsState = LowProductCountEnabled.Value;
                _logger.LogInfo($"Low Count Product Enabled: {_tempLowProductCountToggle}");
            }

            if (SmartPricesEnabled.Value != _tempSmartPricesToggle)
            {
                SmartPricesEnabled.Value = _tempSmartPricesToggle;
                SmartPrices.SmartPricesState = SmartPricesEnabled.Value;
                _logger.LogInfo($"Smart Prices Enabled: {_tempSmartPricesToggle}");
            }

            if (CheckoutVolumeEnabled.Value != _tempCheckoutVolumeToggle)
            {
                CheckoutVolumeEnabled.Value = _tempCheckoutVolumeToggle;
                CheckoutVolume.CheckoutVolumeState = CheckoutVolumeEnabled.Value;
                _logger.LogInfo($"Checkout Volume Enabled: {_tempCheckoutVolumeToggle}");
            }
        }
        
        private void OpenLowCountProductsSettings()
        {
            // _logger.LogInfo("Opening low count products settings");
            Plugin.Instance.IsMainWindowEnabled.Value = false;
            Plugin.Instance.IsLowCountProductsWindowEnabled.Value = true;
        }

        private void OpenSmartPricesSettings()
        {
            Plugin.Instance.IsMainWindowEnabled.Value = false;
            Plugin.Instance.IsSmartPricesWindowEnabled.Value = true;
        }

        private void OpenCheckoutVolumeSettings()
        {
            Plugin.Instance.IsMainWindowEnabled.Value = false;
            Plugin.Instance.IsCheckoutVolumeWindowEnabled.Value = true;
        }
    }
}
