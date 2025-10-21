using System;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
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
}
