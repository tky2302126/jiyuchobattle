using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MeshRenderer backRenderer;      // 背面の MeshRenderer を割り当て
    [SerializeField] private Material normalMaterial;       // 通常表示用マテリアル（アセット）
    [SerializeField] private Material glitchMaterial;       // グリッチ用マテリアル（アセット）

    // インスタンス化されたマテリアルを保持しておく（変更を戻すため）
    private Material _instanceMaterial;

    // グリッチシェーダー側の強度プロパティ名（シェーダーによって異なる）
    private const string GlitchIntensityProp = "_GlitchIntensity";

    private void Awake()
    {
        if (backRenderer == null)
            backRenderer = GetComponentInChildren<MeshRenderer>();

        // 最初は正常なマテリアルをインスタンス化して使う（必要なら）
        if (backRenderer != null && normalMaterial != null)
        {
            // material を使うと renderer のマテリアルがインスタンス化される
            backRenderer.material = new Material(normalMaterial);
            _instanceMaterial = backRenderer.material;
        }
    }

    private void Start()
    {
        ApplyGlitch();
    }

    /// <summary>
    /// グリッチ用マテリアルに差し替える（即時）
    /// </summary>
    public void ApplyGlitch()
    {
        if (backRenderer == null || glitchMaterial == null) return;

        // 新しいインスタンスを作って割り当て（共有アセットを汚さない）
        _instanceMaterial = new Material(glitchMaterial);
        backRenderer.material = _instanceMaterial;
    }

    /// <summary>
    /// 通常のマテリアルに即時で戻す
    /// </summary>
    public void RevertToNormal()
    {
        if (backRenderer == null || normalMaterial == null) return;

        _instanceMaterial = new Material(normalMaterial);
        backRenderer.material = _instanceMaterial;
    }

    /// <summary>
    /// グリッチ強度を徐々に下げてから通常マテリアルに戻す（シェーダーに該当プロパティが必要）
    /// </summary>
    public void RevertToNormalFade(float duration = 0.5f)
    {
        if (backRenderer == null || normalMaterial == null) return;
        // コルーチンでフェード処理
        StopCoroutine(nameof(FadeOutGlitchAndRevert));
        StartCoroutine(FadeOutGlitchAndRevert(duration));
    }

    private IEnumerator FadeOutGlitchAndRevert(float duration)
    {
        if (_instanceMaterial == null)
        {
            // 現在のマテリアルがインスタンス化されていなければ作る
            _instanceMaterial = backRenderer.material;
        }

        // グリッチ強度プロパティがない場合は即時戻し
        if (!_instanceMaterial.HasProperty(GlitchIntensityProp))
        {
            RevertToNormal();
            yield break;
        }

        float start = _instanceMaterial.GetFloat(GlitchIntensityProp);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float value = Mathf.Lerp(start, 0f, t);
            _instanceMaterial.SetFloat(GlitchIntensityProp, value);
            yield return null;
        }

        // 完全に0にしてから通常マテリアルに戻す（メモリ的に新しいインスタンスを作る）
        _instanceMaterial.SetFloat(GlitchIntensityProp, 0f);
        RevertToNormal();
    }

    private void OnDestroy()
    {
        // 動的に生成したマテリアルは明示的に破棄しておく
        if (_instanceMaterial != null)
        {
            Destroy(_instanceMaterial);
            _instanceMaterial = null;
        }
    }
}
