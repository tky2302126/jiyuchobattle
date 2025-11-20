using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeedSetter : MonoBehaviour
{
    void Start()
    {
        var renderer = GetComponent<Renderer>();
        var mpb = new MaterialPropertyBlock();
        if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Seed"))
        {
            float seed = Mathf.Abs(gameObject.GetInstanceID());
            renderer.GetPropertyBlock(mpb);
            mpb.SetFloat("_Seed", seed);
            renderer.SetPropertyBlock(mpb);

           // Debug.Log($"_Seed をセット: {seed}");
        }
        else
        {
            Debug.LogWarning("_Seed プロパティが見つかりません");
        }
    }

    bool HasSeedProperty(Renderer renderer)
    {
        return renderer.sharedMaterial != null
            && renderer.sharedMaterial.HasProperty("_Seed");
    }


}
