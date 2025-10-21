using System;
using System.Collections.Generic;
using UnityEngine;

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
    public abstract void Execute(GameObject caster, GameObject target);
}

// 例：攻撃アクション
[CreateAssetMenu(fileName = "AttackAction", menuName = "Card/Action/Attack")]
public class AttackAction : CommandAction
{
    public int damage;
    public override void Execute(GameObject caster, GameObject target)
    {
        Debug.Log($"{caster.name} attacks {target.name} for {damage} damage!");
    }
}

// 今は空のCommandクラスを仮で作成
[CreateAssetMenu(fileName = "NewCommand", menuName = "Card/コマンド", order = 3)]
public class Command : ScriptableObject
{
    public string commandName;
    public AttackCategory category;
    public CommandAction action;

    public void Execute(GameObject caster, GameObject target) // 引数は仮置き 
    {
        action?.Execute(caster, target);
    }
}