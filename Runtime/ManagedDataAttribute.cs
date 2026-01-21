using System;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// 标记 ScriptableObject 类型为可管理的数据。
    /// 被标记的类型的资源实例会被 DataManager 扫描并注册。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ManagedDataAttribute : Attribute
    {
        /// <summary>
        /// 数据分类名称。相同分类的数据会在 UI 中分组显示。
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// 数据的显示优先级（可选）。数值越小越靠前显示。
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 标记一个 ScriptableObject 类型为可管理的数据。
        /// </summary>
        /// <param name="category">数据分类名称</param>
        public ManagedDataAttribute(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Category 不能为空", nameof(category));

            Category = category;
            Priority = 0;
        }
    }
}
