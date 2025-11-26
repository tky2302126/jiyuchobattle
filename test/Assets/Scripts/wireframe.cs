using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

public class WireframeEffect : MonoBehaviour
{
    // 元のインデックスを保持する辞書
    private Dictionary<Mesh, int[]> originalIndices = new Dictionary<Mesh, int[]>();

    void Start()
    {
        ApplyWireframe();
        // 例: 3秒後に元に戻す
        Invoke(nameof(RestoreOriginal), 3f);
    }

    // ワイヤーフレーム化
    public void ApplyWireframe()
    {
        var meshFilters = GetComponentsInChildren<MeshFilter>(true);
        foreach (var mf in meshFilters)
        {
            if (!originalIndices.ContainsKey(mf.mesh))
            {
                // 元のインデックスを保存
                originalIndices[mf.mesh] = mf.mesh.GetIndices(0);
            }
            // ワイヤーフレーム化
            mf.mesh.SetIndices(originalIndices[mf.mesh], MeshTopology.Lines, 0);
        }
    }

    // 元に戻す
    public async UniTask RestoreOriginal()
    {
        var meshFilters = GetComponentsInChildren<MeshFilter>(true);
        foreach (var mf in meshFilters)
        {
            if (originalIndices.TryGetValue(mf.mesh, out var indices))
            {
                mf.mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            }
            await UniTask.Delay(100);
        }
    }

    //[SerializeField] private Material wireMaterial;      // Shader Graphで作った線用
    //private Material[] originalMaterials;                // 元のマテリアルを保存
    //private MeshRenderer[] meshRenderers;

    //[SerializeField] private float fadeDuration = 2f;

    //void Start()
    //{
    //    meshRenderers = GetComponentsInChildren<MeshRenderer>(true);

    //    // 元マテリアルを保存
    //    originalMaterials = new Material[meshRenderers.Length];
    //    for (int i = 0; i < meshRenderers.Length; i++)
    //    {
    //        originalMaterials[i] = meshRenderers[i].material;
    //        meshRenderers[i].material = new Material(wireMaterial); // ワイヤーフレームに切替
    //    }

    //    // フェード開始
    //    StartCoroutine(FadeToOriginal());
    //}

    //private System.Collections.IEnumerator FadeToOriginal()
    //{
    //    float elapsed = 0f;

    //    while (elapsed < fadeDuration)
    //    {
    //        elapsed += Time.deltaTime;
    //        float t = elapsed / fadeDuration;

    //        // Shaderのパラメータで線→面フェード
    //        foreach (var r in meshRenderers)
    //        {
    //            r.material.SetFloat("_WireAmount", 1f - t);
    //        }

    //        yield return null;
    //    }

    //    // 最終的に元マテリアルに戻す
    //    for (int i = 0; i < meshRenderers.Length; i++)
    //    {
    //        meshRenderers[i].material = originalMaterials[i];
    //    }
    //}
}

