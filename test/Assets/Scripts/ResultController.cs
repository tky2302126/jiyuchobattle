using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System;

public class ResultSceneController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform monitorTarget;

    [Header("Card Display")]
    [SerializeField] private Transform playerCardParent;
    [SerializeField] private Transform cpuCardParent;
    [SerializeField] private GameObject cardPrefab;

    [Header("Result UI")]
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private ParticleSystem winEffect;
    [SerializeField] private ParticleSystem loseEffect;
    [SerializeField] private ParticleSystem drawEffect;
    [SerializeField] private GameObject resultUI;

    [Header("Transition")]
    [SerializeField] private float cameraMoveDuration = 2f;
    [SerializeField] private float cardShowDelay = 0.15f;

    [SerializeField] private GameObject starEffect;

    // 内部状態
    private bool isPlaying = false;

    public enum BattleResult { Win, Lose, Draw }

    private async void Start()
    {
        await FadeManager.Instance.FadeIn();
        var brm = FindObjectOfType<BattleResultManager>();
        var record = brm.Record;
        PlayResultSequenceAsync(record).Forget();
    }

    /// <summary>
    /// リザルト演出開始
    /// </summary>
    public async UniTask PlayResultSequenceAsync(
        BattleRecord record)
    {
        if (isPlaying) return;
        isPlaying = true;

        // BattleResultをrecordから判定
        ResultSceneController.BattleResult result = DetermineBattleResult(record);

        try 
        {
            // 2️⃣ カメラ移動（モニターに寄る）
            await PlayCameraTransitionAsync();

            // 3️⃣ カード表示
            await ShowUsedCardsAsync(record);

            // 4️⃣ 勝敗表示
            await ShowResultAsync(result);
        }

        catch(Exception e) 
        {
            Debug.LogError(e);
        }

        finally 
        {
            await ShowMenuAsync();
        }

        

        // 5️⃣ 少し待ってから次へ
        // await UniTask.Delay(300);

        // 6️⃣ フェードアウト（次のラウンド or メニューへ）
        

        isPlaying = false;
    }

    /// <summary>
    /// BattleRecordから勝敗を判定
    /// </summary>
    private ResultSceneController.BattleResult DetermineBattleResult(BattleRecord record)
    {
        if (record.playerWins > record.cpuWins)
            return ResultSceneController.BattleResult.Win;
        else if (record.playerWins < record.cpuWins)
            return ResultSceneController.BattleResult.Lose;
        else
            return ResultSceneController.BattleResult.Draw;
    }

    // ----------------------------------------------------------
    // ✴ カメラ演出
    // ----------------------------------------------------------
    private async UniTask PlayCameraTransitionAsync()
    {
        // カメラの移動先
        Vector3 targetPos = monitorTarget.position;

        // カメラ移動（DOTween で簡単にアニメーション）
        await mainCamera.transform.DOMove(targetPos, cameraMoveDuration).SetEase(Ease.InOutQuad).AsyncWaitForCompletion();
    }

    // ----------------------------------------------------------
    // ✴ 使用カードの表示
    // ----------------------------------------------------------
    private async UniTask ShowUsedCardsAsync(BattleRecord record)
    {
        foreach (Transform child in playerCardParent) Destroy(child.gameObject);
        foreach (Transform child in cpuCardParent) Destroy(child.gameObject);

        float offset = 0;
        float spacing = 1.6f;

        foreach (var round in record.rounds)
        {
            // プレイヤー側カード
            var playerObj = Instantiate(cardPrefab, playerCardParent);
            var playerTextSetter = playerObj.GetComponent<CardTextSetter>();
            playerTextSetter?.SetMonsterCardText(round.playerUsedCard);
            var playerMeshController = playerObj.GetComponent<MeshController>();
            playerMeshController?.SetIllust(round.playerMonsterSprite);

            playerObj.transform.localPosition = new Vector3(0, offset, 0);
            playerObj.transform.localScale = Vector3.zero;
            await playerObj.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).AsyncWaitForCompletion();
            await UniTask.Delay((int)(cardShowDelay * 1000));

            // CPU側カード
            var cpuObj = Instantiate(cardPrefab, cpuCardParent);
            var cpuTextSetter = cpuObj.GetComponent<CardTextSetter>();
            cpuTextSetter?.SetMonsterCardText(round.cpuUsedCard);
            var cpuMeshController = cpuObj.GetComponent<MeshController>();
            cpuMeshController?.SetIllust(round.cpuMonsterSprite);

            cpuObj.transform.localPosition = new Vector3(0, offset, 0);
            cpuObj.transform.localScale = Vector3.zero;
            await cpuObj.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).AsyncWaitForCompletion();
            await UniTask.Delay((int)(cardShowDelay * 1000));

            AudioManager.Instance.PlaySE("Impact");

            // ===== 勝敗に応じた演出 =====
            switch (round.result)
            {
                case BattleResultType.PlayerWin:
                    await PlayWinnerEffectAsync(playerObj.transform);
                    PlayLoserEffect(cpuObj.transform);
                    break;

                case BattleResultType.CpuWin:
                    await PlayWinnerEffectAsync(cpuObj.transform);
                    PlayLoserEffect(playerObj.transform);
                    break;

                case BattleResultType.Draw:
                    // 引き分け → 両方軽く強調するなども可
                    break;
            }

            offset -= spacing;

            
        }
    }

    private async UniTask PlayWinnerEffectAsync(Transform card)
    {
        Vector3 offset = new Vector3(0, 0, 0.12f);
        var obj = Instantiate
            (starEffect,
            card,
            false);

        obj.transform.localPosition = offset;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        var seq = DOTween.Sequence();

        seq.Append(card.DOPunchScale(Vector3.one * 0.18f, 0.4f, 10, 0.9f));
        seq.Join(card.DOLocalMoveY(card.localPosition.y + 0.25f, 0.25f)
                      .SetLoops(2, LoopType.Yoyo));

        await seq.AsyncWaitForCompletion();
        
    }
    private void PlayLoserEffect(Transform card)
    {
        card.DOScale(0.9f, 0.2f);
    }
    // ----------------------------------------------------------
    // ✴ 勝敗表示
    // ----------------------------------------------------------
    private async UniTask ShowResultAsync(BattleResult result)
    {
        resultText.alpha = 0;
        resultText.transform.localScale = Vector3.one;

        switch (result)
        {
            case BattleResult.Win:
                resultText.text = "WIN";
                resultText.color = Color.cyan;
                winEffect?.Play();
                AudioManager.Instance.PlaySE("Win");
                break;
            case BattleResult.Lose:
                resultText.text = "LOSE";
                resultText.color = Color.red;
                // loseEffect?.Play();
                break;
            case BattleResult.Draw:
                resultText.text = "DRAW";
                resultText.color = Color.gray;
                // drawEffect?.Play();
                break;
        }

        AudioManager.Instance.PlayBGM("Result");

        await resultText.DOFade(1f, 1f).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
        await resultText.transform.DOScale(1.2f, 0.5f).SetLoops(2, LoopType.Yoyo).AsyncWaitForCompletion();

        
    }


    private async UniTask ShowMenuAsync()
    {
        await UniTask.Delay(500);
        resultUI.SetActive(true);
    }
}
