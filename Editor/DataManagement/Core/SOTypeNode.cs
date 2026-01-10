using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// ScriptableObject 类型树节点。用于构建分类树结构。
    /// </summary>
    public sealed class SOTypeNode
    {
        /// <summary>
        /// 节点显示名称。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 节点对应的 ScriptableObject 类型。
        /// 如果为 null，表示这是一个文件夹节点（用于分组）。
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// 子节点列表。
        /// </summary>
        public List<SOTypeNode> Children { get; }

        /// <summary>
        /// 此节点下的所有资产实例（仅当 Type 不为 null 时有值）。
        /// </summary>
        public List<ScriptableObject> Assets { get; }

        /// <summary>
        /// 此节点下的资产总数（包括所有子节点）。
        /// </summary>
        public int AssetCount { get; private set; }

        /// <summary>
        /// 是否展开（UI 状态）。
        /// </summary>
        public bool IsExpanded { get; set; }

        /// <summary>
        /// 是否为文件夹节点。
        /// </summary>
        public bool IsFolder => Type == null;

        /// <summary>
        /// 创建文件夹节点。
        /// </summary>
        public SOTypeNode(string displayName)
        {
            DisplayName = displayName;
            Type = null;
            Children = new List<SOTypeNode>();
            Assets = new List<ScriptableObject>();
            AssetCount = 0;
            IsExpanded = true;
        }

        /// <summary>
        /// 创建类型节点。
        /// </summary>
        public SOTypeNode(Type type, List<ScriptableObject> assets)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            DisplayName = GetDisplayName(type);
            Type = type;
            Children = new List<SOTypeNode>();
            Assets = assets ?? new List<ScriptableObject>();
            AssetCount = Assets.Count;
            IsExpanded = false;
        }

        /// <summary>
        /// 添加子节点。
        /// </summary>
        public void AddChild(SOTypeNode child)
        {
            if (child == null)
                return;

            Children.Add(child);
            UpdateAssetCount();
        }

        /// <summary>
        /// 更新资产计数（递归计算所有子节点）。
        /// </summary>
        public void UpdateAssetCount()
        {
            if (IsFolder)
            {
                // 文件夹节点：计算所有子节点的资产总数
                AssetCount = Children.Sum(c => c.AssetCount);
            }
            else
            {
                // 类型节点：资产数就是 Assets.Count
                AssetCount = Assets.Count;
            }
        }

        /// <summary>
        /// 获取类型的显示名称。简化类型名称，移除通用后缀。
        /// </summary>
        private static string GetDisplayName(Type type)
        {
            string name = type.Name;

            // 移除常见后缀
            string[] suffixesToRemove = { "Definition", "Config", "ConfigSO", "SO", "Data", "Base" };
            foreach (var suffix in suffixesToRemove)
            {
                if (name.EndsWith(suffix))
                {
                    name = name.Substring(0, name.Length - suffix.Length);
                    break;
                }
            }

            return name;
        }

        /// <summary>
        /// 按分类构建分类树。
        /// 分类规则：有 ManagedDataAttribute 的使用其 Category，否则归入 "Other"。
        /// </summary>
        public static List<SOTypeNode> BuildCategoryTree(
            Dictionary<Type, List<ScriptableObject>> assetsByType)
        {
            var rootNodes = new List<SOTypeNode>();
            var categoryMap = new Dictionary<string, SOTypeNode>();

            foreach (var kvp in assetsByType)
            {
                Type type = kvp.Key;
                var assets = kvp.Value;

                // 跳过内部类型
                if (type.IsNested || type.IsGenericType)
                    continue;

                // 确定分类（优先使用 ManagedDataAttribute，然后命名空间）
                string category = GetCategoryForType(type);

                // 获取或创建分类节点
                if (!categoryMap.TryGetValue(category, out var categoryNode))
                {
                    categoryNode = new SOTypeNode(category);
                    categoryMap[category] = categoryNode;
                    rootNodes.Add(categoryNode);
                }

                // 创建类型节点并添加到分类
                var typeNode = new SOTypeNode(type, assets);
                categoryNode.AddChild(typeNode);
            }

            // 更新所有节点的资产计数
            foreach (var node in rootNodes)
            {
                node.UpdateAssetCount();
            }

            // 按名称排序
            return rootNodes
                .OrderBy(n => n.DisplayName)
                .ToList();
        }

        /// <summary>
        /// 获取类型所属分类。
        /// 规则：有 ManagedDataAttribute 的使用其 Category，否则归入 "Other"。
        /// </summary>
        private static string GetCategoryForType(Type type)
        {
            if (type == null)
                return "Other";

            // 检查 ManagedDataAttribute 特性
            var managedAttr = type.GetCustomAttributes(typeof(ManagedDataAttribute), false)
                .FirstOrDefault() as ManagedDataAttribute;
            if (managedAttr != null && !string.IsNullOrEmpty(managedAttr.Category))
            {
                return managedAttr.Category;
            }

            // 默认分类
            return "Other";
        }
    }
}
