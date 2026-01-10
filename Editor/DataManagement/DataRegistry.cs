using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// ScriptableObject 数据注册表。存储扫描到的带有 ManagedDataAttribute 的资源。
    /// </summary>
    public static class DataRegistry
    {
        private static Dictionary<string, List<ScriptableObject>> _map = new();

        /// <summary>注册的资源总数</summary>
        public static int Count { get; private set; }

        /// <summary>分类映射表（只读）</summary>
        public static IReadOnlyDictionary<string, List<ScriptableObject>> Map => _map;

        /// <summary>注册发生的回调事件</summary>
        public static event Action<ScriptableObject, string> OnRegistered;

        /// <summary>清空发生的回调事件</summary>
        public static event Action OnCleared;

        /// <summary>清空所有注册数据</summary>
        public static void Clear()
        {
            _map.Clear();
            Count = 0;
            OnCleared?.Invoke();
        }

        /// <summary>注册一个 ScriptableObject 到指定分类</summary>
        public static void Register(ManagedDataAttribute attr, ScriptableObject so)
        {
            if (attr == null)
                throw new ArgumentNullException(nameof(attr));
            if (so == null)
                throw new ArgumentNullException(nameof(so));

            if (!_map.TryGetValue(attr.Category, out var list))
            {
                list = new List<ScriptableObject>();
                _map[attr.Category] = list;
            }

            list.Add(so);
            Count++;
            OnRegistered?.Invoke(so, attr.Category);
        }

        /// <summary>获取指定分类下的所有资源</summary>
        public static IReadOnlyList<ScriptableObject> GetCategory(string category)
        {
            return _map.TryGetValue(category, out var list) ? list : Array.Empty<ScriptableObject>();
        }

        /// <summary>获取所有分类名称（已排序）</summary>
        public static IEnumerable<string> GetCategories()
        {
            return _map.Keys.OrderBy(x => x);
        }

        /// <summary>检查是否存在指定分类</summary>
        public static bool HasCategory(string category)
        {
            return _map.ContainsKey(category);
        }

        /// <summary>获取指定类型的所有资源</summary>
        public static IEnumerable<T> GetAllOfType<T>() where T : ScriptableObject
        {
            return _map.Values.SelectMany(list => list).OfType<T>();
        }

        /// <summary>按名称查找资源</summary>
        public static ScriptableObject FindByName(string name)
        {
            return _map.Values.SelectMany(list => list).FirstOrDefault(so => so.name == name);
        }

        /// <summary>按名称查找指定类型的资源</summary>
        public static T FindByName<T>(string name) where T : ScriptableObject
        {
            return _map.Values.SelectMany(list => list).OfType<T>().FirstOrDefault(so => so.name == name);
        }

        /// <summary>获取所有资源（不分分类）</summary>
        public static IEnumerable<ScriptableObject> GetAll()
        {
            return _map.Values.SelectMany(list => list);
        }
    }
}