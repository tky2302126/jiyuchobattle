using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class SDUnityExample : MonoBehaviour
{
    [ContextMenu("Generate Image")]
    public void GenerateImage()
    {
        StartCoroutine(GenerateCoroutine());
    }

    IEnumerator GenerateCoroutine()
    {
        string url = "http://127.0.0.1:7860/sdapi/v1/txt2img";

        // JSON body
        string json = JsonUtility.ToJson(new Txt2ImgRequest()
        {
            prompt = "A cute robot in a futuristic city",
            steps = 20,
            width = 512,
            height = 512
        });

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string resultJson = request.downloadHandler.text;
                Debug.Log(resultJson);
                // Base64画像をTexture2Dに変換する処理もここで可能
            }
            else
            {
                Debug.LogError(request.error);
            }
        }
    }
}

// JSON用のクラス
[System.Serializable]
public class Txt2ImgRequest
{
    public string prompt;
    public int steps;
    public int width;
    public int height;
}
