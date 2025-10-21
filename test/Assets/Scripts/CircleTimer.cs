using UnityEngine;
using UnityEngine.UI;
using uPalette.Generated;
using uPalette.Runtime.Core;
using uPalette.Runtime.Core.Model;
using uPalette.Runtime.Core.Synchronizer.Gradient;

public class CircleTimer : MonoBehaviour
{
    [Header("タイマー設定")]
    public float limitTime = 30f; // 制限時間（秒）
    private float currentTime;
    private bool isRunning = false;

    [Header("UI")]
    public Image timerImage; // 円形のImage

    [Header("グラデーション設定")]
    public Gradient gradient; // Unity標準のGradient

    void Start()
    {
        currentTime = limitTime;

        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0].color = Color.green;
        colorKeys[0].time = 0f;   // 開始時は緑
        colorKeys[1].color = Color.yellow;
        colorKeys[1].time = 0.5f; // 中間は黄色
        colorKeys[2].color = Color.red;
        colorKeys[2].time = 1f;   // 終了時は赤

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0].alpha = 1f;
        alphaKeys[0].time = 0f;
        alphaKeys[1].alpha = 1f;
        alphaKeys[1].time = 1f;

        gradient.SetKeys(colorKeys, alphaKeys);

#if UNITY_EDITOR
        // isRunning = true;
#endif
    }

    void Update()
    {
        if (!isRunning) return;
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            float fill = Mathf.Clamp01(currentTime / limitTime);
            timerImage.fillAmount = fill;

            timerImage.color = gradient.Evaluate(1 - fill);
        }
        else 
        {
            isRunning = false;
        }
    }

    // 外部からタイマーを開始する
    public void StartTimer()
    {
        currentTime = limitTime;
        isRunning = true;
    }

    // 外部からタイマーを停止する
    public void StopTimer()
    {
        isRunning = false;
    }
}
