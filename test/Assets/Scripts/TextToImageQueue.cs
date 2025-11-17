using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TextToImageQueue : MonoBehaviour
{
    private Queue<QueuedT2ITask> taskQueue = new Queue<QueuedT2ITask>();
    private bool isProcessing = false;

    private TextToImage t2i;

    void Awake()
    {
        t2i = new TextToImage();
    }

    /// <summary>
    /// キューにタスクを登録
    /// </summary>
    public void EnqueueTask(string json, ProgressBarController progressBar, CancellationToken token = default)
    {
        var task = new QueuedT2ITask
        {
            Json = json,
            ProgressBar = progressBar,
            CancellationToken = token
        };

        taskQueue.Enqueue(task);

        if (!isProcessing)
            _ = ProcessNextTaskAsync();
    }

    /// <summary>
    /// キューから順番にタスクを実行
    /// </summary>
    private async UniTask ProcessNextTaskAsync()
    {
        if (taskQueue.Count == 0)
        {
            isProcessing = false;
            return;
        }

        isProcessing = true;

        var currentTask = taskQueue.Dequeue();

        if (currentTask.ProgressBar != null)
            currentTask.ProgressBar.gameObject.SetActive(true);

        try
        {
            // 実際の TextToImage API 呼び出し
            Texture2D tex = await t2i.PostT2I(
                currentTask.Json,
                currentTask.ProgressBar,
                currentTask.CancellationToken
            );

            // ここで生成された tex を UI にセットするなど自由に処理可能
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Task canceled");
        }
        catch (Exception ex)
        {
            Debug.LogError("Task failed: " + ex.Message);
        }

        if (currentTask.ProgressBar != null)
        {
            currentTask.ProgressBar.SetProgress(1f);
            currentTask.ProgressBar.gameObject.SetActive(false);
        }

        // 次のタスクへ
        await ProcessNextTaskAsync();
    }

    /// <summary>
    /// キューに入れるタスク情報
    /// </summary>
    private class QueuedT2ITask
    {
        public string Json;
        public ProgressBarController ProgressBar;
        public CancellationToken CancellationToken;
    }
}
