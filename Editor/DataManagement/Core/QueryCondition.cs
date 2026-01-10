using System;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// 查询操作符。
    /// </summary>
    public enum QueryOperator
    {
        Equal,          // 等于
        NotEqual,       // 不等于
        Greater,        // 大于
        GreaterOrEqual, // 大于等于
        Less,           // 小于
        LessOrEqual,    // 小于等于
        Contains,       // 包含
        NotContains,    // 不包含
        StartsWith,     // 开始于
        EndsWith,       // 结束于
        Regex,          // 正则表达式
        IsNull,         // 为空
        IsNotNull       // 不为空
    }

    /// <summary>
    /// 逻辑操作符。
    /// </summary>
    public enum LogicalOperator
    {
        And,
        Or
    }

    /// <summary>
    /// 查询条件。用于构建高级搜索查询。
    /// </summary>
    [Serializable]
    public sealed class QueryCondition
    {
        /// <summary>
        /// 字段名称。
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 操作符。
        /// </summary>
        public QueryOperator Operator { get; set; }

        /// <summary>
        /// 比较值。
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// 是否启用该条件。
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 评估指定对象是否满足条件。
        /// </summary>
        public bool Evaluate(UnityEngine.ScriptableObject target)
        {
            if (!IsEnabled || target == null)
                return false;

            // 通过反射获取字段值
            var field = GetField(target.GetType(), FieldName);
            if (field == null)
                return false;

            object fieldValue = GetFieldValue(target, field);

            // 根据操作符进行评估
            return EvaluateOperator(fieldValue, Value, Operator);
        }

        /// <summary>
        /// 获取字段信息。
        /// </summary>
        private System.Reflection.FieldInfo GetField(Type type, string fieldName)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                return field;

            // 检查基类
            if (type.BaseType != null && type.BaseType != typeof(UnityEngine.ScriptableObject))
                return GetField(type.BaseType, fieldName);

            return null;
        }

        /// <summary>
        /// 获取字段值。
        /// </summary>
        private object GetFieldValue(UnityEngine.ScriptableObject target, System.Reflection.FieldInfo field)
        {
            try
            {
                return field.GetValue(target);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 根据操作符评估条件。
        /// </summary>
        private bool EvaluateOperator(object fieldValue, object compareValue, QueryOperator op)
        {
            switch (op)
            {
                case QueryOperator.IsNull:
                    return fieldValue == null;

                case QueryOperator.IsNotNull:
                    return fieldValue != null;

                case QueryOperator.Equal:
                    return CompareValues(fieldValue, compareValue) == 0;

                case QueryOperator.NotEqual:
                    return CompareValues(fieldValue, compareValue) != 0;

                case QueryOperator.Greater:
                    return CompareValues(fieldValue, compareValue) > 0;

                case QueryOperator.GreaterOrEqual:
                    return CompareValues(fieldValue, compareValue) >= 0;

                case QueryOperator.Less:
                    return CompareValues(fieldValue, compareValue) < 0;

                case QueryOperator.LessOrEqual:
                    return CompareValues(fieldValue, compareValue) <= 0;

                case QueryOperator.Contains:
                    return fieldValue?.ToString()?.IndexOf(compareValue?.ToString(), System.StringComparison.OrdinalIgnoreCase) >= 0;

                case QueryOperator.NotContains:
                    return fieldValue?.ToString()?.IndexOf(compareValue?.ToString(), System.StringComparison.OrdinalIgnoreCase) < 0;

                case QueryOperator.StartsWith:
                    return fieldValue?.ToString()?.StartsWith(compareValue?.ToString(), System.StringComparison.OrdinalIgnoreCase) ?? false;

                case QueryOperator.EndsWith:
                    return fieldValue?.ToString()?.EndsWith(compareValue?.ToString(), System.StringComparison.OrdinalIgnoreCase) ?? false;

                case QueryOperator.Regex:
                    if (compareValue is string pattern && fieldValue is string str)
                    {
                        return System.Text.RegularExpressions.Regex.IsMatch(str, pattern);
                    }
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 比较两个值。
        /// </summary>
        private int CompareValues(object a, object b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            // 尝试转换为数值比较
            if (a is IComparable comparableA)
            {
                try
                {
                    if (b.GetType() == a.GetType())
                        return comparableA.CompareTo(b);

                    // 尝试转换比较
                    var convertedB = Convert.ChangeType(b, a.GetType());
                    return comparableA.CompareTo(convertedB);
                }
                catch
                {
                    // 转换失败，使用字符串比较
                }
            }

            // 字符串比较
            return string.Compare(a?.ToString(), b?.ToString(), System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取条件的显示文本。
        /// </summary>
        public string GetDisplayText()
        {
            string valueText = Value?.ToString() ?? "null";
            if (Value is string str)
                valueText = $"\"{str}\"";

            return $"{FieldName} {GetOperatorSymbol()} {valueText}";
        }

        /// <summary>
        /// 获取操作符符号。
        /// </summary>
        private string GetOperatorSymbol()
        {
            return Operator switch
            {
                QueryOperator.Equal => "==",
                QueryOperator.NotEqual => "!=",
                QueryOperator.Greater => ">",
                QueryOperator.GreaterOrEqual => ">=",
                QueryOperator.Less => "<",
                QueryOperator.LessOrEqual => "<=",
                QueryOperator.Contains => "包含",
                QueryOperator.NotContains => "不包含",
                QueryOperator.StartsWith => "开始于",
                QueryOperator.EndsWith => "结束于",
                QueryOperator.Regex => "匹配",
                QueryOperator.IsNull => "为空",
                QueryOperator.IsNotNull => "不为空",
                _ => "?"
            };
        }
    }

    /// <summary>
    /// 查询组。包含多个条件，支持 AND/OR 逻辑。
    /// </summary>
    [Serializable]
    public sealed class QueryGroup
    {
        /// <summary>
        /// 条件列表。
        /// </summary>
        public System.Collections.Generic.List<QueryCondition> Conditions { get; set; } = new System.Collections.Generic.List<QueryCondition>();

        /// <summary>
        /// 逻辑操作符（AND 或 OR）。
        /// </summary>
        public LogicalOperator LogicalOp { get; set; } = LogicalOperator.And;

        /// <summary>
        /// 评估指定对象是否满足查询组。
        /// </summary>
        public bool Evaluate(UnityEngine.ScriptableObject target)
        {
            if (Conditions.Count == 0)
                return true;

            // 过滤启用的条件
            var enabledConditions = Conditions.FindAll(c => c.IsEnabled);

            if (enabledConditions.Count == 0)
                return true;

            if (LogicalOp == LogicalOperator.And)
            {
                // 所有条件都必须满足
                foreach (var condition in enabledConditions)
                {
                    if (!condition.Evaluate(target))
                        return false;
                }
                return true;
            }
            else // OR
            {
                // 至少一个条件满足
                foreach (var condition in enabledConditions)
                {
                    if (condition.Evaluate(target))
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 添加新条件。
        /// </summary>
        public QueryCondition AddCondition(string fieldName = null, QueryOperator op = QueryOperator.Equal, object value = null)
        {
            var condition = new QueryCondition
            {
                FieldName = fieldName ?? "name",
                Operator = op,
                Value = value
            };
            Conditions.Add(condition);
            return condition;
        }

        /// <summary>
        /// 移除条件。
        /// </summary>
        public void RemoveCondition(QueryCondition condition)
        {
            Conditions.Remove(condition);
        }

        /// <summary>
        /// 清除所有条件。
        /// </summary>
        public void Clear()
        {
            Conditions.Clear();
        }

        /// <summary>
        /// 获取条件的数量。
        /// </summary>
        public int Count => Conditions.Count;

        /// <summary>
        /// 获取启用条件的数量。
        /// </summary>
        public int EnabledCount => Conditions.FindAll(c => c.IsEnabled).Count;
    }
}
