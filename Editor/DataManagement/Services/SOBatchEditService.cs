using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// ScriptableObject 批量编辑服务。提供批量修改字段的功能。
    /// </summary>
    public static class SOBatchEditService
    {
        private const string UndoGroupName = "Batch Edit SO";

        /// <summary>
        /// 批量设置字段值。
        /// </summary>
        /// <param name="targets">目标 SO 列表</param>
        /// <param name="propertyPath">字段路径</param>
        /// <param name="value">新值</param>
        /// <returns>实际修改的数量</returns>
        public static int SetFieldValue(
            IReadOnlyList<ScriptableObject> targets,
            string propertyPath,
            object value)
        {
            if (targets == null || targets.Count == 0)
                return 0;

            int modifiedCount = 0;

            foreach (var so in targets)
            {
                if (so == null)
                    continue;

                var serializedObject = new SerializedObject(so);
                var property = serializedObject.FindProperty(propertyPath);

                if (property != null)
                {
                    Undo.RecordObject(so, $"Set {propertyPath}");
                    if (SetValueToProperty(property, value))
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(so);
                        modifiedCount++;
                    }
                    else
                    {
                        // 修改失败，不做任何处理
                    }
                }
            }

            if (modifiedCount > 0)
            {
                AssetDatabase.SaveAssets();
            }

            return modifiedCount;
        }

        /// <summary>
        /// 批量增加数值字段（适用于 int/float）。
        /// </summary>
        public static int AddToValue(
            IReadOnlyList<ScriptableObject> targets,
            string propertyPath,
            float delta)
        {
            if (targets == null || targets.Count == 0)
                return 0;

            int modifiedCount = 0;

            foreach (var so in targets)
            {
                if (so == null)
                    continue;

                var serializedObject = new SerializedObject(so);
                var property = serializedObject.FindProperty(propertyPath);

                if (property != null)
                {
                    bool modified = false;

                    switch (property.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            Undo.RecordObject(so, $"Add to {propertyPath}");
                            property.intValue = property.intValue + (int)delta;
                            modified = true;
                            break;

                        case SerializedPropertyType.Float:
                            Undo.RecordObject(so, $"Add to {propertyPath}");
                            property.floatValue = property.floatValue + delta;
                            modified = true;
                            break;
                    }

                    if (modified)
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(so);
                        modifiedCount++;
                    }
                }
            }

            if (modifiedCount > 0)
            {
                AssetDatabase.SaveAssets();
            }

            return modifiedCount;
        }

        /// <summary>
        /// 批量乘以数值字段。
        /// </summary>
        public static int MultiplyValue(
            IReadOnlyList<ScriptableObject> targets,
            string propertyPath,
            float multiplier)
        {
            if (targets == null || targets.Count == 0)
                return 0;

            int modifiedCount = 0;

            foreach (var so in targets)
            {
                if (so == null)
                    continue;

                var serializedObject = new SerializedObject(so);
                var property = serializedObject.FindProperty(propertyPath);

                if (property != null)
                {
                    bool modified = false;

                    switch (property.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            Undo.RecordObject(so, $"Multiply {propertyPath}");
                            property.intValue = Mathf.RoundToInt(property.intValue * multiplier);
                            modified = true;
                            break;

                        case SerializedPropertyType.Float:
                            Undo.RecordObject(so, $"Multiply {propertyPath}");
                            property.floatValue = property.floatValue * multiplier;
                            modified = true;
                            break;
                    }

                    if (modified)
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(so);
                        modifiedCount++;
                    }
                }
            }

            if (modifiedCount > 0)
            {
                AssetDatabase.SaveAssets();
            }

            return modifiedCount;
        }

        /// <summary>
        /// 批量设置对象引用（为 null 或指定对象）。
        /// </summary>
        public static int SetObjectReference(
            IReadOnlyList<ScriptableObject> targets,
            string propertyPath,
            UnityEngine.Object reference)
        {
            if (targets == null || targets.Count == 0)
                return 0;

            int modifiedCount = 0;

            foreach (var so in targets)
            {
                if (so == null)
                    continue;

                var serializedObject = new SerializedObject(so);
                var property = serializedObject.FindProperty(propertyPath);

                if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    Undo.RecordObject(so, $"Set {propertyPath} Reference");
                    property.objectReferenceValue = reference;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(so);
                    modifiedCount++;
                }
            }

            if (modifiedCount > 0)
            {
                AssetDatabase.SaveAssets();
            }

            return modifiedCount;
        }

        /// <summary>
        /// 批量设置字符串值。
        /// </summary>
        public static int SetStringValue(
            IReadOnlyList<ScriptableObject> targets,
            string propertyPath,
            string value)
        {
            if (targets == null || targets.Count == 0)
                return 0;

            int modifiedCount = 0;

            foreach (var so in targets)
            {
                if (so == null)
                    continue;

                var serializedObject = new SerializedObject(so);
                var property = serializedObject.FindProperty(propertyPath);

                if (property != null && property.propertyType == SerializedPropertyType.String)
                {
                    Undo.RecordObject(so, $"Set {propertyPath}");
                    property.stringValue = value;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(so);
                    modifiedCount++;
                }
            }

            if (modifiedCount > 0)
            {
                AssetDatabase.SaveAssets();
            }

            return modifiedCount;
        }

        /// <summary>
        /// 批量设置布尔值。
        /// </summary>
        public static int SetBoolValue(
            IReadOnlyList<ScriptableObject> targets,
            string propertyPath,
            bool value)
        {
            if (targets == null || targets.Count == 0)
                return 0;

            int modifiedCount = 0;

            foreach (var so in targets)
            {
                if (so == null)
                    continue;

                var serializedObject = new SerializedObject(so);
                var property = serializedObject.FindProperty(propertyPath);

                if (property != null && property.propertyType == SerializedPropertyType.Boolean)
                {
                    Undo.RecordObject(so, $"Set {propertyPath}");
                    property.boolValue = value;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(so);
                    modifiedCount++;
                }
            }

            if (modifiedCount > 0)
            {
                AssetDatabase.SaveAssets();
            }

            return modifiedCount;
        }

        /// <summary>
        /// 批量替换字符串（支持查找替换）。
        /// </summary>
        public static int ReplaceString(
            IReadOnlyList<ScriptableObject> targets,
            string propertyPath,
            string oldValue,
            string newValue)
        {
            if (targets == null || targets.Count == 0)
                return 0;

            int modifiedCount = 0;

            foreach (var so in targets)
            {
                if (so == null)
                    continue;

                var serializedObject = new SerializedObject(so);
                var property = serializedObject.FindProperty(propertyPath);

                if (property != null && property.propertyType == SerializedPropertyType.String)
                {
                    string current = property.stringValue;
                    if (current.Contains(oldValue))
                    {
                        Undo.RecordObject(so, $"Replace in {propertyPath}");
                        property.stringValue = current.Replace(oldValue, newValue);
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(so);
                        modifiedCount++;
                    }
                }
            }

            if (modifiedCount > 0)
            {
                AssetDatabase.SaveAssets();
            }

            return modifiedCount;
        }

        /// <summary>
        /// 获取可编辑的字段列表（排除 Unity 内置字段）。
        /// </summary>
        public static List<string> GetEditableFields(Type soType)
        {
            var fields = new List<string>();

            // 创建临时实例获取序列化字段
            ScriptableObject tempInstance = null;
            try
            {
                tempInstance = ScriptableObject.CreateInstance(soType);
                var serializedObject = new SerializedObject(tempInstance);
                var property = serializedObject.GetIterator();
                var next = property.NextVisible(true);

                while (next)
                {
                    // 跳过 Unity 内置字段
                    if (ShouldSkipProperty(property.propertyPath))
                    {
                        next = property.NextVisible(false);
                        continue;
                    }

                    fields.Add(property.propertyPath);
                    next = property.NextVisible(false);
                }
            }
            finally
            {
                if (tempInstance != null)
                    ScriptableObject.DestroyImmediate(tempInstance);
            }

            return fields;
        }

        /// <summary>
        /// 获取字段信息。
        /// </summary>
        public static FieldInfo GetFieldInfo(Type soType, string propertyPath)
        {
            var serializedObject = new SerializedObject(ScriptableObject.CreateInstance(soType));
            var property = serializedObject.FindProperty(propertyPath);

            if (property == null)
                return null;

            return new FieldInfo
            {
                PropertyPath = propertyPath,
                DisplayName = property.displayName,
                PropertyType = GetPropertyTypeName(property),
                ValueType = GetPropertyValueType(property)
            };
        }

        /// <summary>
        /// 字段信息。
        /// </summary>
        public sealed class FieldInfo
        {
            public string PropertyPath { get; set; }
            public string DisplayName { get; set; }
            public string PropertyType { get; set; }
            public Type ValueType { get; set; }
        }

        // ============ 辅助方法 ============

        /// <summary>
        /// 将值设置到属性。
        /// </summary>
        private static bool SetValueToProperty(SerializedProperty property, object value)
        {
            if (value == null)
            {
                // 设置为 null
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    property.objectReferenceValue = null;
                    return true;
                }
                else if (property.propertyType == SerializedPropertyType.String)
                {
                    property.stringValue = null;
                    return true;
                }
                return false;
            }

            try
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        property.intValue = Convert.ToInt32(value);
                        return true;

                    case SerializedPropertyType.Boolean:
                        property.boolValue = Convert.ToBoolean(value);
                        return true;

                    case SerializedPropertyType.Float:
                        property.floatValue = Convert.ToSingle(value);
                        return true;

                    case SerializedPropertyType.String:
                        property.stringValue = Convert.ToString(value);
                        return true;

                    case SerializedPropertyType.Color:
                        if (value is Color c)
                        {
                            property.colorValue = c;
                            return true;
                        }
                        return false;

                    case SerializedPropertyType.ObjectReference:
                        if (value is UnityEngine.Object obj)
                        {
                            property.objectReferenceValue = obj;
                            return true;
                        }
                        return false;

                    case SerializedPropertyType.LayerMask:
                    case SerializedPropertyType.Enum:
                        property.enumValueIndex = Convert.ToInt32(value);
                        return true;

                    case SerializedPropertyType.Vector2:
                        if (value is Vector2 v2)
                        {
                            property.vector2Value = v2;
                            return true;
                        }
                        return false;

                    case SerializedPropertyType.Vector3:
                        if (value is Vector3 v3)
                        {
                            property.vector3Value = v3;
                            return true;
                        }
                        return false;

                    case SerializedPropertyType.Vector4:
                        if (value is Vector4 v4)
                        {
                            property.vector4Value = v4;
                            return true;
                        }
                        return false;

                    case SerializedPropertyType.Quaternion:
                        if (value is Quaternion q)
                        {
                            property.quaternionValue = q;
                            return true;
                        }
                        return false;

                    case SerializedPropertyType.Rect:
                        if (value is Rect r)
                        {
                            property.rectValue = r;
                            return true;
                        }
                        return false;

                    case SerializedPropertyType.Bounds:
                        if (value is Bounds b)
                        {
                            property.boundsValue = b;
                            return true;
                        }
                        return false;

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断是否应该跳过该属性。
        /// </summary>
        private static bool ShouldSkipProperty(string propertyPath)
        {
            return propertyPath == "m_Script" ||
                   propertyPath.StartsWith("m_") && (
                       propertyPath.Contains("hideFlags") ||
                       propertyPath.Contains("objectHideFlags")
                   );
        }

        /// <summary>
        /// 获取属性类型名称。
        /// </summary>
        private static string GetPropertyTypeName(SerializedProperty property)
        {
            return property.propertyType.ToString();
        }

        /// <summary>
        /// 获取属性值类型。
        /// </summary>
        private static Type GetPropertyValueType(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return typeof(int);
                case SerializedPropertyType.Boolean:
                    return typeof(bool);
                case SerializedPropertyType.Float:
                    return typeof(float);
                case SerializedPropertyType.String:
                    return typeof(string);
                case SerializedPropertyType.Color:
                    return typeof(Color);
                case SerializedPropertyType.ObjectReference:
                    return typeof(UnityEngine.Object);
                case SerializedPropertyType.Vector2:
                    return typeof(Vector2);
                case SerializedPropertyType.Vector3:
                    return typeof(Vector3);
                case SerializedPropertyType.Vector4:
                    return typeof(Vector4);
                case SerializedPropertyType.Quaternion:
                    return typeof(Quaternion);
                case SerializedPropertyType.Rect:
                    return typeof(Rect);
                case SerializedPropertyType.Bounds:
                    return typeof(Bounds);
                default:
                    return typeof(object);
            }
        }
    }
}
