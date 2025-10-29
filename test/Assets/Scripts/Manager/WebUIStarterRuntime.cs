using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class WebUIStarterRuntime : MonoBehaviour
{
    private Process webUIProcess;
    private bool isReady = false;

    public WebUIStarterRuntime Instance => _instance;
    private WebUIStarterRuntime _instance;

    public float progressValue = 0.0f;

    void Start()
    {
        _instance = this;
        StartCoroutine(StartAndWaitForWebUI());
    }

    void OnApplicationQuit()
    {
        StopWebUI();
    }

    private IEnumerator StartAndWaitForWebUI()
    {
        StartWebUITest();

        UnityEngine.Debug.Log("Stable Diffusion 起動中...");

        // 起動完了待ちループ
        string healthUrl = "http://127.0.0.1:7860/sdapi/v1/txt2img";

        while (!isReady)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(healthUrl))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success || req.responseCode == 405)
                {
                    // 405 = Method Not Allowed（GETは禁止だがサーバーは応答している状態）
                    isReady = true;
                    UnityEngine.Debug.Log("Stable Diffusion 準備完了！");
                }
                else
                {
                    UnityEngine.Debug.Log("Stable Diffusion 起動待ち...");
                }
            }

            if (!isReady)
                yield return new WaitForSeconds(1f); // 1秒ごとに再試行
        }

        // ここから API リクエストを投げられる
        // （例）StartCoroutine(SendTxt2ImgRequest());
    }

    private void StartWebUI()
    {
        string webUIDir = @"C:\StableDiffusion\stable-diffusion-webui";
        string batPath = Path.Combine(webUIDir, "webui-user.bat");


        if (!File.Exists(batPath))
        {
            UnityEngine.Debug.LogError("WebUI バッチが見つかりません: " + batPath);
            return;
        }

        webUIProcess = new Process();
        webUIProcess.StartInfo.FileName = batPath;
        webUIProcess.StartInfo.WorkingDirectory = webUIDir;
        webUIProcess.StartInfo.UseShellExecute = false;
        webUIProcess.StartInfo.CreateNoWindow = true;
        webUIProcess.Start();

        UnityEngine.Debug.Log("▶ Stable Diffusion を起動しました。");
    }

    /// <summary>
    /// memo ブラウザが開くので見せないようにしたい
    /// </summary>
    private void StartWebUITest()
    {
        string webUIDir = @"C:\StableDiffusion\stable-diffusion-webui";
        string pythonPath = @"C:\StableDiffusion\stable-diffusion-webui\venv\Scripts\python.exe";
        string launchScript = Path.Combine(webUIDir, "launch.py");

        webUIProcess = new Process();
        webUIProcess.StartInfo.FileName = pythonPath;
        webUIProcess.StartInfo.Arguments = $"\"{launchScript}\" --listen --api";
        webUIProcess.StartInfo.WorkingDirectory = webUIDir;

        webUIProcess.StartInfo.UseShellExecute = false;
        webUIProcess.StartInfo.CreateNoWindow = true; // ここでウィンドウ非表示
        webUIProcess.StartInfo.RedirectStandardOutput = true;
        webUIProcess.StartInfo.RedirectStandardError = true;

        webUIProcess.OutputDataReceived += (sender, args) => 
        { if (args.Data != null) UnityEngine.Debug.Log(args.Data);

            // tqdm 形式の進捗を解析
            var match = System.Text.RegularExpressions.Regex.Match(args.Data, @"(\d+)/(\d+)");
            if (match.Success)
            {
                int current = int.Parse(match.Groups[1].Value);
                int total = int.Parse(match.Groups[2].Value);
                progressValue = (float)current / total;
            }
        };
        webUIProcess.ErrorDataReceived += (sender, args) => 
        { if (args.Data != null) UnityEngine.Debug.LogError(args.Data);

            // tqdm が stderr に出力される場合も同様に解析
            var match = System.Text.RegularExpressions.Regex.Match(args.Data, @"(\d+)/(\d+)");
            if (match.Success)
            {
                int current = int.Parse(match.Groups[1].Value);
                int total = int.Parse(match.Groups[2].Value);
                progressValue = (float)current / total;
            }
        };

        webUIProcess.Start();
        webUIProcess.BeginOutputReadLine();
        webUIProcess.BeginErrorReadLine();

        UnityEngine.Debug.Log("Stable Diffusion WebUI をバックグラウンドで起動しました");
    }

    private void StopWebUI()
    {
        if (webUIProcess != null && !webUIProcess.HasExited)
        {
            webUIProcess.Kill();
            UnityEngine.Debug.Log("Stable Diffusion を停止しました。");
        }
    }
}
