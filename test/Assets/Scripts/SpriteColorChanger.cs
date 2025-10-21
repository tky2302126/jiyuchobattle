using System;
using UnityEngine;
using uPalette.Runtime.Core;
using uPalette.Runtime.Core.Synchronizer.Color;
using uPalette.Generated;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteColorChanger : ColorSynchronizer<SpriteRenderer>
{

    [SerializeField] private SpriteRenderer spriteRenderer;

    private IDisposable colorSubscription;

    protected override Color GetValue()
    {
        return Component.color;
    }

    protected override void SetValue(Color value)
    {
        Component.color = value;
    }

    public bool EqualsToCurrentValue(Color value)
    {
        return Component.color == value;
    }

    void Start()
    {
        // アタッチされてなかったら警告
        if(spriteRenderer == null) 
        {
            Debug.LogAssertion("スプライトレンダラーがアタッチされていません");
        }

       // 赤色に変更
       //  spriteRenderer.color = Color.red;

        // 好きな色 (R,G,B,A) で指定も可能
        // spriteRenderer.color = new Color(0.2f, 0.8f, 1f, 1f);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.N)) 
        {
            SetColorByEntryId(ColorEntry.Noun.ToEntryId());
        }

        if (Input.GetKey(KeyCode.V))
        {
            SetColorByEntryId(ColorEntry.Verb.ToEntryId());
        }

        if (Input.GetKey(KeyCode.A))
        {
            SetColorByEntryId(ColorEntry.Adjective.ToEntryId());
        }   
#endif
    }

    public void SetColorByEntryId(string id) 
    {
        if (string.IsNullOrEmpty(id)) return;

        // 前の購読があれば解除
        colorSubscription?.Dispose();

        var palette = PaletteStore.Instance?.ColorPalette;
        if (palette == null || !palette.Entries.ContainsKey(id))
        {
            Debug.LogWarning($"uPalette: エントリ '{id}' が見つかりません");
            return;
        }

        // 色プロパティを取得
        var colorProp = palette.GetActiveValue(id);

        // 初回適用
        spriteRenderer.color = colorProp.Value;

        // 変化も監視
        colorSubscription = colorProp.Subscribe(new ActionObserver<Color>(c => spriteRenderer.color = c));
    }

    // Subscribe 用の簡易 IObserver<T>
    private class ActionObserver<T> : IObserver<T>
    {
        private readonly Action<T> onNext;
        public ActionObserver(Action<T> onNext) => this.onNext = onNext;
        public void OnNext(T value) => onNext(value);
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }

    // 動的に変更したい場合はこういうメソッドを用意
    public void SetColor(Color newColor)
    {
        spriteRenderer.color = newColor;
    }
}
