using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;
using HarmonyLib;
using Mirror;

namespace SMTQualityOfLife
{
    public class NPCAdder
    {
        // === GUI STUFF
        private readonly GUIUtilities _guiUtilities;
        private Rect _windowRect = new Rect(0, 0, Mathf.Min(Screen.width, 650), Screen.height < 560 ? Screen.height : Screen.height - 100);
        private bool _showWindow;
        
        // === CLASS REFERENCES
        private readonly MainManager _manager;
        
        // === NPC_Manager
        public static int CurrentMaxNpc;
        public static int NewMaxNpc;

        // === OTHER STUFF
        public static ManualLogSource Logger;
        
        public NPCAdder(ConfigFile config, ManualLogSource logger, MainManager manager, GUIUtilities guiUtilities)
        {
            Logger = logger;
            _guiUtilities = guiUtilities;
            _manager = manager;
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
                _windowRect = GUILayout.Window(1, _windowRect, DrawWindowContent, "SMTQualityOfLife - NPCAdder Mod");
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
            if (_manager.NpcAdderEnabled.Value)
            {
                // Start a scroll view in case content overflows
                GUILayout.BeginScrollView(Vector2.zero, GUILayout.Width(630), GUILayout.Height(420));
                
                _guiUtilities.DrawIntButtonAddSection(
                    "Max available NPC's",
                    "Increase or decrease the max spawnable NPC's. This will only be possible when all of the NPC's upgrades have been purchased.",
                    CurrentMaxNpc,
                    OnAddButtonClicked,
                    OnRemoveButtonClicked);
                
                GUILayout.EndScrollView();
            }
            else
            {
                _guiUtilities.DrawModDisabledContent("NPC Adder");
            }
        }

        private void OnAddButtonClicked()
        {
            if (CurrentMaxNpc < 15)
            {
                NewMaxNpc = CurrentMaxNpc + 1;
            }
        }

        private void OnRemoveButtonClicked()
        {
            if (CurrentMaxNpc > 10)
            {
                NewMaxNpc = CurrentMaxNpc - 1;
            }
        }
        
        private void OnBackButtonClicked()
        {
            Plugin.Instance.IsNpcAdderWindowEnabled.Value = false;
            Plugin.Instance.IsMainWindowEnabled.Value = true;
        }
    }
}

namespace SMTQualityOfLife.Patches
{
    using SMTQualityOfLife;
    
    [HarmonyPatch(typeof(NPC_Manager))]
    internal class SmtQualityOfLifeNpcManager
    {
        [HarmonyPatch("FixedUpdate")]
        [HarmonyPostfix]
        public static void FixedUpdatePostfix(NPC_Manager __instance)
        {
            // Updates display of the current max npc's
            NPCAdder.CurrentMaxNpc = __instance.maxEmployees;
            
            // Ensures that the player has reached the max amount of buy-able npc's
            if (__instance.maxEmployees != 0 && __instance.maxEmployees >= 10)
            {
                // Updates in-game max npc's and npc's manager blackboard
                if (NPCAdder.NewMaxNpc != 0 && NPCAdder.NewMaxNpc != __instance.maxEmployees)
                {
                    if (NPCAdder.NewMaxNpc < __instance.maxEmployees && __instance.maxEmployees >= 11)
                    {
                        Transform parentTransform = __instance.employeeParentOBJ.transform;
                        int childCount = parentTransform.childCount - 1;
                        Transform child = parentTransform.GetChild(childCount);
                        Object.Destroy(child.gameObject);
                        NetworkManager.Destroy(child.gameObject);
                    }
                    
                    __instance.maxEmployees = NPCAdder.NewMaxNpc;
                    __instance.UpdateEmployeesNumberInBlackboard();
                }
            }
        }
    }
}