using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ScriptableObject), true)]
public class SOPopupDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 计算 UI 布局：给按钮留出 25 像素宽度
        float buttonWidth = 25f;
        Rect fieldRect = new Rect(position.x, position.y, position.width - buttonWidth - 5, position.height);
        Rect buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, position.height);

        // 1. 绘制标准的引用框
        EditorGUI.PropertyField(fieldRect, property, label);

        // 2. 如果当前字段有引用 SO，则绘制按钮
        if (property.objectReferenceValue != null && property.objectReferenceValue is ScriptableObject targetSO)
        {
            if (GUI.Button(buttonRect, "🔍"))
            {
                // 修复点：使用 EditorWindow.HasOpenInstances 和 EditorWindow.GetWindow
                if (EditorWindow.HasOpenInstances<GenericSOWindow>())
                {
                    GenericSOWindow window = EditorWindow.GetWindow<GenericSOWindow>();
                    window.NavigateTo(targetSO);
                    window.Focus();
                }
                else
                {
                    GenericSOWindow.Open(targetSO);
                }
            }
        }
    }
}