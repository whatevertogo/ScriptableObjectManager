using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// 依赖分析服务。构建和分析 ScriptableObject 的依赖关系图。
    /// </summary>
    public static class DependencyAnalysisService
    {
        private static DependencyGraph _cachedGraph;
        private static DateTime _lastBuildTime;
        private const double CacheValiditySeconds = 30; // 缓存有效期（秒）

        /// <summary>
        /// 静态构造函数：订阅扫描完成事件，自动失效缓存。
        /// </summary>
        static DependencyAnalysisService()
        {
            // 延迟订阅，避免 SODataManager 静态初始化顺序问题
            // 实际订阅会在首次访问时发生
        }

        /// <summary>
        /// 初始化事件订阅。在首次调用时执行。
        /// </summary>
        private static bool _initialized = false;
        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                _initialized = true;
                if (SODataManager.Instance != null)
                {
                    SODataManager.Instance.ScanCompleted += OnScanCompleted;
                }
            }
        }

        /// <summary>
        /// 扫描完成时自动失效依赖图缓存。
        /// </summary>
        private static void OnScanCompleted(SOScanResult result)
        {
            InvalidateCache();
            Debug.Log("[DependencyAnalysis] 扫描完成，依赖图缓存已失效");
        }

        /// <summary>
        /// 构建依赖图。
        /// </summary>
        public static DependencyGraph BuildGraph(bool useCache = true)
        {
            EnsureInitialized();

            // 检查缓存是否有效
            if (useCache && _cachedGraph != null)
            {
                var elapsed = (DateTime.Now - _lastBuildTime).TotalSeconds;
                if (elapsed < CacheValiditySeconds)
                {
                    return _cachedGraph;
                }
            }

            var graph = new DependencyGraph();
            var scanResult = SODataManager.Instance.CurrentResult;

            if (scanResult == null)
            {
                Debug.LogWarning("[DependencyAnalysis] 没有扫描结果，请先执行 Scan");
                return graph;
            }

            // 收集所有资产
            var allAssets = scanResult.AssetsByType.Values.SelectMany(x => x).ToList();

            // 添加所有节点
            foreach (var asset in allAssets)
            {
                graph.AddNode(asset);
            }

            // 构建依赖关系
            foreach (var asset in allAssets)
            {
                if (asset == null)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                // 获取该资产引用的所有资产
                var dependencies = GetDependencies(assetPath);

                foreach (var dep in dependencies)
                {
                    if (dep != null && dep != asset) // 排除自引用
                    {
                        graph.AddDependency(asset, dep);
                    }
                }
            }

            // 更新缓存
            _cachedGraph = graph;
            _lastBuildTime = DateTime.Now;

            Debug.Log($"[DependencyAnalysis] 构建依赖图完成: {graph.NodeCount} 个节点");
            return graph;
        }

        /// <summary>
        /// 获取缓存的依赖图。
        /// </summary>
        public static DependencyGraph GetCachedGraph()
        {
            if (_cachedGraph != null)
            {
                var elapsed = (DateTime.Now - _lastBuildTime).TotalSeconds;
                if (elapsed < CacheValiditySeconds)
                    return _cachedGraph;
            }

            return BuildGraph();
        }

        /// <summary>
        /// 使缓存失效。
        /// </summary>
        public static void InvalidateCache()
        {
            EnsureInitialized();
            _cachedGraph = null;
        }

        /// <summary>
        /// 查找孤立资产（排除特定类型）。
        /// </summary>
        public static List<ScriptableObject> FindOrphans(HashSet<Type> excludedTypes = null)
        {
            var graph = GetCachedGraph();
            var orphanNodes = graph.GetOrphanNodes();

            var orphans = new List<ScriptableObject>();
            foreach (var node in orphanNodes)
            {
                if (node.Asset != null)
                {
                    // 检查是否需要排除该类型
                    if (excludedTypes == null || !excludedTypes.Contains(node.Asset.GetType()))
                    {
                        orphans.Add(node.Asset);
                    }
                }
            }

            return orphans;
        }

        /// <summary>
        /// 查找引用最多的资产。
        /// </summary>
        public static List<DependencyGraph.Node> FindMostReferenced(int topN = 10)
        {
            var graph = GetCachedGraph();
            return graph.GetMostReferenced(topN).ToList();
        }

        /// <summary>
        /// 获取资产的所有引用者。
        /// </summary>
        public static List<ScriptableObject> GetReferencers(ScriptableObject asset)
        {
            var graph = GetCachedGraph();
            var referencerNodes = graph.GetReferencers(asset);

            return referencerNodes.Select(n => n.Asset).Where(a => a != null).ToList();
        }

        /// <summary>
        /// 获取资产的所有依赖项。
        /// </summary>
        public static List<ScriptableObject> GetDependencies(ScriptableObject asset)
        {
            var graph = GetCachedGraph();
            var dependencyNodes = graph.GetDependencies(asset);

            return dependencyNodes.Select(n => n.Asset).Where(a => a != null).ToList();
        }

        /// <summary>
        /// 查找两个资产之间的最短路径。
        /// </summary>
        public static List<ScriptableObject> FindShortestPath(ScriptableObject from, ScriptableObject to)
        {
            var graph = GetCachedGraph();
            var pathNodes = graph.FindShortestPath(from, to);

            return pathNodes?.Select(n => n.Asset).Where(a => a != null).ToList();
        }

        /// <summary>
        /// 获取资产的依赖统计。
        /// </summary>
        public static DependencyStats GetStats(ScriptableObject asset)
        {
            var graph = GetCachedGraph();
            var node = graph.GetNode(asset);

            if (node == null)
            {
                return new DependencyStats
                {
                    Asset = asset,
                    ReferenceCount = 0,
                    DependencyCount = 0,
                    IsOrphan = true
                };
            }

            return new DependencyStats
            {
                Asset = asset,
                ReferenceCount = node.ReferenceCount,
                DependencyCount = node.DependencyCount,
                IsOrphan = node.IsOrphan
            };
        }

        // ============ 辅助方法 ============

        /// <summary>
        /// 获取指定资产路径的所有依赖项。
        /// </summary>
        private static HashSet<ScriptableObject> GetDependencies(string assetPath)
        {
            var dependencies = new HashSet<ScriptableObject>();

            // 方法 1: 使用 AssetDatabase.GetDependencies（Unity 2020.3+）
            try
            {
                string[] allDependencies = AssetDatabase.GetDependencies(assetPath, false);
                foreach (var depPath in allDependencies)
                {
                    // 跳过非 SO 资产
                    if (depPath.EndsWith(".asset"))
                    {
                        var dep = AssetDatabase.LoadAssetAtPath<ScriptableObject>(depPath);
                        if (dep is ScriptableObject so)
                        {
                            dependencies.Add(so);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DependencyAnalysis] GetDependencies 失败: {e.Message}");
            }

            return dependencies;
        }
    }

    /// <summary>
    /// 依赖统计信息。
    /// </summary>
    public sealed class DependencyStats
    {
        public ScriptableObject Asset { get; set; }
        public int ReferenceCount { get; set; }
        public int DependencyCount { get; set; }
        public bool IsOrphan { get; set; }

        /// <summary>
        /// 资产名称。
        /// </summary>
        public string AssetName => Asset?.name ?? "Null";

        /// <summary>
        /// 资产类型。
        /// </summary>
        public string AssetType => Asset?.GetType().Name ?? "Unknown";

        /// <summary>
        /// 资产路径。
        /// </summary>
        public string AssetPath => Asset != null ? AssetDatabase.GetAssetPath(Asset) : string.Empty;
    }
}
