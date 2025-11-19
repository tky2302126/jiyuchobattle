using UnityEngine;
using TMPro;

public class CardTooltip : MonoBehaviour
{
    public static CardTooltip Instance;

    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;

    private void Awake()
    {
        Instance = this;
        tooltipPanel.SetActive(false);
    }

    public void Show(string text, Vector3 position)
    {
        tooltipPanel.SetActive(true);
        tooltipText.text = text;
    }

    public void Hide()
    {
        tooltipPanel.SetActive(false);
    }
}
