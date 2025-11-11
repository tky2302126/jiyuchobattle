using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MonitorScanlineEffect : MonoBehaviour
{
    private Material displayMat;

    [Header("Scanline設定")]
    [SerializeField] private float lineSpeed = 1.0f;
    [SerializeField] private float lineIntensity = 0.2f;
    [SerializeField] private float noiseSpeed = 0.5f;
    [SerializeField] private float noiseScale = 10f;

    void Start()
    {
        displayMat = GetComponent<MeshRenderer>().materials[0];
    }

    void Update()
    {
        // スキャンラインを時間で上下に流す
        float scan = Mathf.Repeat(Time.time * lineSpeed, 1f);
        float scanValue = Mathf.PingPong(scan, 0.5f) * lineIntensity;

        // ノイズっぽい輝き（PerlinNoise使用）
        float noise = Mathf.PerlinNoise(Time.time * noiseSpeed, 0f) * 0.5f;

        // 発光を変化させる
        Color baseColor = Color.Lerp(Color.cyan, Color.magenta, scanValue + noise);
        displayMat.SetColor("_EmissionColor", baseColor * 2f); // 強めの発光
    }
}
