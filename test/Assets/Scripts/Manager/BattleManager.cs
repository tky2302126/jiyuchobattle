using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using CardEase;

/// <summary>
/// ゲーム全体の進行を制御するメインループマネージャー
/// </summary>
public class BattleManager : MonoBehaviour
{
    public enum BattleState
    {
        Initialize,
        WaitingForReady,
        InBattle,
        BattleEnd
    }

    [Header("参照")]
    [SerializeField] private CardManager playerController;
    [SerializeField] private CPUController cpuController;
    [SerializeField] private float tickInterval = 0.3f; // 戦闘の更新間隔

    private List<MonsterCard> playerMonsters = new();
    private List<MonsterCard> cpuMonsters = new();

    private BattleState currentState = BattleState.Initialize;
    private bool isBattleRunning = false;

    private void Start()
    {
        InitializeAsync().Forget();
    }

    /// <summary>
    /// バトル初期化：カード配布など
    /// </summary>
    private async UniTask InitializeAsync()
    {
        currentState = BattleState.Initialize;
        Debug.Log("🟢 バトル初期化開始");

        // プレイヤーとCPUにカードを配布
        await playerController.DealInitialCardsAsync();
        await cpuController.DealInitialCardsAsync();

        currentState = BattleState.WaitingForReady;
        Debug.Log("✅ 配布完了。各陣営のモンスター生成を待機中...");
    }

    /// <summary>
    /// 両者がモンスター生成完了したらバトル開始
    /// </summary>
    public async UniTask TryStartBattleAsync()
    {
        if (currentState != BattleState.WaitingForReady) return;

        // 両方のモンスターが準備完了したらバトル開始


        if (playerMonsters.Count == 0 || cpuMonsters.Count == 0)
        {
            Debug.LogWarning("❌ モンスターが生成されていません。");
            return;
        }

        Debug.Log("⚔️ バトル開始！");
        currentState = BattleState.InBattle;
        isBattleRunning = true;

        await StartBattleLoopAsync();
    }

    /// <summary>
    /// 戦闘メインループ
    /// </summary>
    private async UniTask StartBattleLoopAsync()
    {
        float elapsed = 0f;

        while (isBattleRunning)
        {
            // 勝敗チェック
            if (CheckBattleEnd())
            {
               
            }

            elapsed += tickInterval;

            // プレイヤー側の攻撃処理
            foreach (var p in playerMonsters)
            {
                
            }

            // CPU側の攻撃処理
            foreach (var e in cpuMonsters)
            {
                
            }
        }


    }

    /// <summary>
    /// 勝敗判定
    /// </summary>
    private bool CheckBattleEnd()
    {
        bool playerAllDead = playerMonsters.TrueForAll(m => m.HP <= 0);
        bool cpuAllDead = cpuMonsters.TrueForAll(m => m.HP <= 0);

        if (playerAllDead || cpuAllDead)
        {
            isBattleRunning = false;
            currentState = BattleState.BattleEnd;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 戦闘終了処理
    /// </summary>
    private async UniTask HandleBattleEndAsync()
    {
        bool playerAllDead = playerMonsters.TrueForAll(m => m.HP <= 0);
        bool cpuAllDead = cpuMonsters.TrueForAll(m => m.HP <= 0);

        if (playerAllDead && cpuAllDead)
            Debug.Log("🤝 引き分け！");
        else if (playerAllDead)
            Debug.Log("💀 プレイヤーの敗北！");
        else
            Debug.Log("🏆 プレイヤーの勝利！");

        await UniTask.Delay(2000);

        // 次ラウンドを再開するなどの処理
        // await InitializeAsync();
    }

    /// <summary>
    /// ランダムに生きているモンスターを取得
    /// </summary>
    private MonsterCard GetRandomAlive(List<MonsterCard> list)
    {
        var alive = list.FindAll(m => m.HP > 0);
        if (alive.Count == 0) return null;
        return alive[Random.Range(0, alive.Count)];
    }
}