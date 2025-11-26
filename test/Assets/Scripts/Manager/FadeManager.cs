using UnityEngine;
using Cysharp.Threading.Tasks;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;

    [SerializeField] private CanvasGroup canvasGroup;

    void Awake()
    {
        Instance = this;
        canvasGroup.alpha = 0; // 最初は透明
    }

    public async UniTask FadeOut(float duration = 0.5f)
    {
        canvasGroup.alpha = 1f;
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / duration);
            await UniTask.Yield();
        }
        canvasGroup.alpha = 1f;
    }

    public async UniTask FadeIn(float duration = 0.5f)
    {
        canvasGroup.alpha = 1f;
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / duration);
            await UniTask.Yield();
        }
        canvasGroup.alpha = 0f;
    }
}
