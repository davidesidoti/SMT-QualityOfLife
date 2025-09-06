using System;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using BepInEx;
using System.IO;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SMTQualityOfLife
{
    public static class DebugBlackboard
    {
        public static void DumpManagerBlackboard()
        {
            var mb = Object.FindObjectOfType<ManagerBlackboard>();
            if (mb == null)
            {
                Debug.Log("[SMT QoL] Blackboard debug: ManagerBlackboard not found in scene.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("[SMT QoL] ===== ManagerBlackboard Dump (skills/achievements scan) =====");
            sb.AppendLine($"Time: {DateTime.Now:O}");

            // Traverse transform tree and collect interesting nodes
            int printed = 0;
            TraverseChildren(mb.transform, "", ref printed, sb);

            // Additionally, try to find any MonoBehaviours with names suggesting achievements/skills
            var behaviours = Object.FindObjectsOfType<MonoBehaviour>(true)
                .Where(b => b != null && b.GetType() != null &&
                            (b.GetType().Name.IndexOf("Achiev", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             b.GetType().Name.IndexOf("Skill", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             b.GetType().Name.IndexOf("Employee", StringComparison.OrdinalIgnoreCase) >= 0))
                .Take(100)
                .ToArray();

            sb.AppendLine($"[SMT QoL] Candidate achievement/skill components found: {behaviours.Length}");
            foreach (var b in behaviours)
            {
                try
                {
                    var t = b.GetType();
                    string path = GetFullPath(b.gameObject.transform);
                    sb.AppendLine($"Component: {t.Name} on {path}");

                    // Print boolean fields/properties that look like unlock flags
                    foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (f.FieldType == typeof(bool) && f.Name.IndexOf("unlock", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            object val = SafeGet(() => f.GetValue(b));
                            sb.AppendLine($"  Field {f.Name} = {val}");
                        }
                    }
                    foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (p.PropertyType == typeof(bool) && p.Name.IndexOf("unlock", StringComparison.OrdinalIgnoreCase) >= 0 && p.GetIndexParameters().Length == 0)
                        {
                            object val = SafeGet(() => p.GetValue(b));
                            sb.AppendLine($"  Prop {p.Name} = {val}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"  [err] {ex.Message}");
                }
            }

            // Emit in chunks to avoid log truncation
            var text = sb.ToString();
            foreach (var chunk in Chunk(text, 800))
            {
                Debug.Log(chunk);
            }
            TryWriteToDumpFile(text);
        }

        public static void DumpAchievementsAndEmployees()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[SMT QoL] ===== Achievements/Employees Dump =====");
            sb.AppendLine($"Time: {DateTime.Now:O}");

            // Find AchievementsManager component
            var allBehaviours = Object.FindObjectsOfType<MonoBehaviour>(true);
            var achievements = allBehaviours.FirstOrDefault(b => b != null && b.GetType().Name.Equals("AchievementsManager", StringComparison.OrdinalIgnoreCase));
            var empGen = allBehaviours.FirstOrDefault(b => b != null && b.GetType().Name.Equals("EmployeesDataGeneration", StringComparison.OrdinalIgnoreCase));

            if (achievements != null)
            {
                sb.AppendLine($"Component: {achievements.GetType().Name} on {GetFullPath(achievements.transform)}");
                // Explicitly dump the achievements name/index and unlock state
                try
                {
                    var t = achievements.GetType();
                    var namesField = t.GetField("achievementStrings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var unlockedField = t.GetField("unlockedArray", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    var names = namesField?.GetValue(achievements) as System.Collections.IEnumerable;
                    var unlocked = unlockedField?.GetValue(achievements) as System.Collections.IEnumerable;

                    if (names != null)
                    {
                        var nameList = names.Cast<object>().Select(o => o?.ToString() ?? "").ToList();
                        var unlockList = unlocked != null ? unlocked.Cast<object>().Select(o => Convert.ToBoolean(o)).ToList() : new System.Collections.Generic.List<bool>();
                        sb.AppendLine($"Counts: names={nameList.Count}, unlocked={unlockList.Count}");

                        // First print only likely employee-related achievements for easier copy
                        sb.AppendLine("EMP-ACH (likely employee-related achievements)");
                        for (int i = 0; i < nameList.Count; i++)
                        {
                            string nm = nameList[i];
                            bool isUnlocked = (i < unlockList.Count) && unlockList[i];
                            if (LooksEmployeeRelated(nm))
                            {
                                sb.AppendLine($"[SMT QoL] EMP-ACH: {i} → {nm} [{isUnlocked}]");
                            }
                        }

                        sb.AppendLine("Achievement Index → Name [Unlocked]");
                        for (int i = 0; i < nameList.Count; i++)
                        {
                            bool isUnlocked = (i < unlockList.Count) && unlockList[i];
                            string nm = nameList[i];
                            string tag = LooksEmployeeRelated(nm) ? " <EMP>" : string.Empty;
                            sb.AppendLine($"  {i} → {nm} [{isUnlocked}]{tag}");
                        }
                    }
                    else
                    {
                        sb.AppendLine("  Could not read achievementStrings.") ;
                    }
                }
                catch (System.Exception ex)
                {
                    sb.AppendLine($"  Achievements explicit dump failed: {ex.Message}");
                }
            }
            else
            {
                sb.AppendLine("AchievementsManager not found.");
            }

            if (empGen != null)
            {
                sb.AppendLine($"Component: {empGen.GetType().Name} on {GetFullPath(empGen.transform)}");
                DumpObjectGraph(empGen, sb, maxDepth: 2, maxItems: 50, filterNames: new[] { "employee", "max", "min", "count", "npc" });
            }
            else
            {
                sb.AppendLine("EmployeesDataGeneration not found.");
            }

            var text = sb.ToString();
            foreach (var chunk in Chunk(text, 800))
            {
                Debug.Log(chunk);
            }
            TryWriteToDumpFile(text);
        }

        public static void DumpNpcManager()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[SMT QoL] ===== NPC_Manager Dump =====");
            sb.AppendLine($"Time: {DateTime.Now:O}");

            var npc = Object.FindObjectOfType<NPC_Manager>();
            if (npc == null)
            {
                Debug.Log("[SMT QoL] NPC_Manager not found in scene.");
                return;
            }

            var t = npc.GetType();
            sb.AppendLine($"Type: {t.FullName}");

            // Key numeric properties
            TryPrintField(sb, t, npc, "maxEmployees");
            TryPrintField(sb, t, npc, "minEmployees");
            TryPrintField(sb, t, npc, "employeesCap");
            TryPrintField(sb, t, npc, "maxEmployeesCap");
            TryPrintField(sb, t, npc, "employeesLimit");

            // Dump arrays/flags that look related
            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var f in fields)
            {
                string n = f.Name.ToLowerInvariant();
                if (!(n.Contains("employee") || n.Contains("npc") || n.Contains("unlock") || n.Contains("upgrade") || n.Contains("achiev") || n.Contains("max")))
                    continue;

                object val = SafeGet(() => f.GetValue(npc));
                if (val == null) { sb.AppendLine($"  {f.Name} = <null>"); continue; }

                var ft = f.FieldType;
                if (ft.IsArray)
                {
                    var arr = (Array)val;
                    sb.AppendLine($"  {f.Name} (array len={arr.Length})");
                    int len = Math.Min(arr.Length, 50);
                    for (int i = 0; i < len; i++)
                    {
                        var iv = arr.GetValue(i);
                        sb.AppendLine($"    [{i}] = {iv}");
                    }
                    if (arr.Length > len) sb.AppendLine("    … (truncated)");
                }
                else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(ft) && ft != typeof(string))
                {
                    sb.AppendLine($"  {f.Name} (enumerable)");
                    int i = 0;
                    foreach (var item in (System.Collections.IEnumerable)val)
                    {
                        if (i++ >= 50) { sb.AppendLine("    … (truncated)"); break; }
                        sb.AppendLine($"    - {item}");
                    }
                }
                else
                {
                    sb.AppendLine($"  {f.Name} = {val}");
                }
            }

            // Heuristic: look for achievement index mappings inside NPC_Manager
            sb.AppendLine("Heuristic: Achievement index-like fields");
            foreach (var f in fields)
            {
                if (!f.FieldType.IsArray) continue;
                var et = f.FieldType.GetElementType();
                if (et != typeof(int) && et != typeof(short)) continue;
                string n = f.Name.ToLowerInvariant();
                if (!(n.Contains("achiev") || n.Contains("unlock") || n.Contains("employee"))) continue;
                var arr = (Array)SafeGet(() => f.GetValue(npc));
                if (arr == null) continue;
                int len = Math.Min(arr.Length, 20);
                sb.AppendLine($"  {f.Name} (int[] len={arr.Length}) sample:");
                for (int i = 0; i < len; i++)
                {
                    sb.AppendLine($"    [{i}] = {arr.GetValue(i)}");
                }
                if (arr.Length > len) sb.AppendLine("    … (truncated)");
            }

            var text = sb.ToString();
            foreach (var chunk in Chunk(text, 800))
            {
                Debug.Log(chunk);
            }
            TryWriteToDumpFile(text);
        }

        private static void TryPrintField(StringBuilder sb, Type t, object instance, string name)
        {
            var f = AccessTools.Field(t, name);
            if (f != null)
            {
                var val = SafeGet(() => f.GetValue(instance));
                sb.AppendLine($"  {name} = {val}");
            }
        }

        private static void TryDumpBoolArrayFieldOrProp(StringBuilder sb, object instance, Type t, string name)
        {
            object val = null;
            var f = AccessTools.Field(t, name);
            if (f != null)
            {
                val = SafeGet(() => f.GetValue(instance));
            }
            else
            {
                var p = AccessTools.Property(t, name);
                if (p != null && p.GetIndexParameters().Length == 0)
                {
                    val = SafeGet(() => p.GetValue(instance));
                }
            }

            if (val is bool[] arr)
            {
                sb.AppendLine($"  {name} (bool[] len={arr.Length})");
                for (int i = 0; i < arr.Length; i++)
                {
                    sb.AppendLine($"    [{i}] = {arr[i]}");
                }
            }
            else if (val is System.Collections.IEnumerable en && val is not string)
            {
                int i = 0; bool any = false;
                sb.AppendLine($"  {name} (IEnumerable)");
                foreach (var item in en)
                {
                    any = true;
                    sb.AppendLine($"    [{i++}] = {item}");
                }
                if (!any)
                {
                    sb.AppendLine("    <empty>");
                }
            }
            else if (val != null)
            {
                sb.AppendLine($"  {name} = {val}");
            }
        }

        private static void DumpAllStringArrays(StringBuilder sb, object instance, Type t)
        {
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (f.FieldType == typeof(string[]))
                {
                    var arr = SafeGet(() => f.GetValue(instance)) as string[];
                    if (arr == null) continue;
                    sb.AppendLine($"  {f.Name} (string[] len={arr.Length})");
                    for (int i = 0; i < arr.Length; i++)
                    {
                        sb.AppendLine($"    [{i}] = {arr[i]}");
                    }
                }
            }
            foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (p.PropertyType == typeof(string[]) && p.GetIndexParameters().Length == 0)
                {
                    var arr = SafeGet(() => p.GetValue(instance)) as string[];
                    if (arr == null) continue;
                    sb.AppendLine($"  {p.Name} (string[] len={arr.Length})");
                    for (int i = 0; i < arr.Length; i++)
                    {
                        sb.AppendLine($"    [{i}] = {arr[i]}");
                    }
                }
            }
        }

        private static void DumpAllUnityObjectArrays(StringBuilder sb, object instance, Type t)
        {
            bool IsUnityObjectArray(Type ft) => ft.IsArray && typeof(UnityEngine.Object).IsAssignableFrom(ft.GetElementType());

            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (IsUnityObjectArray(f.FieldType))
                {
                    var arr = SafeGet(() => f.GetValue(instance)) as UnityEngine.Object[];
                    if (arr == null) continue;
                    sb.AppendLine($"  {f.Name} ({f.FieldType.Name} len={arr.Length})");
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var o = arr[i];
                        if (o == null) { sb.AppendLine($"    [{i}] = <null>"); continue; }
                        string nm = o.name;
                        string txt = TryGetLabelText(o);
                        if (!string.IsNullOrEmpty(txt))
                            sb.AppendLine($"    [{i}] = {nm} | Text={txt}");
                        else
                            sb.AppendLine($"    [{i}] = {nm}");
                    }
                }
            }
            foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (p.GetIndexParameters().Length == 0 && IsUnityObjectArray(p.PropertyType))
                {
                    var arr = SafeGet(() => p.GetValue(instance)) as UnityEngine.Object[];
                    if (arr == null) continue;
                    sb.AppendLine($"  {p.Name} ({p.PropertyType.Name} len={arr.Length})");
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var o = arr[i];
                        if (o == null) { sb.AppendLine($"    [{i}] = <null>"); continue; }
                        string nm = o.name;
                        string txt = TryGetLabelText(o);
                        if (!string.IsNullOrEmpty(txt))
                            sb.AppendLine($"    [{i}] = {nm} | Text={txt}");
                        else
                            sb.AppendLine($"    [{i}] = {nm}");
                    }
                }
            }
        }

        private static string TryGetLabelText(UnityEngine.Object obj)
        {
            try
            {
                if (obj is GameObject go)
                {
                    var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (label != null && !string.IsNullOrEmpty(label.text)) return label.text;
                }
                if (obj is Component c)
                {
                    var label = c.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (label != null && !string.IsNullOrEmpty(label.text)) return label.text;
                }
            }
            catch { }
            return null;
        }

        public static void DumpButtonsBar()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[SMT QoL] ===== Buttons_Bar Dump =====");
            sb.AppendLine($"Time: {DateTime.Now:O}");

            Transform root = null;
            var go = GameObject.Find("Buttons_Bar");
            if (go != null) root = go.transform;
            if (root == null)
            {
                var all = Object.FindObjectsOfType<Transform>(true);
                root = all.FirstOrDefault(t => t != null && t.name.Equals("Buttons_Bar", StringComparison.OrdinalIgnoreCase));
                if (root == null)
                {
                    // Fallback: any transform containing both words
                    root = all.FirstOrDefault(t => t != null && t.name.ToLowerInvariant().Contains("button") && t.name.ToLowerInvariant().Contains("bar"));
                }
            }

            if (root == null)
            {
                sb.AppendLine("Buttons_Bar not found. Please open the manager UI before dumping.");
                var text0 = sb.ToString();
                foreach (var chunk in Chunk(text0, 800)) Debug.Log(chunk);
                TryWriteToDumpFile(text0);
                return;
            }

            sb.AppendLine($"Root: {GetFullPath(root)}");
            DumpUIHierarchy(root, sb, 0, maxDepth: 4, maxNodes: 300);

            var text = sb.ToString();
            foreach (var chunk in Chunk(text, 800)) Debug.Log(chunk);
            TryWriteToDumpFile(text);
        }

        private static void DumpUIHierarchy(Transform t, StringBuilder sb, int depth, int maxDepth, int maxNodes, ref int count)
        {
            if (t == null || count >= maxNodes || depth > maxDepth) return;
            count++;

            string indent = new string(' ', depth * 2);
            var btn = t.GetComponent<UnityEngine.UI.Button>();
            var tmp = t.GetComponentInChildren<TextMeshProUGUI>(true);
            var rect = t as RectTransform;

            string tag = btn != null ? "[Button]" : string.Empty;
            string txt = tmp != null && !string.IsNullOrEmpty(tmp.text) ? $" text=\"{tmp.text}\"" : string.Empty;
            string size = rect != null ? $" rect=({rect.rect.width}x{rect.rect.height}) pos=({rect.anchoredPosition.x},{rect.anchoredPosition.y})" : string.Empty;
            sb.AppendLine($"{indent}- {t.name} {tag}{txt}{size}");

            for (int i = 0; i < t.childCount; i++)
            {
                DumpUIHierarchy(t.GetChild(i), sb, depth + 1, maxDepth, maxNodes, ref count);
            }
        }

        private static void DumpUIHierarchy(Transform t, StringBuilder sb, int depth, int maxDepth, int maxNodes)
        {
            int c = 0;
            DumpUIHierarchy(t, sb, depth, maxDepth, maxNodes, ref c);
        }

        public static void DumpSkillSystems()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[SMT QoL] ===== Skill/Upgrade Systems Dump =====");
            sb.AppendLine($"Time: {DateTime.Now:O}");

            var behaviours = Object.FindObjectsOfType<MonoBehaviour>(true)
                .Where(b => b != null && b.GetType() != null)
                .ToArray();

            // Candidates by type name
            var candidates = behaviours.Where(b =>
                NameHas(b.GetType().Name, new[] { "Skill", "Skills", "Upgrade", "Upgrades", "Blackboard" }))
                .ToArray();

            sb.AppendLine($"Candidates: {candidates.Length}");
            foreach (var b in candidates)
            {
                try
                {
                    var t = b.GetType();
                    sb.AppendLine($"Component: {t.Name} on {GetFullPath(b.transform)}");
                    // Dump fields/properties with relevant names
                    DumpObjectGraph(b, sb, maxDepth: 1, maxItems: 80, filterNames: new[] {
                        "skill", "point", "upgrade", "unlocked", "unlock", "employee", "max", "cap", "limit", "npc"
                    });

                    // UpgradesManager: dump upgrade flag arrays explicitly
                    if (t.Name.Equals("UpgradesManager", StringComparison.OrdinalIgnoreCase))
                    {
                        TryDumpBoolArrayFieldOrProp(sb, b, t, "storeSpaceUpgrades");
                        TryDumpBoolArrayFieldOrProp(sb, b, t, "storageSpaceUpgrades");
                        TryDumpBoolArrayFieldOrProp(sb, b, t, "extraUpgrades");
                        TryDumpBoolArrayFieldOrProp(sb, b, t, "NetworkstoreSpaceUpgrades");
                        TryDumpBoolArrayFieldOrProp(sb, b, t, "NetworkstorageSpaceUpgrades");
                        TryDumpBoolArrayFieldOrProp(sb, b, t, "NetworkextraUpgrades");

                        // Also dump any string[] arrays that may contain names/descriptions
                        DumpAllStringArrays(sb, b, t);
                        // And UnityEngine.Object arrays (names), helpful for mapping indices
                        DumpAllUnityObjectArrays(sb, b, t);
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"  [err] {ex.Message}");
                }
            }

            foreach (var chunk in Chunk(sb.ToString(), 800))
            {
                Debug.Log(chunk);
            }
        }

        private static bool LooksEmployeeRelated(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            var n = name.ToLowerInvariant();
            return n.Contains("employee") || n.Contains("employees") || n.Contains("npc") || n.Contains("staff") || n.Contains("hire") || n.Contains("hired") || n.Contains("worker");
        }

        private static bool NameHas(string text, string[] keys)
        {
            if (string.IsNullOrEmpty(text)) return false;
            var n = text.ToLowerInvariant();
            return keys.Any(k => n.Contains(k.ToLowerInvariant()));
        }

        private static void DumpObjectGraph(object obj, StringBuilder sb, int maxDepth, int maxItems, string[] filterNames)
        {
            if (obj == null) return;
            Type t = obj.GetType();
            sb.AppendLine($"  Type: {t.FullName}");

            // Fields
            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int printed = 0;
            foreach (var f in fields)
            {
                if (printed >= maxItems) break;
                if (filterNames != null && filterNames.Length > 0)
                {
                    string n = f.Name.ToLowerInvariant();
                    if (!filterNames.Any(k => n.Contains(k))) continue;
                }

                object val = SafeGet(() => f.GetValue(obj));
                PrintMember(sb, "Field", f.Name, f.FieldType, val, maxDepth, ref printed, maxItems, filterNames);
            }

            // Properties
            var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .Where(p => p.GetIndexParameters().Length == 0);
            foreach (var p in props)
            {
                if (printed >= maxItems) break;
                if (filterNames != null && filterNames.Length > 0)
                {
                    string n = p.Name.ToLowerInvariant();
                    if (!filterNames.Any(k => n.Contains(k))) continue;
                }

                object val = SafeGet(() => p.GetValue(obj));
                PrintMember(sb, "Prop ", p.Name, p.PropertyType, val, maxDepth, ref printed, maxItems, filterNames);
            }
        }

        private static void PrintMember(StringBuilder sb, string kind, string name, Type type, object val, int depth, ref int printed, int maxItems, string[] filterNames)
        {
            printed++;
            if (val == null)
            {
                sb.AppendLine($"  {kind} {name} ({type.Name}) = <null>");
                return;
            }

            // Simple types
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
            {
                sb.AppendLine($"  {kind} {name} ({type.Name}) = {val}");
                return;
            }

            // Collections
            if (val is System.Collections.IEnumerable enumerable && type != typeof(string))
            {
                sb.AppendLine($"  {kind} {name} ({type.Name}) → enumerable");
                int i = 0;
                foreach (var item in enumerable)
                {
                    if (i >= 25) { sb.AppendLine("    … (truncated)"); break; }
                    if (item == null) { sb.AppendLine("    - <null>"); i++; continue; }
                    var it = item.GetType();
                    if (it.IsPrimitive || it == typeof(string) || it.IsEnum)
                    {
                        sb.AppendLine($"    - {item}");
                    }
                    else
                    {
                        // Heuristic: print common fields
                        var info = new StringBuilder();
                        foreach (var sf in it.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            string n = sf.Name.ToLowerInvariant();
                            if (n.Contains("name") || n.Contains("title") || n.Contains("id") || n.Contains("unlock") || n.Contains("employee") || n.Contains("skill"))
                            {
                                var v = SafeGet(() => sf.GetValue(item));
                                info.Append($" {sf.Name}={v};");
                            }
                        }
                        foreach (var sp in it.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            if (sp.GetIndexParameters().Length > 0) continue;
                            string n = sp.Name.ToLowerInvariant();
                            if (n.Contains("name") || n.Contains("title") || n.Contains("id") || n.Contains("unlock") || n.Contains("employee") || n.Contains("skill"))
                            {
                                var v = SafeGet(() => sp.GetValue(item));
                                info.Append($" {sp.Name}={v};");
                            }
                        }
                        sb.AppendLine($"    - {it.Name}:{info}");
                    }
                    i++;
                }
                return;
            }

            // Nested object (limited depth)
            sb.AppendLine($"  {kind} {name} ({type.Name}) → object");
            if (depth > 0)
            {
                DumpObjectGraph(val, sb, depth - 1, maxItems - printed, filterNames);
            }
        }

        private static void TraverseChildren(Transform t, string prefix, ref int printed, StringBuilder sb)
        {
            // Limit total lines to avoid log spam
            if (printed > 300) return;

            string name = t.gameObject.name;
            string path = string.IsNullOrEmpty(prefix) ? name : prefix + "/" + name;

            bool nameInteresting = name.IndexOf("skill", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   name.IndexOf("achiev", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   name.IndexOf("employee", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   name.IndexOf("upgrade", StringComparison.OrdinalIgnoreCase) >= 0;

            // If this node looks interesting, print its info
            if (nameInteresting)
            {
                printed++;
                sb.AppendLine($"Node: {path}");

                // Text label(s)
                foreach (var tmp in t.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (!string.IsNullOrEmpty(tmp.text))
                    {
                        sb.AppendLine($"  TMP: {tmp.text}");
                    }
                }

                // InteractableData: log indices and names if present
                foreach (var comp in t.GetComponents<MonoBehaviour>())
                {
                    if (comp == null) continue;
                    var type = comp.GetType();
                    if (type.Name.Equals("InteractableData", StringComparison.OrdinalIgnoreCase))
                    {
                        var idx = AccessTools.Field(type, "thisSkillIndex");
                        var nm = AccessTools.Field(type, "thisName");
                        if (idx != null)
                        {
                            var val = SafeGet(() => idx.GetValue(comp));
                            sb.AppendLine($"  InteractableData.thisSkillIndex = {val}");
                        }
                        if (nm != null)
                        {
                            var val = SafeGet(() => nm.GetValue(comp));
                            sb.AppendLine($"  InteractableData.thisName = {val}");
                        }
                    }
                }
            }

            // Continue traversal
            for (int i = 0; i < t.childCount; i++)
            {
                TraverseChildren(t.GetChild(i), path, ref printed, sb);
            }
        }

        private static string GetFullPath(Transform t)
        {
            var parts = new System.Collections.Generic.List<string>();
            while (t != null)
            {
                parts.Add(t.name);
                t = t.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static object SafeGet(Func<object> f)
        {
            try { return f(); } catch { return "<err>"; }
        }

        private static System.Collections.Generic.IEnumerable<string> Chunk(string s, int size)
        {
            for (int i = 0; i < s.Length; i += size)
                yield return s.Substring(i, Math.Min(size, s.Length - i));
        }

        private static void TryWriteToDumpFile(string text)
        {
            try
            {
                string root = Paths.BepInExRootPath;
                if (!string.IsNullOrEmpty(root))
                {
                    string path = Path.Combine(root, "SMTQoL_dump.txt");
                    File.AppendAllText(path, text + "\n\n");
                    Debug.Log($"[SMT QoL] Dump appended to {path}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.Log($"[SMT QoL] Failed to write dump file: {ex.Message}");
            }
        }
    }
}
