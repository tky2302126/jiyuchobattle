using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Linq;
using static SDController;


public class MonsterCardGenerator : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject fusionEffect;

    private TextToImage _t2I = new TextToImage();

    /// <summary>
    /// 共通のカード生成演出
    /// </summary>
    public async UniTask<GameObject> CreateMonsterCardAsync(
        List<GameObject> sourceCards,
        Transform spawnPoint,
        bool isPlayer)
    {
        // --- 生成可能か判定する処理 ---
        bool hasNounCard = sourceCards.Exists(c =>
        {
            var data = c.GetComponent<CardPresenter>()?.cardData;
            return data is NounData;
        });

        List<CardDataBase> fieldCards = new List<CardDataBase>();
        foreach (var cardObj in sourceCards)
        {
            var data = cardObj.GetComponent<CardPresenter>()?.cardData;
            if (data != null)
                fieldCards.Add(data);
        }

        if (fieldCards.Count == 0)
        {
            Debug.LogWarning("フィールドにカードがありません。");
            return null;
        }

        if (!hasNounCard)
        {
            Debug.LogWarning("名詞カードが含まれていません。生成できません。");
            return null;
        }

        // --- カードを中央に集めて裏返す ---
        await MoveAndFlipCards(sourceCards, spawnPoint);

        // --- 光エフェクト ---
        await PlayFusionEffect(spawnPoint.position);

        // --- モンスターカードクラス生成---
        var monsterCard = CombineCards(fieldCards);
        
        if (monsterCard == null)
        {
            Debug.LogError("モンスターカードの生成に失敗しました。");
            return null;
        }

        // --- 元カード削除 ---
        DestroyCards(sourceCards);

        // --- 新しいカードを生成 ---
        GameObject newCard = await SpawnCard(monsterCard, fieldCards, spawnPoint, isPlayer);

        return newCard;
    }

    private async UniTask MoveAndFlipCards(List<GameObject> cards, Transform spawnPoint)
    {
        float duration = 0.3f;
        var moveTasks = cards.Select(c =>
            c.transform.DOMove(spawnPoint.position, duration).SetEase(Ease.InOutSine).AsyncWaitForCompletion().AsUniTask()
        ).ToList();

        await UniTask.WhenAll(moveTasks);

        var flipTasks = new List<UniTask>();
        foreach (var c in cards)
        {
            flipTasks.Add(c.transform.DORotate(new Vector3(0, 180, 0), 0.4f).AsyncWaitForCompletion().AsUniTask());
            flipTasks.Add(c.transform.DOMoveZ(c.transform.position.z + 0.2f, 0.4f).AsyncWaitForCompletion().AsUniTask());
        }
        await UniTask.WhenAll(flipTasks);
    }

    private async UniTask PlayFusionEffect(Vector3 position)
    {
        // 🌟 光用の球体を生成
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.transform.position = position;
        flash.transform.localScale = Vector3.zero;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = new Color(1f, 1f, 0.5f, 0.8f); // 黄色っぽい光
        flash.GetComponent<MeshRenderer>().material = mat;

        // 拡大＆フェードアウト
        var seq = DOTween.Sequence();
        seq.Append(flash.transform.DOScale(1.5f, 0.3f).SetEase(Ease.OutQuad));
        seq.Join(mat.DOFade(0, 0.5f));

        await seq.AsyncWaitForCompletion();

        Destroy(flash);

        await PlayMagicCircleEffect(position);
    }

    private async UniTask PlayMagicCircleEffect(Vector3 position)
    {
        if (fusionEffect == null)
        {
            Debug.LogWarning("FusionEffect が設定されていません");
            return;
        }

        // 魔法陣を生成
        GameObject effect = Instantiate(fusionEffect, position, Quaternion.identity);

        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }

        // 粒子が吸い込まれるようにスケール縮小
        float duration = 0.5f;
        Vector3 startScale = effect.transform.localScale;
        Vector3 endScale = Vector3.zero;

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, time / duration);
            effect.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        Destroy(effect);
    }

    private MonsterCard CombineCards(List<CardDataBase> cards)
    {
        // 新しいモンスターカードを作成
        MonsterCard newCard = ScriptableObject.CreateInstance<MonsterCard>();
        newCard.sourceCards = new List<CardDataBase>(cards);

        // --- 名前生成ロジック例 ---
        string name = "";
        string nounPart = "";
        foreach (var card in cards)
        {
            if (card is VerbData verb) name += verb.CardName;
            if (card is AdjectiveData adj) name += adj.CardName; // 形容詞
            if (card is NounData noun) nounPart = noun.CardName; // 名詞は後で追加  // 名詞
        }

        // 名詞を末尾に追加
        name += nounPart;
        newCard.CardName = name;

        // --- ステータス合成 ---
        int totalHP = 0, totalAttack = 0, totalDefense = 0;
        float totalEvasion = 0; List<Command> Skills = new List<Command>();
        float attackInterval = 0; CardDataBase target = null; float multiplier = 0;
        foreach (var card in cards)
        {
            switch (card)
            {
                case NounData n:
                    totalHP += n.HP;
                    totalAttack += n.attack;
                    totalDefense += n.defense;
                    totalEvasion += n.evasion;
                    Skills.AddRange(n.skills);
                    attackInterval = n.attackInterval;
                    break;
                case VerbData v:
                    Skills.Add(v.skillToAdd);
                    break;
                case AdjectiveData a:
                    totalHP += a.hpBonus;
                    totalAttack += a.attackBonus;
                    totalDefense += a.defenseBonus;
                    totalEvasion += a.evasionBonus;
                    if (a.targetCard != null)
                    {
                        target = a.targetCard;
                        multiplier = a.specialMultiplier;
                    }
                    break;
            }
        }

        newCard.HP = totalHP;
        newCard.Attack = totalAttack;
        newCard.Defense = totalDefense;
        newCard.Evasion = totalEvasion;
        newCard.Skills = new List<Command>(Skills);
        newCard.AttackInterval = attackInterval;
        newCard.targetCard = target;
        newCard.specialMultiplier = multiplier;

        return newCard;
    }
    private void DestroyCards(List<GameObject> cards)
    {
        foreach (var card in cards)
            Destroy(card);
        Debug.Log("融合前カードを削除しました。");
    }

    private async UniTask<GameObject> SpawnCard(
        MonsterCard monsterCard,
        List<CardDataBase> sourceData,
        Transform spawnPoint,
        bool isPlayer)
    {
        Vector3 startOffset = isPlayer ? new Vector3(0, 0, -2f) : new Vector3(0, 0, 2f);
        Quaternion startRot = isPlayer ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 180, 0);
        Quaternion endRot = isPlayer ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 0, 180);

        GameObject newCard = Instantiate(cardPrefab, spawnPoint.position + startOffset, startRot);

        float duration = 0.5f;
        await newCard.transform.DOMove(spawnPoint.position, duration).SetEase(Ease.OutQuad).AsyncWaitForCompletion();

        // --- 情報設定 ---
        var presenter = newCard.GetComponent<CardPresenter>();
        if (presenter != null) presenter.cardData = monsterCard;

        var textSetter = newCard.GetComponent<CardTextSetter>();
        if (textSetter != null) textSetter.SetMonsterCardText(monsterCard);

        var renderer = newCard.GetComponentInChildren<MeshRenderer>();
        await SetIllust(renderer, sourceData);

        // --- 回転（表向きに） ---
        await newCard.transform.DORotateQuaternion(endRot, 0.5f).SetEase(Ease.InOutSine).AsyncWaitForCompletion();

        return newCard;
    }

    private async UniTask SetIllust(MeshRenderer mr, List<CardDataBase> cards)
    {
        List<string> adjectives = new();
        List<string> verbs = new();
        string noun = "";

        foreach (var card in cards)
        {
            if (card is AdjectiveData adj) adjectives.Add(adj.name);
            else if (card is VerbData v) verbs.Add(v.name);
            else if (card is NounData n) noun += n.name + " ";
        }

        string prompt = $"{string.Join(" ", adjectives)} {string.Join(" ", verbs)} {noun}".Trim().ToLower();
        prompt += ", fantasy monster, concept art, digital painting";

        RequestData req = new() { prompt = prompt };
        var json = JsonUtility.ToJson(req);
        Texture2D tex = await _t2I.PostT2I(json);
        if (tex != null)
        {
            mr.material.mainTexture = tex;
            Debug.Log("イラスト生成成功");
        }
        else Debug.LogError("イラスト生成失敗");
    }
}
