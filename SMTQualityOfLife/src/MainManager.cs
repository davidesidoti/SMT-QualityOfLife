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

        private bool _tempTwentyCentsToggle;
        private bool _tempNpcAdderToggle;
        private bool _tempLowProductCountToggle;
        private bool _tempSmartPricesToggle;
        
        // === MOD REFERENCES
        private LowCountProducts _lowCountProducts;
        private NPCAdder _npcAdder;
        
        // === CONFIG STUFF
        public readonly ConfigEntry<bool> TwentyCentsModEnabled;
        public readonly ConfigEntry<bool> NpcAdderEnabled;
        public readonly ConfigEntry<bool> LowProductCountEnabled;
        public readonly ConfigEntry<bool> SmartPricesEnabled;
        
        // === OTHER STUFF
        private readonly ManualLogSource _logger;

        public MainManager(ConfigFile config, ManualLogSource logger)
        {
            _logger = logger;
            TwentyCentsModEnabled = config.Bind(
                "TwentyCentsMod",
                "TwentyCents Mod",
                false,
                "TwentyCents Mod enabled");
            
            NpcAdderEnabled = config.Bind(
                "NPCAdderMod",
                "NPCAdder",
                false,
                "NPC Adder enabled");
            
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
            
            _tempTwentyCentsToggle = TwentyCentsModEnabled.Value;
            _tempNpcAdderToggle = NpcAdderEnabled.Value;
            _tempLowProductCountToggle = LowProductCountEnabled.Value;
            _tempSmartPricesToggle = SmartPricesEnabled.Value;

            _lowCountProducts = new LowCountProducts(config, logger, this, _guiUtilities);
            _npcAdder = new NPCAdder(config, logger, this, _guiUtilities);
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
            GUILayout.BeginScrollView(Vector2.zero, GUILayout.Width(630), GUILayout.Height(420));

            // _guiUtilities.DrawModSection(
            //     "TwentyCents Mod",
            //     "When enabled, the 25 cents at the checkout will be replaced with 20 cents",
            //     ref _tempTwentyCentsToggle,
            //     null);
            
            _guiUtilities.DrawModSection(
                "NPCAdder Mod",
                "This mod allows you to add up to 15 NPC's employees to your store once you unlock all of the possible upgrades.",
                ref _tempNpcAdderToggle,
                OpenNpcAdderSettings);
            
            // _guiUtilities.DrawModSection(
            //     "LowCountProducts Mod",
            //     "This mod allows you to add all your low count products to the shopping cart at the manager blackboard by simply clicking a button.",
            //     ref _tempLowProductCountToggle,
            //     OpenLowCountProductsSettings);
            //
            // _guiUtilities.DrawModSection(
            //     "SmartPrices Mod",
            //     "This mod allows you to maximize your income by modifying the price set by the pricing machine to the highest price possible.",
            //     ref _tempSmartPricesToggle,
            //     OpenSmartPricesSettings);

            GUILayout.EndScrollView();
            
            // Update the config value based on the toggle state
            if (TwentyCentsModEnabled.Value != _tempTwentyCentsToggle)
            {
                TwentyCentsModEnabled.Value = _tempTwentyCentsToggle;
                _logger.LogInfo($"TwentyCents Mod enabled: {_tempTwentyCentsToggle}");
            }
            
            if (NpcAdderEnabled.Value != _tempNpcAdderToggle)
            {
                NpcAdderEnabled.Value = _tempNpcAdderToggle;
                _logger.LogInfo($"NPCAdder Mod enabled: {_tempNpcAdderToggle}");
            }

            if (LowProductCountEnabled.Value != _tempLowProductCountToggle)
            {
                LowProductCountEnabled.Value = _tempLowProductCountToggle;
                _logger.LogInfo($"Low Count Product Enabled: {_tempLowProductCountToggle}");
            }

            if (SmartPricesEnabled.Value != _tempSmartPricesToggle)
            {
                SmartPricesEnabled.Value = _tempSmartPricesToggle;
                _logger.LogInfo($"Smart Prices Enabled: {_tempSmartPricesToggle}");
            }
        }
        
        private void OpenNpcAdderSettings()
        {
            // _logger.LogInfo("Opening npc adder settings");
            Plugin.Instance.IsMainWindowEnabled.Value = false;
            Plugin.Instance.IsNpcAdderWindowEnabled.Value = true;
        }

        private void OpenLowCountProductsSettings()
        {
            // _logger.LogInfo("Opening low count products settings");
            Plugin.Instance.IsMainWindowEnabled.Value = false;
            Plugin.Instance.IsLowCountProductsWindowEnabled.Value = true;
        }

        private void OpenSmartPricesSettings()
        {
            _logger.LogInfo("Opening smart prices settings");
        }
    }
}
