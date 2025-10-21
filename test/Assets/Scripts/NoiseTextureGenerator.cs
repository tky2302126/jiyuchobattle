using UnityEngine;

public class NoiseTextureGenerator : MonoBehaviour
{
    [ContextMenu("Generate Noise Texture")]
    public void GenerateNoise()
    {
        int width = 512;
        int height = 512;
        Texture2D tex = new Texture2D(width, height, TextureFormat.R8, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = Random.value > 0.5f ? 1f : 0f; // 白か黒
                tex.SetPixel(x, y, new Color(value, value, value, 1f));
            }
        }

        tex.Apply();

        // アセットとして保存する場合
#if UNITY_EDITOR
        string path = "Assets/Noise512.png";
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log("ノイズ画像生成完了: " + path);
#endif
    }
}
