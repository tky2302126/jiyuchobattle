using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Card/形容詞", order = 1)]
public class AdjectiveData : CardDataBase
{
    [Header("形容詞名")]
    [SerializeField] private string cardName;
    public override string CardName
    { get => cardName; set => cardName = value; }

    [Header("補正値")]
    public int hpBonus;
    public int attackBonus;
    public int defenseBonus;
    [Range(-1f, 1f)]
    public float evasionBonus;
    public float attackIntervalBonus;

    [Header("特攻対象（任意）")]
    public CardDataBase targetCard; // 特定のカードに対して特攻
    public float specialMultiplier = 1f; // 特攻倍率（例: 2.0 = 2倍ダメージ）
}
