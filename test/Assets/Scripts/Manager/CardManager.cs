using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

public class CardManager : MonoBehaviour, IBattleParticipant
{
    [Header("対象プレハブ")]
    public GameObject targetPrefab;

    [Header("カードデータリスト")]
    public List<CardDataBase> cardDataList;

    [Header("手札管理スクリプト")]
    public Drag3DObject dragManager;

    [Header("アニメーション設定")]
    public Transform spawnPoint; // 生成位置（例：山札）
    public Transform stopPoint;
    public float moveDuration = 0.5f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] private MonsterCardGenerator cardGenerator;

    // 既存プレハブに MonoBehaviour を付けて ScriptableObject を渡すクラス
    public void AttachRandomCard()
    {
        if (targetPrefab == null || cardDataList.Count == 0) return;

        // ランダムにカードを選ぶ
        int index = Random.Range(0, cardDataList.Count);
        CardDataBase selectedCard = cardDataList[index];

        // プレハブを生成（シーン上に置く場合）
        GameObject obj = Instantiate(targetPrefab);

        // MonoBehaviour でカードデータを保持するスクリプトをアタッチ
        CardPresenter presenter = obj.AddComponent<CardPresenter>();
        presenter.cardData = selectedCard;

        // 手札へのアニメーション付き移動を開始
        MoveCardToHandAsync(obj).Forget();

        Debug.Log($"Attached {selectedCard.name} to {obj.name} and started move animation");
    }

    private async UniTaskVoid MoveCardToHandAsync(GameObject cardObj)
    {
        Vector3 startPos = cardObj.transform.position;
        Quaternion startRot = cardObj.transform.rotation;
        Vector3 endPos = stopPoint.position;
        Quaternion endRot = stopPoint.rotation;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveCurve.Evaluate(elapsed / moveDuration);

            cardObj.transform.position = Vector3.Lerp(startPos, endPos, t);
            cardObj.transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            await UniTask.Yield();
        }

        // 最後に正確にスナップ
        cardObj.transform.position = endPos;
        cardObj.transform.rotation = endRot;

        AddCardToHand(cardObj);
    }

    public void AddCardToHand(GameObject cardObj) 
    {
        // 手札に正式登録
        dragManager.AddCardToHand(cardObj);
    }

    // カード生成＋手札登録
    public void AttachRandomCardToHand()
    {
        if (targetPrefab == null || cardDataList.Count == 0 || dragManager == null) return;

        // ランダムにカードを選ぶ
        int index = Random.Range(0, cardDataList.Count);
        CardDataBase selectedCard = cardDataList[index];

        // プレハブを生成
        GameObject cardObj = Instantiate(targetPrefab);

        // CardPresenter をアタッチしてカードデータを設定
        CardPresenter presenter = cardObj.AddComponent<CardPresenter>();
        presenter.cardData = selectedCard;

        // 生成したカードを手札に登録
        dragManager.AddCardToHand(cardObj);

        Debug.Log($"Attached {selectedCard.name} to {cardObj.name} and added to hand slot");
    }

    private void Start()
    {
        // await DealInitialCardsAsync();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.G)) 
        {
            AttachRandomCard();
           // AttachRandomCardToHand();
        }

#endif
    }

    /// <summary>
    /// ゲーム開始時に名詞・動詞・形容詞カードを2枚ずつ加える
    /// </summary>
    public async UniTask DealInitialCardsAsync()
    {
        // 各タイプごとにフィルタリング
        List<CardDataBase> nounCards = cardDataList.FindAll(c => c is NounData);
        List<CardDataBase> verbCards = cardDataList.FindAll(c => c is VerbData);
        List<CardDataBase> adjCards = cardDataList.FindAll(c => c is AdjectiveData);

        // 各タイプ2枚ずつ生成（存在する場合）
        await SpawnCardsFromList(nounCards, 2);
        await SpawnCardsFromList(verbCards, 2);
        await SpawnCardsFromList(adjCards, 2);
    }

    private async UniTask SpawnCardsFromList(List<CardDataBase> sourceList, int count)
    {
        if (sourceList == null || sourceList.Count == 0) return;

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, sourceList.Count);
            CardDataBase selectedCard = sourceList[index];

            GameObject cardObj = Instantiate(targetPrefab, spawnPoint.position, Quaternion.identity);
            CardPresenter presenter = cardObj.AddComponent<CardPresenter>();
            presenter.cardData = selectedCard;

            // アニメーション移動
           dragManager.AddCardToHand(cardObj);
            await UniTask.Delay(300);
        }
    }
    // 2ラウンド目以降にカードを配る
    public async UniTask DealCardAsync() 
    {
        int cardsToDeal = Mathf.Max(0, 6 - dragManager.GetHandCount());

        if(cardsToDeal <= 0) 
        {
            await UniTask.Delay(1);
            return;
        }


        var hasNoun = dragManager.HasNounCardInHand();
        if (!hasNoun) 
        {
            List<CardDataBase> nounCards = cardDataList.FindAll(c => c is NounData);
            await SpawnCardsFromList(nounCards, 1);
            cardsToDeal--;
        }

        await SpawnCardsFromList(cardDataList, cardsToDeal);
    }

    public GameObject CloneCard(MonsterCard monster, GameObject obj)
    {
        var result = cardGenerator.CloneCard(monster, obj);
        return null;
    }

    public void AddCardToField(GameObject obj) 
    {
        dragManager.AddCardToField(obj);
    }
}

// プレハブにアタッチする MonoBehaviour
public class CardPresenter : MonoBehaviour
{
    private CardDataBase _cardData;

    public CardDataBase cardData
    {
        get => _cardData;
        set
        {
            _cardData = value;
            OnCardDataSet();
        }
    }

    private void OnCardDataSet()
    {
        var cardColorSetter = GetComponentInChildren<CardColorSetter>();
        if (cardColorSetter != null)
        {
            cardColorSetter.ApplyColorByCardType(_cardData);
        }

        var cardTextSetter = GetComponent<CardTextSetter>();
        if(cardTextSetter != null) 
        {
            cardTextSetter.SetBaseCardText(_cardData);
        }
    }
}