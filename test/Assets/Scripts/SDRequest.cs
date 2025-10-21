using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SDRequest : MonoBehaviour
{
    public InputField promptInput;
    public RawImage outputImage;

    public void GenerateImage()
    {
        StartCoroutine(SendRequest(promptInput.text));
    }

    IEnumerator SendRequest(string prompt)
    {
        string url = "http://127.0.0.1:7860/sdapi/v1/txt2img";
        string json = JsonUtility.ToJson(new
        {
            prompt = prompt,
            steps = 20,
            width = 512,
            height = 512
        });

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + www.error);
            }
            else
            {
                // APIから返ってくるBase64画像をUnityで表示
                var response = JsonUtility.FromJson<ResponseData>(www.downloadHandler.text);
                byte[] imageBytes = System.Convert.FromBase64String(response.images[0]);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(imageBytes);
                outputImage.texture = tex;
            }
        }
    }

    [System.Serializable]
    private class ResponseData
    {
        public string[] images;
    }
}
