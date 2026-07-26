using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 音频管理器
/// 负责背景音乐和音效的播放、暂停、停止、音量控制等
/// 使用Resources加载音频资源
/// </summary>
public class AudioManager : SingletonMono<AudioManager>
{
    private GameObject _audioRoot;
    private GameObject _bgmGameObject;
    private GameObject _sfxGameObject;

    private AudioSource bgmSource;
    private List<AudioSource> sfxSourceList = new List<AudioSource>();
    //持有加载的资源
    private Dictionary<string, AudioClip> audioResources = new Dictionary<string, AudioClip>();

    private readonly string _audioRootKey = "prefab/audio/audio_root";

    // 背景音乐默认音量大小 0.7
    private float _bgmVolume = 0.7f;
    public float BgmVolume => _bgmVolume;
    //private readonly string BGMFilePath = "Audios/BGM/";
    private readonly string _bgmKeyPrefix = "audio/bgm/";

    // 音效默认音量大小 0.7
    private float _sfxVolume = 0.4f;
    public float SfxVolume => _sfxVolume;
    //private readonly string SFXFilePath = "Audios/SFX/";
    private readonly string _sfxKeyPrefix = "audio/sfx/";

    //初始化变量
    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;

    private bool _initFailed;
    private bool _audioRootLoad;

    public IEnumerator InitializeAsync()
    {
        if (_isInitialized)
        {
            yield break;
        }

        _initFailed = false;
        _audioRootLoad = false;
        if (!_audioRootLoad)
        {
            ResourcesManager.Instance.AddressablesLoadAsync<GameObject>(_audioRootKey, HandlAuidoObjectLoad);
        }

        yield return new WaitUntil(() => _audioRootLoad);

        if (_initFailed)
        {
            _isInitialized = false;
            Debug.LogError(this.name + " Initialization Failed.");
        }
        else
        {
            _isInitialized = true;
            //StartPlayBGM("testBgm");
            Debug.Log(this.name + " Initialization Successful.");
        }
    }

    private void Update()
    {
        // 每帧检查音效是否播放结束，销毁播放结束的音效源
        for (int i = 0; i < sfxSourceList.Count; i++)
        {
            if (!sfxSourceList[i].isPlaying)
            {
                GameObject.Destroy(sfxSourceList[i]);
                sfxSourceList.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 播放背景音乐
    /// 以名称作为加载和播放背景音乐的标识
    /// 使用Resources加载音效资源
    /// </summary>
    /// <param name="name">背景音乐名</param>
    public void StartPlayBGM(string name)
    {

        if (audioResources.ContainsKey(name))
        {
            AudioClip bgm = audioResources[name];
            bgmSource.clip = bgm;
            bgmSource.Play();
            bgmSource.loop = true;
            bgmSource.volume = _bgmVolume;
        }
        else
        {
            ResourcesManager.Instance.AddressablesLoadAsync<AudioClip>(_bgmKeyPrefix + name, (clip) =>
            {
                bgmSource.clip = clip;
                bgmSource.Play();
                bgmSource.loop = true;
                bgmSource.volume = _bgmVolume;
                audioResources.Add(name, clip);
            });
        }
    }

    public void StartPlayBGM(AudioClip clip)
    {
        bgmSource.clip = clip;
        bgmSource.Play();
        bgmSource.loop = true;
        bgmSource.volume = _bgmVolume;
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="name">音效名称</param>
    /// <param name="isLoop">是否循环</param>
    /// <param name="callback">回调函数</param>
    public void StartPlaySound(string name, bool isLoop, UnityAction<AudioSource> callback = null)
    {
        AudioSource sfxSource = _sfxGameObject.AddComponent<AudioSource>();
        if (audioResources.ContainsKey(name))
        {
            AudioClip sfx = audioResources[name];
            sfxSource.clip = sfx;
            sfxSource.volume = _sfxVolume;
            sfxSource.loop = isLoop;
            sfxSource.Play();
            sfxSourceList.Add(sfxSource);

            // 如果需要回调函数则执行回调函数
            callback?.Invoke(sfxSource);
        }
        else
        {
            ResourcesManager.Instance.AddressablesLoadAsync<AudioClip>(_sfxKeyPrefix + name, (clip) =>
            {
                sfxSource.clip = clip;
                sfxSource.volume = _sfxVolume;
                sfxSource.loop = isLoop;
                sfxSource.Play();
                sfxSourceList.Add(sfxSource);

                audioResources[name] = clip;

                // 如果需要回调函数则执行回调函数
                callback?.Invoke(sfxSource);
            });
        }
    }

    public void StartPlaySound(AudioClip clip, bool isLoop, UnityAction<AudioSource> callback = null)
    {
        AudioSource sfxSource = _sfxGameObject.AddComponent<AudioSource>();
        sfxSource.clip = clip;
        sfxSource.volume = _sfxVolume;
        sfxSource.loop = isLoop;
        sfxSource.Play();
        sfxSourceList.Add(sfxSource);
    }

    public void StopPlayBGM()
    {
        if (bgmSource == null)
            return;
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void PausePlayBGM()
    {
        if (bgmSource == null)
            return;
        bgmSource.Pause();
    }

    public void ResumePlayBGM()
    {
        if (bgmSource == null)
            return;
        bgmSource.UnPause();
    }

    public void SetBGMVolume(float volume)
    {
        // 限制音量在0到1之间
        _bgmVolume = Mathf.Clamp(volume, 0f, 1f);

        if (bgmSource != null)
        {
            bgmSource.volume = _bgmVolume;
        }
    }


    public void SetSoundVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp(volume, 0f, 1f);
        if (_sfxGameObject != null)
        {
            foreach (var source in sfxSourceList)
            {
                source.volume = _sfxVolume;
            }
        }
    }

    public void StopPlaySound(AudioSource source)
    {
        if (sfxSourceList.Contains(source))
        {
            sfxSourceList.Remove(source);
            source.Stop();
            GameObject.Destroy(source);
        }
    }

    public void StopPlayAllSound()
    {
        foreach (var source in sfxSourceList)
        {
            source.Stop();
            GameObject.Destroy(source);
        }
        sfxSourceList.Clear();
    }

    public void ReleaseAudioSources()
    {
        foreach (var pair in audioResources)
        {
            ResourcesManager.Instance.ReleaseAddressable(pair.Value);
        }
        audioResources.Clear();
    }

    private void HandlAuidoObjectLoad(GameObject audioGameObject)
    {
        if (audioGameObject == null)
        {
            _initFailed = true;
        }
        else
        {
            _audioRoot = audioGameObject;
            _bgmGameObject = audioGameObject.transform.Find("BGMGameObject").gameObject;
            bgmSource = _bgmGameObject.GetComponent<AudioSource>();

            _sfxGameObject = audioGameObject.transform.Find("SFXGameObject").gameObject;
        }
        _audioRootLoad = true;
    }
}
