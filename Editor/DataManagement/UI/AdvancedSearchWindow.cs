using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// 高级搜索窗口。提供按字段值搜索 ScriptableObject 的功能。
    /// </summary>
    public sealed class AdvancedSearchWindow : EditorWindow
    {
        private QueryGroup _queryGroup = new QueryGroup();
        private List<ScriptableObject> _results = new();
        private Vector2 _resultsScrollPosition;
        private Vector2 _conditionsScrollPosition;
        private Type _selectedType;
        private string _searchSummary = string.Empty;
        private bool _isSearching;

        /// <summary>
        /// 显示高级搜索窗口。
        /// </summary>
        public new static void Show()
        {
            var window = CreateWindow<AdvancedSearchWindow>("Advanced Search");
            window.Initialize();
            // 调用基类的 Show 方法
            EditorWindow showWindow = window;
            showWindow.Show();
        }

        /// <summary>
        /// 初始化窗口。
        /// </summary>
        private void Initialize()
        {
            _queryGroup = new QueryGroup();
            _results = new();
            _selectedType = null;

            // 添加默认条件（名称搜索）
            _queryGroup.AddCondition("name", QueryOperator.Contains, "");
        }

        void OnGUI()
        {
            DrawHeader();
            DrawTypeSelector();
            DrawQueryBuilder();
            DrawResults();
        }

        /// <summary>
        /// 绘制头部。
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("高级搜索", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            // 清除按钮
            if (GUILayout.Button("清除", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                _queryGroup.Clear();
                _results.Clear();
                _searchSummary = string.Empty;
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制类型选择器。
        /// </summary>
        private void DrawTypeSelector()
        {
            var scanResult = SODataManager.Instance.CurrentResult;
            if (scanResult == null)
                return;

            var types = scanResult.GetAllTypes().OrderBy(t => t.Name).ToList();
            types.Insert(0, null); // "All Types"

            string[] typeNames = types.Select(t => t == null ? "所有类型" : t.Name).ToArray();
            int currentIndex = types.IndexOf(_selectedType);
            if (currentIndex < 0)
                currentIndex = 0;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("类型:", GUILayout.Width(40));

            EditorGUI.BeginChangeCheck();
            currentIndex = EditorGUILayout.Popup(currentIndex, typeNames);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedType = types[currentIndex];
            }

            GUILayout.FlexibleSpace();

            // 显示结果数量
            if (_results.Count > 0)
            {
                GUILayout.Label($"找到 {_results.Count} 个结果", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制查询构建器。
        /// </summary>
        private void DrawQueryBuilder()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("查询条件", EditorStyles.boldLabel);

            // 逻辑操作符选择
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("逻辑:", GUILayout.Width(40));
            _queryGroup.LogicalOp = (LogicalOperator)EditorGUILayout.EnumPopup(_queryGroup.LogicalOp);
            GUILayout.FlexibleSpace();

            // 添加条件按钮
            if (GUILayout.Button("+ 添加条件", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                _queryGroup.AddCondition();
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            // 条件列表
            _conditionsScrollPosition = EditorGUILayout.BeginScrollView(_conditionsScrollPosition, GUILayout.Height(150));

            if (_queryGroup.Conditions.Count == 0)
            {
                GUILayout.Label("没有条件，点击 '+ 添加条件' 开始", EditorStyles.miniLabel, GUILayout.Height(40));
            }
            else
            {
                for (int i = 0; i < _queryGroup.Conditions.Count; i++)
                {
                    DrawConditionRow(_queryGroup.Conditions[i], i);
                }
            }

            EditorGUILayout.EndScrollView();

            // 搜索按钮
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(_queryGroup.EnabledCount == 0))
            {
                if (GUILayout.Button("搜索", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    ExecuteSearch();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
        }

        /// <summary>
        /// 绘制单行条件。
        /// </summary>
        private void DrawConditionRow(QueryCondition condition, int index)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // 启用复选框
            condition.IsEnabled = EditorGUILayout.Toggle(condition.IsEnabled, GUILayout.Width(16));

            // 字段名
            GUILayout.Label("字段:", GUILayout.Width(36));
            condition.FieldName = DrawFieldNameDropdown(condition.FieldName);

            // 操作符
            GUILayout.Label("操作:", GUILayout.Width(36));
            condition.Operator = (QueryOperator)EditorGUILayout.EnumPopup(condition.Operator, GUILayout.Width(80));

            // 值输入
            GUILayout.Label("值:", GUILayout.Width(28));
            DrawValueInput(condition);

            GUILayout.FlexibleSpace();

            // 删除按钮
            if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(24)))
            {
                _queryGroup.RemoveCondition(condition);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制字段名下拉框。
        /// </summary>
        private string DrawFieldNameDropdown(string currentFieldName)
        {
            var allFields = SOQueryService.GetAllQueryableFields();

            if (allFields.Count == 0)
            {
                return EditorGUILayout.TextField(currentFieldName ?? "", GUILayout.Width(120));
            }

            // 收集所有字段名
            var allFieldNames = new HashSet<string>();
            foreach (var kvp in allFields)
            {
                foreach (var fieldName in kvp.Value.Keys)
                {
                    allFieldNames.Add(fieldName);
                }
            }

            var fieldNamesList = allFieldNames.ToList();
            fieldNamesList.Sort();

            // 添加默认字段
            if (!fieldNamesList.Contains("name"))
                fieldNamesList.Insert(0, "name");

            int currentIndex = fieldNamesList.IndexOf(currentFieldName ?? "");
            if (currentIndex < 0)
                currentIndex = 0;

            currentIndex = EditorGUILayout.Popup(currentIndex, fieldNamesList.ToArray(), GUILayout.Width(120));
            return fieldNamesList[currentIndex];
        }

        /// <summary>
        /// 绘制值输入控件。
        /// </summary>
        private void DrawValueInput(QueryCondition condition)
        {
            // 根据操作符判断是否需要值输入
            bool needsValue = condition.Operator != QueryOperator.IsNull &&
                              condition.Operator != QueryOperator.IsNotNull;

            if (!needsValue)
            {
                GUILayout.Label("(N/A)", EditorStyles.miniLabel, GUILayout.Width(40));
                condition.Value = null;
                return;
            }

            // 根据当前值类型显示不同的输入
            if (condition.Value is float f || condition.Value is int)
            {
                float floatValue = Convert.ToSingle(condition.Value ?? 0f);
                floatValue = EditorGUILayout.FloatField(floatValue, GUILayout.Width(60));
                condition.Value = floatValue;
            }
            else if (condition.Value is bool b)
            {
                b = EditorGUILayout.Toggle(b, GUILayout.Width(60));
                condition.Value = b;
            }
            else
            {
                string stringValue = condition.Value?.ToString() ?? "";
                stringValue = EditorGUILayout.TextField(stringValue, GUILayout.Width(100));
                condition.Value = stringValue;
            }
        }

        /// <summary>
        /// 执行搜索。
        /// </summary>
        private void ExecuteSearch()
        {
            _results.Clear();

            try
            {
                var allResults = SOQueryService.Query(_queryGroup);

                // 按类型过滤
                if (_selectedType != null)
                {
                    _results = allResults.Where(so => so.GetType() == _selectedType).ToList();
                }
                else
                {
                    _results = allResults;
                }

                // 生成搜索摘要
                GenerateSearchSummary();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdvancedSearch] 搜索失败: {e.Message}");
                _searchSummary = $"搜索失败: {e.Message}";
            }
        }

        /// <summary>
        /// 生成搜索摘要。
        /// </summary>
        private void GenerateSearchSummary()
        {
            var conditions = _queryGroup.Conditions.FindAll(c => c.IsEnabled);
            if (conditions.Count == 0)
            {
                _searchSummary = string.Empty;
                return;
            }

            var summaryParts = new System.Text.StringBuilder();
            summaryParts.Append(_queryGroup.LogicalOp == LogicalOperator.And ? "所有条件满足: " : "任一条件满足: ");

            for (int i = 0; i < conditions.Count; i++)
            {
                if (i > 0)
                    summaryParts.Append(_queryGroup.LogicalOp == LogicalOperator.And ? " 且 " : " 或 ");

                summaryParts.Append($"({conditions[i].GetDisplayText()})");
            }

            _searchSummary = summaryParts.ToString();
        }

        /// <summary>
        /// 绘制结果列表。
        /// </summary>
        private void DrawResults()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("搜索结果", EditorStyles.boldLabel);

            // 搜索摘要
            if (!string.IsNullOrEmpty(_searchSummary))
            {
                GUILayout.Label(_searchSummary, EditorStyles.miniLabel);
                GUILayout.Space(2);
            }

            _resultsScrollPosition = EditorGUILayout.BeginScrollView(_resultsScrollPosition);

            if (_results.Count == 0)
            {
                GUILayout.Label("没有找到匹配的资产", EditorStyles.miniLabel, GUILayout.Height(40));
            }
            else
            {
                foreach (var result in _results)
                {
                    if (result == null)
                        continue;

                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    // 选择按钮
                    if (GUILayout.Button(result.name, EditorStyles.miniButton))
                    {
                        Selection.activeObject = result;
                        EditorGUIUtility.PingObject(result);
                    }

                    GUILayout.FlexibleSpace();

                    // 类型标签
                    GUILayout.Label(result.GetType().Name, EditorStyles.miniLabel);

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }
}
