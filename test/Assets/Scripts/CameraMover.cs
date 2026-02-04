using UnityEngine;
using Cysharp.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;

public class CameraMover : MonoBehaviour
{
    public Camera targetCamera;    // ← カメラを指定
    public Transform StartPoint;
    public Transform Setup;
    public Transform InBattle;
    public Transform Target;      // 回転の中心
    public float orbitRadius = 5f;
    public float orbitHeight = 2f;
    public float orbitDuration = 2f;
    public float zoomFov = 30f;
    public float zoomDuration = 1.5f;

    void Start()
    {
        
    }

    public async UniTask PlayIntroAsync()
    {
        await OrbitAsync();                         // ① 回転
        await MoveCameraToStartPointAsync(1.5f);    // ② 開始点へ移動
        await ZoomAsync();                          // ③ ズーム
    }

    private async UniTask ZoomAsync()
    {
        float startFov = targetCamera.fieldOfView;
        float t = 0f;

        while (t < zoomDuration)
        {
            t += Time.deltaTime;
            float lerp = t / zoomDuration;
            targetCamera.fieldOfView = Mathf.Lerp(startFov, zoomFov, lerp);
            await UniTask.Yield();
        }

        targetCamera.fieldOfView = zoomFov;
    }

    public UniTask MoveCameraToStartPointAsync(float time)
        => MoveCameraAsync(StartPoint.position, time);

    public UniTask MoveCameraToSetupAsync(float time)
        => MoveCameraAsync(Setup.position, time);

    public UniTask MoveCameraToInBattleAsync(float time)
        => MoveCameraAsync(InBattle.position, time);


    private async UniTask OrbitAsync()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        float t = 0f;

        while (t < orbitDuration)
        {
            t += Time.deltaTime;
            float angle = (t / orbitDuration) * 360f;

            float rad = angle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(rad) * orbitRadius,
                orbitHeight,
                Mathf.Sin(rad) * orbitRadius
            );

            targetCamera.transform.position = Target.position + offset;
            targetCamera.transform.LookAt(Target.position);

            await UniTask.Yield();
        }
    }
    private async UniTask MoveCameraAsync(Vector3 targetPos, float duration)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (duration <= 0f)
        {
            targetCamera.transform.position = targetPos;
            return;
        }

       // targetCamera.transform.position = StartPoint.position;

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
