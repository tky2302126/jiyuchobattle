using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Card/名詞", order = 0)]
public class NounData : CardDataBase
{
    [Header("基本ステータス")]
    [SerializeField] private string cardName;
    public override string CardName 
    { get => cardName; set => cardName = value; }
    public int HP;
    public int attack;
    public int defense;
    [Range(0f, 1f)]
    public float evasion; //回避率 0~1の割合

    [Header("攻撃")]
    public Command[] skills = new Command[3]; // 今後Commandクラスに置き換え
    public float attackInterval; // 攻撃頻度（秒）
}
