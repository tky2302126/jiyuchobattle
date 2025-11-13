using UnityEngine;
using System.Collections;

public class SpriteAnimator : MonoBehaviour
{
    public enum PlayMode
    {
        Loop,         // 無限ループ
        Once,         // 1回だけ
        CustomCount,  // 指定回数
        CustomTime    // 指定時間だけ再生
    }

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private float frameTime = 0.1f;
    [SerializeField] private PlayMode playMode = PlayMode.Loop;
    [SerializeField] private int customLoopCount = 1; // CustomCountの回数
    [SerializeField] private float customPlayTime = 1f; // CustomTimeでの再生時間（秒）

    private Coroutine animationCoroutine;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        Play();
    }

    public void Play()
    {
        Stop(); // 既存アニメーションを止める
        animationCoroutine = StartCoroutine(Animate());
    }

    public void Stop()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    private IEnumerator Animate()
    {
        if (sprites == null || sprites.Length == 0) yield break;

        int currentIndex = 0;
        float elapsedTime = 0f;

        while (true)
        {
            spriteRenderer.sprite = sprites[currentIndex];
            yield return new WaitForSeconds(frameTime);

            currentIndex = (currentIndex + 1) % sprites.Length;
            elapsedTime += frameTime;

            if (playMode == PlayMode.Once && currentIndex == 0)
            {
                // 1回再生したら終了
                break;
            }
            else if (playMode == PlayMode.CustomTime && elapsedTime >= customPlayTime)
            {
                // 指定時間再生したら終了
                break;
            }
            // Loopは制限なし
        }

        if (playMode != PlayMode.Loop)
        {
            Destroy(gameObject); // 再生終了後に自身を破棄
        }
    }
}
