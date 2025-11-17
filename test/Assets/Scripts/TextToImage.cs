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
    public async UniTask<Texture2D> PostT2I(string json, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);
        var request = new UnityWebRequest(url + "/sdapi/v1/txt2img", "POST");
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(postData);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // APIリクエストを送信
        Debug.Log("Send Prompt");
        await request.SendWebRequest().WithCancellation(cancellationToken);

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Request Success");

            // レスポンスのJsonを取得
            var response = request.downloadHandler.text;

            // 「"」で囲まれた文字列を抽出
            var matches = new Regex("\"(.+?)\"").Matches(response);

            // 画像データを取得
            var imageData = matches[1].ToString().Trim('"');

            // Base64をbyte型配列に変換
            byte[] data = Convert.FromBase64String(imageData);

            // byte型配列をテクスチャに変換
            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(data);

            return texture;
        }
        else
        {
            Debug.Log("Error:" + request.result);

            Texture2D texture = new Texture2D(1, 1);
            return texture;
        }
    }

    public async UniTask<Texture2D> PostT2I(string json,
        ProgressBarController progressBar,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // ★ Progress監視スタート
        var progressWatcher = WatchProgressAsync(progressBar, cancellationToken);

        byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);
        var request = new UnityWebRequest(url + "/sdapi/v1/txt2img", "POST");
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(postData);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("Send Prompt");
        await request.SendWebRequest().WithCancellation(cancellationToken);

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Request Success");

            var response = request.downloadHandler.text;

            var matches = new Regex("\"(.+?)\"").Matches(response);
            var imageData = matches[1].ToString().Trim('"');

            byte[] data = Convert.FromBase64String(imageData);

            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(data);

            return texture;
        }
        else
        {
            Debug.Log("Error:" + request.result);
            return new Texture2D(1, 1);
        }
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
            var req = UnityWebRequest.Get(url + "/sdapi/v1/progress");
            await req.SendWebRequest().WithCancellation(token);

            if (req.result == UnityWebRequest.Result.Success)
            {
                var json = req.downloadHandler.text;

                var progress = JsonUtility.FromJson<ProgressResponse>(json).progress;

                    progressBar.SetProgress(progress);                
            }

            await UniTask.Delay(100, cancellationToken: token);
        }
    }


    [Serializable]
    public class ProgressResponse
    {
        public float progress;  // 0.0~1.0
        public string state;
    }
}
