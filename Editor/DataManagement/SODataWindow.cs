using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// ScriptableObject 数据管理器主窗口。
    /// </summary>
    public sealed class SODataWindow : EditorWindow
    {
        [MenuItem("Tools/SO Data Manager %#M")] // Ctrl+Shift+M
        public static void Open()
        {
            var window = GetWindow<SODataWindow>("SO Data Manager");
            window.Show();
        }

        // ============ 状态 ============

        private Vector2 _scrollPosition;
        private ScriptableObject _selectedObject;
        private string _searchText = string.Empty;

        // 分类过滤
        private string _selectedCategoryFilter = "All";

        // 类型过滤
        private Type _selectedTypeFilter = null;

        // 折叠状态：分类名 -> 是否展开
        private Dictionary<string, bool> _categoryExpandedStates = new Dictionary<string, bool>();

        // 折叠状态：类型名 -> 是否展开
        private Dictionary<string, bool> _typeExpandedStates = new Dictionary<string, bool>();

        // 多选状态
        private HashSet<ScriptableObject> _selectedObjects = new HashSet<ScriptableObject>();
        private bool _isMultiSelectMode;

        // ============ 生命周期 ============

        void OnEnable()
        {
            // 订阅扫描完成事件
            SODataManager.Instance.ScanCompleted += OnScanCompleted;
            SODataManager.Instance.ScanStarted += OnScanStarted;

            // 如果从未扫描过，自动扫描一次
            if (SODataManager.Instance.CurrentResult == null)
            {
                SODataManager.Instance.Scan();
            }
        }

        void OnDisable()
        {
            // 取消订阅
            SODataManager.Instance.ScanCompleted -= OnScanCompleted;
            SODataManager.Instance.ScanStarted -= OnScanStarted;
        }

        void OnFocus()
        {
            Repaint();
        }

        // ============ GUI ============

        void OnGUI()
        {
            DrawHeader();
            DrawToolbar();
            DrawContent();
            DrawFooter();
        }

        // ============ 头部工具栏 ============

        void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Scan 按钮
            if (GUILayout.Button("Scan", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                SODataManager.Instance.Scan();
            }

            // Create 按钮
            if (GUILayout.Button("Create +", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                SOCreationService.ShowCreateDialog();
            }

            // Export Paths 按钮
            if (GUILayout.Button("Export Paths", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                ExportPaths();
            }

            // Find References 按钮
            using (new EditorGUI.DisabledScope(_selectedObject == null))
            {
                if (GUILayout.Button("Find References", EditorStyles.toolbarButton, GUILayout.Width(110)))
                {
                    SOReferenceFinder.ShowReferenceWindow(_selectedObject);
                }
            }

            // Show Dependencies 按钮
            using (new EditorGUI.DisabledScope(_selectedObject == null))
            {
                if (GUILayout.Button("Dependencies", EditorStyles.toolbarButton, GUILayout.Width(95)))
                {
                    DependencyViewerWindow.ShowWindow(_selectedObject);
                }
            }

            // Show Orphans 按钮
            if (GUILayout.Button("Orphans", EditorStyles.toolbarButton, GUILayout.Width(75)))
            {
                DependencyViewerWindow.ShowOrphansWindow();
            }

            // Batch Edit 按钮
            using (new EditorGUI.DisabledScope(_selectedObjects.Count < 2))
            {
                if (GUILayout.Button("Batch Edit", EditorStyles.toolbarButton, GUILayout.Width(85)))
                {
                    BatchEditWindow.Show(_selectedObjects.ToList());
                }
            }

            // 多选模式切换
            bool previousMultiSelectMode = _isMultiSelectMode;
            _isMultiSelectMode = GUILayout.Toggle(_isMultiSelectMode, "多选", EditorStyles.toolbarButton, GUILayout.Width(50));

            // 关闭多选模式时清空多选状态
            if (previousMultiSelectMode && !_isMultiSelectMode)
            {
                _selectedObjects.Clear();
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        // ============ 搜索和过滤工具栏 ============

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 搜索框
            EditorGUI.BeginChangeCheck();
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck())
            {
                _selectedObject = null;
            }

            // 高级搜索按钮
            if (GUILayout.Button("高级搜索", EditorStyles.toolbarButton, GUILayout.Width(75)))
            {
                AdvancedSearchWindow.Show();
            }

            // 分类过滤器
            DrawCategoryFilter();

            // 类型过滤器
            DrawTypeFilter();

            GUILayout.FlexibleSpace();

            // 统计信息
            DrawStatistics();

            EditorGUILayout.EndHorizontal();
        }

        void DrawCategoryFilter()
        {
            var result = SODataManager.Instance.CurrentResult;
            if (result == null)
                return;

            // 从实际分类中获取
            var categories = new List<string> { "All" };
            foreach (var node in result.CategoryTree)
            {
                categories.Add(node.DisplayName);
            }

            int currentIndex = categories.IndexOf(_selectedCategoryFilter);
            if (currentIndex < 0)
                currentIndex = 0;

            EditorGUI.BeginChangeCheck();
            currentIndex = EditorGUILayout.Popup(currentIndex, categories.ToArray(), GUILayout.Width(120));
            if (EditorGUI.EndChangeCheck())
            {
                _selectedCategoryFilter = categories[currentIndex];
                _selectedObject = null;
            }
        }

        void DrawTypeFilter()
        {
            var result = SODataManager.Instance.CurrentResult;
            if (result == null)
                return;

            // 获取所有类型
            var types = result.GetAllTypes().OrderBy(t => t.Name).ToList();
            types.Insert(0, null); // "All" 选项

            string[] typeNames = types.Select(t => t == null ? "All Types" : t.Name).ToArray();

            int currentIndex = types.IndexOf(_selectedTypeFilter);
            if (currentIndex < 0)
                currentIndex = 0;

            EditorGUI.BeginChangeCheck();
            currentIndex = EditorGUILayout.Popup(currentIndex, typeNames, GUILayout.Width(150));
            if (EditorGUI.EndChangeCheck())
            {
                _selectedTypeFilter = types[currentIndex];
                _selectedObject = null;
            }
        }

        void DrawStatistics()
        {
            var result = SODataManager.Instance.CurrentResult;
            if (result == null)
            {
                GUILayout.Label("No data", EditorStyles.miniLabel);
                return;
            }

            GUILayout.Label($"{result.TotalTypeCount} Types | {result.TotalAssetCount} Assets", EditorStyles.miniLabel);
        }

        // ============ 内容区域 ============

        void DrawContent()
        {
            var result = SODataManager.Instance.CurrentResult;
            if (result == null)
            {
                DrawEmptyState();
                return;
            }

            // 垂直滚动
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // 内容区域
            DrawFlatListView(result);

            EditorGUILayout.EndScrollView();
        }

        void DrawEmptyState()
        {
            EditorGUILayout.HelpBox(
                SODataManager.Instance.IsScanning
                    ? "Scanning...\nPlease wait."
                    : "No data found.\nClick 'Scan' to search for ScriptableObjects.",
                SODataManager.Instance.IsScanning ? MessageType.Info : MessageType.Warning
            );
        }

        // ============ 平铺列表视图 ============

        void DrawFlatListView(SOScanResult result)
        {
            foreach (var categoryNode in result.CategoryTree)
            {
                if (!ShouldShowCategory(categoryNode))
                    continue;

                DrawFlatCategory(categoryNode);
            }
        }

        void DrawFlatCategory(SOTypeNode categoryNode)
        {
            // 收集该分类下所有需要显示的类型和资产
            var displayAssets = new List<(Type type, string typeName, ScriptableObject asset)>();
            CollectAssetsInCategory(categoryNode, displayAssets);

            if (displayAssets.Count == 0)
                return;

            // 分类标题 - 使用更明显的样式
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawCategoryHeader(categoryNode.DisplayName, displayAssets.Count);

                // 检查分类是否展开
                if (!_categoryExpandedStates.ContainsKey(categoryNode.DisplayName))
                    _categoryExpandedStates[categoryNode.DisplayName] = true;

                if (_categoryExpandedStates[categoryNode.DisplayName])
                {
                    // 按类型分组显示
                    var groupedByType = displayAssets.GroupBy(a => a.type).OrderBy(g => g.Key.Name);

                    foreach (var group in groupedByType)
                    {
                        DrawFlatTypeGroup(group.Key, group.ToList());
                    }
                }
            }
        }

        void DrawCategoryHeader(string categoryName, int count)
        {
            // 获取或初始化折叠状态
            if (!_categoryExpandedStates.ContainsKey(categoryName))
                _categoryExpandedStates[categoryName] = true;

            EditorGUILayout.BeginHorizontal();
            // 折叠箭头（不包含文本，单独绘制）
            _categoryExpandedStates[categoryName] = EditorGUILayout.Foldout(
                _categoryExpandedStates[categoryName],
                "",
                true,
                EditorStyles.foldout
            );
            // 单独绘制分类名称（适中字体）
            GUILayout.Label(categoryName, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{count} assets", EditorStyles.label);
            EditorGUILayout.EndHorizontal();
        }

        void DrawFlatTypeGroup(Type type, List<(Type type, string typeName, ScriptableObject asset)> assets)
        {
            string typeKey = type.FullName ?? type.Name;

            // 获取或初始化折叠状态
            if (!_typeExpandedStates.ContainsKey(typeKey))
                _typeExpandedStates[typeKey] = assets.Count <= 6; // 超过6个默认折叠

            // 类型子标题带折叠
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            // 折叠箭头（不包含文本）
            _typeExpandedStates[typeKey] = EditorGUILayout.Foldout(
                _typeExpandedStates[typeKey],
                "",
                true,
                EditorStyles.foldout
            );
            // 单独绘制类型名称（适中字体）
            GUILayout.Label($"{type.Name} ({assets.Count})", EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            // 检查类型是否展开
            if (!_typeExpandedStates[typeKey])
                return;

            // 自适应网格布局
            DrawAdaptiveAssetGrid(assets);
        }

        /// <summary>
        /// 自适应网格布局：根据窗口宽度自动计算列数和卡片宽度。
        /// </summary>
        void DrawAdaptiveAssetGrid(List<(Type type, string typeName, ScriptableObject asset)> assets)
        {
            // 最小卡片宽度
            const float minItemWidth = 140f;
            const float spacing = 4f;

            // 使用 position.width 而不是 currentViewWidth
            float availableWidth = position.width - 50f;

            // 计算可以放多少列（最多4列）
            int columns = Mathf.Clamp(
                Mathf.FloorToInt(availableWidth / (minItemWidth + spacing)),
                1, 4
            );

            // 计算实际卡片宽度
            float itemWidth = Mathf.Floor((availableWidth - spacing * (columns - 1)) / columns);
            itemWidth = Mathf.Clamp(itemWidth, 120f, 200f);

            // 绘制网格
            int index = 0;
            while (index < assets.Count)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < columns && index < assets.Count; col++)
                {
                    DrawCompactAssetItemGUILayout(assets[index].asset, itemWidth);
                    index++;

                    if (col < columns - 1 && index < assets.Count)
                        GUILayout.Space(spacing);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 使用 GUILayout 模式绘制资产项。
        /// </summary>
        void DrawCompactAssetItemGUILayout(ScriptableObject so, float width)
        {
            bool isSelected = _selectedObject == so;
            bool isMultiSelected = _selectedObjects.Contains(so);

            if (isSelected || isMultiSelected)
            {
                GUI.backgroundColor = new Color(0.5f, 0.8f, 1f, 0.3f);
            }

            using (new GUILayout.VerticalScope(GUILayout.Width(width)))
            {
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.Space(2);

                    GUILayout.BeginHorizontal();

                    // 多选模式下的复选框
                    if (_isMultiSelectMode)
                    {
                        bool selected = _selectedObjects.Contains(so);
                        bool newSelected = GUILayout.Toggle(selected, "", GUILayout.Width(16));
                        if (newSelected != selected)
                        {
                            if (newSelected)
                                _selectedObjects.Add(so);
                            else
                                _selectedObjects.Remove(so);
                        }
                        GUILayout.Space(2);
                    }
                    else
                    {
                        GUILayout.Label("📋", GUILayout.Width(18));
                    }

                    GUIStyle nameStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                        fontSize = 12
                    };

                    if (GUILayout.Button(so.name, nameStyle, GUILayout.Height(22)))
                    {
                        if (_isMultiSelectMode)
                        {
                            if (_selectedObjects.Contains(so))
                                _selectedObjects.Remove(so);
                            else
                                _selectedObjects.Add(so);
                        }
                        else
                        {
                            SelectAndPingAsset(so);
                        }
                    }

                    if (GUILayout.Button("✏️", EditorStyles.miniButton, GUILayout.Width(24), GUILayout.Height(22)))
                    {
                        SOQuickEditWindow.Show(so);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label(so.GetType().Name, EditorStyles.miniLabel);
                    GUILayout.Space(2);
                }
            }

            GUI.backgroundColor = Color.white;

            // 右键菜单
            Rect lastRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.ContextClick && lastRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                ShowContextMenu(so);
            }
        }

        // ============ 底部信息栏 ============

        void DrawFooter()
        {
            // 多选模式下的底部信息
            if (_isMultiSelectMode && _selectedObjects.Count > 0)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

                GUILayout.Label($"✓ 已选中 {_selectedObjects.Count} 个资产", EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                // 批量编辑按钮
                if (_selectedObjects.Count >= 2)
                {
                    if (GUILayout.Button("批量编辑", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    {
                        BatchEditWindow.Show(_selectedObjects.ToList());
                    }
                }

                // 清除选择按钮
                if (GUILayout.Button("清除选择", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    _selectedObjects.Clear();
                }

                EditorGUILayout.EndHorizontal();
                return;
            }

            // 单选模式下的底部信息
            if (_selectedObject != null)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

                // 选中资产信息
                GUILayout.Label($"📋 {_selectedObject.name}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"({_selectedObject.GetType().Name})", EditorStyles.miniLabel, GUILayout.Width(150));

                // 资产路径
                string path = AssetDatabase.GetAssetPath(_selectedObject);
                if (!string.IsNullOrEmpty(path))
                {
                    EditorGUILayout.LabelField(path, EditorStyles.miniLabel, GUILayout.Width(300));
                }

                GUILayout.FlexibleSpace();

                // 操作按钮
                if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    EditorGUIUtility.PingObject(_selectedObject);
                }

                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    _selectedObject = null;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        // ============ 右键菜单 ============

        void ShowContextMenu(ScriptableObject so)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Ping in Project"), false, () => EditorGUIUtility.PingObject(so));
            menu.AddItem(new GUIContent("Select in Inspector"), false, () => Selection.activeObject = so);
            menu.AddItem(new GUIContent("Copy Path"), false, () => CopyAssetPath(so));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Find References"), false, () => SOReferenceFinder.ShowReferenceWindow(so));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete"), false, () => DeleteAsset(so));
            menu.ShowAsContext();
        }

        // ============ 辅助方法 ============

        /// <summary>
        /// 选中资产并在 Inspector 中打开。
        /// </summary>
        void SelectAndPingAsset(ScriptableObject so)
        {
            _selectedObject = so;
            Selection.activeObject = so;  // 选中资产，Inspector 会显示
            EditorGUIUtility.PingObject(so); // 在 Project 窗口中高亮
        }

        void CopyAssetPath(ScriptableObject so)
        {
            string path = AssetDatabase.GetAssetPath(so);
            GUIUtility.systemCopyBuffer = path;
            Debug.Log($"[SOManager] Copied: {path}");
        }

        void DeleteAsset(ScriptableObject so)
        {
            string path = AssetDatabase.GetAssetPath(so);
            if (!string.IsNullOrEmpty(path))
            {
                if (EditorUtility.DisplayDialog("Delete Asset", $"Delete '{so.name}'?", "Delete", "Cancel"))
                {
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.SaveAssets();
                    SODataManager.Instance.Scan();

                    if (_selectedObject == so)
                        _selectedObject = null;
                }
            }
        }

        void ExportPaths()
        {
            var paths = new List<string>();
            var result = SODataManager.Instance.CurrentResult;

            if (result != null)
            {
                foreach (var kvp in result.AssetsByType)
                {
                    foreach (var asset in kvp.Value)
                    {
                        string path = AssetDatabase.GetAssetPath(asset);
                        if (!string.IsNullOrEmpty(path))
                            paths.Add(path);
                    }
                }
            }

            if (paths.Count == 0)
            {
                EditorUtility.DisplayDialog("Export", "No paths to export.", "OK");
                return;
            }

            string content = string.Join("\n", paths);
            string fileName = EditorUtility.SaveFilePanel("Export Paths", "", "SO_Paths", "txt");
            if (!string.IsNullOrEmpty(fileName))
            {
                System.IO.File.WriteAllText(fileName, content);
                Debug.Log($"[SOManager] Exported {paths.Count} paths to: {fileName}");
            }
        }

        void CollectAssetsInCategory(SOTypeNode node, List<(Type type, string typeName, ScriptableObject asset)> result)
        {
            if (node.IsFolder)
            {
                foreach (var child in node.Children)
                {
                    CollectAssetsInCategory(child, result);
                }
            }
            else if (node.Type != null)
            {
                foreach (var asset in node.Assets)
                {
                    if (AssetMatchesFilter(asset))
                    {
                        result.Add((node.Type, node.Type.Name, asset));
                    }
                }
            }
        }

        bool ShouldShowCategory(SOTypeNode node)
        {
            if (_selectedCategoryFilter != "All" && node.DisplayName != _selectedCategoryFilter)
                return false;

            return ShouldShowNode(node);
        }

        bool ShouldShowNode(SOTypeNode node)
        {
            // 检查是否有匹配搜索的资产
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                if (!NodeMatchesSearch(node, _searchText))
                    return false;
            }

            // 检查类型过滤
            if (_selectedTypeFilter != null)
            {
                if (node.Type != _selectedTypeFilter && !ChildrenContainType(node, _selectedTypeFilter))
                    return false;
            }

            return true;
        }

        bool NodeMatchesSearch(SOTypeNode node, string search)
        {
            // 检查节点名称
            if (node.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            // 检查资产
            foreach (var asset in node.Assets)
            {
                if (asset.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    asset.GetType().Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            // 检查子节点
            foreach (var child in node.Children)
            {
                if (NodeMatchesSearch(child, search))
                    return true;
            }

            return false;
        }

        bool ChildrenContainType(SOTypeNode node, Type type)
        {
            if (node.Type == type)
                return true;

            foreach (var child in node.Children)
            {
                if (ChildrenContainType(child, type))
                    return true;
            }

            return false;
        }

        bool AssetMatchesFilter(ScriptableObject so)
        {
            // 搜索过滤
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                if (so.name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) < 0 &&
                    so.GetType().Name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            // 类型过滤
            if (_selectedTypeFilter != null && so.GetType() != _selectedTypeFilter)
                return false;

            return true;
        }

        // ============ 事件回调 ============

        void OnScanStarted()
        {
            Repaint();
        }

        void OnScanCompleted(SOScanResult result)
        {
            Repaint();
            Debug.Log($"[SOManager] Scan complete: {result.TotalTypeCount} types, {result.TotalAssetCount} assets");
        }
    }
}
