using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SDUI : MonoBehaviour
{
    public InputField promptInput;
    public Button generateButton;

    void Start()
    {
        generateButton.onClick.AddListener(() =>
        {
            string prompt = promptInput.text;
            Debug.Log("入力テキスト: " + prompt);
            // ここで画像生成を呼ぶ
        });
    }
}
