using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// ScriptableObject 扫描服务。自动发现项目中所有的 ScriptableObject 类型及其资产实例。
    /// </summary>
    public static class SOScanService
    {
        /// <summary>
        /// 需要排除的 Unity 内置命名空间前缀。
        /// </summary>
        private static readonly string[] ExcludedNamespaces = new string[]
        {
            "UnityEngine",
            "UnityEditor",
            "UnityEditorInternal",
            "TMPro",
            "DOTween",
            "UnityEditor.UI",
            "UnityEngine.UI",
            "UnityEngine.InputSystem",
            "UnityEngine.Audio",
            "UnityEngine.Video",
            "Unity.VisualScripting"
        };

        /// <summary>
        /// 需要排除的 Unity 内置程序集前缀。
        /// </summary>
        private static readonly string[] ExcludedAssemblies = new string[]
        {
            "Unity.",
            "UnityEngine.",
            "UnityEditor.",
            "TextMeshPro",
            "DOTween"
        };

        /// <summary>
        /// 执行完整扫描，返回扫描结果。
        /// </summary>
        public static SOScanResult ScanAll()
        {
            var assetsByType = new Dictionary<Type, List<ScriptableObject>>();

            // 获取所有 ScriptableObject 资产的 GUID
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (so == null)
                    continue;

                var type = so.GetType();

                // 跳过 Unity 内置类型
                if (IsExcludedType(type))
                    continue;

                // 按类型分组
                if (!assetsByType.ContainsKey(type))
                    assetsByType[type] = new List<ScriptableObject>();

                assetsByType[type].Add(so);
            }

            // 构建分类树
            var categoryTree = SOTypeNode.BuildCategoryTree(assetsByType);

            return new SOScanResult(assetsByType, categoryTree);
        }

        /// <summary>
        /// 获取项目中所有的 ScriptableObject 类型（包括没有实例的类型）。
        /// </summary>
        public static IEnumerable<Type> GetAllSOTypes()
        {
            // 使用 TypeCache 获取所有 ScriptableObject 子类
            var soTypes = TypeCache.GetTypesDerivedFrom<ScriptableObject>();

            return soTypes
                .Where(t => !IsExcludedType(t) && !t.IsAbstract && !t.IsGenericType)
                .OrderBy(t => t.Name);
        }

        /// <summary>
        /// 获取可创建的 ScriptableObject 类型（非抽象、非泛型、非排除）。
        /// </summary>
        public static IEnumerable<Type> GetCreatableSOTypes()
        {
            var soTypes = TypeCache.GetTypesDerivedFrom<ScriptableObject>();

            return soTypes
                .Where(t => !IsExcludedType(t) &&
                           !t.IsAbstract &&
                           !t.IsGenericType &&
                           !t.IsNested &&
                           !t.IsInterface)
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name);
        }

        /// <summary>
        /// 检查类型是否应被排除。
        /// </summary>
        private static bool IsExcludedType(Type type)
        {
            if (type == null)
                return true;

            // 检查命名空间
            string ns = type.Namespace;
            if (!string.IsNullOrEmpty(ns))
            {
                foreach (var excluded in ExcludedNamespaces)
                {
                    if (ns.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            // 检查程序集名称（捕获那些命名空间不规范但来自 Unity 包的类型）
            string assembly = type.Assembly.GetName().Name;
            if (!string.IsNullOrEmpty(assembly))
            {
                foreach (var excluded in ExcludedAssemblies)
                {
                    if (assembly.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            // 排除内部类型
            if (type.IsNestedPrivate || type.IsNestedFamily)
                return true;

            return false;
        }

        /// <summary>
        /// 按命名空间过滤 ScriptableObject 类型。
        /// </summary>
        public static IEnumerable<Type> GetSOTypesByNamespace(string namespacePattern)
        {
            return GetCreatableSOTypes()
                .Where(t => t.Namespace?.Contains(namespacePattern, StringComparison.OrdinalIgnoreCase) == true);
        }

        /// <summary>
        /// 按名称搜索 ScriptableObject 类型。
        /// </summary>
        public static IEnumerable<Type> SearchSOTypes(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return GetCreatableSOTypes();

            return GetCreatableSOTypes()
                .Where(t => t.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                           (t.Namespace?.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
        }

        /// <summary>
        /// 获取指定类型的所有资产。
        /// </summary>
        public static List<ScriptableObject> GetAssetsOfType(Type type)
        {
            var results = new List<ScriptableObject>();
            var guids = AssetDatabase.FindAssets($"t:{type.Name}");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (so != null && so.GetType() == type)
                {
                    results.Add(so);
                }
            }

            return results;
        }
    }
}
