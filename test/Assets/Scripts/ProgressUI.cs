using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressUI : MonoBehaviour
{
    public Slider progressSlider;

    private WebUIStarterRuntime webUI;

    public void SetProgress(float value)
    {
        progressSlider.value = Mathf.Clamp01(value); // 0~1
    }

    private IEnumerator Start()
    {
        while(Singleton.Instance == null) 
        {
            yield return null;
        }
       webUI = Singleton.Instance.GetComponentInChildren<WebUIStarterRuntime>();
        if(webUI != null) { Debug.Log("webUI取得"); }
       StartCoroutine(UpdateProgress());
    }

    private IEnumerator UpdateProgress()
    {
        while (true)
        {
            if (webUI != null)
            {
                // Singleton から現在の進捗を取得
                float progress = webUI.progressValue;

                // Slider に反映
                progressSlider.value = Mathf.Clamp01(progress);
            }

            yield return null; // 毎フレーム更新
        }
    }

}
