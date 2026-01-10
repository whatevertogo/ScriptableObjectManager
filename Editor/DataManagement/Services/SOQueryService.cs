using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// ScriptableObject 查询服务。提供高级搜索功能。
    /// </summary>
    public static class SOQueryService
    {
        private static readonly Dictionary<Type, Dictionary<string, QueryableFieldInfo>> _queryableFieldsCache = new();

        /// <summary>
        /// 执行查询。
        /// </summary>
        public static List<ScriptableObject> Query(QueryGroup query, IReadOnlyList<ScriptableObject> source = null)
        {
            if (query == null || query.Count == 0)
                return new List<ScriptableObject>();

            // 如果未指定源，使用当前扫描结果
            if (source == null)
            {
                var scanResult = SODataManager.Instance.CurrentResult;
                if (scanResult != null)
                {
                    source = scanResult.AssetsByType.Values.SelectMany(x => x).ToList();
                }
                else
                {
                    return new List<ScriptableObject>();
                }
            }

            // 执行查询
            var results = new List<ScriptableObject>();
            foreach (var so in source)
            {
                if (so != null && query.Evaluate(so))
                {
                    results.Add(so);
                }
            }

            return results;
        }

        /// <summary>
        /// 按字段值查询。
        /// </summary>
        public static List<ScriptableObject> QueryByField(
            string fieldName,
            QueryOperator op,
            object value,
            IReadOnlyList<ScriptableObject> source = null)
        {
            var queryGroup = new QueryGroup();
            queryGroup.AddCondition(fieldName, op, value);
            return Query(queryGroup, source);
        }

        /// <summary>
        /// 按名称搜索。
        /// </summary>
        public static List<ScriptableObject> SearchByName(string searchTerm, bool caseSensitive = false)
        {
            var scanResult = SODataManager.Instance.CurrentResult;
            if (scanResult == null)
                return new List<ScriptableObject>();

            var comparison = caseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            var results = new List<ScriptableObject>();
            foreach (var kvp in scanResult.AssetsByType)
            {
                foreach (var asset in kvp.Value)
                {
                    if (asset.name.IndexOf(searchTerm, comparison) >= 0)
                    {
                        results.Add(asset);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 获取类型的可查询字段。
        /// </summary>
        public static Dictionary<string, QueryableFieldInfo> GetQueryableFields(Type soType)
        {
            if (soType == null)
                return new Dictionary<string, QueryableFieldInfo>();

            if (_queryableFieldsCache.TryGetValue(soType, out var cached))
                return cached;

            var fields = new Dictionary<string, QueryableFieldInfo>();
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (var field in soType.GetFields(bindingFlags))
            {
                // 跳过静态字段
                if (field.IsStatic)
                    continue;

                // 跳过只读字段
                if (field.IsInitOnly)
                    continue;

                // 跳过 Unity 内置字段
                if (IsUnityBuiltInField(field.Name))
                    continue;

                // 跳过委托和事件类型
                if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                    continue;

                fields[field.Name] = new QueryableFieldInfo
                {
                    Name = field.Name,
                    Type = field.FieldType,
                    TypeName = GetFieldTypeDisplayName(field.FieldType)
                };
            }

            // 检查基类
            if (soType.BaseType != null && soType.BaseType != typeof(ScriptableObject))
            {
                var baseFields = GetQueryableFields(soType.BaseType);
                foreach (var kvp in baseFields)
                {
                    if (!fields.ContainsKey(kvp.Key))
                    {
                        fields[kvp.Key] = kvp.Value;
                    }
                }
            }

            _queryableFieldsCache[soType] = fields;
            return fields;
        }

        /// <summary>
        /// 获取所有可查询字段的名称（按类型分组）。
        /// </summary>
        public static Dictionary<Type, Dictionary<string, QueryableFieldInfo>> GetAllQueryableFields()
        {
            var result = new Dictionary<Type, Dictionary<string, QueryableFieldInfo>>();
            var scanResult = SODataManager.Instance.CurrentResult;

            if (scanResult == null)
                return result;

            foreach (var kvp in scanResult.AssetsByType)
            {
                if (!result.ContainsKey(kvp.Key))
                {
                    result[kvp.Key] = GetQueryableFields(kvp.Key);
                }
            }

            return result;
        }

        /// <summary>
        /// 获取字段的值（用于显示）。
        /// </summary>
        public static object GetFieldValue(ScriptableObject so, string fieldName)
        {
            if (so == null || string.IsNullOrEmpty(fieldName))
                return null;

            var reflectionField = GetReflectionFieldRecursive(so.GetType(), fieldName);
            if (reflectionField == null)
                return null;

            try
            {
                return reflectionField.GetValue(so);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 设置字段的值。
        /// </summary>
        public static bool SetFieldValue(ScriptableObject so, string fieldName, object value)
        {
            if (so == null || string.IsNullOrEmpty(fieldName))
                return false;

            var reflectionField = GetReflectionFieldRecursive(so.GetType(), fieldName);
            if (reflectionField == null)
                return false;

            try
            {
                reflectionField.SetValue(so, value);
                EditorUtility.SetDirty(so);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============ 辅助方法 ============

        /// <summary>
        /// 递归获取字段（包括基类）。
        /// </summary>
        private static FieldInfo GetReflectionFieldRecursive(Type type, string fieldName)
        {
            if (type == null)
                return null;

            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            var field = type.GetField(fieldName, bindingFlags);

            if (field != null)
                return field;

            // 检查基类
            if (type.BaseType != null)
                return GetReflectionFieldRecursive(type.BaseType, fieldName);

            return null;
        }

        /// <summary>
        /// 判断是否为 Unity 内置字段。
        /// </summary>
        private static bool IsUnityBuiltInField(string fieldName)
        {
            return fieldName == "m_Script" ||
                   fieldName.StartsWith("m_") && (
                       fieldName.Contains("hideFlags") ||
                       fieldName.Contains("objectHideFlags") ||
                       fieldName.Contains("icon") ||
                       fieldName.Contains("gameObject")
                   );
        }

        /// <summary>
        /// 获取字段类型的显示名称。
        /// </summary>
        private static string GetFieldTypeDisplayName(Type type)
        {
            if (type == null)
                return "null";

            if (type == typeof(int))
                return "int";
            if (type == typeof(float))
                return "float";
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(string))
                return "string";
            if (type == typeof(Vector2))
                return "Vector2";
            if (type == typeof(Vector3))
                return "Vector3";
            if (type == typeof(Color))
                return "Color";

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var itemType = type.GetGenericArguments()[0];
                return $"List<{itemType.Name}>";
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return $"{elementType.Name}[]";
            }

            return type.Name;
        }

        /// <summary>
        /// 可查询字段信息。
        /// </summary>
        public sealed class QueryableFieldInfo
        {
            public string Name { get; set; }
            public Type Type { get; set; }
            public string TypeName { get; set; }
        }
    }
}
