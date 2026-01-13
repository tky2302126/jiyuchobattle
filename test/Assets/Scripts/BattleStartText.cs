using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BattleStartText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cpuCardNameText;
    [SerializeField] private TextMeshProUGUI playerCardNameText;
    [SerializeField] private GameObject VSsprite;

    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float stayDuration = 1.0f;

    private void Start()
    {
        // Start時点で非表示
        SetAlpha(0f);

        cpuCardNameText.gameObject.SetActive(false);
        playerCardNameText.gameObject.SetActive(false);
        VSsprite.SetActive(false);
    }
    public void SetPlayerMonsterName(string name) 
    {
        playerCardNameText.text = "";
        playerCardNameText.text = name;
    }

    public void SetCPUMonsterName(string name)
    {
        cpuCardNameText.text = "";
        cpuCardNameText.text = name;
    }

    public void StartCutIn() 
    {
        StartCoroutine(CutInCoroutine());
    }

    private IEnumerator CutInCoroutine()
    {
        // 初期化（透明）
        SetAlpha(0f);

        cpuCardNameText.gameObject.SetActive(true);
        playerCardNameText.gameObject.SetActive(true);
        VSsprite.SetActive(true);

        // フェードイン
        yield return Fade(0f, 1f, fadeDuration);

        // 表示維持
        yield return new WaitForSeconds(stayDuration);

        // フェードアウト
        yield return FadeOut();
    }

    private IEnumerator FadeOut()
    {
        yield return Fade(1f, 0f, fadeDuration);

        cpuCardNameText.gameObject.SetActive(false);
        playerCardNameText.gameObject.SetActive(false);
        VSsprite.SetActive(false);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            float alpha = Mathf.Lerp(from, to, time / duration);
            SetAlpha(alpha);
            time += Time.deltaTime;
            yield return null;
        }

        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        cpuCardNameText.alpha = alpha;
        playerCardNameText.alpha = alpha;

        var vsImage = VSsprite.GetComponent<CanvasGroup>();
        if (vsImage == null)
        {
            vsImage = VSsprite.AddComponent<CanvasGroup>();
        }
        vsImage.alpha = alpha;
    }
}
