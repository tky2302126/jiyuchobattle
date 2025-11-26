using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleToMenu : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform titleCameraPos;
    [SerializeField] private Transform menuCameraPos;
    [SerializeField] private float cameraMoveDuration = 1.5f;

    [Header("UI Settings")]
    [SerializeField] private GameObject titlePromptText; // "Press Any Key"
    [SerializeField] private GameObject menuUI;          // メニュー画面のUI
    [SerializeField] private RectTransform titleLogo;    // タイトルロゴ
    [SerializeField] private Vector2 logoTargetPosition; // 最終位置（画面上部）

    private bool isTransitioning = false;
    private Vector2 logoStartPosition;

    async void Start()
    {
        if (FadeManager.Instance != null)
        {
            // 自分の破棄時に自動キャンセルされるようにする
            await FadeManager.Instance.FadeIn(2,cancellationToken: this.GetCancellationTokenOnDestroy());
        }
        // 初期状態：タイトル用カメラ位置とUI
        mainCamera.transform.position = titleCameraPos.position;
        mainCamera.transform.rotation = titleCameraPos.rotation;

        titlePromptText.SetActive(true);
        menuUI.SetActive(false);
        logoStartPosition = titleLogo.anchoredPosition;
    }

    void Update()
    {
        if (!isTransitioning && Input.anyKeyDown)
        {
            MoveCameraToMenu();
        }
    }

    private void MoveCameraToMenu()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        // タイトルUI非表示
        titlePromptText.SetActive(false);

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        // カメラ移動
        mainCamera.transform.DOMove(menuCameraPos.position, cameraMoveDuration).SetEase(Ease.InOutSine);
        mainCamera.transform.DORotateQuaternion(menuCameraPos.rotation, cameraMoveDuration).SetEase(Ease.InOutSine);

        // タイトルロゴ移動
        titleLogo.DOAnchorPos(logoTargetPosition, cameraMoveDuration).SetEase(Ease.OutCubic)
                 .OnComplete(() =>
                 {
                     // アニメ完了後にメニューUI表示
                     menuUI.SetActive(true);
                     isTransitioning = false;
                 });

        // 最終位置調整
        // mainCamera.transform.position = menuCameraPos.position;
        // mainCamera.transform.rotation = menuCameraPos.rotation;
    }
}
