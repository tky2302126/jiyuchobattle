using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq;
using Unity.VisualScripting;

public class CPUController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ReadyButton readyButton; // 既存の生成処理を利用
    [SerializeField] private Drag3DObject cpuDragManager; // CPU専用のスロット管理
    [SerializeField] private CardManager cardManager;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cpuHandSlot;
    [SerializeField] private Transform cpuFieldSlot;
    [SerializeField] private MonsterCardGenerator cardGenerator;

    
    private List<GameObject> cpuCardsInFieldSlot = new();
    private List<GameObject> cpuCardsInHandSlot = new();

    public bool IsReady { get; private set; } = false;

    private void Update()
    {
        // CキーでCPUアクションを実行
        if (Input.GetKeyDown(KeyCode.C))
        {
            ExecuteCPUActionAsync().Forget();
        }
    }

    /// <summary>
    /// CPUがカードを配り、セットし、イラストを生成する一連の動作
    /// </summary>
    public async UniTask ExecuteCPUActionAsync()
    {
        Debug.Log("🧠 CPUが行動を開始しました");

        await DealInitialCardsAsync();
        await UniTask.Delay(1000);
        await SetCardsToFieldAsync();
        await UniTask.Delay(500);

        await GenerateMonsterCardAsync();
    }

    /// <summary>
    /// 名詞・形容詞・動詞カードを2枚ずつCPUに配る
    /// </summary>
    public async UniTask DealInitialCardsAsync()
    {
        var cardDataList = cardManager.cardDataList;
        if (cardDataList == null || cardDataList.Count == 0)
        {
            Debug.LogWarning("カードデータリストが空です。");
            return;
        }

        // 各カテゴリごとに抽出
        List<CardDataBase> nounCards = cardDataList.FindAll(c => c is NounData);
        List<CardDataBase> adjCards = cardDataList.FindAll(c => c is AdjectiveData);
        List<CardDataBase> verbCards = cardDataList.FindAll(c => c is VerbData);

        // 各カテゴリ2枚ずつ配布
        await SpawnCardsFromList(nounCards, 2);
        await SpawnCardsFromList(adjCards, 2);
        await SpawnCardsFromList(verbCards, 2);

        Debug.Log("CPUに名詞・形容詞・動詞カードを2枚ずつ配布しました。");
    }


    public async UniTask SetCardAndGenerateCardAsync() 
    {
        IsReady = false;
        await SetCardsToFieldAsync();
        await GenerateMonsterCardAsync();
        IsReady = true;
    }
    /// <summary>
    /// 指定カテゴリのカードを生成・配布
    /// </summary>
    private async UniTask SpawnCardsFromList(List<CardDataBase> sourceList, int count)
    {
        if (sourceList == null || sourceList.Count == 0) return;

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, sourceList.Count);
            CardDataBase selectedCard = sourceList[index];

            GameObject cardObj = Instantiate(cardPrefab, cpuHandSlot.position, Quaternion.identity);
            CardPresenter presenter = cardObj.AddComponent<CardPresenter>();
            presenter.cardData = selectedCard;



            cpuCardsInHandSlot.Add(cardObj);
            UpdateCardPositions();

            await UniTask.Delay(200); // 配布間隔を少し空ける
        }
    }

    private void UpdateCardPositions()
    {
        // フィールドスロット整列
        if (cpuCardsInFieldSlot.Count > 0)
        {
            float spacing = 0.9f;
            Vector3 startPos = cpuFieldSlot.position - new Vector3((cpuCardsInFieldSlot.Count - 1) * spacing / 2, 0, 0);
            for (int i = 0; i < cpuCardsInFieldSlot.Count; i++)
            {
                cpuCardsInFieldSlot[i].transform.position = startPos + new Vector3(i * spacing, 0, 0);
            }
        }

        // 手札スロット整列
        if (cpuCardsInHandSlot.Count > 0)
        {
            // まずはカテゴリ順にソート（名詞→形容詞→動詞）
            cpuCardsInHandSlot.Sort((a, b) =>
            {
                var dataA = a.GetComponent<CardPresenter>()?.cardData;
                var dataB = b.GetComponent<CardPresenter>()?.cardData;

                if (dataA == null || dataB == null) return 0;

                // cardCategory は enum として定義されている想定
                // 並び順を明示的に制御するために、優先順位を設定
                int orderA = GetCategoryOrder(dataA.category);
                int orderB = GetCategoryOrder(dataB.category);
                return orderA.CompareTo(orderB);
            });

            float spacing = 0.9f; // 手札は少し詰める
            float padding = 0.02f;
            Vector3 startPos = cpuHandSlot.position - new Vector3((cpuCardsInHandSlot.Count - 1) * spacing / 2, 0, 0);
            for (int i = 0; i < cpuCardsInHandSlot.Count; i++)
            {
                cpuCardsInHandSlot[i].transform.position = startPos + new Vector3(i * spacing, 0, -padding);
            }
        }
    }

    int GetCategoryOrder(cardCategory category)
    {
        switch (category)
        {
            case cardCategory.Noun: return 0;
            case cardCategory.Adj: return 1;
            case cardCategory.Verb: return 2;
            default: return 99;
        }
    }

    /// <summary>
    /// CPUがカードをセットする（FieldSlotに置く想定）
    /// </summary>
    private async UniTask SetCardsToFieldAsync()
    {
        if (cpuCardsInHandSlot.Count == 0)
        {
            Debug.LogWarning("CPUが持つカードがありません。");
            return;
        }

        Debug.Log("📍 CPUがカードをフィールドにセットします。");

        // 名詞カードをランダムに1枚選ぶ
        var nounCards = cpuCardsInHandSlot
            .Where(c => c.GetComponent<CardPresenter>()?.cardData is NounData)
            .ToList();
        if (nounCards.Count == 0)
        {
            Debug.LogWarning("CPUの手札に名詞カードがありません。");
            return;
        }

        var selectedNoun = nounCards[Random.Range(0, nounCards.Count)];

        // 他のカードを1～2枚ランダムに選ぶ
        var otherCards = cpuCardsInHandSlot
            .Where(c => !(c.GetComponent<CardPresenter>()?.cardData is NounData))
            .GroupBy(c => c.GetComponent<CardPresenter>().cardData.name)
            .Select(g => g.First())
            .ToList();
        int otherCount = Mathf.Min(Random.Range(1, 3), otherCards.Count); // 1～2枚
        var selectedOthers = otherCards.OrderBy(_ => Random.value).Take(otherCount).ToList();

        // 選ばれたカードをまとめる
        var cardsToSet = new List<GameObject> { selectedNoun };
        cardsToSet.AddRange(selectedOthers);

        // フィールドへ移動
        foreach (var card in cardsToSet)
        {
            cpuCardsInFieldSlot.Add(card);
            cpuCardsInHandSlot.Remove(card);

            Vector3 targetPos = cpuFieldSlot.position;
            await card.transform.DOMove(targetPos, 0.3f).AsyncWaitForCompletion();
            UpdateCardPositions();
        }
    }

    /// <summary>
    /// ReadyButtonの合成処理を再利用して、CPUのモンスターカードを生成
    /// </summary>
    private async UniTask GenerateMonsterCardAsync()
    {
        if (cpuCardsInFieldSlot.Count == 0)
        {
            Debug.LogWarning("フィールドにカードがありません。");
            return;
        }

        var monsterCardObj = await cardGenerator.CreateMonsterCardAsync(
            cpuCardsInFieldSlot,
            cpuFieldSlot,
            isPlayer: false
            );

        if(monsterCardObj != null) cpuCardsInFieldSlot.Add(monsterCardObj);
        var presenter = monsterCardObj.GetComponent<CardPresenter>();
        if (presenter != null)
        {
            var monsterCard = presenter.cardData as MonsterCard;
            BattleManager.Instance.SetMonster(monsterCard, isPlayer: false);
        }
        Debug.Log("CPUカードのイラスト生成が完了しました。");
    }
}