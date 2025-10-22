using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.CullingGroup;

public enum cardCategory 
{
  None,
  Noun,
  Adj,
  Verb
};

// 全てのカードデータの基底クラス
public abstract class CardDataBase : ScriptableObject
{
    public cardCategory category;
    public abstract string CardName { get; set; }
}

[Flags]
public enum AttackCategory
{
    [InspectorName("なし")]
    None = 0,
    [InspectorName("突き")]
    Stab = 1 << 0,
    [InspectorName("打撃")]
    Strike = 1 << 1,
    [InspectorName("斬撃")]
    Slash = 1 << 2,
    [InspectorName("特殊")]
    Special = 1 << 3,
    [InspectorName("変化")]
    Transform = 1 << 4,
}

[CreateAssetMenu(fileName = "NewCommandAction", menuName = "Card/Action/BaseAction")]
public abstract class CommandAction : ScriptableObject
{
    public abstract void Execute(MonsterCard caster, MonsterCard target);
}

// 例：攻撃アクション
[CreateAssetMenu(fileName = "AttackAction", menuName = "Card/Action/Attack")]
public class AttackAction : CommandAction
{
    public int damage;
    public override void Execute(MonsterCard caster, MonsterCard target)
    {
        Debug.Log($"{caster.name} attacks {target.name} for {damage} damage!");
    }
}

// 今は空のCommandクラスを仮で作成
[CreateAssetMenu(fileName = "NewCommand", menuName = "Card/コマンド", order = 3)]
public class Command : ScriptableObject
{
    public string CommandName;      // 表示名
    public AttackCategory category; // カテゴリ
    public int power;               // 威力

    public int targetNum;           // 対象数

    public CommandEffect action;    // 付帯効果
}

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
    [Range(-6, 6)] public int changeAmount;

    [Header("持続管理")]
    public EffectDurationType durationType;
    public int durationValue; // 例：nターン、n回行動など
}

public enum StatType
{
    Attack,
    Defense,
    Evasion
}

public enum EffectDurationType
{
    Permanent,      // 永続
    UntilNextAttack, // 次の攻撃まで
    TurnCount,      // nターンまで
    ActionCount     // n回行動まで
}

[Flags]
public enum StatusCondition
{
    None = 0,
    Paralyze = 1 << 0,  // 麻痺
    Burn = 1 << 1,  // 火傷
    Poison = 1 << 2,  // 毒
    Sleep = 1 << 3,  // 眠り
    Freeze = 1 << 4,  // 凍り
    Confuse = 1 << 5   // 混乱
}