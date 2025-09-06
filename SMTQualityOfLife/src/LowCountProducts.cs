using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

namespace SMTQualityOfLife
{
    public class LowCountProducts
    {
        // === GUI STUFF
        private readonly GUIUtilities _guiUtilities;
        private Rect _windowRect = new Rect(0, 0, Mathf.Min(Screen.width, 650), Screen.height < 560 ? Screen.height : Screen.height - 100);
        private bool _showWindow;
        private Vector2 _scrollPosition;
        
        // === MOD STUFF
        public static bool LowCountProductsState = true;
        public static bool AddLowCountProducts;
        
        // === CONFIG STUFF
        public static ConfigEntry<int> LowCountProductsThreshold;
        
        // ==== Notification stuff
        public static bool Notify;
        public static string NotificationType;
        
        // === CLASS REFERENCES
        private readonly MainManager _manager;

        public LowCountProducts(ConfigFile config, MainManager manager, GUIUtilities guiUtilities)
        {
            _guiUtilities = guiUtilities;
            _manager = manager;
            LowCountProductsThreshold = config.Bind("General", "LowCountProducts Threshold", 10);
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
            
            GUILayout.Space(50);

            // Check if mod is enabled
            if (_manager.LowProductCountEnabled.Value)
            {
                // Start a scroll view in case content overflows
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(630), GUILayout.Height(420));
                
                _guiUtilities.DrawIntButtonAddSection(
                    "Product Threshold",
                    "Adjust the lowest possible product count value that will be added to the shopping cart.",
                    LowCountProductsThreshold.Value,
                    OnAddThresholdButtonClicked,
                    OnRemoveThresholdButtonClicked);
                
                GUILayout.EndScrollView();
            }
            else
            {
                _guiUtilities.DrawModDisabledContent("Low Count Products");
            }

        }

        private void OnAddThresholdButtonClicked()
        {
            LowCountProductsThreshold.Value++;
        }

        private void OnRemoveThresholdButtonClicked()
        {
            if (LowCountProductsThreshold.Value > 0)
            {
                LowCountProductsThreshold.Value--;
            }
        }
        
        private void OnBackButtonClicked()
        {
            Plugin.Instance.IsLowCountProductsWindowEnabled.Value = false;
            Plugin.Instance.IsMainWindowEnabled.Value = true;
        }

    }
}

namespace SMTQualityOfLife.Patches
{
    using SMTQualityOfLife;
    
    [HarmonyPatch(typeof(ManagerBlackboard))]
    internal class LowCountProductsManagerBlackboardPatch
    {
        [HarmonyPatch("FixedUpdate")]
        [HarmonyPostfix]
        public static void LowCountProductsPostfix(ManagerBlackboard __instance)
        {
            if (LowCountProducts.LowCountProductsState)
            {
                // === start: ADD LOW COUNT BUTTON
                if (GameObject.Find("AddLowCountProductsButton") == null)
                {
                    GameObject buttonsBar = GameObject.Find("Buttons_Bar");
                    if (buttonsBar != null)
                    {
                        // Try to locate the "Buy Empty Box" button anywhere in the scene first
                        Transform sceneLabelAnchor = FindButtonByLabel(new[] {"buy empty box", "empty box", "empty"});
                        if (sceneLabelAnchor != null && sceneLabelAnchor.parent != null && sceneLabelAnchor.parent.Find("AddLowCountProductsButton") == null)
                        {
                            GameObject newButton = Object.Instantiate(sceneLabelAnchor.gameObject, sceneLabelAnchor.parent);
                            newButton.name = "AddLowCountProductsButton";
                            SetupLowCountButton(newButton, sceneLabelAnchor, sceneLabelAnchor.parent);
                            return; // created successfully in correct panel
                        }

                        // Prefer anchoring next to the "Buy Empty Box" button (by name or label text)
                        Transform anchor = FindButtonAnchor(buttonsBar.transform, new[] {"empty", "buy empty", "emptybox"});
                        if (anchor == null)
                        {
                            // Fallback to supermarket button
                            anchor = buttonsBar.transform.Find("Button_Supermarket");
                        }

                        GameObject templateButton = anchor != null ? anchor.gameObject : buttonsBar.GetComponentInChildren<Button>(true)?.gameObject;
                        if (templateButton != null)
                        {
                            // Clone template
                            GameObject newButton = Object.Instantiate(templateButton, buttonsBar.transform);
                            newButton.name = "AddLowCountProductsButton";

                            // Remove the PlayMakerFSM component to prevent old event listeners
                            PlayMakerFSM fsm = newButton.GetComponent<PlayMakerFSM>();
                            if (fsm != null)
                            {
                                Object.Destroy(fsm);
                            }

                            // Smaller size: scale down slightly
                            newButton.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

                            // Change the button's text (find any TMP child)
                            TextMeshProUGUI newButtonText = newButton.GetComponentInChildren<TextMeshProUGUI>(true);
                            if (newButtonText != null)
                            {
                                // Remove localization component if present
                                SetLocalizationString localizationComponent = newButtonText.GetComponent<SetLocalizationString>();
                                if (localizationComponent != null)
                                {
                                    Object.Destroy(localizationComponent);
                                }
                                newButtonText.text = "Add Low Count Products";
                                // Slightly reduce font if autosizing is off
                                if (!newButtonText.enableAutoSizing)
                                {
                                    newButtonText.fontSize = Mathf.Max(10, newButtonText.fontSize - 2);
                                }
                            }

                            // Position: place next to anchor if available
                            var layout = buttonsBar.GetComponent<HorizontalLayoutGroup>();
                            var grid = buttonsBar.GetComponent<GridLayoutGroup>();
                            if (anchor != null)
                            {
                                // Maintain sibling order (layout groups handle placement)
                                newButton.transform.SetSiblingIndex(anchor.GetSiblingIndex() + 1);

                                // If no layout, offset by anchor width
                                if (layout == null && grid == null)
                                {
                                    RectTransform anchorRt = anchor.GetComponent<RectTransform>();
                                    RectTransform newRt = newButton.GetComponent<RectTransform>();
                                    if (anchorRt != null && newRt != null)
                                    {
                                        float spacing = 10f;
                                        var newPos = anchorRt.anchoredPosition + new Vector2(anchorRt.rect.width + spacing, 0);
                                        newRt.anchoredPosition = newPos;
                                    }
                                }
                            }
                            else
                            {
                                // No anchor found: try to append at end; if no layout, place near first button
                                if (layout == null && grid == null)
                                {
                                    RectTransform refRt = templateButton != null ? templateButton.GetComponent<RectTransform>() : null;
                                    if (refRt == null)
                                    {
                                        var anyBtn = buttonsBar.GetComponentInChildren<Button>(true);
                                        refRt = anyBtn != null ? anyBtn.GetComponent<RectTransform>() : null;
                                    }
                                    RectTransform newRt = newButton.GetComponent<RectTransform>();
                                    if (refRt != null && newRt != null)
                                    {
                                        float spacing = 10f;
                                        var newPos = refRt.anchoredPosition + new Vector2(refRt.rect.width + spacing, 0);
                                        newRt.anchoredPosition = newPos;
                                    }
                                }
                            }

                            FinalizeLowCountButton(newButton);
                        }
                        else
                        {
                            Debug.LogError("Template button not found to clone.");
                        }
                    }
                }
                // === end: ADD LOW COUNT BUTTON
                
                if (LowCountProducts.AddLowCountProducts)
                {
                    // Explore the Buttons_Bar GameObject
                    // ExploreGameObject(buttonsBar);
                    
                    LowCountProducts.AddLowCountProducts = false;

                    Dictionary<int, Dictionary<string, object>> lowProductList = new Dictionary<int, Dictionary<string, object>>();

                    ProductListing productListing = __instance.GetComponent<ProductListing>();
                    if (productListing == null)
                    {
                        Debug.LogError("ProductListing component not found on ManagerBlackboard.");
                        return;
                    }

                    // Iterate over unlocked products
                    foreach (int productID in productListing.availableProducts)
                    {
                        GameObject productPrefab = productListing.productPrefabs[productID];
                        if (productPrefab != null)
                        {
                            int[] quantities = GetProductsExistences(__instance, productID);

                            int shelvesQuantity = quantities[0];
                            int storageQuantity = quantities[1];
                            int boxesQuantity = quantities[2];

                            if (shelvesQuantity <= LowCountProducts.LowCountProductsThreshold.Value && storageQuantity == 0 && boxesQuantity == 0)
                            {
                                if (!lowProductList.ContainsKey(productID))
                                {
                                    string price = GetProductPrice(productListing, productID);
                                    Dictionary<string, object> productInfo = new Dictionary<string, object>
                                    {
                                        { "ID", productID },
                                        { "price", price }
                                    };

                                    lowProductList[productID] = productInfo;
                                }
                            }
                        }
                    }

                    // Add low count products to cart
                    AddProductsToCart(__instance, lowProductList);

                    if (lowProductList.Count > 0)
                    {
                        LowCountProducts.NotificationType = "lowCountAddToCart";
                        LowCountProducts.Notify = true;
                    }
                }
            }
            else
            {
                // Remove button from ui if mod is disabled
                GameObject addLowBtn = GameObject.Find("AddLowCountProductsButton");
                if (addLowBtn != null)
                {
                    Object.Destroy(addLowBtn);
                }
            }
        }

        private static Transform FindButtonAnchor(Transform root, string[] keywords)
        {
            if (root == null || keywords == null || keywords.Length == 0) return null;

            // Depth-first search for a Button whose name or label matches keywords
            Transform MatchIn(Transform t)
            {
                string n = t.name.ToLowerInvariant();
                foreach (var k in keywords)
                {
                    if (string.IsNullOrEmpty(k)) continue;
                    if (n.Contains(k)) return t;
                }

                // Look for text label matches
                var tmp = t.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                {
                    string txt = tmp.text.ToLowerInvariant();
                    foreach (var k in keywords)
                    {
                        if (!string.IsNullOrEmpty(k) && txt.Contains(k)) return t;
                    }
                }
                return null;
            }

            Transform result = null;
            void Dfs(Transform t)
            {
                if (result != null) return;
                if (t.GetComponent<Button>() != null)
                {
                    var m = MatchIn(t);
                    if (m != null) { result = m; return; }
                }
                foreach (Transform c in t)
                {
                    Dfs(c);
                    if (result != null) return;
                }
            }
            Dfs(root);
            return result;
        }

        private static Transform FindButtonByLabel(string[] keywords)
        {
            try
            {
                var buttons = Object.FindObjectsOfType<Button>(true);
                foreach (var btn in buttons)
                {
                    if (btn == null) continue;
                    var t = btn.transform;
                    string n = t.name.ToLowerInvariant();
                    foreach (var k in keywords)
                    {
                        if (string.IsNullOrEmpty(k)) continue;
                        var key = k.ToLowerInvariant();
                        if (n.Contains(key)) return t;
                    }

                    var tmp = t.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                    {
                        string txt = tmp.text.ToLowerInvariant();
                        foreach (var k in keywords)
                        {
                            if (!string.IsNullOrEmpty(k) && txt.Contains(k.ToLowerInvariant()))
                                return t;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static void SetupLowCountButton(GameObject newButton, Transform anchor, Transform container)
        {
            // Remove FSM
            PlayMakerFSM fsm = newButton.GetComponent<PlayMakerFSM>();
            if (fsm != null) Object.Destroy(fsm);

            // Smaller size
            newButton.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            // Label text
            TextMeshProUGUI newButtonText = newButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (newButtonText != null)
            {
                var loc = newButtonText.GetComponent<SetLocalizationString>();
                if (loc != null) Object.Destroy(loc);
                newButtonText.text = "Add Low Count Products";
                if (!newButtonText.enableAutoSizing) newButtonText.fontSize = Mathf.Max(10, newButtonText.fontSize - 2);
            }

            // Position next to anchor
            var layout = container.GetComponent<HorizontalLayoutGroup>();
            var grid = container.GetComponent<GridLayoutGroup>();
            if (anchor != null)
            {
                newButton.transform.SetSiblingIndex(anchor.GetSiblingIndex() + 1);
                if (layout == null && grid == null)
                {
                    RectTransform anchorRt = anchor.GetComponent<RectTransform>();
                    RectTransform newRt = newButton.GetComponent<RectTransform>();
                    if (anchorRt != null && newRt != null)
                    {
                        float spacing = 10f;
                        newRt.anchoredPosition = anchorRt.anchoredPosition + new Vector2(anchorRt.rect.width + spacing, 0);
                    }
                }
            }

            FinalizeLowCountButton(newButton);
        }

        private static void FinalizeLowCountButton(GameObject newButton)
        {
            newButton.SetActive(true);
            var le = newButton.GetComponent<LayoutElement>();
            if (le != null && le.preferredWidth > 0)
            {
                le.preferredWidth *= 0.85f; // match visual scale
            }

            var buttonComponent = newButton.GetComponent<Button>();
            if (buttonComponent != null)
            {
                buttonComponent.onClick.RemoveAllListeners();
                buttonComponent.onClick.AddListener(() =>
                {
                    if (!LowCountProducts.AddLowCountProducts)
                        LowCountProducts.AddLowCountProducts = true;
                });
            }
        }
        
        private static int[] GetProductsExistences(ManagerBlackboard instance, int productIDToCompare)
        {
            // Use Harmony's AccessTools to access the private method
            MethodInfo method = AccessTools.Method(typeof(ManagerBlackboard), "GetProductsExistences");
            if (method != null)
            {
                object result = method.Invoke(instance, [productIDToCompare]);
                return (int[])result;
            }
            else
            {
                Debug.LogError("Could not find method GetProductsExistences");
                return new int[3];
            }
        }
        
        private static string GetProductPrice(ProductListing productListing, int productID)
        {
            GameObject productPrefab = productListing.productPrefabs[productID];

            float basePricePerUnit = productPrefab.GetComponent<Data_Product>().basePricePerUnit;
            int productTier = productPrefab.GetComponent<Data_Product>().productTier;

            float inflationFactor = productListing.tierInflation[productTier];
            float pricePerUnit = Mathf.Round(basePricePerUnit * inflationFactor * 100f) / 100f;

            int maxItemsPerBox = productPrefab.GetComponent<Data_Product>().maxItemsPerBox;

            float boxPrice = Mathf.Round(pricePerUnit * maxItemsPerBox * 100f) / 100f;

            return "$" + boxPrice.ToString("F2", CultureInfo.InvariantCulture);
        }
        
        private static void AddProductsToCart(ManagerBlackboard manager, Dictionary<int, Dictionary<string, object>> lowProductList)
        {
            foreach (var product in lowProductList)
            {
                var productInfo = product.Value;
                int productID = (int)productInfo["ID"];
                
                // Check if product is already in shopping list
                if (IsProductInShoppingList(manager, productID))
                {
                    // Skip adding this product
                    continue;
                }
                
                string productPriceText = productInfo["price"].ToString().Replace("$", "").Replace(",", ".");
                if (float.TryParse(productPriceText, NumberStyles.Float, CultureInfo.InvariantCulture, out float finalProductPrice))
                {
                    manager.AddShoppingListProduct(productID, finalProductPrice);
                }
            }
        }
        
        private static bool IsProductInShoppingList(ManagerBlackboard manager, int productID)
        {
            foreach (Transform item in manager.shoppingListParent.transform)
            {
                InteractableData data = item.GetComponent<InteractableData>();
                if (data != null && data.thisSkillIndex == productID)
                {
                    // Product is already in the shopping list
                    return true;
                }
            }

            return false;
        }
    }
    
    [HarmonyPatch(typeof(GameCanvas))]
    internal class NotificationHandler
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void NotificationHandler_Postfix(GameCanvas __instance, ref bool ___inCooldown)
        {
            if (LowCountProducts.Notify)
            {
                ___inCooldown = false;
                LowCountProducts.Notify = false;
                string text = "`";
                switch (LowCountProducts.NotificationType)
                {
                    case "lowCountToggle":
                        text = text + "Low Count Products: " + (LowCountProducts.LowCountProductsState ? "ON" : "OFF");
                        break;
                    case "lowCountAddToCart":
                        text = text + "Low Count Products: Added almost out of stock products to cart.";
                        break;
                }

                __instance.CreateCanvasNotification(text);
            }
        }
    }
    
    [HarmonyPatch(typeof(LocalizationManager))]
    internal class LocalizationHandler
    {
        [HarmonyPatch("GetLocalizationString")]
        [HarmonyPrefix]
        public static bool noLocalization_Prefix(ref string key, ref string __result)
        {
            if (key[0] == '`')
            {
                __result = key.Substring(1);
                return false;
            }
            return true;
        }
    }
}
