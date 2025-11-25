using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uPalette.Generated;

[RequireComponent(typeof(SpriteRenderer))]
public class CardColorSetter : SpriteColorChanger
{
    public void ApplyColorByCardType(CardDataBase cardData)
    {
        if (cardData == null) return;

        if (cardData is NounData)
            SetColorByEntryId(ColorEntry.Noun.ToEntryId());
        else if (cardData is VerbData)
            SetColorByEntryId(ColorEntry.Verb.ToEntryId());
        else if (cardData is AdjectiveData)
            SetColorByEntryId(ColorEntry.Adjective.ToEntryId());
        else
            SetColor(Color.green);
    }
}
