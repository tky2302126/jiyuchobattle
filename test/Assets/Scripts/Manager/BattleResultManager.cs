using UnityEngine;

[System.Serializable]
public class BattleResult
{
    public bool IsPlayerWin;
    public bool IsDraw;

    public BattleResult(bool isPlayerWin, bool isDraw)
    {
        IsPlayerWin = isPlayerWin;
        IsDraw = isDraw;
    }
}


public class BattleResultManager : MonoBehaviour
{
    public BattleResult LastResult { get; private set; }
    public BattleRecord Record { get; private set; } = new BattleRecord();

    public void SetRecord(BattleRecord record) 
    {
        Record = record;
    }

    public void SetResult(BattleResult result)
    {
        LastResult = result;

        Debug.Log($"✅ 記録更新：{Record.playerWins}勝 {Record.cpuWins}敗 {Record.draws}引き分け");
    }

    public void ResetRecord()
    {
        Record.Reset();
        Debug.Log("🧹 戦績リセット");
    }
}
