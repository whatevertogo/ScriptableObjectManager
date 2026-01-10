using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// ScriptableObject 创建服务。提供创建新 SO 资产的功能。
    /// </summary>
    public static class SOCreationService
    {
        /// <summary>
        /// 获取类型的默认保存路径。基于类型命名空间推断。
        /// </summary>
        public static string GetDefaultPathForType(Type type)
        {
            // 尝试从现有资产路径推断
            var existingAssets = SOScanService.GetAssetsOfType(type);
            if (existingAssets.Count > 0)
            {
                string existingPath = AssetDatabase.GetAssetPath(existingAssets[0]);
                return Path.GetDirectoryName(existingPath);
            }

            // 根据命名空间推断默认路径
            string ns = type.Namespace;
            if (!string.IsNullOrEmpty(ns))
            {
                // 将命名空间转换为路径
                string assetPath = "Assets/" + ns.Replace(".", "/");
                if (AssetDatabase.IsValidFolder(assetPath))
                    return assetPath;

                // 尝试创建文件夹
                string parentPath = "Assets";
                string[] segments = ns.Split('.');
                string currentPath = parentPath;

                for (int i = 0; i < segments.Length; i++)
                {
                    string segment = segments[i];
                    string testPath = Path.Combine(currentPath, segment);

                    if (!AssetDatabase.IsValidFolder(testPath))
                    {
                        // 尝试创建文件夹
                        string guid = AssetDatabase.CreateFolder(currentPath, segment);
                        if (string.IsNullOrEmpty(guid))
                        {
                            // 创建失败，返回 Assets 根目录
                            return "Assets";
                        }
                    }

                    currentPath = Path.Combine(currentPath, segment);
                }

                return currentPath.Replace("\\", "/");
            }

            return "Assets";
        }

        /// <summary>
        /// 创建新的 ScriptableObject 资产。
        /// </summary>
        public static ScriptableObject CreateAsset(Type type, string name, string folderPath)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
            if (string.IsNullOrWhiteSpace(folderPath))
                folderPath = "Assets";

            // 确保路径存在
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogWarning($"Folder does not exist: {folderPath}. Using Assets/ instead.");
                folderPath = "Assets";
            }

            // 创建实例
            var instance = ScriptableObject.CreateInstance(type);
            string assetPath = $"{folderPath}/{name}.asset";

            // 处理文件名冲突
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            // 创建资产
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 选中新创建的资产
            Selection.activeObject = instance;
            EditorGUIUtility.PingObject(instance);

            Debug.Log($"[SOManager] Created {type.Name} at: {assetPath}");

            return instance;
        }

        /// <summary>
        /// 显示创建资产对话框并创建资产。
        /// </summary>
        public static void ShowCreateDialog(Type type = null)
        {
            if (type == null)
            {
                // 显示类型选择对话框
                ShowTypeSelectionDialog();
            }
            else
            {
                // 直接显示创建对话框
                ShowCreateAssetDialog(type);
            }
        }

        /// <summary>
        /// 显示类型选择对话框。
        /// </summary>
        private static void ShowTypeSelectionDialog()
        {
            var creatableTypes = SOScanService.GetCreatableSOTypes().ToList();

            if (creatableTypes.Count == 0)
            {
                EditorUtility.DisplayDialog("No Types", "No creatable ScriptableObject types found.", "OK");
                return;
            }

            // 使用简易对话框选择类型
            var categories = GroupTypesByCategory(creatableTypes);

            // 创建选择窗口
            TypeSelectionWindow.Show(categories, OnTypeSelected);
        }

        /// <summary>
        /// 类型选择完成回调。
        /// </summary>
        private static void OnTypeSelected(Type selectedType)
        {
            if (selectedType != null)
            {
                ShowCreateAssetDialog(selectedType);
            }
        }

        /// <summary>
        /// 显示创建资产对话框（输入名称和路径）。
        /// </summary>
        private static void ShowCreateAssetDialog(Type type)
        {
            string defaultPath = GetDefaultPathForType(type);
            string defaultName = $"New{type.Name}";

            // 使用 SaveFilePanel 选择路径和名称
            string path = EditorUtility.SaveFilePanel(
                "Create ScriptableObject",
                defaultPath,
                defaultName,
                "asset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                // 转换为相对于 Assets 的路径
                if (path.StartsWith(Application.dataPath))
                {
                    string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    string fileName = Path.GetFileNameWithoutExtension(relativePath);
                    string folder = Path.GetDirectoryName(relativePath).Replace("\\", "/");

                    CreateAsset(type, fileName, folder);
                }
            }
        }

        /// <summary>
        /// 按分类分组类型。
        /// 规则：有 ManagedDataAttribute 的使用其 Category，否则归入 "Other"。
        /// </summary>
        private static SOCategoryGroup[] GroupTypesByCategory(System.Collections.Generic.List<Type> types)
        {
            var categoryMap = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Type>>();

            foreach (var type in types)
            {
                // 检查 ManagedDataAttribute 特性
                string category = "Other";
                var managedAttr = type.GetCustomAttributes(typeof(ManagedDataAttribute), false)
                    .FirstOrDefault() as ManagedDataAttribute;
                if (managedAttr != null && !string.IsNullOrEmpty(managedAttr.Category))
                {
                    category = managedAttr.Category;
                }

                if (!categoryMap.ContainsKey(category))
                    categoryMap[category] = new System.Collections.Generic.List<Type>();
                categoryMap[category].Add(type);
            }

            return categoryMap
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new SOCategoryGroup(kvp.Key, kvp.Value.OrderBy(t => t.Name).ToList()))
                .ToArray();
        }
    }

    /// <summary>
    /// 类型分类组。
    /// </summary>
    public sealed class SOCategoryGroup
    {
        public string CategoryName { get; }
        public System.Collections.Generic.List<Type> Types { get; }

        public SOCategoryGroup(string categoryName, System.Collections.Generic.List<Type> types)
        {
            CategoryName = categoryName;
            Types = types;
        }
    }
}
