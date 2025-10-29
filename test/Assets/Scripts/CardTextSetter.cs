using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardTextSetter : MonoBehaviour
{
    [SerializeField] TextMeshPro title;
    [SerializeField] TextMeshPro description;
    private Camera mainCamera;

    private bool isHovered = false;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                if (!isHovered)
                {
                    isHovered = true;
                    CardTooltip.Instance.Show(description.text, Input.mousePosition);
                }
                return; // 他のカードを誤ってHideしないようにここでリターン
            }
        }

        if (isHovered)
        {
            isHovered = false;
            CardTooltip.Instance.Hide();
        }
    }
    // Start is called before the first frame update
    public void SetBaseCardText(CardDataBase cardData) 
    {
        if(title == null) 
        {
            Debug.LogWarning("title is null reference");
            return;
        }

        if(description == null) 
        {
            Debug.LogWarning("description is null reference");
            return;
        }
        switch (cardData.category) 
        {
            case cardCategory.Noun:
                var nData = (NounData)cardData;
                title.text = nData.CardName;
                description.text = $"HP: {nData.HP}\n" +
                                   $"攻撃力: {nData.attack}\n" +
                                   $"防御力: {nData.defense}\n" +
                                   $"回避: {nData.evasion * 100f:F1}% \n";
                break;
            
            case cardCategory.Verb:
                var vData = (VerbData)cardData;
                title.text = vData.CardName;
                description.text = $"{vData.CardName}をカードに付与する";
                if (vData.modifyBehavior) 
                {
                    description.text += "さらに、このコマンドを選びやすくなる";
                }
                break;
            
            case cardCategory.Adj:
                var aData = (AdjectiveData)cardData;
                title.text = aData.CardName;
                description.text = "以下の補正を与える\n"+
                                   $"HP: {aData.hpBonus}\n" +
                                   $"攻撃力: {aData.attackBonus}\n" +
                                   $"防御力: {aData.defenseBonus}\n" +
                                   $"回避: {aData.evasionBonus * 100f:F1}% \n";

                if(aData.targetCard != null) 
                {
                    description.text = $"さらに、{cardData.name}を持つカードに有効";
                }
                break;

            default:
                break;
        }
    }

    public void SetMonsterCardText(MonsterCard monsterCard) 
    {
        if (monsterCard == null)
        {
            Debug.LogWarning("monsterCard is null");
            return;
        }

        if (title == null || description == null)
        {
            Debug.LogWarning("title or description is null reference");
            return;
        }

        title.text = monsterCard.CardName;

        // HP/攻撃力/防御力/回避率を表示
        description.text = $"HP: {monsterCard.HP}\n" +
                           $"攻撃力: {monsterCard.Attack}\n" +
                           $"防御力: {monsterCard.Defense}\n" +
                           $"回避: {monsterCard.Evasion * 100f:F1}%\n";

        // スキル情報も列挙（Commandクラスの内容に応じて調整）
        // if (monsterCard.Skills != null && monsterCard.Skills.Count > 0)
        // {
        //     description.text += "\nスキル:\n";
        //     foreach (var skill in monsterCard.Skills)
        //     {
        //         description.text += $"- {skill.name}\n"; // Commandクラスにnameがある前提
        //     }
        // }

        // 特殊効果があれば表示
        if (monsterCard.targetCard != null)
        {
            description.text += $"\n特効: {monsterCard.targetCard.CardName} に対して {monsterCard.specialMultiplier} 倍";
        }
    }
}
