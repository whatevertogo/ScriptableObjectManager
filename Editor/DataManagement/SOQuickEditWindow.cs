using System;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// ScriptableObject 快速编辑小窗口。
    /// 使用 Editor.CreateCachedEditor 提供完整的 Inspector 编辑体验。
    /// </summary>
    public sealed class SOQuickEditWindow : EditorWindow
    {
        private ScriptableObject _target;
        private Editor _cachedEditor;
        private Vector2 _scrollPosition;
        private string _assetPath;

        /// <summary>
        /// 显示快速编辑窗口。
        /// </summary>
        public static void Show(ScriptableObject target)
        {
            if (target == null)
                return;

            var window = CreateInstance<SOQuickEditWindow>();
            window._target = target;
            window._assetPath = AssetDatabase.GetAssetPath(target);

            // 设置窗口大小和位置
            window.titleContent = new GUIContent($"✏️ {target.name}");
            window.minSize = new Vector2(350, 300);
            window.ShowAuxWindow(); // 显示为辅助窗口（不抢占焦点）
        }

        void OnGUI()
        {
            if (_target == null)
            {
                EditorGUILayout.HelpBox("Target asset has been deleted.", MessageType.Warning);
                return;
            }

            // 头部工具栏
            DrawHeader();

            // 内容滚动区域
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawInspector();
            EditorGUILayout.EndScrollView();

            // 底部工具栏
            DrawFooter();
        }

        void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"📋 {_target.name}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // 保存按钮
            using (new EditorGUI.DisabledScope(!HasModifiedProperties()))
            {
                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    SaveChangesInternal();
                }
            }

            EditorGUILayout.EndHorizontal();

            // 资产信息（可选择路径）
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Type: {_target.GetType().Name}", EditorStyles.miniLabel);
            EditorGUILayout.SelectableLabel(_assetPath, EditorStyles.miniLabel, GUILayout.Height(18));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);
        }

        void DrawInspector()
        {
            // 创建或更新缓存的 Editor
            if (_cachedEditor == null || _cachedEditor.target != _target)
            {
                Editor.CreateCachedEditor(_target, null, ref _cachedEditor);
            }

            // 使用原生 Inspector 绘制
            _cachedEditor.OnInspectorGUI();
        }

        void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Ping 按钮
            if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                EditorGUIUtility.PingObject(_target);
            }

            // 在 Inspector 中打开
            if (GUILayout.Button("Inspector", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                Selection.activeObject = _target;
            }

            GUILayout.FlexibleSpace();

            // 显示修改状态
            if (HasModifiedProperties())
            {
                GUILayout.Label("• Unsaved changes", EditorStyles.miniLabel);
            }

            // 关闭按钮
            if (GUILayout.Button("Close", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }

        bool HasModifiedProperties()
        {
            return _cachedEditor != null && _cachedEditor.serializedObject.hasModifiedProperties;
        }

        void SaveChangesInternal()
        {
            if (_cachedEditor != null)
            {
                _cachedEditor.serializedObject.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log($"[SOQuickEdit] Saved: {_assetPath}");
            }
        }

        void OnDisable()
        {
            // 窗口关闭时，如果有修改则提示保存
            if (HasModifiedProperties())
            {
                bool save = EditorUtility.DisplayDialog(
                    "Unsaved Changes",
                    $"Do you want to save changes to '{_target.name}'?",
                    "Save",
                    "Discard"
                );

                if (save)
                {
                    SaveChangesInternal();
                }
            }
        }

        void OnDestroy()
        {
            if (_cachedEditor != null)
            {
                UnityEngine.Object.DestroyImmediate(_cachedEditor);
            }
        }
    }
}
