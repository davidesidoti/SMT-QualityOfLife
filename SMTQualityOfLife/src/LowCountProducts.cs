using System;
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
    /// <summary>
    /// Selects how the <c>Add Low Count Products</c> button behaves when clicked.
    /// </summary>
    public enum LowCountMode
    {
        /// <summary>Legacy behaviour: skip products that have stock in storage or unopened boxes, and skip products already in the cart.</summary>
        Original = 0,
        /// <summary>Skip the legacy exclusions; each click adds exactly 1 box per low-stock product.</summary>
        OneBoxPerClick = 1,
        /// <summary>Skip the legacy exclusions; each click adds enough boxes to bring total stock up to the threshold.</summary>
        AutoFillToThreshold = 2,
    }

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
        public static ConfigEntry<int> LowCountProductsHighThreshold;
        public static ConfigEntry<bool> UseHighThreshold;
        public static ConfigEntry<LowCountMode> Mode;

        // ==== Notification stuff
        public static bool Notify;
        public static string NotificationType;

        // === CLASS REFERENCES
        private readonly MainManager _manager;

        public LowCountProducts(ConfigFile config, MainManager manager, GUIUtilities guiUtilities)
        {
            _guiUtilities = guiUtilities;
            _manager = manager;
            LowCountProductsThreshold = config.Bind(
                "General",
                "LowCountProducts Low Threshold",
                20,
                "The low-end threshold value used by 'Add Low Count Products'. " +
                "Products whose total stock is below the active threshold will be restocked.");
            LowCountProductsHighThreshold = config.Bind(
                "General",
                "LowCountProducts High Threshold",
                60,
                "The high-end threshold value used by 'Add Low Count Products'. " +
                "Toggle between this and the low threshold with Ctrl+Y (or the in-window button).");
            UseHighThreshold = config.Bind(
                "General",
                "LowCountProducts Use High Threshold",
                false,
                "When true, the high threshold is used by 'Add Low Count Products'. " +
                "When false, the low threshold is used. Toggled by Ctrl+Y and the GUI button.");
            Mode = config.Bind(
                "General",
                "LowCountProducts Mode",
                LowCountMode.AutoFillToThreshold,
                "How 'Add Low Count Products' decides which products to add and how many boxes to add:\n" +
                "  Original           - Legacy behaviour: skip if product has stock in storage or unopened boxes, and skip if already in cart (adds 1 box per product).\n" +
                "  One Box Per Click  - Skip the legacy exclusions; each click adds 1 box per low-stock product. Click again to add more boxes.\n" +
                "  Auto-Fill          - Skip the legacy exclusions; each click adds enough boxes to bring total stock up to the threshold.");
        }

        /// <summary>
        /// Threshold value currently active for 'Add Low Count Products'.
        /// </summary>
        public static int ActiveThreshold =>
            UseHighThreshold.Value ? LowCountProductsHighThreshold.Value : LowCountProductsThreshold.Value;

        /// <summary>
        /// Name of the currently active threshold ("Low" or "High") for UI display.
        /// </summary>
        public static string ActiveThresholdName =>
            UseHighThreshold.Value ? "High" : "Low";

        /// <summary>
        /// Flip between the low and high threshold. Returns the new state.
        /// </summary>
        public static bool ToggleActiveThreshold()
        {
            UseHighThreshold.Value = !UseHighThreshold.Value;
            return UseHighThreshold.Value;
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

                // === ACTIVE THRESHOLD (display + toggle)
                GUILayout.Label("Active Threshold", _guiUtilities.HeaderStyle);
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    $"Currently using: {ActiveThresholdName} = {ActiveThreshold}",
                    _guiUtilities.LabelStyle);
                if (GUILayout.Button(
                        UseHighThreshold.Value ? "Switch to Low" : "Switch to High",
                        GUILayout.Width(140)))
                {
                    ToggleActiveThresholdAndNotify();
                }
                GUILayout.EndHorizontal();
                GUILayout.Label(
                    "Tip: press Ctrl+Y in-game (or rebind 'LowCountProducts ToggleThresholdHotkey' in the config) " +
                    "to flip between Low and High without opening this window.",
                    _guiUtilities.DescriptionStyle);
                GUILayout.Space(20);
                _guiUtilities.DrawHorizontalLine();
                GUILayout.Space(20);

                // === LOW / HIGH THRESHOLD VALUES
                _guiUtilities.DrawIntButtonAddSection(
                    "Low Threshold",
                    "Threshold used when 'Use High Threshold' is OFF. " +
                    "In 'Original' mode, only shelf count is compared. In 'One Box Per Click' and 'Auto-Fill' modes, " +
                    "total stock (shelves + storage + unopened boxes) is compared.",
                    LowCountProductsThreshold.Value,
                    OnAddLowThresholdButtonClicked,
                    OnRemoveLowThresholdButtonClicked);

                _guiUtilities.DrawIntButtonAddSection(
                    "High Threshold",
                    "Threshold used when 'Use High Threshold' is ON. " +
                    "Press Ctrl+Y (or click 'Switch to High' above) to activate.",
                    LowCountProductsHighThreshold.Value,
                    OnAddHighThresholdButtonClicked,
                    OnRemoveHighThresholdButtonClicked);

                // === ADD MODE
                GUILayout.Label("Add Mode", _guiUtilities.HeaderStyle);
                GUILayout.Space(5);
                GUILayout.Label(
                    "Choose how 'Add Low Count Products' decides which products to add and how many boxes.",
                    _guiUtilities.DescriptionStyle);
                GUILayout.Space(8);

                string[] modeLabels =
                {
                    "Original",
                    "One Box Per Click",
                    "Auto-Fill",
                };
                LowCountMode[] modeValues =
                {
                    LowCountMode.Original,
                    LowCountMode.OneBoxPerClick,
                    LowCountMode.AutoFillToThreshold,
                };
                int currentIndex = (int)Mode.Value;
                int newIndex = GUILayout.Toolbar(currentIndex, modeLabels);
                if (newIndex != currentIndex && newIndex >= 0 && newIndex < modeValues.Length)
                {
                    Mode.Value = modeValues[newIndex];
                }

                GUILayout.Space(6);
                GUILayout.Label(DescribeMode(Mode.Value), _guiUtilities.DescriptionStyle);

                GUILayout.Space(20);
                _guiUtilities.DrawHorizontalLine();
                GUILayout.Space(20);

                GUILayout.EndScrollView();
            }
            else
            {
                _guiUtilities.DrawModDisabledContent("Low Count Products");
            }

        }

        private void OnAddLowThresholdButtonClicked()
        {
            LowCountProductsThreshold.Value++;
        }

        private void OnRemoveLowThresholdButtonClicked()
        {
            if (LowCountProductsThreshold.Value > 0)
            {
                LowCountProductsThreshold.Value--;
            }
        }

        private void OnAddHighThresholdButtonClicked()
        {
            LowCountProductsHighThreshold.Value++;
        }

        private void OnRemoveHighThresholdButtonClicked()
        {
            if (LowCountProductsHighThreshold.Value > 0)
            {
                LowCountProductsHighThreshold.Value--;
            }
        }

        private static void ToggleActiveThresholdAndNotify()
        {
            bool nowHigh = ToggleActiveThreshold();
            NotificationType = "lowCountThresholdToggle";
            Notify = true;
            Debug.Log($"[SMT QoL] Threshold toggled: now using {(nowHigh ? "High" : "Low")} = {ActiveThreshold}");
        }

        private static string DescribeMode(LowCountMode mode)
        {
            switch (mode)
            {
                case LowCountMode.Original:
                    return "Original — adds 1 box per product only when shelf count is below the threshold AND " +
                           "no stock is sitting in storage or unopened boxes. Products already in the cart are skipped.";
                case LowCountMode.OneBoxPerClick:
                    return "One Box Per Click — adds 1 box per product whose total stock is below the threshold, " +
                           "regardless of storage/boxes. Click again to add more boxes (the cart is not de-duplicated).";
                case LowCountMode.AutoFillToThreshold:
                    return "Auto-Fill — adds the number of boxes needed to bring total stock up to the threshold " +
                           "in one click (rounded up). Ignores storage/boxes and is not de-duplicated against the cart.";
                default:
                    return string.Empty;
            }
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
                    LowCountProducts.AddLowCountProducts = false;

                    Dictionary<int, Dictionary<string, object>> lowProductList = new Dictionary<int, Dictionary<string, object>>();

                    ProductListing productListing = __instance.GetComponent<ProductListing>();
                    if (productListing == null)
                    {
                        Debug.LogError("[SMT QoL] ProductListing component not found on ManagerBlackboard.");
                        return;
                    }

                    // Access productsData array via reflection (was productPrefabs before game update)
                    var productsDataField = AccessTools.Field(typeof(ProductListing), "productsData");
                    if (productsDataField == null)
                    {
                        Debug.LogError("[SMT QoL] ProductListing.productsData field not found.");
                        return;
                    }
                    Array productsData = productsDataField.GetValue(productListing) as Array;
                    if (productsData == null)
                    {
                        Debug.LogError("[SMT QoL] ProductListing.productsData is null.");
                        return;
                    }

                    // Iterate over unlocked products
                    foreach (int productID in productListing.availableProducts)
                    {
                        if (productID < 0 || productID >= productsData.Length) continue;

                        int[] quantities = GetProductsExistences(__instance, productID);

                        int shelvesQuantity = quantities[0];
                        int storageQuantity = quantities[1];
                        int boxesQuantity = quantities[2];

                        int totalUnits = shelvesQuantity + storageQuantity + boxesQuantity;
                        int threshold = LowCountProducts.ActiveThreshold;
                        LowCountMode mode = LowCountProducts.Mode.Value;

                        // Per-mode eligibility check
                        bool eligible;
                        switch (mode)
                        {
                            case LowCountMode.Original:
                                // Legacy behaviour: shelf-only threshold, plus storage/boxes must be empty.
                                eligible = shelvesQuantity <= threshold
                                           && storageQuantity == 0
                                           && boxesQuantity == 0;
                                break;
                            case LowCountMode.OneBoxPerClick:
                            case LowCountMode.AutoFillToThreshold:
                            default:
                                // Compare against total stock so already-stocked products aren't re-ordered.
                                eligible = totalUnits < threshold;
                                break;
                        }
                        if (!eligible) continue;

                        // Determine how many boxes to add (legacy mode is always 1)
                        int boxesNeeded = 1;
                        if (mode == LowCountMode.AutoFillToThreshold)
                        {
                            int maxItemsPerBox = GetMaxItemsPerBoxFromData(productsData, productID);
                            if (maxItemsPerBox <= 0) maxItemsPerBox = 1; // safety fallback
                            int unitsNeeded = threshold - totalUnits;
                            boxesNeeded = (int)Math.Ceiling((double)unitsNeeded / maxItemsPerBox);
                        }

                        if (boxesNeeded <= 0) continue;

                        if (!lowProductList.ContainsKey(productID))
                        {
                            string price = GetProductPriceFromData(productsData, productListing.tierInflation, productID);
                            Dictionary<string, object> productInfo = new Dictionary<string, object>
                            {
                                { "ID", productID },
                                { "price", price },
                                { "boxesNeeded", boxesNeeded },
                                { "mode", mode }
                            };

                            lowProductList[productID] = productInfo;
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
        
        // Cached reflection fields for ProductData struct (resolved once)
        private static FieldInfo _pdBasePriceField;
        private static FieldInfo _pdProductTierField;
        private static FieldInfo _pdMaxItemsField;
        private static bool _pdFieldsResolved;

        // Resolve ProductData fields via reflection on the first ProductData entry we encounter.
        // Returns true if all three fields were found.
        private static bool EnsureProductDataFieldsResolved(Array productsData)
        {
            if (_pdFieldsResolved)
            {
                return _pdBasePriceField != null && _pdProductTierField != null && _pdMaxItemsField != null;
            }

            // Find the first non-null entry to derive the element type
            Type elemType = null;
            for (int i = 0; i < productsData.Length; i++)
            {
                object entry = productsData.GetValue(i);
                if (entry != null)
                {
                    elemType = entry.GetType();
                    break;
                }
            }
            if (elemType == null)
            {
                _pdFieldsResolved = true; // give up; do not retry
                return false;
            }

            _pdBasePriceField = AccessTools.Field(elemType, "basePricePerUnit");
            _pdProductTierField = AccessTools.Field(elemType, "productTier");
            _pdMaxItemsField = AccessTools.Field(elemType, "maxItemsPerBox");
            _pdFieldsResolved = true;

            if (_pdBasePriceField == null || _pdProductTierField == null || _pdMaxItemsField == null)
            {
                Debug.LogError("[SMT QoL] ProductData fields not found: " +
                    $"basePricePerUnit={_pdBasePriceField != null}, " +
                    $"productTier={_pdProductTierField != null}, " +
                    $"maxItemsPerBox={_pdMaxItemsField != null}");
                return false;
            }
            return true;
        }

        private static string GetProductPriceFromData(Array productsData, float[] tierInflation, int productID)
        {
            if (productID < 0 || productID >= productsData.Length)
                return "$0.00";

            object entry = productsData.GetValue(productID);
            if (entry == null) return "$0.00";

            if (!EnsureProductDataFieldsResolved(productsData))
                return "$0.00";

            float basePricePerUnit = (float)_pdBasePriceField.GetValue(entry);
            int productTier = (int)_pdProductTierField.GetValue(entry);
            int maxItemsPerBox = (int)_pdMaxItemsField.GetValue(entry);

            float inflationFactor = (productTier >= 0 && productTier < tierInflation.Length)
                ? tierInflation[productTier] : 1f;
            float pricePerUnit = Mathf.Round(basePricePerUnit * inflationFactor * 100f) / 100f;
            float boxPrice = Mathf.Round(pricePerUnit * maxItemsPerBox * 100f) / 100f;

            return "$" + boxPrice.ToString("F2", CultureInfo.InvariantCulture);
        }

        private static int GetMaxItemsPerBoxFromData(Array productsData, int productID)
        {
            if (productID < 0 || productID >= productsData.Length)
                return 0;

            object entry = productsData.GetValue(productID);
            if (entry == null) return 0;

            if (!EnsureProductDataFieldsResolved(productsData) || _pdMaxItemsField == null)
                return 0;

            try
            {
                return (int)_pdMaxItemsField.GetValue(entry);
            }
            catch
            {
                return 0;
            }
        }
        
        private static void AddProductsToCart(ManagerBlackboard manager, Dictionary<int, Dictionary<string, object>> lowProductList)
        {
            foreach (var product in lowProductList)
            {
                var productInfo = product.Value;
                int productID = (int)productInfo["ID"];

                int boxesNeeded = 1;
                if (productInfo.TryGetValue("boxesNeeded", out object bnObj) && bnObj is int bn)
                {
                    boxesNeeded = bn;
                }

                LowCountMode mode = LowCountMode.OneBoxPerClick;
                if (productInfo.TryGetValue("mode", out object modeObj) && modeObj is LowCountMode m)
                {
                    mode = m;
                }

                // In Original mode, skip products that are already in the shopping list.
                if (mode == LowCountMode.Original && IsProductInShoppingList(manager, productID))
                {
                    continue;
                }

                string productPriceText = productInfo["price"].ToString().Replace("$", "").Replace(",", ".");
                if (!float.TryParse(productPriceText, NumberStyles.Float, CultureInfo.InvariantCulture, out float finalProductPrice))
                    continue;

                // Add the required number of boxes (AddShoppingListProduct adds one box per call).
                // In OneBoxPerClick and AutoFill modes, this naturally increments the cart even if
                // a previous click already added the product (matching the user's expectation).
                for (int i = 0; i < boxesNeeded; i++)
                {
                    manager.AddShoppingListProduct(productID, finalProductPrice);
                }
            }
        }

        private static bool IsProductInShoppingList(ManagerBlackboard manager, int productID)
        {
            if (manager.shoppingListParent == null) return false;
            foreach (Transform item in manager.shoppingListParent.transform)
            {
                InteractableData data = item.GetComponent<InteractableData>();
                if (data != null && data.thisSkillIndex == productID)
                {
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
                    case "lowCountThresholdToggle":
                        text = text + $"Low Count Products: Threshold set to {LowCountProducts.ActiveThresholdName} ({LowCountProducts.ActiveThreshold}).";
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
