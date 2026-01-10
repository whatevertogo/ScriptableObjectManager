using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// 依赖关系可视化窗口。显示 ScriptableObject 之间的引用关系。
    /// </summary>
    public sealed class DependencyViewerWindow : EditorWindow
    {
        private ScriptableObject _targetAsset;
        private DependencyGraph.Node _targetNode;
        private Vector2 _scrollPosition;
        private Vector2 _dependenciesScroll;
        private Vector2 _dependentsScroll;

        // 显示选项
        private bool _showDependencies = true;
        private bool _showDependents = true;
        private bool _showStats = true;

        // 孤立资产视图
        private bool _showOrphansView;
        private List<ScriptableObject> _orphanAssets = new();

        // 热门资产视图
        private bool _showTopReferencedView;
        private List<DependencyGraph.Node> _topReferencedNodes = new();
        private int _topN = 20;

        // 搜索过滤
        private string _searchFilter = string.Empty;

        /// <summary>
        /// 显示依赖关系窗口。
        /// </summary>
        public static void ShowWindow(ScriptableObject asset)
        {
            var window = GetWindow<DependencyViewerWindow>("Dependency Viewer");
            window.SetTarget(asset);
            window.Show();
        }

        /// <summary>
        /// 显示孤立资产窗口。
        /// </summary>
        public static void ShowOrphansWindow()
        {
            var window = GetWindow<DependencyViewerWindow>("Orphan Assets");
            window._showOrphansView = true;
            window.RefreshOrphans();
            window.Show();
        }

        void OnEnable()
        {
            // 刷新依赖图缓存
            DependencyAnalysisService.BuildGraph(useCache: false);
        }

        void SetTarget(ScriptableObject asset)
        {
            _targetAsset = asset;
            _showOrphansView = false;
            _showTopReferencedView = false;
            RefreshTargetNode();
        }

        void RefreshTargetNode()
        {
            if (_targetAsset != null)
            {
                var graph = DependencyAnalysisService.GetCachedGraph();
                _targetNode = graph?.GetNode(_targetAsset);
            }
        }

        void RefreshOrphans()
        {
            _orphanAssets = DependencyAnalysisService.FindOrphans() ?? new List<ScriptableObject>();
        }

        void RefreshTopReferenced()
        {
            _topReferencedNodes = DependencyAnalysisService.FindMostReferenced(_topN)?.ToList()
                ?? new List<DependencyGraph.Node>();
        }

        void OnGUI()
        {
            DrawToolbar();

            if (_showOrphansView)
            {
                DrawOrphansView();
            }
            else if (_showTopReferencedView)
            {
                DrawTopReferencedView();
            }
            else
            {
                DrawDependencyView();
            }
        }

        #region 工具栏

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 视图切换按钮
            if (_showOrphansView)
            {
                if (GUILayout.Button("← 返回", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    _showOrphansView = false;
                }
                GUILayout.Label("孤立资产视图", EditorStyles.boldLabel);
            }
            else if (_showTopReferencedView)
            {
                if (GUILayout.Button("← 返回", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    _showTopReferencedView = false;
                }
                GUILayout.Label("热门资产视图", EditorStyles.boldLabel);
            }
            else
            {
                // 目标选择器
                var newTarget = EditorGUILayout.ObjectField(
                    _targetAsset,
                    typeof(ScriptableObject),
                    false,
                    GUILayout.Width(200)
                ) as ScriptableObject;

                if (newTarget != _targetAsset)
                {
                    _targetAsset = newTarget;
                    RefreshTargetNode();
                }

                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    DependencyAnalysisService.InvalidateCache();
                    RefreshTargetNode();
                }

                GUILayout.Space(10);

                // 视图按钮
                if (GUILayout.Button("孤立资产", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    _showOrphansView = true;
                    RefreshOrphans();
                }

                if (GUILayout.Button("热门资产", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    _showTopReferencedView = true;
                    RefreshTopReferenced();
                }
            }

            GUILayout.FlexibleSpace();

            // 搜索框
            if (_showOrphansView || _showTopReferencedView)
            {
                GUILayout.Label("过滤:", EditorStyles.miniLabel);
                _searchFilter = EditorGUILayout.TextField(
                    _searchFilter,
                    EditorStyles.toolbarSearchField,
                    GUILayout.Width(150)
                );
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 依赖关系视图

        void DrawDependencyView()
        {
            if (_targetAsset == null)
            {
                DrawEmptyState("请选择一个 ScriptableObject 查看依赖关系");
                return;
            }

            // 如果节点不存在（不在依赖图中），显示基本信息但不显示依赖关系
            if (_targetNode == null)
            {
                DrawTargetAssetCard();
                EditorGUILayout.HelpBox(
                    "该资产未在依赖图中找到，可能是因为：\n" +
                    "1. 资产没有被扫描\n" +
                    "2. 资产路径无效\n" +
                    "请点击「刷新」按钮重新构建依赖图",
                    MessageType.Warning
                );
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // 目标资产卡片
            DrawTargetAssetCard();

            GUILayout.Space(10);

            // 统计信息
            if (_showStats)
            {
                DrawStatsSection();
            }

            GUILayout.Space(10);

            // 依赖关系和被依赖关系并排显示
            EditorGUILayout.BeginHorizontal();

            // 左侧：该资产依赖的其他资产（Dependencies）
            if (_showDependencies)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 10));
                DrawDependenciesPanel();
                EditorGUILayout.EndVertical();
            }

            // 右侧：依赖该资产的其他资产（Dependents）
            if (_showDependents)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 10));
                DrawDependentsPanel();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        void DrawTargetAssetCard()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.BeginHorizontal();

                // 图标
                GUILayout.Label("📦", GUILayout.Width(30));

                // 名称和类型
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(_targetAsset.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(_targetAsset.GetType().Name, EditorStyles.miniLabel);

                // 路径
                string path = AssetDatabase.GetAssetPath(_targetAsset);
                if (!string.IsNullOrEmpty(path))
                {
                    EditorGUILayout.LabelField(path, EditorStyles.miniLabel, GUILayout.Height(30));
                }

                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // 操作按钮
                if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    EditorGUIUtility.PingObject(_targetAsset);
                }

                if (GUILayout.Button("选择", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    Selection.activeObject = _targetAsset;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawStatsSection()
        {
            var stats = DependencyAnalysisService.GetStats(_targetAsset);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("统计信息", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("依赖数量:", EditorStyles.miniLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField(stats?.DependencyCount.ToString() ?? "0", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("被引用次数:", EditorStyles.miniLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField(stats?.ReferenceCount.ToString() ?? "0", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("是否孤立:", EditorStyles.miniLabel, GUILayout.Width(80));
                bool isOrphan = stats?.ReferenceCount == 0;
                EditorGUILayout.LabelField(isOrphan ? "是" : "否", EditorStyles.miniLabel);
                if (isOrphan)
                {
                    GUILayout.Label("⚠️", GUILayout.Width(20));
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawDependenciesPanel()
        {
            var dependencies = _targetNode?.Dependencies.ToList() ?? new List<DependencyGraph.Node>();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // 标题栏
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("📤 依赖 (Dependencies)", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(dependencies.Count.ToString(), EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                if (dependencies.Count == 0)
                {
                    EditorGUILayout.HelpBox("该资产不依赖任何其他资产", MessageType.Info);
                    return;
                }

                // 依赖列表
                _dependenciesScroll = EditorGUILayout.BeginScrollView(_dependenciesScroll);
                DrawNodeList(dependencies);
                EditorGUILayout.EndScrollView();
            }
        }

        void DrawDependentsPanel()
        {
            var dependents = _targetNode?.Dependents.ToList() ?? new List<DependencyGraph.Node>();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // 标题栏
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("📥 被引用 (Referenced By)", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(dependents.Count.ToString(), EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                if (dependents.Count == 0)
                {
                    EditorGUILayout.HelpBox("该资产没有被任何其他资产引用\n(孤立资产)", MessageType.Warning);
                    return;
                }

                // 引用者列表
                _dependentsScroll = EditorGUILayout.BeginScrollView(_dependentsScroll);
                DrawNodeList(dependents);
                EditorGUILayout.EndScrollView();
            }
        }

        void DrawNodeList(List<DependencyGraph.Node> nodes)
        {
            foreach (var node in nodes)
            {
                if (node?.Asset == null)
                    continue;

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    // 类型图标
                    GUILayout.Label("📄", GUILayout.Width(20));

                    // 名称和类型
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(node.Asset.name, EditorStyles.label);
                    EditorGUILayout.LabelField(node.Asset.GetType().Name, EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    // 引用计数
                    GUILayout.Label($"🔗 {node.ReferenceCount}", EditorStyles.miniLabel, GUILayout.Width(50));

                    // Ping 按钮
                    if (GUILayout.Button("", EditorStyles.miniButton, GUILayout.Width(24), GUILayout.Height(24)))
                    {
                        EditorGUIUtility.PingObject(node.Asset);
                    }
                }

                // 点击整个行切换目标
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseUp && lastRect.Contains(Event.current.mousePosition))
                {
                    // 检查是否点击在按钮上
                    var buttonRect = new Rect(lastRect.xMax - 30, lastRect.y, 30, lastRect.height);
                    if (!buttonRect.Contains(Event.current.mousePosition))
                    {
                        SetTarget(node.Asset);
                        Event.current.Use();
                    }
                }
            }
        }

        #endregion

        #region 孤立资产视图

        void DrawOrphansView()
        {
            var filtered = FilterAssets(_orphanAssets);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"孤立资产 ({filtered.Count} 个)", EditorStyles.boldLabel);

                if (filtered.Count == 0)
                {
                    EditorGUILayout.HelpBox("没有发现孤立资产", MessageType.Info);
                    return;
                }

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                DrawAssetList(filtered);
                EditorGUILayout.EndScrollView();
            }
        }

        void DrawAssetList(List<ScriptableObject> assets)
        {
            foreach (var asset in assets)
            {
                if (asset == null)
                    continue;

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    // 类型图标
                    GUILayout.Label("📄", GUILayout.Width(20));

                    // 名称和类型
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(asset.name, EditorStyles.label);
                    EditorGUILayout.LabelField(asset.GetType().Name, EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    // Ping 按钮
                    if (GUILayout.Button("查看", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        SetTarget(asset);
                    }
                }

                // 点击整个行切换目标
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseUp && lastRect.Contains(Event.current.mousePosition))
                {
                    var buttonRect = new Rect(lastRect.xMax - 60, lastRect.y, 60, lastRect.height);
                    if (!buttonRect.Contains(Event.current.mousePosition))
                    {
                        SetTarget(asset);
                        Event.current.Use();
                    }
                }
            }
        }

        #endregion

        #region 热门资产视图

        void DrawTopReferencedView()
        {
            var filtered = FilterNodes(_topReferencedNodes);

            // Top N 选择
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("显示前", EditorStyles.miniLabel);
            _topN = EditorGUILayout.IntField(_topN, GUILayout.Width(40));
            GUILayout.Label("个", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshTopReferenced();
            }

            EditorGUILayout.EndHorizontal();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"热门资产 (Top {filtered.Count})", EditorStyles.boldLabel);

                if (filtered.Count == 0)
                {
                    EditorGUILayout.HelpBox("没有数据", MessageType.Info);
                    return;
                }

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                for (int i = 0; i < filtered.Count; i++)
                {
                    var node = filtered[i];
                    if (node?.Asset == null)
                        continue;

                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        // 排名
                        GUILayout.Label($"#{i + 1}", EditorStyles.boldLabel, GUILayout.Width(40));

                        // 名称和类型
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField(node.Asset.name, EditorStyles.label);
                        EditorGUILayout.LabelField(node.Asset.GetType().Name, EditorStyles.miniLabel);
                        EditorGUILayout.EndVertical();

                        GUILayout.FlexibleSpace();

                        // 引用计数
                        GUILayout.Label($"🔗 {node.ReferenceCount}", EditorStyles.boldLabel, GUILayout.Width(60));

                        // Ping 按钮
                        if (GUILayout.Button("查看", EditorStyles.miniButton, GUILayout.Width(50)))
                        {
                            SetTarget(node.Asset);
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        #endregion

        #region 辅助方法

        List<ScriptableObject> FilterAssets(List<ScriptableObject> assets)
        {
            if (string.IsNullOrWhiteSpace(_searchFilter))
                return assets;

            return assets
                .Where(a => a != null &&
                    (a.name?.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                     a.GetType().Name?.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
        }

        List<DependencyGraph.Node> FilterNodes(List<DependencyGraph.Node> nodes)
        {
            if (string.IsNullOrWhiteSpace(_searchFilter))
                return nodes;

            return nodes
                .Where(n => n?.Asset != null &&
                    (n.Asset.name?.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                     n.Asset.GetType().Name?.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
        }

        void DrawEmptyState(string message)
        {
            EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        #endregion
    }
}
