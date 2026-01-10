using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// ScriptableObject 扫描结果。不可变数据结构，存储扫描到的所有资产信息。
    /// </summary>
    public sealed class SOScanResult
    {
        /// <summary>
        /// 按 ScriptableObject 类型分组的资产映射。
        /// Key = SO 类型，Value = 该类型的所有资产实例
        /// </summary>
        public IReadOnlyDictionary<Type, List<ScriptableObject>> AssetsByType { get; }

        /// <summary>
        /// 分类树结构。按命名空间和类型层级组织的树形数据。
        /// </summary>
        public IReadOnlyList<SOTypeNode> CategoryTree { get; }

        /// <summary>
        /// 扫描到的资产总数。
        /// </summary>
        public int TotalAssetCount { get; }

        /// <summary>
        /// 扫描到的类型总数。
        /// </summary>
        public int TotalTypeCount { get; }

        /// <summary>
        /// 扫描完成时的时间戳。
        /// </summary>
        public DateTime ScanTimestamp { get; }

        /// <summary>
        /// 创建扫描结果。
        /// </summary>
        public SOScanResult(
            Dictionary<Type, List<ScriptableObject>> assetsByType,
            List<SOTypeNode> categoryTree)
        {
            AssetsByType = assetsByType;
            CategoryTree = categoryTree;
            TotalAssetCount = assetsByType.Values.Sum(list => list.Count);
            TotalTypeCount = assetsByType.Count;
            ScanTimestamp = DateTime.Now;
        }

        /// <summary>
        /// 获取指定类型的所有资产。
        /// </summary>
        public IReadOnlyList<ScriptableObject> GetAssetsOfType(Type type)
        {
            return AssetsByType.TryGetValue(type, out var list) ? list : Array.Empty<ScriptableObject>();
        }

        /// <summary>
        /// 获取指定类型的所有资产（泛型版本）。
        /// </summary>
        public IReadOnlyList<T> GetAssetsOfType<T>() where T : ScriptableObject
        {
            if (AssetsByType.TryGetValue(typeof(T), out var list))
            {
                return list.Cast<T>().ToList();
            }
            return Array.Empty<T>();
        }

        /// <summary>
        /// 按名称查找资产。
        /// </summary>
        public ScriptableObject FindByName(string name)
        {
            foreach (var list in AssetsByType.Values)
            {
                foreach (var so in list)
                {
                    if (so.name == name)
                        return so;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取所有 SO 类型。
        /// </summary>
        public IEnumerable<Type> GetAllTypes()
        {
            return AssetsByType.Keys;
        }

        /// <summary>
        /// 按命名空间过滤类型。
        /// </summary>
        public IEnumerable<Type> GetTypesInNamespace(string namespacePattern)
        {
            return AssetsByType.Keys
                .Where(t => t.Namespace?.Contains(namespacePattern, StringComparison.OrdinalIgnoreCase) == true);
        }
    }
}
