//using UnityEngine;
//using System.Diagnostics;
//using System.IO;
//using UnityEngine.Networking;
//using System.Collections;


//#if UNITY_EDITOR
//using UnityEditor;
//[InitializeOnLoad]
//public static class WebUIStarter 
//{
//    private static Process webUIProcess;

//    static WebUIStarter()
//    {
//        EditorApplication.playModeStateChanged += OnPlayModeChanged;
//    }

//    private static void OnPlayModeChanged(PlayModeStateChange state)
//    {
//        if (state == PlayModeStateChange.EnteredPlayMode)
//        {
//            StartWebUI();
//        }
//        else if (state == PlayModeStateChange.ExitingPlayMode)
//        {
//            StopWebUI();
//        }
//    }

//    private static void StartWebUI()
//    {
//        // WebUI のディレクトリパス
//        string webUIDir = @"C:\StableDiffusion\stable-diffusion-webui";

//        // webui-user.bat のフルパス
//        string batPath = Path.Combine(webUIDir, "webui-user.bat");

//        if (!File.Exists(batPath))
//        {
//            UnityEngine.Debug.LogError("WebUI バッチが見つかりません: " + batPath);
//            return;
//        }

//        webUIProcess = new Process();
//        webUIProcess.StartInfo.FileName = batPath;
//        webUIProcess.StartInfo.WorkingDirectory = webUIDir;
//        webUIProcess.StartInfo.UseShellExecute = true; // コンソール表示
//        webUIProcess.Start();

//        UnityEngine.Debug.Log("WebUI を起動しました。");
//    }

//    private static void StopWebUI()
//    {
//        if (webUIProcess != null && !webUIProcess.HasExited)
//        {
//            webUIProcess.Kill();
//            UnityEngine.Debug.Log("WebUI を停止しました。");
//        }
//    }
//}

//#endif
