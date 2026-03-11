using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using HarmonyLib;

namespace SMTQualityOfLife
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Supermarket Together.exe")]
    public class Plugin : BaseUnityPlugin
    {
        // === GENERAL STUFF
        public static Plugin Instance;
        
        // === CONFIG STUFF
        public ConfigEntry<bool> IsMainWindowEnabled;
        public ConfigEntry<bool> IsLowCountProductsWindowEnabled;
        public ConfigEntry<bool> IsNpcAdderWindowEnabled;
        public ConfigEntry<bool> IsSmartPricesWindowEnabled;
        
        // === REFERENCES STUFF
        private MainManager _mainManager;
        private LowCountProducts _lowCountProducts;
        private NPCAdder _npcAdder;
        private SmartPrices _smartPrices;
        
        // === PLUGIN STUFF
        private static readonly Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        
        // === CAMERA LOCK
        private bool _anyWindowOpen;
        private Quaternion _savedCamLocalRot;
        private Quaternion _savedCamParentLocalRot;
        private bool _hasSavedCamState;

        // === KEYBOARD SHORTCUTS
        private static ConfigEntry<KeyboardShortcut> _keyboardShortcutEnableMainWindow;
        private static ConfigEntry<KeyboardShortcut> _keyboardShortcutDumpBlackboard;
        private static ConfigEntry<KeyboardShortcut> _keyboardShortcutDumpAchievements;
        private static ConfigEntry<KeyboardShortcut> _keyboardShortcutDumpSkills;
        private static ConfigEntry<KeyboardShortcut> _keyboardShortcutDumpNpcManager;
        private static ConfigEntry<KeyboardShortcut> _keyboardShortcutDumpButtonsBar;
        private static ConfigEntry<KeyboardShortcut> _keyboardShortcutDumpProductListing;
        private static ConfigEntry<KeyboardShortcut> _keyboardShortcutDumpPricing;

        private void Awake()
        {
            // KEYBOARD SHORTCUTS START-UP
            _keyboardShortcutEnableMainWindow = Config.Bind("General",
                "KeyboardShortcutEnableMainWindowKey", new KeyboardShortcut(KeyCode.H, new[] { KeyCode.LeftControl }),
                (ConfigDescription.Empty));
            _keyboardShortcutDumpBlackboard = Config.Bind("General",
                "KeyboardShortcutDumpBlackboard", new KeyboardShortcut(KeyCode.F7, new[] { KeyCode.LeftControl }),
                (ConfigDescription.Empty));
            _keyboardShortcutDumpAchievements = Config.Bind("General",
                "KeyboardShortcutDumpAchievements", new KeyboardShortcut(KeyCode.F8, new[] { KeyCode.LeftControl }),
                (ConfigDescription.Empty));
            _keyboardShortcutDumpSkills = Config.Bind("General",
                "KeyboardShortcutDumpSkills", new KeyboardShortcut(KeyCode.F9, new[] { KeyCode.LeftControl }),
                (ConfigDescription.Empty));
            _keyboardShortcutDumpNpcManager = Config.Bind("General",
                "KeyboardShortcutDumpNpcManager", new KeyboardShortcut(KeyCode.F10, new[] { KeyCode.LeftControl }),
                (ConfigDescription.Empty));
            _keyboardShortcutDumpButtonsBar = Config.Bind("General",
                "KeyboardShortcutDumpButtonsBar", new KeyboardShortcut(KeyCode.F11, new[] { KeyCode.LeftControl }),
                (ConfigDescription.Empty));
            _keyboardShortcutDumpProductListing = Config.Bind("General",
                "KeyboardShortcutDumpProductListing", new KeyboardShortcut(KeyCode.F12, new[] { KeyCode.LeftControl }),
                (ConfigDescription.Empty));
            _keyboardShortcutDumpPricing = Config.Bind("General",
                "KeyboardShortcutDumpPricing", new KeyboardShortcut(KeyCode.F6, new[] { KeyCode.LeftControl }),
                (ConfigDescription.Empty));

            _mainManager = new MainManager(Config, Logger);
            _lowCountProducts = new LowCountProducts(Config, _mainManager, new GUIUtilities());
            _npcAdder = new NPCAdder(Config, Logger, _mainManager, new GUIUtilities());
            _smartPrices = new SmartPrices(Config, Logger, _mainManager, new GUIUtilities());
            
            IsMainWindowEnabled = Config.Bind(
                "SMTQualityOfLife",
                "Main QualityOfLife",
                false,
                "Enable or disable the display of the main window.");
            
            IsLowCountProductsWindowEnabled = Config.Bind(
                "SMTQualityOfLife",
                "Low Count Products",
                false,
                "Enable or disable the display of the LowCountProducts mod window.");
            
            IsNpcAdderWindowEnabled = Config.Bind(
                "SMTQualityOfLife",
                "NPC Adder",
                false,
                "Enable or disable the display of the NPCAdder mod window.");

            IsSmartPricesWindowEnabled = Config.Bind(
                "SMTQualityOfLife",
                "Smart Prices",
                false,
                "Enable or disable the display of the SmartPrices mod window.");
            
            Instance = this;

            IsMainWindowEnabled.SettingChanged += OnMainWindowEnableChanged;
            IsLowCountProductsWindowEnabled.SettingChanged += OnLowCountProductWindowEnableChanged;
            IsNpcAdderWindowEnabled.SettingChanged += OnNpcAdderWindowEnableChanged;
            IsSmartPricesWindowEnabled.SettingChanged += OnSmartPricesWindowEnableChanged;
            
            Harmony.PatchAll();
            
            // Plugin startup logic
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void Update()
        {
            if (_keyboardShortcutEnableMainWindow.Value.IsDown())
            {
                if (IsMainWindowEnabled.Value || IsLowCountProductsWindowEnabled.Value || IsNpcAdderWindowEnabled.Value || IsSmartPricesWindowEnabled.Value)
                {
                    IsMainWindowEnabled.Value = false;
                    IsLowCountProductsWindowEnabled.Value = false;
                    IsNpcAdderWindowEnabled.Value = false;
                    IsSmartPricesWindowEnabled.Value = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    IsMainWindowEnabled.Value = true;
                    IsLowCountProductsWindowEnabled.Value = false;
                    IsNpcAdderWindowEnabled.Value = false;
                    IsSmartPricesWindowEnabled.Value = false;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            if (_keyboardShortcutDumpBlackboard.Value.IsDown())
            {
                try
                {
                    DebugBlackboard.DumpManagerBlackboard();
                }
                catch (System.Exception ex)
                {
                    Logger.LogError($"Blackboard debug failed: {ex}");
                }
            }

            if (_keyboardShortcutDumpAchievements.Value.IsDown())
            {
                try
                {
                    DebugBlackboard.DumpAchievementsAndEmployees();
                }
                catch (System.Exception ex)
                {
                    Logger.LogError($"Achievements debug failed: {ex}");
                }
            }

            if (_keyboardShortcutDumpSkills.Value.IsDown())
            {
                try
                {
                    DebugBlackboard.DumpSkillSystems();
                }
                catch (System.Exception ex)
                {
                    Logger.LogError($"Skills debug failed: {ex}");
                }
            }

            if (_keyboardShortcutDumpNpcManager.Value.IsDown())
            {
                try
                {
                    DebugBlackboard.DumpNpcManager();
                }
                catch (System.Exception ex)
                {
                    Logger.LogError($"NPC Manager debug failed: {ex}");
                }
            }

            if (_keyboardShortcutDumpButtonsBar.Value.IsDown())
            {
                try
                {
                    DebugBlackboard.DumpButtonsBar();
                }
                catch (System.Exception ex)
                {
                    Logger.LogError($"Buttons_Bar debug failed: {ex}");
                }
            }

            if (_keyboardShortcutDumpProductListing.Value.IsDown())
            {
                try
                {
                    DebugBlackboard.DumpProductListing();
                }
                catch (System.Exception ex)
                {
                    Logger.LogError($"ProductListing debug failed: {ex}");
                }
            }

            if (_keyboardShortcutDumpPricing.Value.IsDown())
            {
                try
                {
                    DebugBlackboard.DumpPricingClasses();
                }
                catch (System.Exception ex)
                {
                    Logger.LogError($"Pricing discovery dump failed: {ex}");
                }
            }

            // Track whether any mod window is open and manage camera freeze
            _anyWindowOpen = IsMainWindowEnabled.Value || IsLowCountProductsWindowEnabled.Value ||
                             IsNpcAdderWindowEnabled.Value || IsSmartPricesWindowEnabled.Value;

            if (_anyWindowOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Save camera state the first frame the GUI opens
                if (!_hasSavedCamState)
                {
                    var cam = Camera.main;
                    if (cam != null)
                    {
                        _savedCamLocalRot = cam.transform.localRotation;
                        if (cam.transform.parent != null)
                            _savedCamParentLocalRot = cam.transform.parent.localRotation;
                        _hasSavedCamState = true;
                    }
                }
            }
            else
            {
                _hasSavedCamState = false;
            }
        }

        private void LateUpdate()
        {
            if (!_anyWindowOpen || !_hasSavedCamState) return;

            var cam = Camera.main;
            if (cam == null) return;

            cam.transform.localRotation = _savedCamLocalRot;
            if (cam.transform.parent != null)
                cam.transform.parent.localRotation = _savedCamParentLocalRot;
        }

        private void OnMainWindowEnableChanged(object sender, System.EventArgs e)
        {
            _mainManager.SetWindowVisibility(IsMainWindowEnabled.Value);
        }

        private void OnLowCountProductWindowEnableChanged(object sender, System.EventArgs e)
        {
            _lowCountProducts.SetWindowVisibility(IsLowCountProductsWindowEnabled.Value);
        }

        private void OnNpcAdderWindowEnableChanged(object sender, System.EventArgs e)
        {
            _npcAdder.SetWindowVisibility(IsNpcAdderWindowEnabled.Value);
        }

        private void OnSmartPricesWindowEnableChanged(object sender, System.EventArgs e)
        {
            _smartPrices.SetWindowVisibility(IsSmartPricesWindowEnabled.Value);
        }

        private void OnGUI()
        {
            if (IsMainWindowEnabled.Value)
            {
                _mainManager.DrawWindow();
            }

            if (IsLowCountProductsWindowEnabled.Value)
            {
                _lowCountProducts.DrawWindow();
            }

            if (IsNpcAdderWindowEnabled.Value)
            {
                _npcAdder.DrawWindow();
            }

            if (IsSmartPricesWindowEnabled.Value)
            {
                _smartPrices.DrawWindow();
            }
        }
    }
}
