using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;

namespace SMTQualityOfLife
{
    public class LowCountProducts
    {
        // === GUI STUFF
        private readonly GUIUtilities _guiUtilities;
        private Rect _windowRect = new Rect(0, 0, Mathf.Min(Screen.width, 650), Screen.height < 560 ? Screen.height : Screen.height - 100);
        private bool _showWindow;

        // === OTHER STUFF
        private readonly ManualLogSource _logger;

        public LowCountProducts(ConfigFile config, ManualLogSource logger, MainManager manager, GUIUtilities guiUtilities)
        {
            _logger = logger;
            _guiUtilities = guiUtilities;
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
                _windowRect = GUILayout.Window(1, _windowRect, DrawWindowContent, "SMTQualityOfLife - LowCountProducts Mod");
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

            // Start a scroll view in case content overflows
            GUILayout.BeginScrollView(Vector2.zero, GUILayout.Width(630), GUILayout.Height(420));

            GUILayout.EndScrollView();
        }
        
        private void OnBackButtonClicked()
        {
            Plugin.Instance.IsLowCountProductsWindowEnabled.Value = false;
            Plugin.Instance.IsMainWindowEnabled.Value = true;
        }

    }
}