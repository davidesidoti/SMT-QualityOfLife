using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SMTQualityOfLife
{
    internal static class NpcUpgradeGating
    {
        private static bool? _cachedEmployeeExtrasUnlocked;
        private static float _lastCheckTime;
        private const float CacheSeconds = 2f;

        public static bool EmployeeExtrasUnlocked()
        {
            if (_cachedEmployeeExtrasUnlocked.HasValue && Time.unscaledTime - _lastCheckTime < CacheSeconds)
                return _cachedEmployeeExtrasUnlocked.Value;

            bool result = false;
            try
            {
                result = CheckEmployeeExtrasUnlocked();
            }
            catch (Exception ex)
            {
                Debug.Log($"[SMT QoL] EmployeeExtrasUnlocked check failed: {ex.Message}");
            }
            _cachedEmployeeExtrasUnlocked = result;
            _lastCheckTime = Time.unscaledTime;
            return result;
        }

        private static bool CheckEmployeeExtrasUnlocked()
        {
            var upgradesManager = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true)
                .FirstOrDefault(b => b != null && b.GetType().Name.Equals("UpgradesManager", StringComparison.OrdinalIgnoreCase));
            if (upgradesManager == null)
                return false;

            var t = upgradesManager.GetType();

            // Prefer network-synced property; fallback to field
            bool[] extra = TryGetBoolArrayProperty(t, upgradesManager, "NetworkextraUpgrades")
                           ?? TryGetBoolArrayField(t, upgradesManager, "extraUpgrades");
            if (extra == null || extra.Length == 0)
                return false;

            // Attempt to locate a string[] that maps names/descriptions to the same length
            var candidateNameArrays = new List<(string Name, string[] Arr)>();
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (f.FieldType == typeof(string[]))
                {
                    var arr = f.GetValue(upgradesManager) as string[];
                    if (arr != null && arr.Length == extra.Length)
                        candidateNameArrays.Add(($"Field:{f.Name}", arr));
                }
            }
            foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (p.PropertyType == typeof(string[]) && p.GetIndexParameters().Length == 0)
                {
                    var arr = p.GetValue(upgradesManager) as string[];
                    if (arr != null && arr.Length == extra.Length)
                        candidateNameArrays.Add(($"Prop:{p.Name}", arr));
                }
            }

            var employeeIndices = new List<int>();
            foreach (var entry in candidateNameArrays)
            {
                for (int i = 0; i < entry.Arr.Length; i++)
                {
                    string s = entry.Arr[i] ?? string.Empty;
                    var n = s.ToLowerInvariant();
                    if (n.Contains("employee") || n.Contains("employees") || n.Contains("npc") || n.Contains("staff") || n.Contains("hire"))
                    {
                        if (!employeeIndices.Contains(i)) employeeIndices.Add(i);
                    }
                }
            }

            // If we couldn't discover names, fallback using NPC_Manager.maxEmployees heuristic
            if (employeeIndices.Count == 0)
            {
                var npcMgr = UnityEngine.Object.FindObjectOfType<NPC_Manager>();
                if (npcMgr != null)
                {
                    // Assume base 10 employees; consider unlocked if > 10
                    return npcMgr.maxEmployees > 10;
                }
                return false;
            }

            // Require all employee-related extra indices to be true
            foreach (var idx in employeeIndices)
            {
                if (idx < 0 || idx >= extra.Length) continue;
                if (!extra[idx]) return false;
            }
            return true;
        }

        private static bool[] TryGetBoolArrayProperty(Type t, object instance, string name)
        {
            var p = AccessTools.Property(t, name);
            if (p != null && p.GetIndexParameters().Length == 0)
            {
                try { return p.GetValue(instance) as bool[]; } catch { }
            }
            return null;
        }

        private static bool[] TryGetBoolArrayField(Type t, object instance, string name)
        {
            var f = AccessTools.Field(t, name);
            if (f != null)
            {
                try { return f.GetValue(instance) as bool[]; } catch { }
            }
            return null;
        }
    }
}

