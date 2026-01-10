using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// 类型选择窗口。用于选择 ScriptableObject 类型进行创建。
    /// </summary>
    internal sealed class TypeSelectionWindow : EditorWindow
    {
        private SOCategoryGroup[] _categories;
        private Action<Type> _onTypeSelected;
        private Vector2 _scrollPosition;
        private string _searchText = string.Empty;
        private Vector2 _categoryScroll;

        /// <summary>
        /// 显示类型选择窗口。
        /// </summary>
        public static void Show(SOCategoryGroup[] categories, Action<Type> onTypeSelected)
        {
            var window = GetWindow<TypeSelectionWindow>("Select SO Type");
            window._categories = categories;
            window._onTypeSelected = onTypeSelected;
            window._searchText = string.Empty;
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck())
            {
                _categoryScroll = Vector2.zero;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", EditorStyles.toolbarButton))
            {
                Close();
                return;
            }

            EditorGUILayout.EndHorizontal();

            if (_categories == null || _categories.Length == 0)
            {
                EditorGUILayout.HelpBox("No types found.", MessageType.Warning);
                return;
            }

            _categoryScroll = EditorGUILayout.BeginScrollView(_categoryScroll);

            foreach (var category in _categories)
            {
                DrawCategory(category);
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawCategory(SOCategoryGroup category)
        {
            // 过滤类型
            var filteredTypes = category.Types
                .Where(t => TypeMatchesSearch(t, _searchText))
                .ToList();

            if (filteredTypes.Count == 0)
                return;

            // 分类标题
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{category.CategoryName} ({filteredTypes.Count})", EditorStyles.boldLabel);

            foreach (var type in filteredTypes)
            {
                DrawTypeItem(type);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        void DrawTypeItem(Type type)
        {
            EditorGUILayout.BeginHorizontal();

            // 类型图标
            GUILayout.Label("📋", GUILayout.Width(20));

            // 类型名称
            EditorGUILayout.LabelField(type.Name, EditorStyles.label);

            // 命名空间
            EditorGUILayout.LabelField($"({type.Namespace ?? "Global"})", EditorStyles.miniLabel, GUILayout.Width(200));

            GUILayout.FlexibleSpace();

            // Select 按钮
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                _onTypeSelected?.Invoke(type);
                // 不自动关闭窗口，允许用户连续创建多个资产
            }

            EditorGUILayout.EndHorizontal();
        }

        bool TypeMatchesSearch(Type type, string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return true;

            return type.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (type.Namespace?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
        }
    }
}
