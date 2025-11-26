using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;

    [SerializeField] private CanvasGroup canvasGroup;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f; // 初期は透明
    }

    public async UniTask FadeOut(float duration = 0.5f, CancellationToken cancellationToken = default)
    {
        if (canvasGroup == null) return;
        Debug.Log("フェードアウトするよ");
        float t = 0f;
        while (t < duration)
        {
            if (canvasGroup == null) return;

            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / duration);

            // 破棄時に自動中断
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        Debug.Log("フェードアウトおわたよ");
    }

    public async UniTask FadeIn(float duration = 0.5f, CancellationToken cancellationToken = default)
    {
        Debug.Log("フェードインするよ");
        if (canvasGroup == null) return;

        float t = 0f;
        while (t < duration)
        {
            if (canvasGroup == null) return;

            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / duration);

            // 破棄時に自動中断
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        Debug.Log("フェードインおわたよ");
    }

    public bool CanvasGroupExists => canvasGroup != null;
}
