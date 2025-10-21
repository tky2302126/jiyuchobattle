using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Card/動詞", order = 2)]
public class VerbData : CardDataBase
{
    [Header("動詞名")]
    [SerializeField] private string cardName;
    public override string CardName
    { get => cardName; set => cardName = value; }

    [Header("抽選重みフラグ")]
    public bool modifyBehavior = false; // 行動を変更するか

    [Header("追加する技")]
    public Command skillToAdd;          // 技を追加する場合


    // [Header("対象カード（任意）")]
    // public NounData targetNoun;        // どの名詞カードに効果を付与するか
}
