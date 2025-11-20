using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class HPBarController : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Vector3 faceCameraOffset = Vector3.zero;
    private Camera mainCamera;
    private Canvas hpCanvas;
    private Transform parentObject;
    private SpriteRenderer sr;
    private TextMeshPro tmp;

    [Header("UI")]
    [SerializeField] private Image hpFillImage;

    [Header("グラデーション設定")]
    [SerializeField] private Gradient hpGradient;

    void Start()
    {
        Init();
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // カメラの方向を常に向く（ビルボード）
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }

        // 背面なら非表示
        if (parentObject != null)
        {
            Vector3 toCamera = (mainCamera.transform.position - parentObject.position).normalized;
            float dot = Vector3.Dot(parentObject.forward, toCamera);
            bool isFacingFront = dot < 0f;

            hpCanvas.enabled = isFacingFront;



            //// CanvasGroupでUI要素を制御
            //CanvasGroup cg = hpCanvas.GetComponent<CanvasGroup>();
            //if (cg == null)
            //    cg = hpCanvas.gameObject.AddComponent<CanvasGroup>();

            //cg.alpha = isFacingFront ? 1f : 0f;
            //cg.interactable = isFacingFront;
            //cg.blocksRaycasts = isFacingFront;

            // 子にあるSpriteRendererを全て非表示/表示
            sr.enabled = isFacingFront;
            tmp.enabled = isFacingFront;
        }
    }

    public void Init() 
    {
        mainCamera = Camera.main;
        hpCanvas = GetComponentInChildren<Canvas>();
        parentObject = transform.parent; // モンスター本体を想定
        sr = hpCanvas.GetComponentInChildren<SpriteRenderer>();
        tmp = hpCanvas.GetComponentInChildren<TextMeshPro>();
    }

    public void SetHP(float current, float max)
    {
        if (hpSlider == null) Debug.LogError("Slider が null です！");
        if (hpFillImage == null) Debug.LogError("fillImage が null です！");
        UpdateUI(current, max);
    }

    private void UpdateUI(float current, float max)
    {
        float ratio = (float)current / max;
        Debug.Log($"current: {current}, max : {max} ratio: {ratio}");
        hpSlider.value = ratio;
        hpFillImage.color = hpGradient.Evaluate(ratio);
        tmp.text = current.ToString();
    }
}
