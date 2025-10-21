using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class StableDiffusionUnity : MonoBehaviour
{
    [Header("API設定")]
    public string apiUrl = "http://127.0.0.1:7860/sdapi/v1/txt2img";

    [Header("生成パラメータ")]
    public string prompt = "A cute robot in a futuristic city";
    public int width = 512;
    public int height = 512;
    public int steps = 20;

    [Header("生成画像を表示するRenderer")]
    public Renderer targetRenderer;

    [ContextMenu("Generate Image")]
    public void GenerateImage()
    {
        StartCoroutine(GenerateCoroutine());
    }

    IEnumerator GenerateCoroutine()
    {
        // JSONリクエスト作成
        Txt2ImgRequest requestData = new Txt2ImgRequest()
        {
            prompt = prompt,
            width = width,
            height = height,
            steps = steps
        };
        string json = JsonUtility.ToJson(requestData);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // リクエスト送信
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // JSONの解析
                string resultJson = request.downloadHandler.text;
                SDResponse response = JsonUtility.FromJson<SDResponse>(resultJson);

                if (response.images != null && response.images.Length > 0)
                {
                    // Base64をTexture2Dに変換
                    byte[] imageBytes = System.Convert.FromBase64String(response.images[0]);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(imageBytes);

                    // Rendererに適用
                    if (targetRenderer != null)
                        targetRenderer.material.mainTexture = tex;

                    Debug.Log("画像生成成功");
                }
                else
                {
                    Debug.LogError("画像が返ってきませんでした");
                }
            }
            else
            {
                Debug.LogError("リクエスト失敗: " + request.error);
            }
        }
    }

    [System.Serializable]
    public class Txt2ImgRequest
    {
        public string prompt;
        public int width;
        public int height;
        public int steps;
    }

    [System.Serializable]
    public class SDResponse
    {
        public string[] images;
    }
}
