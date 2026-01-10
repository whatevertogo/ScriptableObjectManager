using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// 依赖图数据结构。用于表示 ScriptableObject 之间的引用关系。
    /// </summary>
    public sealed class DependencyGraph
    {
        /// <summary>
        /// 依赖图节点。
        /// </summary>
        public sealed class Node
        {
            /// <summary>
            /// 关联的 ScriptableObject 资产。
            /// </summary>
            public ScriptableObject Asset { get; }

            /// <summary>
            /// 资产路径。
            /// </summary>
            public string AssetPath { get; }

            /// <summary>
            /// 该节点依赖的其他节点（出边）。
            /// </summary>
            public HashSet<Node> Dependencies { get; } = new HashSet<Node>();

            /// <summary>
            /// 依赖该节点的其他节点（入边）。
            /// </summary>
            public HashSet<Node> Dependents { get; } = new HashSet<Node>();

            /// <summary>
            /// 引用计数（有多少资产依赖此资产）。
            /// </summary>
            public int ReferenceCount => Dependents.Count;

            /// <summary>
            /// 是否为孤立资产（没有被任何资产引用）。
            /// </summary>
            public bool IsOrphan => Dependents.Count == 0;

            /// <summary>
            /// 依赖数量（该资产依赖了多少其他资产）。
            /// </summary>
            public int DependencyCount => Dependencies.Count;

            public Node(ScriptableObject asset)
            {
                Asset = asset;
                AssetPath = AssetDatabase.GetAssetPath(asset);
            }

            /// <summary>
            /// 获取显示名称。
            /// </summary>
            public string GetDisplayName()
            {
                if (Asset == null)
                    return AssetPath ?? "Null";

                string typeName = Asset.GetType().Name;
                return $"{Asset.name} ({typeName})";
            }

            public override bool Equals(object obj)
            {
                return obj is Node other && AssetPath == other.AssetPath;
            }

            public override int GetHashCode()
            {
                return AssetPath?.GetHashCode() ?? 0;
            }
        }

        /// <summary>
        /// 所有节点（按资产路径索引）。
        /// </summary>
        private Dictionary<string, Node> _pathToNode = new Dictionary<string, Node>();

        /// <summary>
        /// 获取所有节点。
        /// </summary>
        public IReadOnlyList<Node> AllNodes => _pathToNode.Values.ToList();

        /// <summary>
        /// 节点总数。
        /// </summary>
        public int NodeCount => _pathToNode.Count;

        /// <summary>
        /// 添加节点。
        /// </summary>
        public Node AddNode(ScriptableObject asset)
        {
            if (asset == null)
                return null;

            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
                return null;

            if (_pathToNode.TryGetValue(path, out var existingNode))
                return existingNode;

            var node = new Node(asset);
            _pathToNode[path] = node;
            return node;
        }

        /// <summary>
        /// 获取节点。
        /// </summary>
        public Node GetNode(ScriptableObject asset)
        {
            if (asset == null)
                return null;

            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? null : _pathToNode.GetValueOrDefault(path);
        }

        /// <summary>
        /// 获取节点。
        /// </summary>
        public Node GetNodeByPath(string assetPath)
        {
            return _pathToNode.GetValueOrDefault(assetPath);
        }

        /// <summary>
        /// 添加依赖关系（from 依赖 to）。
        /// </summary>
        public bool AddDependency(ScriptableObject from, ScriptableObject to)
        {
            var fromNode = AddNode(from);
            var toNode = AddNode(to);

            if (fromNode == null || toNode == null)
                return false;

            // 添加依赖关系
            if (!fromNode.Dependencies.Contains(toNode))
            {
                fromNode.Dependencies.Add(toNode);
            }

            // 添加反向依赖关系
            if (!toNode.Dependents.Contains(fromNode))
            {
                toNode.Dependents.Add(fromNode);
            }

            return true;
        }

        /// <summary>
        /// 获取所有孤立资产（没有被任何资产引用的 SO）。
        /// </summary>
        public IReadOnlyList<Node> GetOrphanNodes()
        {
            return _pathToNode.Values.Where(n => n.IsOrphan).ToList();
        }

        /// <summary>
        /// 获取引用最多的资产（Top N）。
        /// </summary>
        public IReadOnlyList<Node> GetMostReferenced(int topN = 10)
        {
            return _pathToNode.Values
                .OrderByDescending(n => n.ReferenceCount)
                .Take(topN)
                .ToList();
        }

        /// <summary>
        /// 获取依赖最多的资产（Top N）。
        /// </summary>
        public IReadOnlyList<Node> GetMostDependencies(int topN = 10)
        {
            return _pathToNode.Values
                .OrderByDescending(n => n.DependencyCount)
                .Take(topN)
                .ToList();
        }

        /// <summary>
        /// 获取两个节点之间的最短路径（广度优先搜索）。
        /// </summary>
        public List<Node> FindShortestPath(ScriptableObject from, ScriptableObject to)
        {
            var fromNode = GetNode(from);
            var toNode = GetNode(to);

            if (fromNode == null || toNode == null)
                return null;

            if (fromNode == toNode)
                return new List<Node> { fromNode };

            // BFS
            var queue = new Queue<Node>();
            var visited = new HashSet<Node>();
            var parentMap = new Dictionary<Node, Node>();

            queue.Enqueue(fromNode);
            visited.Add(fromNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == toNode)
                {
                    // 重建路径
                    var path = new List<Node>();
                    var node = toNode;

                    while (node != null)
                    {
                        path.Add(node);
                        if (node == fromNode)
                            break;
                        parentMap.TryGetValue(node, out node);
                    }

                    path.Reverse();
                    return path;
                }

                foreach (var dependency in current.Dependencies)
                {
                    if (!visited.Contains(dependency))
                    {
                        visited.Add(dependency);
                        parentMap[dependency] = current;
                        queue.Enqueue(dependency);
                    }
                }
            }

            return null; // 无路径
        }

        /// <summary>
        /// 获取资产的所有引用者。
        /// </summary>
        public IReadOnlyList<Node> GetReferencers(ScriptableObject asset)
        {
            var node = GetNode(asset);
            return node?.Dependents.ToList() ?? new List<Node>();
        }

        /// <summary>
        /// 获取资产的所有依赖项。
        /// </summary>
        public IReadOnlyList<Node> GetDependencies(ScriptableObject asset)
        {
            var node = GetNode(asset);
            return node?.Dependencies.ToList() ?? new List<Node>();
        }

        /// <summary>
        /// 清空图。
        /// </summary>
        public void Clear()
        {
            _pathToNode.Clear();
        }

        /// <summary>
        /// 获取统计信息。
        /// </summary>
        public GraphStats GetStats()
        {
            var stats = new GraphStats
            {
                TotalNodes = _pathToNode.Count,
                OrphanCount = _pathToNode.Values.Count(n => n.IsOrphan),
                TotalEdges = _pathToNode.Values.Sum(n => n.Dependencies.Count)
            };

            if (stats.TotalNodes > 0)
            {
                stats.AverageDependencies = (float)stats.TotalEdges / stats.TotalNodes;
            }

            return stats;
        }
    }

    /// <summary>
    /// 图统计信息。
    /// </summary>
    public sealed class GraphStats
    {
        public int TotalNodes { get; set; }
        public int OrphanCount { get; set; }
        public int TotalEdges { get; set; }
        public float AverageDependencies { get; set; }

        /// <summary>
        /// 孤立资产百分比。
        /// </summary>
        public float OrphanPercentage => TotalNodes > 0 ? (float)OrphanCount / TotalNodes * 100f : 0f;
    }
}
