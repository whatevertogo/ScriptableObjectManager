using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace ScriptableObjectDataManagement
{
    public static class DataScanService
    {
        public static void Scan()
        {
            DataRegistry.Clear();

            var guids = AssetDatabase.FindAssets("t:ScriptableObject");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (so == null) continue;

                var type = so.GetType();
                var attr = type.GetCustomAttribute<ManagedDataAttribute>();
                if (attr == null) continue;

                DataRegistry.Register(attr, so);
            }

            Debug.Log($"[DataManager] Scan Complete. Count: {DataRegistry.Count}");
        }
    }
}