using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using static SDController;

public class ReadyButton : MonoBehaviour
{
    [SerializeField] private Drag3DObject dragmanager;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject fusionEffect;
    [SerializeField] private Material holoMat;
    [SerializeField] private MonsterCardGenerator cardGenerator;

    private TextToImage _t2I = new TextToImage();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClick() 
    {
        List<CardDataBase> fieldCards = new List<CardDataBase>();
        cardGenerator.CreateMonsterCardAsync(
            dragmanager.CardsInFieldSlot,
            spawnPoint,
            isPlayer: true
            ).Forget();
        // OnClickAsync().Forget();
    }
    public async UniTask OnClickAsync() 
    {
        Debug.Log("ボタンが押されたよ");
        bool hasNounCard = dragmanager.CardsInFieldSlot.Exists(c =>
        {
            var data = c.GetComponent<CardPresenter>()?.cardData;
            return data is NounData;
        });

        List<CardDataBase> fieldCards = new List<CardDataBase>();
        foreach (var cardObj in dragmanager.CardsInFieldSlot)
        {
            var data = cardObj.GetComponent<CardPresenter>()?.cardData;
            if (data != null)
                fieldCards.Add(data);
        }

        if (fieldCards.Count == 0)
        {
            Debug.LogWarning("フィールドにカードがありません。");
            return;
        }

        if (!hasNounCard)
        {
            Debug.LogWarning("名詞カードが含まれていません。生成できません。");
            return;
        }

        // カードを集める処理
        var duration = 0.2f;
        var cardTransforms = dragmanager.CardsInFieldSlot
                             .Select(c => c.transform)
                             .ToList();
        var moveTasks = new List<UniTask>();

        foreach (var t in cardTransforms)
        {
            var task = t.DOMove(spawnPoint.position, duration).SetEase(Ease.InOutSine);
            moveTasks.Add(task.AsyncWaitForCompletion().AsUniTask());
        }
        await UniTask.WhenAll(moveTasks);
        // 全てのカードを裏返し、少し下げる
        var flipTasks = new List<UniTask>();
        foreach (var card in cardTransforms)
        {
            // Y軸180度回転で裏面に
            var rotate = card.DORotate(new Vector3(0, 180f, 0), 0.4f);
            // 少し下げる
            var move = card.DOMoveZ(card.position.z + 0.2f, 0.4f);
            flipTasks.Add(rotate.AsyncWaitForCompletion().AsUniTask());
            flipTasks.Add(move.AsyncWaitForCompletion().AsUniTask());
        }

        await UniTask.WhenAll(flipTasks);

        // 🌟 ③ 光エフェクト演出
        await PlayFusionEffect(spawnPoint.position);

        // 🌟 ④ 元のカードをフェードアウト
        // List<UniTask> fadeTasks = new List<UniTask>();
        // foreach (var card in dragmanager.CardsInFieldSlot)
        // {
        //     var renderer = card.GetComponentInChildren<MeshRenderer>();
        //     if (renderer != null)
        //     {
        //         fadeTasks.Add(renderer.material.DOFade(0, 0.5f).AsyncWaitForCompletion().AsUniTask());
        //     }
        // }
        // await UniTask.WhenAll(fadeTasks);

        var monsterCard = CombineCards(fieldCards);

        if (monsterCard == null)
        {
            Debug.LogError("モンスターカードの生成に失敗しました。");
            return;
        }

        // 名詞カードがあるならカード生成処理を実行
       await SpawnCard(monsterCard, fieldCards);

        DestroyCards();
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
    private void DestroyCards() 
    {
        // ✅ ここで元のカードを削除
        foreach (var cardObj in dragmanager.CardsInFieldSlot)
        {
            if (cardObj != null)
                Destroy(cardObj);
        }

        // リストもクリア
        dragmanager.CardsInFieldSlot.Clear();

        Debug.Log("元のカードを削除しました。");
    }

    private async UniTask SpawnCard(MonsterCard monsterCard, List<CardDataBase> cards)
    {
        if (cardPrefab == null || spawnPoint == null)
        {
            Debug.LogError("cardPrefab または spawnPoint が設定されていません。");
            return;
        }

        // 上からカードをおろす
        var startPos = spawnPoint.position + new Vector3(0, 0, -2f);

        GameObject newCard = Instantiate(cardPrefab, startPos, Quaternion.Euler(0, 180f, 0));

        // カードを少し上に浮かせるなども可（演出強化）
        Vector3 endPos = spawnPoint.position;

        // 移動アニメーション
        float time = 0f;
        float appearDuration = 0.5f;
        while (time < appearDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, time / appearDuration);
            newCard.transform.position = Vector3.Lerp(startPos, endPos, t);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        newCard.transform.position = endPos;

        var mc = newCard.GetComponent<MeshController>();

        var cardTextSetter = newCard.GetComponent<CardTextSetter>();
        if (cardTextSetter != null)
        {
            cardTextSetter.SetMonsterCardText(monsterCard);
        }
        CardPresenter presenter = newCard.GetComponent<CardPresenter>();
        if (presenter != null)
        {
            presenter.cardData = monsterCard; // ← ここで色とテキストが更新される
        }

        var meshRenderer = newCard.GetComponentInChildren<MeshRenderer>();
        await SetIllust(meshRenderer, cards);

        mc.RevertToNormalFade();

        float duration = 0.5f;
        Quaternion startRot = newCard.transform.rotation;
        Quaternion endRot = Quaternion.Euler(0, 0, 0);
        float t2 = 0f;
        while (t2 < duration)
        {
            newCard.transform.rotation = Quaternion.Slerp(startRot, endRot, t2 / duration);
            t2 += Time.deltaTime;
            await UniTask.Yield();
        }
        newCard.transform.rotation = endRot;

        Debug.Log($"モンスターカード「{monsterCard.CardName}」を生成しました。");
    }

    private async UniTask SetIllust(MeshRenderer mr, List<CardDataBase> cards) 
    {
        List<string> adjectives = new();
        List<string> verbs = new();
        string noun = "";
        foreach (var card in cards)
        {
            if (card == null) continue;

            // ScriptableObject名（英語名として使用）
            string name = card.name?.Trim();
            if (string.IsNullOrEmpty(name)) continue;

            if (card is AdjectiveData) adjectives.Add(name);
            else if (card is VerbData) verbs.Add(name);
            if (card is NounData) noun += card.name + " ";
        }

        string prompt = string.Join(" ", adjectives)
                        + (verbs.Count > 0 ? " " + string.Join(" ", verbs) : "")
                        + noun;
        prompt = prompt.Trim().ToLower();
        prompt += $", fantasy style monster, highly detailed, digital painting, concept art, 4k";

        Debug.Log($"prompt is {prompt}");

        await SendPrompt(prompt, mr);

        //// 画像生成完了後にカードを表向きに回転させる
        //Transform cardTransform = mr.transform.parent; // MeshRenderer の親がカード本体と仮定
        //if (cardTransform != null)
        //{
        //    // 裏返しアニメーション（任意）
        //    float duration = 0.5f;
        //    Quaternion startRot = cardTransform.rotation;
        //    Quaternion endRot = Quaternion.Euler(0, 0, 0);
        //    float time = 0f;
        //    while (time < duration)
        //    {
        //        cardTransform.rotation = Quaternion.Slerp(startRot, endRot, time / duration);
        //        time += Time.deltaTime;
        //        await UniTask.Yield();
        //    }
        //    cardTransform.rotation = endRot;
        //}
    }

    // 画像生成リクエスト処理
    private async UniTask SendPrompt(string prompt, MeshRenderer mr)
    {
        RequestData requestData = new RequestData { prompt = prompt };
        var json = JsonUtility.ToJson(requestData);

        // _t2I.PostT2I が Texture2D を返す前提
        Texture2D result = await _t2I.PostT2I(json);
        if (result == null)
        {
            Debug.LogError("Image generation failed.");
            return;
        }

        var material = mr.material;
        material.mainTexture = result;
        Debug.Log("Illustration successfully generated and applied.");
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
            if (card is VerbData verb)     name += verb.CardName;
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
                    Skills.AddRange(n.skills) ;
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
                    if(a.targetCard != null) 
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

    private GameObject CreateCardHologram(GameObject card)
    {
        GameObject holo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        holo.transform.SetParent(card.transform);
        holo.transform.localPosition = Vector3.zero + Vector3.up * 0.1f; // カードの少し上
        holo.transform.localRotation = Quaternion.Euler(0, 0, 0);

        holo.GetComponent<MeshRenderer>().material = holoMat;

        Destroy(holo.GetComponent<Collider>());

        // 回転でホログラム感
        // holo.transform.DOLocalRotate(new Vector3(0, 360, 0), 1.5f, RotateMode.FastBeyond360)
        //              .SetEase(Ease.Linear)
        //              .SetLoops(-1, LoopType.Restart);
        // 
        return holo;
    }
}
