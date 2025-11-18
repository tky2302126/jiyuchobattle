using System;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class TextToImage
{
    private string url = "http://127.0.0.1:7860";

    /// <summary>
    /// Text to ImageのAPIリクエスト
    /// </summary>
    /// <param name="json"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
   // ----------------------------------------------
    // ① ProgressBar ナシの通常版
    // ----------------------------------------------
    public async UniTask<Texture2D> PostT2I(string json, CancellationToken token = default)
    {
        return await SendTxt2ImgRequest(json, token);
    }

    // ----------------------------------------------
    // ② ProgressBar 付き版（リクエストごとに独立）
    // ----------------------------------------------
    public async UniTask<Texture2D> PostT2I(
        string json,
        ProgressBarController progressBar,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        // ★ リクエスト専用キャンセル (UIキャンセルも連動)
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        var requestToken = linkedCts.Token;

        // ★ Progress 監視開始
        var watchTask = WatchProgressAsync(progressBar, requestToken);

        // API 実行
        Texture2D tex = null;
        try
        {
            tex = await SendTxt2ImgRequest(json, requestToken);
        }
        finally
        {
            // ★ 生成が終わったら ProgressWatcher を終了
            linkedCts.Cancel();
        }

        return tex;
    }


    // ----------------------------------------------
    // ③ txt2img API 実行本体
    // ----------------------------------------------
    private async UniTask<Texture2D> SendTxt2ImgRequest(string json, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);
        var request = new UnityWebRequest(url + "/sdapi/v1/txt2img", "POST")
        {
            uploadHandler = new UploadHandlerRaw(postData),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("[T2I] Sending request...");
        await request.SendWebRequest().WithCancellation(token);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[T2I] Request failed: " + request.error);
            return null;
        }

        Debug.Log("[T2I] Request Success");

        string response = request.downloadHandler.text;

        // JSON から Base64画像を抽出
        var match = new Regex("\"(.+?)\"").Matches(response);
        if (match.Count < 2) return null;

        string base64 = match[1].Value.Trim('"');
        byte[] data = Convert.FromBase64String(base64);

        var tex = new Texture2D(1, 1);
        tex.LoadImage(data);
        return tex;
    }
    private async UniTask PollProgress(
    ProgressBarController progressBar,
    Func<bool> isFinished,
    CancellationToken cancellationToken)
    {
        while (!isFinished())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var req = new UnityWebRequest(url + "/sdapi/v1/progress", "GET");
            req.downloadHandler = new DownloadHandlerBuffer();

            await req.SendWebRequest().WithCancellation(cancellationToken);

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var json = req.downloadHandler.text;
                    var progressInfo = JsonUtility.FromJson<ProgressResponse>(json);

                    float progress = progressInfo.progress; // 0.0〜1.0

                    progressBar?.SetProgress(progress);
                }
                catch { }
            }

            await UniTask.Delay(100); // 0.1秒ごとにポーリング
        }

        // 完了時は100%に
        progressBar?.SetProgress(1f);
    }

    private async UniTask WatchProgressAsync(
    ProgressBarController progressBar,
    CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var req = UnityWebRequest.Get(url + "/sdapi/v1/progress");
                req.downloadHandler = new DownloadHandlerBuffer();

                await req.SendWebRequest().WithCancellation(token);

                if (req.result == UnityWebRequest.Result.Success) 
                {
                    var json = req.downloadHandler.text;

                    var progress = JsonUtility.FromJson<ProgressResponse>(json).progress;

                    progressBar.SetProgress(progress);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // 通信エラー時も止めない
            }

            await UniTask.Delay(100, cancellationToken: token);
        }

        // ★ 完了時は100%に
        progressBar?.SetProgress(1f);
    }


    [Serializable]
    public class ProgressResponse
    {
        public float progress;  // 0.0~1.0
        public string state;
    }
}
