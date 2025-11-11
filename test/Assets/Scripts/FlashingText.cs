using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FlashingText : MonoBehaviour
{
    public TMP_Text promptText; // Inspectorでセット
    public float fadeDuration = 1f; // フェードイン・アウトの時間
    public float waitDuration = 0.5f; // フェードの間隔

    private void Start()
    {
        StartCoroutine(FadeLoop());
    }

    private IEnumerator FadeLoop()
    {
        while (true)
        {
            // フェードイン
            yield return StartCoroutine(FadeText(0f, 1f));
            // 少し表示したまま待つ
            yield return new WaitForSeconds(waitDuration);
            // フェードアウト
            yield return StartCoroutine(FadeText(1f, 0f));
            yield return new WaitForSeconds(waitDuration);
        }
    }

    private IEnumerator FadeText(float startAlpha, float endAlpha)
    {
        float elapsed = 0f;
        Color color = promptText.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            promptText.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        promptText.color = new Color(color.r, color.g, color.b, endAlpha);
    }
}
