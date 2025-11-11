using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MonitorEffectController : MonoBehaviour
{
    private Material displayMat;
    private Material glassMat;

    void Start()
    {
        var renderer = GetComponent<MeshRenderer>();
        displayMat = renderer.materials[0]; // 背面
        glassMat = renderer.materials[1];   // 前面
    }

    void Update()
    {
        // 背面発光をゆらめかせる（0〜1の間で上下）
        float pulse = Mathf.Sin(Time.time * 3f) * 0.5f + 0.5f;
        Color baseColor = Color.cyan * (1.5f + pulse);
        displayMat.SetColor("_EmissionColor", baseColor);

        // ガラスの透明度を時間で変化（“生きてる”感じ）
        Color glassColor = glassMat.GetColor("_BaseColor");
        glassColor.a = 0.3f + pulse * 0.2f; // 0.3〜0.5の間で変化
        glassMat.SetColor("_BaseColor", glassColor);
    }
}
