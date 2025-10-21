using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SDController : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;

    private TextToImage _t2I = new TextToImage();

    [System.Serializable]
    public class RequestData
    {
        public string prompt;
    }

    void Start()
    {
        sendButton.onClick.AddListener(() =>
        {
            SendPrompt(inputField.text);
        });
    }

    async void SendPrompt(string prompt)
    {
        // Jsonに変換
        RequestData requestData = new RequestData();
        requestData.prompt = prompt;
        var json = JsonUtility.ToJson(requestData);

        // リクエスト
        var result = await _t2I.PostT2I(json);

        // Texture2DからSpriteに変換
        image.sprite = Sprite.Create(result, new Rect(0, 0, result.width, result.height), Vector2.zero);
    }
}
