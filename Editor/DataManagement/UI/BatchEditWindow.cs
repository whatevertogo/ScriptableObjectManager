using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectDataManagement
{
    /// <summary>
    /// 批量编辑窗口。用于批量修改多个同类型 ScriptableObject 的字段。
    /// </summary>
    public sealed class BatchEditWindow : EditorWindow
    {
        private List<ScriptableObject> _targets = new();
        private Type _targetType;
        private string _selectedFieldPath;
        private SOBatchEditService.FieldInfo _selectedFieldInfo;
        private List<string> _editableFields = new();
        private Vector2 _scrollPosition;
        private object _newValue;
        private string _stringValue = string.Empty;
        private float _numericValue;
        private bool _boolValue;
        private UnityEngine.Object _objectValue;
        private int _selectedOperation = 0; // 0=Set, 1=Add, 2=Multiply, 3=Replace

        // 操作类型
        private enum OperationType
        {
            Set,        // 设置值
            Add,        // 增加值
            Multiply,   // 乘以值
            Replace     // 替换字符串
        }

        /// <summary>
        /// 显示批量编辑窗口。
        /// </summary>
        public static void Show(IEnumerable<ScriptableObject> targets)
        {
            var window = CreateWindow<BatchEditWindow>("Batch Edit");
            window._targets = targets?.ToList() ?? new List<ScriptableObject>();

            // 确定所有目标的类型（必须相同）
            if (window._targets.Count > 0)
            {
                window._targetType = window._targets[0]?.GetType();
                foreach (var target in window._targets)
                {
                    if (target?.GetType() != window._targetType)
                    {
                        Debug.LogWarning("[BatchEdit] 所有资产必须是相同类型");
                        window._targets = window._targets.Where(t => t?.GetType() == window._targetType).ToList();
                        break;
                    }
                }

                if (window._targetType != null)
                {
                    window._editableFields = SOBatchEditService.GetEditableFields(window._targetType);
                }
            }

            window.Show();
        }

        void OnGUI()
        {
            if (_targets == null || _targets.Count == 0)
            {
                EditorGUILayout.HelpBox("没有选中任何资产", MessageType.Warning);
                return;
            }

            DrawHeader();
            DrawFieldSelector();
            DrawValueEditor();

            GUILayout.Space(10);
            DrawButtons();
        }

        /// <summary>
        /// 绘制头部信息。
        /// </summary>
        void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"批量编辑 ({_targets.Count} 个资产)", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"类型: {_targetType?.Name}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // 显示选中资产列表
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("选中资产:", EditorStyles.boldLabel);
            foreach (var target in _targets)
            {
                if (target != null)
                {
                    GUILayout.Label($"  • {target.name} ({target.GetType().Name})", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
        }

        /// <summary>
        /// 绘制字段选择器。
        /// </summary>
        void DrawFieldSelector()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("选择要编辑的字段:", EditorStyles.boldLabel);

            if (_editableFields.Count == 0)
            {
                GUILayout.Label("无可编辑字段", EditorStyles.miniLabel);
            }
            else
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(120));

                foreach (var fieldPath in _editableFields)
                {
                    bool isSelected = fieldPath == _selectedFieldPath;
                    GUI.backgroundColor = isSelected ? new Color(0.5f, 0.8f, 1f, 0.3f) : Color.white;

                    if (GUILayout.Button(fieldPath, EditorStyles.miniButton))
                    {
                        _selectedFieldPath = fieldPath;
                        _selectedFieldInfo = SOBatchEditService.GetFieldInfo(_targetType, fieldPath);
                    }

                    GUI.backgroundColor = Color.white;
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(4);

            // 显示选中字段信息
            if (_selectedFieldInfo != null)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label($"字段: {_selectedFieldInfo.DisplayName}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"类型: {_selectedFieldInfo.PropertyType}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 绘制值编辑器。
        /// </summary>
        void DrawValueEditor()
        {
            if (_selectedFieldInfo == null)
            {
                EditorGUILayout.HelpBox("请先选择一个字段", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("设置新值:", EditorStyles.boldLabel);

            // 操作类型选择
            DrawOperationSelector();

            GUILayout.Space(4);

            // 根据字段类型显示不同的输入控件
            switch (_selectedFieldInfo.PropertyType)
            {
                case "Integer":
                    DrawNumericEditor(false);
                    break;
                case "Float":
                    DrawNumericEditor(true);
                    break;
                case "Boolean":
                    _boolValue = EditorGUILayout.Toggle("新值", _boolValue);
                    break;
                case "String":
                    _stringValue = EditorGUILayout.TextField("新值", _stringValue);
                    break;
                case "ObjectReference":
                    _objectValue = EditorGUILayout.ObjectField("新值", _objectValue, typeof(UnityEngine.Object), false);
                    break;
                case "Color":
                    _newValue = EditorGUILayout.ColorField("新值", _newValue as Color? ?? Color.white);
                    break;
                case "Vector2":
                    _newValue = EditorGUILayout.Vector2Field("新值", _newValue as Vector2? ?? Vector2.zero);
                    break;
                case "Vector3":
                    _newValue = EditorGUILayout.Vector3Field("新值", _newValue as Vector3? ?? Vector3.zero);
                    break;
                default:
                    GUILayout.Label($"不支持的类型: {_selectedFieldInfo.PropertyType}", EditorStyles.miniLabel);
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制操作选择器。
        /// </summary>
        void DrawOperationSelector()
        {
            var operations = new[] { "设置值", "增加", "乘以", "替换字符串" };

            // 根据字段类型限制可用操作
            bool isNumeric = _selectedFieldInfo.PropertyType == "Integer" ||
                           _selectedFieldInfo.PropertyType == "Float";
            bool isString = _selectedFieldInfo.PropertyType == "String";

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("操作:", GUILayout.Width(40));

            int prevSelected = _selectedOperation;
            _selectedOperation = GUILayout.Toolbar(_selectedOperation, new[] { "设置", "增加", "乘", "替换" });

            // 如果类型不支持该操作，重置选择
            if (!isNumeric && _selectedOperation > 0 && _selectedOperation < 3)
                _selectedOperation = 0;
            if (!isString && _selectedOperation == 3)
                _selectedOperation = 0;

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制数值编辑器。
        /// </summary>
        void DrawNumericEditor(bool isFloat)
        {
            if (isFloat)
            {
                _numericValue = EditorGUILayout.FloatField("新值", _numericValue);
            }
            else
            {
                _numericValue = EditorGUILayout.IntField("新值", (int)_numericValue);
            }

            // 增加和乘法操作的额外选项
            if (_selectedOperation == 1 || _selectedOperation == 2)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("当前值范围:", EditorStyles.miniLabel);

                // 获取第一个资产的实际值作为参考
                if (_targets.Count > 0 && _targets[0] != null)
                {
                    var so = _targets[0];
                    var serialized = new SerializedObject(so);
                    var prop = serialized.FindProperty(_selectedFieldPath);
                    if (prop != null)
                    {
                        string currentValue = isFloat ? prop.floatValue.ToString() : prop.intValue.ToString();
                        GUILayout.Label($"参考: {currentValue}", EditorStyles.miniLabel);
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 绘制操作按钮。
        /// </summary>
        void DrawButtons()
        {
            EditorGUILayout.BeginHorizontal();

            // 应用按钮
            using (new EditorGUI.DisabledScope(_selectedFieldInfo == null))
            {
                if (GUILayout.Button("应用更改", GUILayout.Height(30)))
                {
                    ApplyChanges();
                }
            }

            GUILayout.Space(4);

            // 取消按钮
            if (GUILayout.Button("取消", GUILayout.Width(80)))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 应用更改到所有选中资产。
        /// </summary>
        void ApplyChanges()
        {
            if (_selectedFieldInfo == null || _targets.Count == 0)
                return;

            int modifiedCount = 0;

            try
            {
                switch (_selectedOperation)
                {
                    case 0: // 设置值
                        modifiedCount = ApplySetValue();
                        break;
                    case 1: // 增加
                        modifiedCount = SOBatchEditService.AddToValue(_targets, _selectedFieldPath, _numericValue);
                        break;
                    case 2: // 乘以
                        modifiedCount = SOBatchEditService.MultiplyValue(_targets, _selectedFieldPath, _numericValue);
                        break;
                    case 3: // 替换字符串
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("查找:");
                        string oldValue = EditorGUILayout.TextField(_oldValue);
                        GUILayout.Label("替换为:");
                        string newValue = EditorGUILayout.TextField(_newValue as string);
                        EditorGUILayout.EndHorizontal();
                        modifiedCount = SOBatchEditService.ReplaceString(_targets, _selectedFieldPath, oldValue, newValue);
                        break;
                }

                if (modifiedCount > 0)
                {
                    Debug.Log($"[BatchEdit] 成功修改了 {modifiedCount} 个资产的字段: {_selectedFieldInfo.DisplayName}");
                    EditorUtility.SetDirty(this);
                    // 刷新主窗口
                    SODataManager.Instance.Scan();
                    Close();
                }
                else
                {
                    Debug.LogWarning("[BatchEdit] 没有资产被修改");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BatchEdit] 批量编辑失败: {e.Message}");
            }
        }

        /// <summary>
        /// 应用设置值操作。
        /// </summary>
        private int ApplySetValue()
        {
            object valueToSet = null;

            switch (_selectedFieldInfo.PropertyType)
            {
                case "Integer":
                    valueToSet = (int)_numericValue;
                    break;
                case "Float":
                    valueToSet = _numericValue;
                    break;
                case "Boolean":
                    valueToSet = _boolValue;
                    break;
                case "String":
                    valueToSet = _stringValue;
                    break;
                case "ObjectReference":
                    valueToSet = _objectValue;
                    break;
                case "Color":
                    valueToSet = _newValue as Color?;
                    break;
                case "Vector2":
                    valueToSet = _newValue as Vector2?;
                    break;
                case "Vector3":
                    valueToSet = _newValue as Vector3?;
                    break;
                default:
                    return 0;
            }

            return SOBatchEditService.SetFieldValue(_targets, _selectedFieldPath, valueToSet);
        }

        private string _oldValue = string.Empty;
    }
}
