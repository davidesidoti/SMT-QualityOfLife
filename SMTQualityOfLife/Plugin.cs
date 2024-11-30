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
        
        // === REFERENCES STUFF
        private MainManager _mainManager;
        private LowCountProducts _lowCountProducts;
        private NPCAdder _npcAdder;
        
        // === PLUGIN STUFF
        private static readonly Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        
        // === KEYBOARD SHORTCUTS
        private static ConfigEntry<KeyboardShortcut> _keyboardShortcutEnableMainWindow;
            
        private void Awake()
        {
            // KEYBOARD SHORTCUTS START-UP
            _keyboardShortcutEnableMainWindow = Config.Bind("General",
                "KeyboardShortcutEnableMainWindowKey", new KeyboardShortcut(KeyCode.H, new[] { KeyCode.LeftControl }),
                (ConfigDescription.Empty));
            
            _mainManager = new MainManager(Config, Logger);
            _lowCountProducts = new LowCountProducts(Config, _mainManager, new GUIUtilities());
            _npcAdder = new NPCAdder(Config, Logger, _mainManager, new GUIUtilities());
            
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
            
            Instance = this;

            IsMainWindowEnabled.SettingChanged += OnMainWindowEnableChanged;
            IsLowCountProductsWindowEnabled.SettingChanged += OnLowCountProductWindowEnableChanged;
            IsNpcAdderWindowEnabled.SettingChanged += OnNpcAdderWindowEnableChanged;
            
            Harmony.PatchAll();
            
            // Plugin startup logic
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void Update()
        {
            if (_keyboardShortcutEnableMainWindow.Value.IsDown())
            {
                if (IsMainWindowEnabled.Value || IsLowCountProductsWindowEnabled.Value || IsNpcAdderWindowEnabled.Value)
                {
                    IsMainWindowEnabled.Value = false;
                    IsLowCountProductsWindowEnabled.Value = false;
                    IsNpcAdderWindowEnabled.Value = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    IsMainWindowEnabled.Value = true;
                    IsLowCountProductsWindowEnabled.Value = false;
                    IsNpcAdderWindowEnabled.Value = false;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
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
        }
    }
}
