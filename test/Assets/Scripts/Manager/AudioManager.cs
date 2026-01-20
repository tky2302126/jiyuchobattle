using MySceneManager;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AudioManager（BGM / SE 管理）
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;

    [Header("Audio Clips")]
    [SerializeField] private List<AudioClip> bgmClips;
    [SerializeField] private List<AudioClip> seClips;

    private Dictionary<string, AudioClip> bgmDict;
    private Dictionary<string, AudioClip> seDict;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        bgmDict = new Dictionary<string, AudioClip>();
        seDict = new Dictionary<string, AudioClip>();

        foreach (var clip in bgmClips)
        {
            if (!bgmDict.ContainsKey(clip.name))
                bgmDict.Add(clip.name, clip);
        }

        foreach (var clip in seClips)
        {
            if (!seDict.ContainsKey(clip.name))
                seDict.Add(clip.name, clip);
        }
    }

    // =====================
    // BGM
    // =====================
    public void PlayBGM(string name, bool loop = true)
    {
        if (!bgmDict.ContainsKey(name))
        {
            Debug.LogWarning($"BGM not found: {name}");
            return;
        }

        if (bgmSource.clip == bgmDict[name] && bgmSource.isPlaying)
            return;

        bgmSource.clip = bgmDict[name];
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = Mathf.Clamp01(volume);
    }

    // =====================
    // SE
    // =====================
    public void PlaySE(string name)
    {
        if (!seDict.ContainsKey(name))
        {
            Debug.LogWarning($"SE not found: {name}");
            return;
        }

        seSource.PlayOneShot(seDict[name]);
    }

    public void SetSEVolume(float volume)
    {
        seSource.volume = Mathf.Clamp01(volume);
    }

    public void PlaySEThenBGM(string seClip, string bgmClip)
    {
        StartCoroutine(PlaySEThenBGM_Coroutine(seClip, bgmClip));
    }

    private IEnumerator PlaySEThenBGM_Coroutine(string seClip, string bgmClip)
    {
        PlaySE(seClip);

        // SEの長さ分待つ
        yield return new WaitForSeconds(seDict[seClip].length);

        PlayBGM(bgmClip);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 起動直後の Title もここに来る
        switch ((SceneTag)scene.buildIndex)
        {
            case SceneTag.Title:
                AudioManager.Instance.PlayBGM("Title");
                break;
        }

    }

    // 特定のワードを含むものに限定して再生できる
    public void PlayRandomAttackSE(string containsKey = "Attack")
    {
        // 条件に合う SE を抽出
        List<AudioClip> candidates = new List<AudioClip>();

        foreach (var pair in seDict)
        {
            if (pair.Key.Contains(containsKey))
            {
                candidates.Add(pair.Value);
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"Random SE not found. keyword = {containsKey}");
            return;
        }

        // ランダム再生
        var clip = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        seSource.PlayOneShot(clip);
    }
}
