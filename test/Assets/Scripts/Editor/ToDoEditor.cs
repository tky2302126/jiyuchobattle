using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ToDoEditor : EditorWindow
{
    private List<string> tasks = new List<string>();
    private string newTask = "";

    [MenuItem("Tools/ToDoリスト")]
    public static void ShowWindow()
    {
        GetWindow<ToDoEditor>("ToDoリスト");
    }

    private void OnGUI()
    {
        GUILayout.Label("簡易ToDoリスト", EditorStyles.boldLabel);

        // 新しいタスク入力
        GUILayout.BeginHorizontal();
        newTask = GUILayout.TextField(newTask);
        if (GUILayout.Button("追加"))
        {
            if (!string.IsNullOrEmpty(newTask))
            {
                tasks.Add(newTask);
                newTask = "";
            }
        }
        GUILayout.EndHorizontal();

        // タスクリスト表示
        for (int i = 0; i < tasks.Count; i++)
        {
            GUILayout.BeginHorizontal();
            tasks[i] = GUILayout.TextField(tasks[i]);
            if (GUILayout.Button("削除"))
            {
                tasks.RemoveAt(i);
                i--;
            }
            GUILayout.EndHorizontal();
        }
    }
}
