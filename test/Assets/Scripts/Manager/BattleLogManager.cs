using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 戦闘ログ管理クラス（プレイヤー表示＋デバッグ兼用）
/// </summary>
public class BattleLogManager : MonoBehaviour
{
    public static BattleLogManager Instance { get; private set; }

    public enum LogLevel
    {
        Normal,     // プレイヤー向け
        Debug,      // デバッグ用
        All         // 両方表示
    }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private int maxLogCount = 100;
    [SerializeField] private ScrollRect scrollRect; // ← 追加

    [Header("設定")]
    public LogLevel currentLogLevel = LogLevel.All;

    private readonly Queue<LogEntry> logs = new Queue<LogEntry>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 通常の戦闘ログを追加
    /// </summary>
    public void AddLog(string message)
    {
        AddLogInternal(message, LogLevel.Normal);
    }

    /// <summary>
    /// デバッグ専用ログを追加
    /// </summary>
    public void AddDebugLog(string message)
    {
        AddLogInternal($"<color=#888888>[Debug]</color> {message}", LogLevel.Debug);
    }

    private void AddLogInternal(string message, LogLevel type)
    {
        logs.Enqueue(new LogEntry(message, type));

        if (logs.Count > maxLogCount)
            logs.Dequeue();

        UpdateLogDisplay();
    }

    private void UpdateLogDisplay()
    {
        var displayedLogs = new List<string>();

        foreach (var log in logs)
        {
            if (currentLogLevel == LogLevel.All ||
               (currentLogLevel == LogLevel.Normal && log.Type == LogLevel.Normal) ||
               (currentLogLevel == LogLevel.Debug && log.Type == LogLevel.Debug))
            {
                displayedLogs.Add(log.Message);
            }
        }

        logText.text = string.Join("\n", displayedLogs);
        // スクロールを一番下に自動移動
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private class LogEntry
    {
        public string Message { get; }
        public LogLevel Type { get; }

        public LogEntry(string message, LogLevel type)
        {
            Message = message;
            Type = type;
        }
    }

    /// <summary>
    /// ログモードを切り替える（UIボタンなどから呼ぶ）
    /// </summary>
    public void ToggleLogLevel()
    {
        if (currentLogLevel == LogLevel.Normal)
            currentLogLevel = LogLevel.Debug;
        else if (currentLogLevel == LogLevel.Debug)
            currentLogLevel = LogLevel.All;
        else
            currentLogLevel = LogLevel.Normal;

        AddDebugLog($"ログモード切替: {currentLogLevel}");
        UpdateLogDisplay();
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleUnityLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleUnityLog;
    }

    private void HandleUnityLog(string condition, string stackTrace, LogType type)
    {
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                AddDebugLog($"<color=red>[Error]</color> {condition}");
                break;

            case LogType.Warning:
                AddDebugLog($"<color=yellow>[Warning]</color> {condition}");
                break;

            default:
                AddDebugLog(condition);
                break;
        }
    }
}
