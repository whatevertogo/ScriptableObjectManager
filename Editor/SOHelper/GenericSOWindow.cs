using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class GenericSOWindow : EditorWindow
{
    private ScriptableObject currentSO;
    private Stack<ScriptableObject> history = new Stack<ScriptableObject>();
    private Editor cachedEditor;
    private Vector2 scrollPos;

    // 打开窗口的入口
    public static void Open(ScriptableObject so)
    {
        GenericSOWindow window = GetWindow<GenericSOWindow>("SO 快速编辑器");
        window.NavigateTo(so, false); // 第一次打开不入栈或根据需求定制
        window.Show();
    }

    // 跳转逻辑
    public void NavigateTo(ScriptableObject newSO, bool addToHistory = true)
    {
        if (newSO == null) return;
        if (addToHistory && currentSO != null)
        {
            history.Push(currentSO);
        }
        currentSO = newSO;
        cachedEditor = null; // 清除缓存以重新生成 Inspector
    }

    private void OnGUI()
    {
        if (currentSO == null) 
        {
            EditorGUILayout.HelpBox("请通过点击 SO 旁的 🔍 按钮打开", MessageType.Info);
            return;
        }

        // --- 顶部导航栏 ---
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        GUI.enabled = history.Count > 0;
        if (GUILayout.Button("◀ 返回", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            currentSO = history.Pop();
            cachedEditor = null;
        }
        GUI.enabled = true;

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("清除历史", EditorStyles.toolbarButton)) { history.Clear(); }
        EditorGUILayout.EndHorizontal();

        // --- 内容绘制 ---
        EditorGUILayout.LabelField($"正在编辑: {currentSO.name}", EditorStyles.boldLabel);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        if (cachedEditor == null || cachedEditor.target != currentSO)
        {
            Editor.CreateCachedEditor(currentSO, null, ref cachedEditor);
        }
        
        cachedEditor.OnInspectorGUI();
        
        EditorGUILayout.EndScrollView();
    }
}