using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;

public class IdlePresentationController : MonoBehaviour
{
    public enum StepType
    {
        FadeUI,
        SlideInPanel,
        SlideOutPanel,
        PlayVideo,
        StopVideo,
        WaitForInput
    }

    [Serializable]
    public class PresentationStep
    {
        public StepType type;

        [Header("Fade")]
        public float fadeFrom = 1f;
        public float fadeTo = 0f;
        public float fadeDuration = 0.5f;

        [Header("Slide")]
        public float slideDuration = 0.4f;

        [Header("Video")]
        public bool loopVideo = true;
    }

    [Header("Timing")]
    [SerializeField] private float inactiveTime = 5f;

    [Header("Sequences")]
    [SerializeField] private List<PresentationStep> startSequence = new();
    [SerializeField] private List<PresentationStep> endSequence = new();

    [Header("References")]
    [SerializeField] private CanvasGroup uiCanvas;
    [SerializeField] private VideoPanelController panel;
    [SerializeField] private VideoPlayer videoPlayer;

    private float inactiveTimer;
    private bool isPlaying;
    private bool waitingForInput;
    private CancellationTokenSource cts;

    void OnEnable()
    {
        cts = new CancellationTokenSource();
    }

    void OnDisable()
    {
        cts?.Cancel();
        cts?.Dispose();
    }

    void Update()
    {
        if (DetectUserInput())
        {
            HandleUserInput();
        }

        if (isPlaying) return;

        inactiveTimer += Time.deltaTime;

        if (inactiveTimer >= inactiveTime)
        {
            inactiveTimer = 0f;
            PlayStartSequence().Forget();
        }
    }

    #region Input

    private bool DetectUserInput()
    {
        return Input.anyKeyDown
            || Input.GetMouseButtonDown(0)
            || Input.GetMouseButtonDown(1)
            || Input.mouseScrollDelta != Vector2.zero;
    }

    private void HandleUserInput()
    {
        inactiveTimer = 0f;

        if (waitingForInput)
        {
            waitingForInput = false;
        }
    }

    #endregion

    #region Sequence

    private async UniTaskVoid PlayStartSequence()
    {
        if (isPlaying) return;

        isPlaying = true;
        await PlaySequence(startSequence, cts.Token);
    }

    private async UniTask PlayEndSequence()
    {
        await PlaySequence(endSequence, cts.Token);
        isPlaying = false;
    }

    private async UniTask PlaySequence(List<PresentationStep> sequence, CancellationToken token)
    {
        foreach (var step in sequence)
        {
            if (token.IsCancellationRequested) return;

            switch (step.type)
            {
                case StepType.FadeUI:
                    uiCanvas.alpha = step.fadeFrom;
                    await uiCanvas
                        .DOFade(step.fadeTo, step.fadeDuration)
                        .SetEase(Ease.OutCubic)
                        .AsyncWaitForCompletion();

                    break;

                case StepType.SlideInPanel:
                    panel.Show(step.slideDuration);
                    await UniTask.Delay(TimeSpan.FromSeconds(step.slideDuration), cancellationToken: token);
                    break;

                case StepType.SlideOutPanel:
                    panel.Hide(step.slideDuration);
                    await UniTask.Delay(TimeSpan.FromSeconds(step.slideDuration), cancellationToken: token);
                    break;

                case StepType.PlayVideo:
                    videoPlayer.isLooping = step.loopVideo;
                    videoPlayer.Play();
                    break;

                case StepType.StopVideo:
                    videoPlayer.Stop();
                    break;

                case StepType.WaitForInput:
                    waitingForInput = true;
                    await UniTask.WaitUntil(() => !waitingForInput, cancellationToken: token);
                    await PlayEndSequence();
                    return;
            }
        }
    }

    #endregion
}
