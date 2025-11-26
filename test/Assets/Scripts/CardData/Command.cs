using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCommand", menuName = "Card/コマンド", order = 3)]
public class Command : ScriptableObject
{
    public string CommandName;      // 表示名
    public AttackCategory category; // カテゴリ
    public int power;               // 威力

    public int targetNum;           // 対象数
    public bool IsSelf;             // 自身に対する効果かどうか

    public CommandEffect Effect;    // 付帯効果
}