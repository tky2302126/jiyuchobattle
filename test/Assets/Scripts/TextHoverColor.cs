using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro; // ← 必須

public class TextHoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI targetText; // TextMeshProUGUIなら型を変える
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    [Header("Scene Settings")]
    [SerializeField] private MySceneManager.SceneTag sceneTag;

    private static MySceneManager.MySceneManager cachedSceneManager;

    private void Awake()
    {
        // Text自動取得
        if (targetText == null)
            targetText = GetComponent<TextMeshProUGUI>();

        // SceneManager をキャッシュ（まだない場合のみ検索）
        if (cachedSceneManager == null)
            cachedSceneManager = FindObjectOfType<MySceneManager.MySceneManager>();

        if (cachedSceneManager == null)
            Debug.LogWarning("MySceneManager がシーンに存在しません！");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetText != null)
            targetText.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetText != null)
            targetText.color = normalColor;
    }

    // クリック時
    public void OnPointerClick(PointerEventData eventData)
    {
        cachedSceneManager.LoadMain();
    }
}
