using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System.Linq;
using Unity.VisualScripting;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UI;
using TMPro;


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

    private List<MonsterCard> playerMonsters = new List<MonsterCard>();
    private List<MonsterCard> cpuMonsters = new List<MonsterCard>();

    private List<GameObject> playerMonsterCards = new();
    private List<GameObject> cpuMonsterCards = new();

    private List<MonsterStatus> pendingPlayerAdditions = new List<MonsterStatus>();
    private List<MonsterStatus> pendingCpuAdditions = new List<MonsterStatus>();

    [SerializeField] private GameObject BuffEffect;
    [SerializeField] private GameObject DebuffEffect;
    [SerializeField] private GameObject HealEffect;
    [SerializeField] private TextMeshProUGUI todoText;
    [SerializeField] private CameraMover cameraMover;

    private BattleState currentState = BattleState.Initialize;
    public  BattleState CurrentState => currentState;
    private bool isBattleRunning = false;

    private readonly Dictionary<GameObject, bool> isAnimating = new();

    private BattleRecord record = new();
    public BattleRecord Record => record;
    private int currentRound = 1;

    private float battleElapsedTime = 0f;   // 戦闘経過時間
    [SerializeField] private float slipDamageInterval = 10f; // スリップダメージ発動間隔（秒）

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
        cameraMover.MoveCameraToSetupAsync(5f).Forget();
    }

    /// <summary>
    /// バトル初期化：カード配布など
    /// </summary>
    private async UniTask InitializeAsync()
    {
        currentState = BattleState.Initialize;
        UpdateTodoText();
        Debug.Log("バトル初期化開始");

        // プレイヤーとCPUにカードを配布
        await playerController.DealInitialCardsAsync();
        await cpuController.DealInitialCardsAsync();

        currentState = BattleState.WaitingForReady;
        Debug.Log("配布完了。各陣営のモンスター生成を待機中...");
        UpdateTodoText();
    }

    /// <summary>
    /// 両者がモンスター生成完了したらバトル開始
    /// </summary>
    public async UniTask TryStartBattleAsync()
    {
        // 両方のモンスターが準備完了したらバトル開始

        if (playerMonsters.Count == 0 || cpuMonsters.Count == 0)
        {
            Debug.LogWarning(" モンスターが生成されていません。");
            return;
        }

        cameraMover.MoveCameraToInBattleAsync(1.5f).Forget();
        // MonsterCard → MonsterStatus に変換
        playerStatuses.Clear();
        foreach(var card in playerMonsterCards) 
        {
            if (card == null) continue;

            var cardPresenter = card.GetComponent<CardPresenter>();
            var monsterCard = cardPresenter.cardData as MonsterCard;
            var HpBar = card.GetComponentInChildren<HPBarController>();
            playerStatuses.Add(new MonsterStatus(monsterCard,HpBar, card));
        }

        cpuStatuses.Clear();
        foreach (var card in cpuMonsterCards)
        {
            if (card == null) continue;

            var cardPresenter = card.GetComponent<CardPresenter>();
            var monsterCard = cardPresenter.cardData as MonsterCard;
            var HpBar = card.GetComponentInChildren<HPBarController>();
            cpuStatuses.Add(new MonsterStatus(monsterCard, HpBar, card));
        }

        // バトル開始演出
        await PlayBattleStartCutInAsync();

        Debug.Log(" バトル開始！");
        currentState = BattleState.InBattle;
        UpdateTodoText();
        isBattleRunning = true;

        await BattleLoopAsync();
    }

    private async UniTask PlayBattleStartCutInAsync()
    {
        // テキストアニメーションを再生
        await UniTask.Delay(2000);
        Debug.Log("戦闘開始演出完了");
    }


    private void SendResult()
    {
        var resultManager = FindAnyObjectByType<BattleResultManager>();
        resultManager.SetRecord(Record);
    }

    /// <summary>
    /// 戦闘メインループ
    /// </summary>
    private async UniTask BattleLoopAsync()
    {
        Debug.Log("戦闘ループを開始します");
        while (isBattleRunning)
        {
            // 経過時間更新
            battleElapsedTime += tickInterval;

            // 一定時間経過でスリップダメージ
            if (battleElapsedTime >= slipDamageInterval)
            {
                ApplySlipDamageToAllMonsters();
            }

            // 勝敗チェック
            if (CheckBattleEnd())
            {
                await HandleBattleEndAsync();
                break;
            }

            // プレイヤー側の攻撃処理
            foreach (var status in playerStatuses)
            {
                if (status.CurrentHP <= 0) continue;

                status.ElapsedTime += tickInterval;
                if (status.ElapsedTime >= status.Monster.AttackInterval)
                {
                    PerformAttack(status, cpuStatuses, isPlayer: true);

                    status.ElapsedTime = 0f; // 攻撃後にリセット
                }
            }

            // CPU側の攻撃処理
            foreach (var status in cpuStatuses)
            {
                if (status.CurrentHP <= 0) continue;

                status.ElapsedTime += tickInterval;
                if (status.ElapsedTime >= status.Monster.AttackInterval)
                {
                    PerformAttack(status, playerStatuses, isPlayer: false);

                    status.ElapsedTime = 0f;
                }
            }
            

            playerStatuses.AddRange(pendingPlayerAdditions);
            pendingPlayerAdditions.Clear();

            cpuStatuses.AddRange(pendingCpuAdditions);
            pendingCpuAdditions.Clear();
            
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
            UpdateTodoText();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 戦闘終了処理
    /// </summary>
    private async UniTask HandleBattleEndAsync()
    {
        bool playerAllDead = playerStatuses.TrueForAll(states => states.CurrentHP <= 0);
        bool cpuAllDead = cpuStatuses.TrueForAll(states => states.CurrentHP <= 0);

        BattleResultType resultType = BattleResultType.Unknown;

        if (playerAllDead && cpuAllDead)
        {
            Debug.Log(" 引き分け！");
            resultType = BattleResultType.Draw;
        }
        else if (cpuAllDead && !playerAllDead)
        {
            Debug.Log(" プレイヤー勝利！");
            resultType = BattleResultType.PlayerWin;
        }
        else if (playerAllDead && !cpuAllDead)
        {
            Debug.Log(" CPU勝利！");
            resultType = BattleResultType.CpuWin;
        }

        RoundRecord roundRecord = new RoundRecord
        {
            roundIndex = currentRound,
            result = resultType
        };

        // プレイヤー代表モンスター
        var playerCardObj = playerMonsterCards.FirstOrDefault(c => c != null);
        if (playerCardObj != null && playerCardObj.TryGetComponent(out CardPresenter playerPresenter))
        {
            if (playerPresenter.cardData is MonsterCard playerMonster)
            {
                roundRecord.playerUsedCard = playerMonster;
                if (playerCardObj.TryGetComponent(out MeshController mc))
                    roundRecord.playerMonsterSprite = mc.GetIllust();
            }
        }

        // CPU代表モンスター
        var cpuCardObj = cpuMonsterCards.FirstOrDefault(c => c != null);
        if (cpuCardObj != null && cpuCardObj.TryGetComponent(out CardPresenter cpuPresenter))
        {
            if (cpuPresenter.cardData is MonsterCard cpuMonster)
            {
                roundRecord.cpuUsedCard = cpuMonster;
                if (cpuCardObj.TryGetComponent(out MeshController mc))
                    roundRecord.cpuMonsterSprite = mc.GetIllust();
            }
        }

        // BattleRecord に登録
        record.AddRoundResult(roundRecord);
        Debug.Log($" Round {roundRecord.roundIndex} 結果登録: {roundRecord.result}");


        // 分裂カード処理
        await HandleCardSplitAsync(resultType);

        // --- 6. 次のラウンドへ ---
        currentRound++;
        if(CheckGameOver()) 
        {
            Debug.Log("ラウンドが規定数に到達したので、終了します…");
            /// 戦闘結果をマネージャークラスに伝達
            SendResult();
            await UniTask.Delay(2000);
            await FadeManager.Instance.FadeOut();
            /// シーン遷移
            var sceneManager = FindObjectOfType<MySceneManager.MySceneManager>();
            sceneManager?.LoadResult();
            return;
        }
        Debug.Log("次のラウンド準備中...");
        battleElapsedTime = 0;
        cameraMover.MoveCameraToSetupAsync(1.5f).Forget();
        await UniTask.Delay(2000);
        await DealCardAsync();

        currentState = BattleState.WaitingForReady;
        UpdateTodoText();
        Debug.Log("配布完了。各陣営のモンスター生成を待機中...");
    }

    private async UniTask HandleCardSplitAsync(BattleResultType result)
    {
        var winnerCards = (result == BattleResultType.PlayerWin) ? playerMonsterCards : cpuMonsterCards;
        var loserCards = (result == BattleResultType.PlayerWin) ? cpuMonsterCards : playerMonsterCards;
        IBattleParticipant winnerController = (result == BattleResultType.PlayerWin) ? playerController : cpuController;
        IBattleParticipant loserController = (result == BattleResultType.PlayerWin) ? cpuController : playerController;

        List<CardDataBase> splitCards = new();
        foreach (var cardObj in loserCards)
        {
            if (cardObj == null) continue;
            if (!cardObj.TryGetComponent(out CardPresenter presenter)) continue;
            if (presenter.cardData is MonsterCard monster && monster.sourceCards != null)
                splitCards.AddRange(monster.sourceCards.Where(c => c != null));
        }

        if (splitCards.Count == 0)
        {
            Debug.Log("分裂カードなし");
            return;
        }

        await UniTask.Delay(1000);

        var selectedByWinner = splitCards[Random.Range(0, splitCards.Count)];
        var selectedByLoser = splitCards[Random.Range(0, splitCards.Count)];

        winnerController.AddCardToHand(CreateCard(selectedByWinner));
        loserController.AddCardToHand(CreateCard(selectedByLoser));

        Debug.Log($"🏆 勝者獲得: {selectedByWinner.CardName}, 敗者獲得: {selectedByLoser.CardName}");

        // モンスターカード破棄
        foreach (var c in playerMonsterCards.Concat(cpuMonsterCards))
            if (c != null) Destroy(c);
    }

    private async Task DealCardAsync()
    {
        await playerController.DealCardAsync();
        await cpuController.DealCardAsync();

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
            Debug.LogError(" attacker が null");
            return;
        }

        if (attacker.Monster == null)
        {
            Debug.LogError(" attacker.Monster が null");
            return;
        }

        if (targets == null || targets.Count == 0)
        {
            Debug.LogWarning(" targets が空");
            return;
        }

        // 0. 攻撃可能判定の確認
        var canAttack = CheckCanAttack(attacker, isPlayer);

        if (!canAttack)  return; 

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

        // 対象が自身
        if (selectedCommand.IsSelf) 
        {
            var effect = selectedCommand.Effect;
            if(effect != null) 
            {
                ApplyEffect(attacker, attacker, effect);
            }
        }
        else 
        {
            // 1. コマンドの対象を決める
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

            // 2. 最初のターゲットを突進目標に
            var firstTarget = chosenTargets[0];
            var targetObj = firstTarget.owner;
            if (targetObj == null)
            {
                Debug.LogWarning("targetObj が見つかりません");
                return;
            }

            // --- 🌀 突進アニメーション ---
            AnimateAttackAsync(attacker.owner, isPlayer).Forget();

            // --- エフェクト再生---
            PlayAttackEffect(attacker, chosenTargets);

            // 3. コマンドを対象に実行

            // stateChangeを含めたMonsterStatusを作成
            var crrAttacker = GetCurrentStatus(attacker);
            foreach (var target in chosenTargets)
            {
                var crrTarget = GetCurrentStatus(target);
                // 回避判定
                if (attacker.Condition.HasFlag(MonsterCondition.Strike))
                {
                    Debug.Log($"{attacker.Monster.CardName}の必中効果発動！");
                    attacker.Condition &= ~MonsterCondition.Strike;
                }
                else if (Random.value < crrTarget.Monster.Evasion)
                {
                    Debug.Log($"{target.Monster.CardName} が {selectedCommand.CommandName} を回避！");
                    continue;
                }

                // CommandAction による攻撃処理
                // *用実装
                var damage = CalculateDamage(crrAttacker, crrTarget, selectedCommand);
                TakeDamage(target, damage);
                var effect = selectedCommand.Effect;
                if (effect != null)
                {
                    ApplyEffect(attacker, target, effect);
                }

                // HP0チェック
                if (target.CurrentHP <= 0)
                {
                    Debug.Log($"{target.Monster.CardName} は倒れた！");
                    bool playerAllDead = playerStatuses.TrueForAll(states => states.CurrentHP <= 0);
                    bool cpuAllDead = cpuStatuses.TrueForAll(states => states.CurrentHP <= 0);

                    if (playerAllDead || cpuAllDead) 
                    {
                        PlayLastBlowSlowMotion().Forget();
                    }
                    SetCardColorGray(target.owner);
                }
            }

        }
            
        // StateChange更新
        UpdateStateChange(attacker);

        // Condition更新
        UpdateCondition(attacker);
    }

    private async UniTask PlayLastBlowSlowMotion()
    {
        Time.timeScale = 0.2f;
        await UniTask.Delay(700, ignoreTimeScale: true);
        Time.timeScale = 1f;
    }

    private void SetCardColorGray(GameObject card) 
    {
        var renderers = card.GetComponentsInChildren<SpriteRenderer>();
        if(renderers != null) 
        {
            foreach (var r in renderers)
            {
                Color c = r.color;

                // RGB → HSV に変換
                Color.RGBToHSV(c, out float h, out float s, out float v);

                // 彩度0
                s = 0f;

                // HSV → RGB
                Color gray = Color.HSVToRGB(h, s, v);

                // 反映
                r.color = gray;
            }
        }
    }

    // 攻撃できるかの判定
    private bool CheckCanAttack(MonsterStatus monster, bool isPlayer) 
    {
        // 増殖
        if(monster.Condition.HasFlag(MonsterCondition.Duplicate)) 
        {
            // 複製用の関数
            Duplicate(monster, isPlayer);
            monster.Condition &= ~MonsterCondition.Duplicate; //フラグ落とす
            Debug.Log($"{monster.Monster.CardName} は自身を複製した！");
            return false;
        }

        // 麻痺
        if(monster.Condition.HasFlag( MonsterCondition.Paralyze)) 
        {
            if (Random.value <= 0.2) 
            {
                Debug.Log( $"{monster.Monster.CardName} は体がしびれて動けない");
                return false; 
            }
        }


        // 凍結
        if (monster.Condition.HasFlag(MonsterCondition.Freeze)) 
        {
            monster.Condition &= ~MonsterCondition.Freeze;
            if (Random.value <= 0.4)
            {
                Debug.Log($"{monster.Monster.CardName} は凍えて体が動かない");
                return false;
            }
        }

        // 睡眠
        if (monster.Condition.HasFlag(MonsterCondition.Sleep)) 
        {
            monster.Condition &= ~MonsterCondition.Sleep;
            Debug.Log($"{monster.Monster.CardName} は眠っている");
            return false;
        }

        // 
        return true;
    }

    private void Duplicate(MonsterStatus monster, bool isPlayer)
    {
        if (monster == null || monster.owner == null) return;

        
        // ① オブジェクトを複製
        GameObject obj = Instantiate(monster.owner, Vector3.zero, Quaternion.identity);

        // ② CardPresenter を取得・設定
        if (!obj.TryGetComponent(out CardPresenter cardPresenter))
        {
            cardPresenter = obj.AddComponent<CardPresenter>();
        }
        var copiedMonster = monster.Monster.Clone();
        copiedMonster.Skills.RemoveAll(cmd => cmd.name == "Duplicate");
        cardPresenter.cardData = copiedMonster;

        // ③ HPBarController を取得（子オブジェクトに存在するか確認）
        HPBarController hpBar = obj.GetComponentInChildren<HPBarController>();
        if (hpBar == null)
        {
            Debug.LogWarning($"複製した {obj.name} に HPBarController が見つかりません。Prefab を確認してください。");
        }

        hpBar.Init();

        // ④ MonsterStatus を作成
        MonsterStatus newStatus = new MonsterStatus(copiedMonster, hpBar, obj);

        // ⑤ プレイヤー/CPU に登録
        if (isPlayer)
        {
            pendingPlayerAdditions.Add(newStatus);
            playerController.AddCardToField(obj);
            playerMonsterCards.Add(obj);
        }
        else
        {
            pendingCpuAdditions.Add(newStatus);
            cpuController.AddCardtoField(obj);
            cpuMonsterCards.Add(obj);
        }

        Debug.Log($"{monster.Monster.CardName} を複製しました → {obj.name}");
    }

    // StateChangeの持続時間を更新
    private void UpdateStateChange(MonsterStatus monster)
    {
        if (monster == null || monster.changes == null || monster.changes.Count == 0)
            return;

        // 後ろから前にループすることで、削除してもインデックスずれを防止
        for (int i = monster.changes.Count - 1; i >= 0; i--)
        {
            var change = monster.changes[i];

            switch (change.durationType)
            {
                case EffectDurationType.Permanent:
                    // 永続効果は削除しない
                    break;

                case EffectDurationType.UntilNextAttack:
                    // 次の攻撃まで持続の効果は攻撃後に削除
                    monster.changes.RemoveAt(i);
                    break;

                case EffectDurationType.ActionCount:
                    if (change.durationValue <= 0)
                    {
                        // HP回復などの効果を適用してから削除
                        if (change.statType == StatType.HP) 
                        {
                            monster.CurrentHP += change.changeAmount;
                            if (change.changeAmount > 0) PlayHealEffect(monster.owner.transform.position);
                            // 最大値を越えないようにする
                            if (monster.CurrentHP > monster.Monster.HP) monster.CurrentHP = monster.Monster.HP; 
                        }
                        monster?.HPBar?.SetHP(monster.CurrentHP, monster.Monster.HP);

                        if(change.statType == StatType.Duplicate) 
                        {
                            monster.Condition |= MonsterCondition.Duplicate;
                        }

                        monster.changes.RemoveAt(i);
                    }
                    else
                    {
                        // 毎ターン / 毎行動の効果を適用
                        if (change.statType == StatType.HP)
                            monster.CurrentHP += change.changeAmount;
                        if (monster.CurrentHP > monster.Monster.HP) monster.CurrentHP = monster.Monster.HP;
                        monster?.HPBar?.SetHP(monster.CurrentHP, monster.Monster.HP);

                        // 残りターン数を減らす
                        change.durationValue--;
                    }
                    break;

                default:
                    Debug.LogWarning($"未知のEffectDurationType: {change.durationType}");
                    break;
            }
        }
    }


    // MonsterConditionの更新
    private void UpdateCondition(MonsterStatus monster) 
    {
        if (monster.Condition.HasFlag(MonsterCondition.Burn))
        {
            // Burn 処理

            monster.Condition &= MonsterCondition.Burn;
        }

        if (monster.Condition.HasFlag(MonsterCondition.Poison))
        {
            // Poison 処理

            monster.Condition &= MonsterCondition.Poison;
        }
    }

    private MonsterStatus GetCurrentStatus(MonsterStatus monster) 
    {
        if (monster == null || monster.Monster == null)
        {
            Debug.LogError("GetCurrentStatus: monster or monster.Monster is null");
            return monster;
        }

        // クローンを作成して result に詰める
        MonsterStatus result = new MonsterStatus(monster.Monster.Clone())
        {
            CurrentHP = monster.CurrentHP,
            Condition = monster.Condition
        };

        // 元ステータス
        int baseHP = monster.Monster.HP;
        int baseAtk = monster.Monster.Attack;
        int baseDef = monster.Monster.Defense;
        float baseEva = monster.Monster.Evasion; 

        // 補正後
        int finalHP = baseHP;
        int finalAtk = baseAtk;
        int finalDef = baseDef;
        float finalEva = baseEva;

        foreach (var change in monster.changes)
        {
            switch (change.statType)
            {
                case StatType.Attack:
                    finalAtk += change.changeAmount;
                    break;
                case StatType.Defense:
                    finalDef += change.changeAmount;
                    break;
                case StatType.Evasion:
                    finalEva += change.changeAmount;
                    break;
            }
        }

        // クローンに反映（原本を壊さない）
        result.Monster.Attack = finalAtk;
        result.Monster.Defense = finalDef;
        result.Monster.Evasion = finalEva;

        return result;
    }

    private void PlayAttackEffect(MonsterStatus attacker, List<MonsterStatus> targets)
    {
        if (attacker.Monster.AttackEffect == null) return;
        var effectPrefab = attacker.Monster.AttackEffect;

        foreach(var target in targets) 
        {
            Vector3 spawnPos = target.owner.transform.position;
            spawnPos += new Vector3(0, 0, -0.1f);

            // 生成
            GameObject effect = GameObject.Instantiate(
                effectPrefab,
                spawnPos,
                Quaternion.identity
            );
        }
    }

    private void PlayBuffEffect(Vector3 positon, bool IsBuff) 
    {
        Vector3 spawnPos = positon + new Vector3(0, 0, -0.1f);
        var effect = IsBuff ? BuffEffect : DebuffEffect;
        GameObject Playeffect = GameObject.Instantiate(
                effect,
                spawnPos,
                Quaternion.identity
            );
    }

    private void PlayHealEffect(Vector3 positon) 
    {
        Vector3 spawnPos = positon + new Vector3(0, 0, -0.1f);
        GameObject Playeffect = GameObject.Instantiate(
                HealEffect,
                spawnPos,
                Quaternion.identity
            );
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
        int baseDamage = caster.Monster.Attack + Selected.power;
        var defence = target.Monster.Defense;
        if (defence < 0) defence = 0;

        bool hasTarget = target.Monster.sourceCards != null &&
                         target.Monster.sourceCards.Exists(src => src != null && src == caster.Monster.targetCard);

        // 特攻補正
        if (caster.Monster.targetCard != null &&
            target.Monster.CardName == caster.Monster.targetCard.CardName)
        {
            baseDamage = Mathf.RoundToInt(baseDamage * caster.Monster.specialMultiplier);
            Debug.Log($"🔥 {caster.Monster.CardName} の特攻！ こうかはばつぐんだ！");
        }

        // クリティカル判定
        if(caster.changes.Any(change => change.statType == StatType.Critical)) 
        {
            if(Random.value <= 0.25) 
            {
                Debug.Log($"クリティカルアタック！");
                baseDamage *= 2;
            }
        }

        int result = (int)Mathf.Max(baseDamage * 0.1f, baseDamage - defence);

        return result;
    }

    /// <summary>
    /// ダメージ適用（HP減少・ログ・死亡判定）
    /// </summary>
    private void TakeDamage(MonsterStatus target, int damage)
    {
        target.CurrentHP -= damage;
        if (target.CurrentHP < 0) target.CurrentHP = 0;

        Debug.Log($" {target.Monster.CardName} は {damage} ダメージを受けた！（残りHP: {target.CurrentHP}）");

        target?.HPBar?.SetHP(target.CurrentHP, target.Monster.HP);

        if (target.CurrentHP == 0)
        {
            Debug.Log($" {target.Monster.CardName} は倒れた！");
        }
    }

    /// <summary>
    /// 効果適用（ダメージや状態異常など、コマンド効果全般）
    /// </summary>
    private void ApplyEffect(MonsterStatus caster, MonsterStatus target, CommandEffect effect)
    {
        if(effect.selfChanges != null) 
        {
            foreach(var change in effect.selfChanges) 
            {
                ApplyStatChange(caster, change);
            }
            if(effect.selfChanges.Any(change => change.statType < StatType.HP)) 
            {
                PlayBuffEffect(caster.owner.transform.position, IsBuff: true);
            }
        }

        if(effect.targetChanges != null) 
        {
            foreach (var change in effect.selfChanges)
            {
                ApplyStatChange(target, change);
            }
            if (effect.selfChanges.Any(change => change.statType < StatType.HP))
            {
                PlayBuffEffect(target.owner.transform.position, IsBuff: false);
            }
            /// 余裕があれば各デバフごとにエフェクトを用意
            
        }
    }

    private void ApplyStatChange(MonsterStatus target, StatChange change) 
    {
        target.changes.Add(change);
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

    private bool CheckGameOver()
    {
#if false
        return false;
#endif

        // 3ラウンド以上経過
        if (currentRound >= 4) 
        {
            isBattleRunning = false; 
            return true; 
        }
        return false;
    }

    private void ApplySlipDamageToAllMonsters()
    {
        Debug.Log("モンスターに疲労がたまる…");

        foreach (var monster in playerStatuses)
            ApplySlipDamage(monster);

        foreach (var monster in cpuStatuses)
            ApplySlipDamage(monster);
    }

    private void ApplySlipDamage(MonsterStatus monster) 
    {
        TakeDamage(monster, 10);
    }

    private void UpdateTodoText() 
    {
        switch (currentState) 
        {
            case BattleState.Initialize:
                todoText.text = "準備中";
                break;

            case BattleState.WaitingForReady:
                todoText.text = "手札を場に出して、準備ができたらボタンを押す";
                break;

            case BattleState.InBattle:
                todoText.text = "";
                break;

            case BattleState.BattleEnd:
                todoText.text = "戦闘終了の処理中";
                break;
        }
    }
}

[System.Serializable]
public class BattleRecord
{
    // トータル結果
    public int playerWins;
    public int cpuWins;
    public int draws;

    // 各ラウンドの詳細
    public List<RoundRecord> rounds = new();

    public void Reset()
    {
        playerWins = 0;
        cpuWins = 0;
        draws = 0;
        rounds.Clear();
    }

    public void AddRoundResult(RoundRecord record)
    {
        rounds.Add(record);

        switch (record.result)
        {
            case BattleResultType.PlayerWin:
                playerWins++;
                break;
            case BattleResultType.CpuWin:
                cpuWins++;
                break;
            case BattleResultType.Draw:
                draws++;
                break;
        }
    }
}

    

public enum BattleResultType
{
    Unknown,
    PlayerWin,
    CpuWin,
    Draw
}

[System.Serializable]
public class RoundRecord
{
    public int roundIndex;
    public MonsterCard playerUsedCard;
    public MonsterCard cpuUsedCard;
    public Texture playerMonsterSprite;
    public Texture cpuMonsterSprite;
    public BattleResultType result;
}