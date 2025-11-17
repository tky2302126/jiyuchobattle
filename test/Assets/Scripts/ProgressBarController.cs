using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBarController : MonoBehaviour
{
    [Header("Progress UI")]
    [SerializeField] private Slider progressSlider;

    private Camera mainCamera;
    private Canvas progressCanvas;
    private Transform parentObject;

    /// <summary>
    /// ★ このプログレスバーを使うタスクの識別子
    /// </summary>
    public string TaskId { get; set; }

    void Start()
    {
        mainCamera = Camera.main;
        progressCanvas = GetComponentInChildren<Canvas>();

        // このUIをくっつける対象（例：モンスターやUI用オブジェクト）
        parentObject = transform.parent;
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        // ビルボード化
        transform.LookAt(
            transform.position + mainCamera.transform.rotation * Vector3.forward,
            mainCamera.transform.rotation * Vector3.up
        );

        // 正面 → 非表示
        // 背面 → 表示
        if (parentObject != null)
        {
            Vector3 toCamera = (mainCamera.transform.position - parentObject.position).normalized;
            float dot = Vector3.Dot(parentObject.forward, toCamera);
            bool isBackSide = dot > 0f;  // ★HPBarの反転版

            progressCanvas.enabled = isBackSide;
        }
    }

    /// <summary>
    /// 進捗を 0〜1 で更新する
    /// </summary>
    public void SetProgress(float progress)
    {
        progressSlider.value = progress;
    }
}
