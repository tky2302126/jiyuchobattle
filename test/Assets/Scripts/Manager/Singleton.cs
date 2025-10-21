using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Singleton : MonoBehaviour
{
    private static Singleton _instance;
    public static Singleton Instance 
    {
        get
        {
            if (_instance == null)
            {
                CreateInstance();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if(Instance != null && Instance != this) 
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitOnLoad() 
    {
        var handle = Addressables.LoadAssetAsync<GameObject>("Manager");
        handle.WaitForCompletion();
        var obj = Instantiate(handle.Result);
        _instance = obj.GetComponent<Singleton>();
        DontDestroyOnLoad(_instance.gameObject);
    }

    private static async UniTask CreateInstanceAsync()
    {
        if (_instance != null) return;

        // シーン内に既存の Singleton を探す
        _instance = FindObjectOfType<Singleton>();

        if (_instance == null)
        {
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>("Manager");
            await handle.Task;
            if(handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null) 
            {
                var obj = GameObject.Instantiate(handle.Result);
                _instance = obj.GetComponent<Singleton>();
            }
            else 
            {
                // 見つからなければ新規生成
                GameObject singletonObj = new GameObject(typeof(Singleton).Name);
                _instance = singletonObj.AddComponent<Singleton>();
            }
                
        }

        // シーンを跨いでも消えないように
        DontDestroyOnLoad(_instance.gameObject);
    }

    private static void CreateInstance() 
    {
        Addressables.LoadAssetAsync<GameObject>("Manager").Completed += handle =>
        {
            var obj = GameObject.Instantiate(handle.Result);
            _instance = obj.GetComponent<Singleton>();
        };
    }
}
