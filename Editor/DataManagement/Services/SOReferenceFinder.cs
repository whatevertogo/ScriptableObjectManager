using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// 引用信息。
    /// </summary>
    public sealed class SOReferenceInfo
    {
        /// <summary>
        /// 引用资产的路径。
        /// </summary>
        public string AssetPath { get; }

        /// <summary>
        /// 引用资产的类型。
        /// </summary>
        public Type AssetType { get; }

        /// <summary>
        /// 引用资产的名称。
        /// </summary>
        public string AssetName => System.IO.Path.GetFileNameWithoutExtension(AssetPath);

        /// <summary>
        /// 是否为场景文件。
        /// </summary>
        public bool IsScene => AssetPath.EndsWith(".unity");

        /// <summary>
        /// 是否为预制体。
        /// </summary>
        public bool IsPrefab => AssetPath.EndsWith(".prefab");

        public SOReferenceInfo(string assetPath, Type assetType)
        {
            AssetPath = assetPath;
            AssetType = assetType;
        }
    }

    /// <summary>
    /// ScriptableObject 引用查找服务。查找哪些资产引用了指定的 SO。
    /// </summary>
    public static class SOReferenceFinder
    {
        /// <summary>
        /// 查找所有引用了目标资产的引用。
        /// </summary>
        public static List<SOReferenceInfo> FindReferences(ScriptableObject target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var references = new List<SOReferenceInfo>();
            string targetPath = AssetDatabase.GetAssetPath(target);

            if (string.IsNullOrEmpty(targetPath))
                return references;

            // 获取目标资产的弱引用
            var weakRef = new SerializedObject(target).FindProperty("m_Script");
            if (weakRef == null)
            {
                // 使用 GUID 方式查找引用
                string targetGuid = AssetDatabase.GUIDFromAssetPath(targetPath).ToString();
                return FindReferencesByGUID(targetGuid);
            }

            // 使用 AssetDatabase.FindReferences（Unity 2020.3+）
            return FindReferencesByAsset(target);
        }

        /// <summary>
        /// 使用 AssetDatabase.FindReferences 查找引用。
        /// </summary>
        private static List<SOReferenceInfo> FindReferencesByAsset(ScriptableObject target)
        {
            // 使用 HashSet 避免重复结果
            var referenceSet = new HashSet<string>();
            var references = new List<SOReferenceInfo>();
            string targetPath = AssetDatabase.GetAssetPath(target);

            // 获取所有可能引用 SO 的资产类型
            var searchTypes = new[]
            {
                "t:Prefab",
                "t:Scene",
                "t:ScriptableObject",
                "t:GameObject"
            };

            foreach (var searchType in searchTypes)
            {
                var guids = AssetDatabase.FindAssets(searchType);

                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);

                    // 跳过目标自身
                    if (path == targetPath)
                        continue;

                    // 跳过已处理过的路径
                    if (!referenceSet.Add(path))
                        continue;

                    // 检查依赖关系
                    var dependencies = AssetDatabase.GetDependencies(path, recursive: false);

                    if (Array.Exists(dependencies, d => d == targetPath))
                    {
                        var assetType = DetermineAssetType(path);
                        references.Add(new SOReferenceInfo(path, assetType));
                    }
                }
            }

            return references.OrderBy(r => r.AssetName).ToList();
        }

        /// <summary>
        /// 使用 GUID 查找引用（更精确但更慢）。
        /// </summary>
        private static List<SOReferenceInfo> FindReferencesByGUID(string targetGuid)
        {
            var references = new List<SOReferenceInfo>();

            // 搜索所有 .meta 文件和资源文件
            var allAssets = AssetDatabase.FindAssets("", new[] { "Assets" });

            foreach (var guid in allAssets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                // 跳过目标自身
                if (guid == targetGuid)
                    continue;

                // 读取文件内容查找 GUID
                if (FileContainsGUID(path, targetGuid))
                {
                    var assetType = DetermineAssetType(path);
                    references.Add(new SOReferenceInfo(path, assetType));
                }
            }

            return references;
        }

        /// <summary>
        /// 检查文件是否包含指定的 GUID。
        /// </summary>
        private static bool FileContainsGUID(string filePath, string guid)
        {
            try
            {
                string content = System.IO.File.ReadAllText(filePath);
                return content.Contains(guid);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 确定资产类型。
        /// </summary>
        private static Type DetermineAssetType(string path)
        {
            if (path.EndsWith(".unity"))
                return typeof(SceneAsset);
            if (path.EndsWith(".prefab"))
                return typeof(GameObject);
            if (path.EndsWith(".asset"))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                return asset?.GetType() ?? typeof(ScriptableObject);
            }
            return typeof(UnityEngine.Object);
        }

        /// <summary>
        /// 显示引用查找结果窗口。
        /// </summary>
        public static void ShowReferenceWindow(ScriptableObject target)
        {
            if (target == null)
                return;

            var references = FindReferences(target);

            if (references.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "No References",
                    $"'{target.name}' is not referenced by any other assets.",
                    "OK"
                );
                return;
            }

            // 创建临时窗口显示结果
            ReferenceViewerWindow.Show(target, references);
        }

        /// <summary>
        /// 统计资产被引用的次数。
        /// </summary>
        public static int GetReferenceCount(ScriptableObject target)
        {
            return FindReferences(target).Count;
        }

        /// <summary>
        /// 检查资产是否为孤立资产（没有任何引用）。
        /// </summary>
        public static bool IsOrphaned(ScriptableObject target)
        {
            // 排除数据库类型的资产（它们可能不需要被引用）
            if (target.GetType().Name.EndsWith("Database") ||
                target.GetType().Name.EndsWith("Manager") ||
                target.GetType().Name.EndsWith("Config"))
            {
                return false;
            }

            return GetReferenceCount(target) == 0;
        }
    }

    /// <summary>
    /// 引用查看器窗口。
    /// </summary>
    internal sealed class ReferenceViewerWindow : EditorWindow
    {
        private ScriptableObject _target;
        private List<SOReferenceInfo> _references;
        private Vector2 _scrollPosition;

        public static void Show(ScriptableObject target, List<SOReferenceInfo> references)
        {
            var window = GetWindow<ReferenceViewerWindow>("References");
            window._target = target;
            window._references = references;
            window.Show();
        }

        void OnGUI()
        {
            if (_target == null || _references == null)
            {
                GUILayout.Label("No data to display.");
                return;
            }

            // 标题
            EditorGUILayout.LabelField($"References to '{_target.name}'", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Found {_references.Count} reference(s)", EditorStyles.miniLabel);
            EditorGUILayout.Space();

            // 引用列表
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var reference in _references)
            {
                DrawReferenceItem(reference);
            }

            EditorGUILayout.EndScrollView();

            // 底部按钮
            EditorGUILayout.Space();
            if (GUILayout.Button("Close"))
            {
                Close();
            }
        }

        void DrawReferenceItem(SOReferenceInfo reference)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // 图标
            GUIContent icon = GetIconForAsset(reference);
            GUILayout.Label(icon, GUILayout.Width(20));

            // 名称和路径
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(reference.AssetName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(reference.AssetPath, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // 操作按钮
            if (GUILayout.Button("Ping", GUILayout.Width(60)))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(reference.AssetPath);
                EditorGUIUtility.PingObject(asset);
            }

            if (GUILayout.Button("Open", GUILayout.Width(60)))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(reference.AssetPath);
                if (asset != null)
                {
                    AssetDatabase.OpenAsset(asset);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        GUIContent GetIconForAsset(SOReferenceInfo reference)
        {
            // 简化的图标显示
            if (reference.IsScene)
                return new GUIContent("📄");
            if (reference.IsPrefab)
                return new GUIContent("🎮");
            return new GUIContent("📋");
        }
    }
}
