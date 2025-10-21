using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using CardEase;
using Unity.VisualScripting;

/// <summary>
/// ゲーム全体の進行を制御するメインループマネージャー
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

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
    [SerializeField] private float tickInterval = 0.1f; // 戦闘の更新間隔

    private List<MonsterCard> playerMonsters = new();
    private List<MonsterCard> cpuMonsters = new();

    private BattleState currentState = BattleState.Initialize;
    public  BattleState CurrentState => currentState;
    private bool isBattleRunning = false;

    private class MonsterStatus
    {
        public MonsterCard Monster;
        public float ElapsedTime = 0f;

        public MonsterStatus(MonsterCard monster)
        {
            Monster = monster;
        }
    }

    private List<MonsterStatus> playerStatuses = new();
    private List<MonsterStatus> cpuStatuses = new();

    private void Awake()
    {
        // シングルトン実装
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // 任意：シーンをまたいで保持したい場合のみ
    }

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
        // 両方のモンスターが準備完了したらバトル開始


        if (playerMonsters.Count == 0 || cpuMonsters.Count == 0)
        {
            Debug.LogWarning("❌ モンスターが生成されていません。");
            return;
        }

        Debug.Log("⚔️ バトル開始！");
        currentState = BattleState.InBattle;
        isBattleRunning = true;

        await BattleLoopAsync();
    }

    /// <summary>
    /// 戦闘メインループ
    /// </summary>
    private async UniTask BattleLoopAsync()
    {
        float elapsed = 0f;

        playerStatuses.Clear();
        foreach (var m in playerMonsters) playerStatuses.Add(new MonsterStatus(m));

        cpuStatuses.Clear();
        foreach (var m in cpuMonsters) cpuStatuses.Add(new MonsterStatus(m));

        while (isBattleRunning)
        {
            // 勝敗チェック
            if (CheckBattleEnd())
            {
                await HandleBattleEndAsync();
                break;
            }

            elapsed += tickInterval;

            // プレイヤー側の攻撃処理
            foreach (var status in playerStatuses)
            {
                if (status.Monster.HP <= 0) continue;

                status.ElapsedTime += tickInterval;
                if (status.ElapsedTime >= status.Monster.AttackInterval)
                {
                    var target = GetRandomAlive(cpuMonsters);
                    if (target != null)
                        PerformAttack(status.Monster, cpuMonsters);

                    status.ElapsedTime = 0f; // 攻撃後にリセット


                }
            }

            // CPU側の攻撃処理
            foreach (var status in cpuStatuses)
            {
                    if (status.Monster.HP <= 0) continue;

                    status.ElapsedTime += tickInterval;
                    if (status.ElapsedTime >= status.Monster.AttackInterval)
                    {
                        var target = GetRandomAlive(playerMonsters);
                        if (target != null)
                            PerformAttack(status.Monster, playerMonsters);

                        status.ElapsedTime = 0f;
                    }
            }

            await UniTask.Delay((int)(tickInterval * 1000));
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

    /// <summary>
    /// 攻撃処理（即時実行）
    /// </summary>
    private void PerformAttack(MonsterCard attacker, List<MonsterCard> targets)
    {
        if (attacker == null || targets == null || targets.Count == 0) return;

        // 1. コマンドをランダム抽選
        Command selectedCommand = null;
        if (attacker.Skills != null && attacker.Skills.Count > 0)
        {
            selectedCommand = attacker.Skills[Random.Range(0, attacker.Skills.Count)];
            Debug.Log($"{attacker.CardName} が {selectedCommand.CommandName} を使用！");
        }

        // 2. コマンドの対象を決める
        List<MonsterCard> chosenTargets = new();

        if (selectedCommand.targetNum >= targets.Count)
        {
            chosenTargets.AddRange(targets); // 全員対象
        }
        else
        {
            // ランダムに targetNum 体選ぶ
            var alive = new List<MonsterCard>(targets);
            for (int i = 0; i < selectedCommand.targetNum; i++)
            {
                if (alive.Count == 0) break;
                int idx = Random.Range(0, alive.Count);
                chosenTargets.Add(alive[idx]);
                alive.RemoveAt(idx);
            }
        }

        // 3. コマンドを対象に実行
        foreach (var target in chosenTargets)
        {
            // 回避判定
            if (Random.value < target.Evasion)
            {
                Debug.Log($"💨 {target.CardName} が {selectedCommand.CommandName} を回避！");
                continue;
            }

            // CommandAction による攻撃処理

            selectedCommand.Execute(attacker, target);

            // 特攻補正（MonsterCard 側の targetCard を参照）
            if (attacker.targetCard != null && target.CardName == attacker.targetCard.CardName)
            {
                int extraDamage = Mathf.RoundToInt(attacker.Attack * (attacker.specialMultiplier - 1));
                target.HP -= extraDamage;
                Debug.Log($"🔥 {attacker.CardName} の特攻！ {target.CardName} に追加 {extraDamage} ダメージ！");
            }

            // HP0チェック
            if (target.HP <= 0)
            {
                target.HP = 0;
                Debug.Log($"💀 {target.CardName} は倒れた！");
            }
        }
    }
}

