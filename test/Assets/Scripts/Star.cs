using UnityEngine;

public class StarEffectController : MonoBehaviour
{
    [Header("Color")]
    [SerializeField] private Color effectColor = Color.cyan;

    [Header("Rotation")]
    [SerializeField] private float rotateSpeed = 90f;

    [Header("Scale")]
    [SerializeField] private float scaleAmplitude = 0.2f;
    [SerializeField] private float scaleSpeed = 2f;

    private Renderer rend;
    private MaterialPropertyBlock mpb;

    private Vector3 baseScale;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
        baseScale = transform.localScale;
        SetColor();
    }

    void Update()
    {
        transform.Rotate(Vector3.forward * rotateSpeed * Time.deltaTime);

        float scale = 1f + Mathf.Sin(Time.time * scaleSpeed) * scaleAmplitude;
        transform.localScale = baseScale * scale;
    }

    public void SetColor()
    {
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", effectColor);
        rend.SetPropertyBlock(mpb);
    }
}
