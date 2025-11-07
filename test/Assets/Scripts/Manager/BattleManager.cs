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
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform spawnPoint;

    private List<MonsterCard> playerMonsters = new();
    private List<MonsterCard> cpuMonsters = new();

    private List<GameObject> playerMonsterCards = new();
    private List<GameObject> cpuMonsterCards = new();

    private BattleState currentState = BattleState.Initialize;
    public  BattleState CurrentState => currentState;
    private bool isBattleRunning = false;

    private readonly Dictionary<GameObject, bool> isAnimating = new();

    private class MonsterStatus
    {
        public GameObject owner { get; private set; }
        public MonsterCard Monster;
        public float ElapsedTime = 0f;
        public int CurrentHP;
        public List<StatChange> changes;
        public MonsterCondition Condition;
        private int maxHp;
        public HPBarController HPBar { get; private set; }

        public bool IsAlive => CurrentHP > 0;
        public MonsterStatus(MonsterCard monster)
        {
            if (monster == null)
            {
                Debug.LogError("MonsterStatus: Monster が null です！");
                return;
            }

            Monster = monster;
            CurrentHP = monster.HP;
            maxHp = monster.HP;
            changes = new List<StatChange>();

        }
        public MonsterStatus(MonsterCard monster, HPBarController hpBar, GameObject owner)
        {
            if (monster == null)
            {
                Debug.LogError("MonsterStatus: Monster が null です！");
                return;
            }

            Monster = monster;
            CurrentHP = monster.HP;
            maxHp = monster.HP;
            changes = new List<StatChange>();
            HPBar = hpBar;
            this.owner = owner; 
            HPBar?.SetHP(CurrentHP, maxHp);
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
        Debug.Log("バトル初期化開始");

        // プレイヤーとCPUにカードを配布
        await playerController.DealInitialCardsAsync();
        await cpuController.DealInitialCardsAsync();

        currentState = BattleState.WaitingForReady;
        Debug.Log("配布完了。各陣営のモンスター生成を待機中...");
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

        // MonsterCard → MonsterStatus に変換
        playerStatuses.Clear();
        foreach(var card in playerMonsterCards) 
        {
            var cardPresenter = card.GetComponent<CardPresenter>();
            var monsterCard = cardPresenter.cardData as MonsterCard;
            var HpBar = card.GetComponentInChildren<HPBarController>();
            playerStatuses.Add(new MonsterStatus(monsterCard,HpBar, card));
        }

        // foreach (var m in playerMonsters) playerStatuses.Add(new MonsterStatus(m));

        cpuStatuses.Clear();
        foreach (var card in cpuMonsterCards)
        {
            var cardPresenter = card.GetComponent<CardPresenter>();
            var monsterCard = cardPresenter.cardData as MonsterCard;
            var HpBar = card.GetComponentInChildren<HPBarController>();
            cpuStatuses.Add(new MonsterStatus(monsterCard, HpBar, card));
        }

        // foreach (var m in cpuMonsters) cpuStatuses.Add(new MonsterStatus(m));

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
        Debug.Log("戦闘ループを開始します");
        while (isBattleRunning)
        {
            // 勝敗チェック
            if (CheckBattleEnd())
            {
                await HandleBattleEndAsync();
                break;
            }

            // プレイヤー側の攻撃処理
            foreach (var status in playerStatuses)
            {
                if (status.Monster.HP <= 0) continue;

                status.ElapsedTime += tickInterval;
                if (status.ElapsedTime >= status.Monster.AttackInterval)
                {
                    // var target = GetRandomAlive(cpuStatuses);
                    // if (target != null)
                        PerformAttack(status, cpuStatuses, isPlayer: true);

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
                   // var target = GetRandomAlive(playerStatuses);
                   // if (target != null)
                        PerformAttack(status, playerStatuses, isPlayer: false);

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
        bool playerAllDead = playerStatuses.TrueForAll(s => !s.IsAlive);
        bool cpuAllDead = cpuStatuses.TrueForAll(s => !s.IsAlive);

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
        {
            Debug.Log("🤝 引き分け！");
            // 両方にカードをランダムに一枚配布
        }

        bool playerWin = cpuAllDead && !playerAllDead;
        bool cpuWin = playerAllDead && !cpuAllDead;

        // --- 1. 敗北側のカードを分裂 ---
        var defeatedCards = cpuWin ? playerMonsterCards : cpuMonsterCards;
        IBattleParticipant winnerController = playerWin ? playerController : cpuController;
        IBattleParticipant loserController = playerWin ? cpuController : playerController;

        List<CardDataBase> splitCards = new();

        foreach (var cardObj in defeatedCards)
        {
            var presenter = cardObj.GetComponent<CardPresenter>();
            if (presenter == null) continue;

            var monster = presenter.cardData as MonsterCard;
            if (monster == null) continue;

            if (monster.sourceCards != null)
            {
                foreach (var source in monster.sourceCards)
                {
                    if (source != null)
                    {
                        splitCards.Add(source);
                        Debug.Log($"{monster.CardName} が分裂 → {source.CardName}");
                    }
                }
            }
        }

        if (splitCards.Count == 0)
        {
            Debug.Log("分裂カードが存在しません。");
            return;
        }

        await UniTask.Delay(1000);

        // --- 2. 勝利側が1枚選択 ---
        CardDataBase selectedByWinner = null;
        if (playerWin)
        {
            // TODO: UIで選択可能にする（暫定：ランダム）
            selectedByWinner = splitCards[Random.Range(0, splitCards.Count)];
            Debug.Log($"🏆 プレイヤーが {selectedByWinner.CardName} を獲得！");
        }
        else
        {
            selectedByWinner = splitCards[Random.Range(0, splitCards.Count)];
            Debug.Log($"🏆 CPUが {selectedByWinner.CardName} を獲得！");
        }

        // --- 3. 敗北側がランダムに1枚獲得 ---
        CardDataBase selectedByLoser = splitCards[Random.Range(0, splitCards.Count)];
        Debug.Log($"🎁 敗北側が {selectedByLoser.CardName} を手札に加えた");

        // --- 4. 手札に追加 ---
        var winnerCardObj = CreateCard(selectedByWinner);
        var loserCardObj = CreateCard(selectedByLoser);

        winnerController.AddCardToHand(winnerCardObj);
        loserController.AddCardToHand(loserCardObj);

        // --- 5. モンスターカードを破棄
        foreach (var card in playerMonsterCards) 
        {
            Destroy(card);
        }

        foreach (var card in cpuMonsterCards)
        {
            Destroy(card);
        }

        // --- 6. 次のラウンドへ ---
        Debug.Log("🔁 次のラウンド準備中...");
        await UniTask.Delay(2000);

        // 初期化とは別のイニシャライズを作る

        // await InitializeAsync();
    }

    /// <summary>
    /// ランダムに生きているモンスターを取得
    /// </summary>
    private MonsterStatus GetRandomAlive(List<MonsterStatus> list)
    {
        var alive = list.FindAll(m => m.CurrentHP > 0);
        if (alive.Count == 0) return null;
        return alive[Random.Range(0, alive.Count)];
    }

    private GameObject CreateCard(CardDataBase cardData) 
    {
        GameObject cardObj = Instantiate(cardPrefab, spawnPoint.position, Quaternion.identity);
        CardPresenter presenter = cardObj.AddComponent<CardPresenter>();
        presenter.cardData = cardData;

        return cardObj;
    }

    /// <summary>
    /// 攻撃処理（即時実行）
    /// </summary>
    private void PerformAttack(MonsterStatus attacker, List<MonsterStatus> targets, bool isPlayer)
    {
        if (attacker == null || targets == null || targets.Count == 0) return;

        var Attacker = attacker.Monster;

        if (attacker == null)
        {
            Debug.LogError("❌ attacker が null");
            return;
        }

        if (attacker.Monster == null)
        {
            Debug.LogError("❌ attacker.Monster が null");
            return;
        }

        if (targets == null || targets.Count == 0)
        {
            Debug.LogWarning("⚠️ targets が空");
            return;
        }

        // 1. コマンドをランダム抽選
        Command selectedCommand = null;
        if (Attacker.Skills != null && Attacker.Skills.Count > 0)
        {
            selectedCommand = Attacker.Skills[Random.Range(0, Attacker.Skills.Count)];
            Debug.Log($"{Attacker.CardName} が {selectedCommand.CommandName} を使用！");
        }

        if (selectedCommand == null)
        {
            Debug.LogWarning($"{Attacker.CardName} はスキルを持っていません。");
            return;
        }

        if (selectedCommand.IsSelf) 
        {
            ApplyEffect(attacker, attacker);
            return;
        }

        // 2. コマンドの対象を決める
        List<MonsterStatus> chosenTargets = new();

        if (selectedCommand.targetNum >= targets.Count)
        {
            chosenTargets.AddRange(targets); // 全員対象
        }
        else
        {
            // ランダムに targetNum 体選ぶ
            var alive = new List<MonsterStatus>(targets);
            for (int i = 0; i < selectedCommand.targetNum; i++)
            {
                if (alive.Count == 0) break;
                int idx = Random.Range(0, alive.Count);
                chosenTargets.Add(alive[idx]);
                alive.RemoveAt(idx);
            }
        }

        // 3. 最初のターゲットを突進目標に
        var firstTarget = chosenTargets[0];
        var targetObj = firstTarget.owner;
        if (targetObj == null)
        {
            Debug.LogWarning("⚠️ targetObj が見つかりません");
            return;
        }

        // --- 🌀 突進アニメーション ---
        AnimateAttackAsync(attacker.owner, isPlayer).Forget();
        // 3. コマンドを対象に実行
        foreach (var target in chosenTargets)
        {
            // 回避判定
            if (Random.value < target.Monster.Evasion)
            {
                Debug.Log($"💨 {target.Monster.CardName} が {selectedCommand.CommandName} を回避！");
                continue;
            }

            // CommandAction による攻撃処理
            // *用実装
            var damage = CalculateDamage(attacker, target, selectedCommand);
            TakeDamage(target, damage);
            ApplyEffect(attacker, target);

            // HP0チェック
            if (target.CurrentHP <= 0)
            {
                Debug.Log($"💀 {target.Monster.CardName} は倒れた！");
            }
        }
    }

    private async UniTask AnimateAttackAsync(GameObject attackerObj, bool isPlayer)
    {
        if (attackerObj == null) return;

        // アニメーション中なら待機（同時再生防止）
        if (isAnimating.TryGetValue(attackerObj, out bool animating) && animating)
        {
            await UniTask.WaitUntil(() => !isAnimating[attackerObj]);
        }

        isAnimating[attackerObj] = true;

        Vector3 startPos = attackerObj.transform.position;

        // Y方向の移動量（プレイヤーは上、敵は下）
        float moveAmount = 0.5f;
        float direction = isPlayer ? 1f : -1f;
        Vector3 attackPos = startPos + Vector3.up * moveAmount * direction;

        float duration = 0.1f;
        float elapsed = 0f;

        // --- 前進 ---
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            attackerObj.transform.position = Vector3.Lerp(startPos, attackPos, t);
            await UniTask.Yield();
        }

        // ヒット演出などの待機時間
        await UniTask.Delay(150);

        // --- 後退（元の位置に戻る） ---
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            attackerObj.transform.position = Vector3.Lerp(attackPos, startPos, t);
            await UniTask.Yield();
        }

        attackerObj.transform.position = startPos;

        isAnimating[attackerObj] = false;
    }

    /// <summary>
    /// ダメージ計算（数値のみ算出）
    /// </summary>
    private int CalculateDamage(MonsterStatus caster, MonsterStatus target, Command Selected)
    {
        int baseDamage = caster.Monster.Attack;

        bool hasTarget = target.Monster.sourceCards != null &&
                         target.Monster.sourceCards.Exists(src => src != null && src == caster.Monster.targetCard);

        // 特攻補正
        if (caster.Monster.targetCard != null &&
            target.Monster.CardName == caster.Monster.targetCard.CardName)
        {
            baseDamage = Mathf.RoundToInt(baseDamage * caster.Monster.specialMultiplier);
            Debug.Log($"🔥 {caster.Monster.CardName} の特攻！ こうかはばつぐんだ！");
        }

        // クリティカルなどを後で追加可能
        // if (Random.value < caster.Monster.CriticalRate) baseDamage *= 2;

        return Mathf.Max(0, baseDamage);
    }

    /// <summary>
    /// ダメージ適用（HP減少・ログ・死亡判定）
    /// </summary>
    private void TakeDamage(MonsterStatus target, int damage)
    {
        target.CurrentHP -= damage;
        if (target.CurrentHP < 0) target.CurrentHP = 0;

        Debug.Log($"💥 {target.Monster.CardName} は {damage} ダメージを受けた！（残りHP: {target.CurrentHP}）");

        target?.HPBar?.SetHP(target.CurrentHP, target.Monster.HP);

        if (target.CurrentHP == 0)
        {
            Debug.Log($"💀 {target.Monster.CardName} は倒れた！");
        }
    }

    /// <summary>
    /// 効果適用（ダメージや状態異常など、コマンド効果全般）
    /// </summary>
    private void ApplyEffect(MonsterStatus caster, MonsterStatus target)
    {
        // 今後ここにバフ・デバフ・状態異常などを追加
        // e.g. if (command.HasStatusEffect) ApplyStatus(target, command.StatusEffect);
    }

    public void SetMonster(MonsterCard monsterCard, bool isPlayer) 
    {
        if (monsterCard == null)
        {
            Debug.LogError("SetMonster: monsterCard が null です！");
            return;
        }

        if (isPlayer) 
        {
            playerMonsters.Add(monsterCard);
        }
        else 
        {
            cpuMonsters.Add(monsterCard);
        }
    }

    public void SetMonster(GameObject monsterCardObj, bool isPlayer)
    {
        if (monsterCardObj == null)
        {
            Debug.LogError("SetMonster: monsterCardObj が null です！");
            return;
        }

        if (isPlayer)
        {
            playerMonsterCards.Add(monsterCardObj);
        }
        else
        {
            cpuMonsterCards.Add(monsterCardObj);
        }
    }
}


