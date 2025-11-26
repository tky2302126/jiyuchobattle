using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MissingSORecoveryWindow : EditorWindow
{
    [MenuItem("Tools/Missing SO Recovery")]
    public static void OpenWindow()
    {
        GetWindow<MissingSORecoveryWindow>("Missing SO Recovery");
    }

    // 復旧用テンプレート ScriptableObject
    public ScriptableObject templateSO;

    private Vector2 scroll;

    void OnGUI()
    {
        EditorGUILayout.LabelField("Missing ScriptableObject Recovery", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        templateSO = EditorGUILayout.ObjectField("Template SO", templateSO, typeof(ScriptableObject), false) as ScriptableObject;

        EditorGUILayout.HelpBox(
            "Missing ScriptableObject は Project ウィンドウで複数選択してください。\n" +
            "Recover ボタンで一括復旧されます。",
            MessageType.Info
        );

        if (GUILayout.Button("Recover Selected Missing SOs"))
        {
            if (templateSO == null)
            {
                Debug.LogWarning("Template SO が未設定です。");
                return;
            }

            RecoverSelected();
        }
    }

    void RecoverSelected()
    {
        // Project ウィンドウで選択されたオブジェクトを取得
        Object[] selectedAssets = Selection.objects;
        if (selectedAssets.Length == 0)
        {
            Debug.LogWarning("Project ウィンドウで対象の Missing ScriptableObject を選択してください。");
            return;
        }

        foreach (var asset in selectedAssets)
        {
            if (asset == null) continue;

            // 新しい ScriptableObject を生成
            ScriptableObject newSO = ScriptableObject.CreateInstance(templateSO.GetType());

            string oldPath = AssetDatabase.GetAssetPath(asset);
            string newPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(oldPath),
                System.IO.Path.GetFileNameWithoutExtension(oldPath) + "_recovered.asset"
            );

            // SerializedObject でフィールドコピー
            SerializedObject soOld = new SerializedObject(asset);
            SerializedObject soNew = new SerializedObject(newSO);
            SerializedProperty prop = soOld.GetIterator();

            while (prop.NextVisible(true))
            {
                SerializedProperty newProp = soNew.FindProperty(prop.name);
                if (newProp != null)
                {
                    newProp.serializedObject.CopyFromSerializedProperty(prop);
                }
            }

            // 新しいアセットとして保存
            AssetDatabase.CreateAsset(newSO, newPath);
            Debug.Log($"Recovered: {newPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Recovery complete!");
    }
}
