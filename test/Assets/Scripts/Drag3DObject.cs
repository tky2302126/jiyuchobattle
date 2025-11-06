using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class Drag3DObject : MonoBehaviour
{
    private Camera mainCamera;
    private GameObject Card;
    private Vector3 offset;
    private float zCoord;

    [SerializeField] private Transform fieldSlot; // ドロップ先スロット
    [SerializeField] private Transform handSlot;          // 手札スロット（必ず戻す先）


    public  List<GameObject> CardsInFieldSlot => cardsInFieldSlot;
    private List<GameObject> cardsInFieldSlot = new List<GameObject>();
    private List<GameObject> cardsInHandSlot = new List<GameObject>();
    void Start()
    {
        mainCamera = Camera.main;
        if (handSlot == null)
        {
            Debug.LogWarning($"{name} の handSlot が未設定です。Inspector で割り当ててください。");
        }
    }

    void Update()
    {
        // マウスボタン押下でオブジェクトを選択
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider != null)
                {
                    if (!hit.collider.CompareTag("Card")) return;
                    Card = hit.collider.gameObject;
                    zCoord = mainCamera.WorldToScreenPoint(Card.transform.position).z;

                    Vector3 mousePoint = Input.mousePosition;
                    mousePoint.z = zCoord;
                    offset = Card.transform.position - mainCamera.ScreenToWorldPoint(mousePoint);

                    // スロットからカードを一旦外す
                    cardsInFieldSlot.Remove(Card);
                    cardsInHandSlot.Remove(Card);
                    UpdateCardPositions();
                }
            }
        }

        // マウスボタンを押している間、オブジェクトを追従させる
        if (Input.GetMouseButton(0) && Card != null)
        {
            Vector3 mousePoint = Input.mousePosition;
            mousePoint.z = zCoord;
            Vector3 targetPos = mainCamera.ScreenToWorldPoint(mousePoint) + offset;
            Card.transform.position = targetPos;
        }

        // マウスボタン離したらオブジェクトを解放
        if (Input.GetMouseButtonUp(0) && Card != null)
        {
            var InSlot = FindOverlappedSlot(Card);

            if (InSlot)
            {
                // 名詞カードかどうか判定
                var cardData = Card.GetComponent<CardPresenter>()?.cardData; // CardPresenter経由でCardDataを取得
                bool isNounCard = cardData is NounData;

                // 既に名詞カードがあるかチェック
                bool hasNounCard = cardsInFieldSlot.Exists(c =>
                {
                    var data = c.GetComponent<CardPresenter>()?.cardData;
                    return data is NounData;
                });

                // フィールドに置けない場合は手札に戻す
                if ((isNounCard && hasNounCard) || cardsInFieldSlot.Count >= 3)
                {
                    Debug.Log("フィールドスロットに置けません。手札に戻します。");
                    Card.transform.position = handSlot.position;
                    if (!cardsInHandSlot.Contains(Card)) cardsInHandSlot.Add(Card);
                }
                else
                {
                    Card.transform.position = fieldSlot.position;
                    if (!cardsInFieldSlot.Contains(Card)) cardsInFieldSlot.Add(Card);
                }
            }
            else
            {
                Card.transform.position = handSlot.transform.position;
                cardsInHandSlot.Add(Card);
            }
            UpdateCardPositions();
            Card = null;
        }
    }

    private bool FindOverlappedSlot(GameObject card)
    {
        Collider[] cols = Physics.OverlapSphere(card.transform.position, 0.5f);
        foreach (var col in cols)
        {
            if (col.CompareTag("Slot"))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateCardPositions()
    {
        // まずリストから削除済みカードを除去
        cardsInFieldSlot.RemoveAll(c => c == null);
        cardsInHandSlot.RemoveAll(c => c == null);

        // フィールドスロット整列
        if (cardsInFieldSlot.Count > 0)
        {
            float spacing = 0.9f;
            Vector3 startPos = fieldSlot.position - new Vector3((cardsInFieldSlot.Count - 1) * spacing / 2, 0, 0);
            for (int i = 0; i < cardsInFieldSlot.Count; i++)
            {
                cardsInFieldSlot[i].transform.position = startPos + new Vector3(i * spacing, 0, 0);
            }
        }

        // 手札スロット整列
        if (cardsInHandSlot.Count > 0)
        {
            // まずはカテゴリ順にソート（名詞→形容詞→動詞）
            cardsInHandSlot.Sort((a, b) =>
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
            Vector3 startPos = handSlot.position - new Vector3((cardsInHandSlot.Count - 1) * spacing / 2, 0, 0);
            for (int i = 0; i < cardsInHandSlot.Count; i++)
            {
                cardsInHandSlot[i].transform.position = startPos + new Vector3(i * spacing, 0, - padding);
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
    /// カードを手札に追加＋アニメーション移動
    /// </summary>
    public async void AddCardToHand(GameObject card)
    {
        if (handSlot == null)
        {
            Debug.LogWarning("handSlot が未設定です。");
            return;
        }

        cardsInHandSlot.Add(card);

        // 手札内での配置位置を計算
        float spacing = 0.2f;
        Vector3 startPos = handSlot.position - new Vector3((cardsInHandSlot.Count - 1) * spacing / 2, 0, 0);
        Vector3 targetPos = startPos + new Vector3((cardsInHandSlot.Count - 1) * spacing, 0, 0);

        await AnimateCardToPosition(card, targetPos);
    }

    private async UniTask AnimateCardToPosition(GameObject card, Vector3 targetPos)
    {
        float duration = 0.5f;
        Vector3 startPos = card.transform.position;
        Vector3 startScale = card.transform.localScale;
        Vector3 endScale = Vector3.one;

        float elapsed = 0f;

        // カードを手札へ移動させるアニメーション
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);

            card.transform.position = Vector3.Lerp(startPos, targetPos, t);
            card.transform.localScale = Vector3.Lerp(startScale * 0.8f, endScale, t);
            card.transform.rotation = Quaternion.Slerp(card.transform.rotation, Quaternion.identity, t);

            await UniTask.Yield();
        }

        card.transform.position = targetPos;
        card.transform.localScale = endScale;

        UpdateCardPositions();
    }


}
