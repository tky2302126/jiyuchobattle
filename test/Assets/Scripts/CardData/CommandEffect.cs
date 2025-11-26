using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCommand", menuName = "Card/コマンドエフェクト", order = 4)]
public class CommandEffect : ScriptableObject
{
    [Header("自分へのパラメータ変化")]
    public StatChange[] selfChanges;

    [Header("相手へのパラメータ変化")]
    public StatChange[] targetChanges;
}

[System.Serializable]
public class StatChange
{
    public StatType statType;
    public int changeAmount;

    [Header("持続管理")]
    public EffectDurationType durationType;
    public int durationValue; // 例：nターン、n回行動など
}

public enum StatType
{
    Attack,
    Defense,
    Evasion,
    HP,
    Duplicate,
    Paralyze,
    Burn,
    Poison,
    Sleep,
    Freeze,
    Confuse,
    Strike,
    Critical,
}

public enum EffectDurationType
{
    Permanent,      // 永続
    UntilNextAttack, // 次の攻撃まで
    ActionCount     // n回行動まで
}

[Flags]
public enum MonsterCondition
{
    None = 0,
    Paralyze = 1 << 0,  // 麻痺
    Burn = 1 << 1,  // 火傷
    Poison = 1 << 2,  // 毒
    Sleep = 1 << 3,  // 眠り
    Freeze = 1 << 4,  // 凍り
    Confuse = 1 << 5,   // 混乱
    Strike = 1 << 6,    // 必中
    Duplicate = 1 << 7, // 複製フラグ
}