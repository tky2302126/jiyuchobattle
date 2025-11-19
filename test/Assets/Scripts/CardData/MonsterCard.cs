using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterCard : CardDataBase
{
    [Header("合成元カード")]
    public List<CardDataBase> sourceCards = new List<CardDataBase>();

    [Header("ステータス")]
    public int HP;
    public int Attack;
    public int Defense;
    [Range(0f, 1f)]
    public float Evasion; //回避率 0~1の割合

    [Header("攻撃")]
    public List<Command> Skills = new List<Command>(); 
    public float AttackInterval; // 攻撃頻度（秒）
    public GameObject AttackEffect; //攻撃エフェクト

    [Header("特攻対象（任意）")]
    public CardDataBase targetCard; // 特定のカードに対して特攻
    public float specialMultiplier = 1f; // 特攻倍率（例: 2.0 = 2倍ダメージ）

    public override string CardName
    {
        get => cardName;
        set => cardName = value;
    }

    [SerializeField] private string cardName;

    public MonsterCard Clone()
    {
        var clone = (MonsterCard)this.MemberwiseClone();

        // --- List のディープコピー ---
        clone.sourceCards = new List<CardDataBase>(this.sourceCards);
        clone.Skills = new List<Command>(this.Skills);

        // --- 参照型はコピー or 共有どちらでも安全なように ---
        clone.targetCard = this.targetCard;
        clone.AttackEffect = this.AttackEffect;

        // cardName（string）はイミュータブルなのでそのままで OK

        return clone;
    }
}
