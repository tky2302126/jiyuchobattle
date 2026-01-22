using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace MySceneManager
{
    enum SceneTag : int
    {
        Title,
        Main,
        Result,
        Sample
    }

    public class MySceneManager : MonoBehaviour
    {
        private CancellationTokenSource cts;

        private void Awake()
        {
            cts = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            cts.Cancel();
            cts.Dispose();
        }

        public void LoadTitle() => LoadSceneAsync((int)SceneTag.Title).Forget();
        public void LoadMain() => LoadSceneAsync((int)SceneTag.Main).Forget();
        public void LoadResult() => LoadSceneAsync((int)SceneTag.Result).Forget();
        public void LoadSample() => LoadSceneAsync((int)SceneTag.Sample).Forget();

        private async UniTask LoadSceneAsync(int index)
        {
            // フェードアウト
            if (FadeManager.Instance != null)
            {
                await FadeManager.Instance.FadeOut(0.5f, cts.Token);
            }

            // BGM停止
            AudioManager.Instance.StopBGM();

            // シーンロード
            await SceneManager.LoadSceneAsync(index);

            // シーンロード後処理
            OnSceneLoaded(index);

            // フェードイン
            if (FadeManager.Instance != null)
            {
                await FadeManager.Instance.FadeIn(0.5f, cts.Token);
            }
        }

        private void OnSceneLoaded(int index)
        {
            switch ((SceneTag)index)
            {
                case SceneTag.Title:
                    AudioManager.Instance.PlayBGM("Title");
                    break;

                case SceneTag.Main:
                    AudioManager.Instance.PlaySE("SetUp1");
                    AudioManager.Instance.PlaySEThenBGM("SetUp2", "BattleSetup");
                    break;

                case SceneTag.Result:
                    break;

                case SceneTag.Sample:
                    break;
            }
        }

        private void Update()
        {
            if (!Input.GetKey(KeyCode.LeftControl)) return;

            if (Input.GetKeyDown(KeyCode.T)) LoadTitle();
            if (Input.GetKeyDown(KeyCode.M)) LoadMain();
            if (Input.GetKeyDown(KeyCode.R)) LoadResult();
            if (Input.GetKeyDown(KeyCode.S)) LoadSample();
        }
    }
}
