using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace MySceneManager
{
    /// <summary>
    /// ビルド設定順に設定
    /// </summary>
    enum SceneTag :int
    {
        Title,
        Main,
        Result,
        Sample
    }

    public class MySceneManager : MonoBehaviour
    {
        public void LoadSceneByIndex(int index) 
        {
            SceneManager.LoadScene(index);
        }

        public void LoadSceneAsync(int index) 
        {
            AudioManager.Instance.StopBGM();
            StartCoroutine(Load(index));
        }

        private IEnumerator Load(int index) 
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(index);

            while (!asyncLoad.isDone) 
            {
                Debug.Log("Loading progress: " + (asyncLoad.progress * 100f) + "%");
                yield return null;
            }

            OnSceneLoaded(index);
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
                    // AudioManager.Instance.PlayBGM("ResultBGM");
                    break;

                case SceneTag.Sample:
                    // AudioManager.Instance.PlayBGM("SampleBGM");
                    break;
            }
        }

        public void LoadTitle() 
        {
            LoadSceneAsync((int)SceneTag.Title);
        }

        public void LoadMain()
        {
            LoadSceneAsync((int)SceneTag.Main);
        }

        public void LoadResult()
        {
            LoadSceneAsync((int)SceneTag.Result);
        }

        public void LoadSample()
        {
            LoadSceneAsync((int)SceneTag.Sample);
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKey(KeyCode.T)) 
                {
                    LoadTitle();
                }

                if (Input.GetKey(KeyCode.M))
                {
                    LoadMain();
                }

                if (Input.GetKey(KeyCode.R))
                {
                    LoadResult();
                }

                if (Input.GetKey(KeyCode.S))
                {
                    LoadSample();
                }
            }
        }
    }






}

