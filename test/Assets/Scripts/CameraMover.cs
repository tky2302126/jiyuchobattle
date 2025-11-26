using UnityEngine;
using Cysharp.Threading.Tasks;

public class CameraMover : MonoBehaviour
{
    public Camera targetCamera;    // ← カメラを指定
    public Transform StartPoint;
    public Transform Setup;
    public Transform InBattle;

    async void Start()
    {
        await MoveCameraAsync(StartPoint.position, 0f);
    }

    public UniTask MoveCameraToStartPointAsync(float time)
        => MoveCameraAsync(StartPoint.position, time);

    public UniTask MoveCameraToSetupAsync(float time)
        => MoveCameraAsync(Setup.position, time);

    public UniTask MoveCameraToInBattleAsync(float time)
        => MoveCameraAsync(InBattle.position, time);

    private async UniTask MoveCameraAsync(Vector3 targetPos, float duration)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (duration <= 0f)
        {
            targetCamera.transform.position = targetPos;
            return;
        }

        Vector3 startPos = targetCamera.transform.position;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;
            targetCamera.transform.position = Vector3.Lerp(startPos, targetPos, lerp);
            await UniTask.Yield();
        }

        targetCamera.transform.position = targetPos;
    }
}
