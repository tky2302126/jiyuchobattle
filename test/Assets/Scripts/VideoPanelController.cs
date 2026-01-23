using UnityEngine;
using DG.Tweening;

public class VideoPanelController : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private Vector2 shownPos;
    [SerializeField] private Vector2 hiddenPos;

    private Tween currentTween;

    public void Show(float duration)
    {
        currentTween?.Kill();
        currentTween = panel
            .DOAnchorPos(shownPos, duration)
            .SetEase(Ease.OutCubic);
    }

    public void Hide(float duration)
    {
        currentTween?.Kill();
        currentTween = panel
            .DOAnchorPos(hiddenPos, duration)
            .SetEase(Ease.InCubic);
    }
}
