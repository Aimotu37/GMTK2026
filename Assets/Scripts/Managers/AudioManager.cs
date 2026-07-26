using System.Collections;
using System.Collections.Generic;
using GMTK.Audio;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

/// <summary>
/// 统一管理背景音乐和音效的加载、播放、并发与音量。
/// </summary>
public class AudioManager : SingletonMono<AudioManager>
{
    private const int MaxSfxVoices = 8;
    private const string GameAudioMixerResourcePath = "Audio/GameAudio";
    private const string BgmVolumeParameter = "BGMVolumeDb";
    private const string SfxVolumeParameter = "SFXVolumeDb";

    private readonly string _audioRootKey = "prefab/audio/audio_root";
    private readonly string _bgmKeyPrefix = "audio/bgm/";
    private readonly string _sfxKeyPrefix = "audio/sfx/";

    private GameObject _audioRoot;
    private GameObject _bgmGameObject;
    private GameObject _sfxGameObject;
    private AudioSource bgmSource;

    private readonly List<ActiveSfxVoice> _activeSfxVoices = new List<ActiveSfxVoice>();
    private readonly Dictionary<string, SfxProfile> _pendingSfxProfiles =
        new Dictionary<string, SfxProfile>();
    private readonly Dictionary<string, float> _lastSfxTriggerTimes =
        new Dictionary<string, float>();
    private readonly HashSet<string> _warnedFallbackKeys = new HashSet<string>();
    private readonly Dictionary<string, AudioClip> audioResources =
        new Dictionary<string, AudioClip>();

    private AudioMixer _audioMixer;
    private AudioMixerGroup _bgmMixerGroup;
    private AudioMixerGroup _sfxMixerGroup;
    private AudioMixerGroup _uiMixerGroup;
    private AudioMixerGroup _interactionMixerGroup;
    private AudioMixerGroup _resultMixerGroup;

    private float _bgmVolume = 0.4f;
    private float _sfxVolume = 0.4f;
    private bool _isInitialized;
    private bool _initFailed;
    private bool _audioRootLoad;
    private int _sfxLoadGeneration;

    public float BgmVolume => _bgmVolume;
    public float SfxVolume => _sfxVolume;
    public bool IsInitialized => _isInitialized;

    private sealed class ActiveSfxVoice
    {
        public string Key;
        public AudioSource Source;
        public SfxProfile Profile;
        public float StartedAt;
    }

    public IEnumerator InitializeAsync()
    {
        if (_isInitialized)
            yield break;

        _initFailed = false;
        _audioRootLoad = false;
        ResourcesManager.Instance.AddressablesLoadAsync<GameObject>(_audioRootKey, HandlAuidoObjectLoad);

        yield return new WaitUntil(() => _audioRootLoad);

        if (!_initFailed && !TryInitializeMixer())
            _initFailed = true;

        if (_initFailed)
        {
            _isInitialized = false;
            Debug.LogError(name + " Initialization Failed.");
            yield break;
        }

        _isInitialized = true;
        // 保留设置系统作为音量的唯一来源。
        SetBGMVolume(SettingsManager.Instance.BgmVolume);
        SetSoundVolume(SettingsManager.Instance.SfxVolume);
        Debug.Log(name + " Initialization Successful.");
    }

    private void Update()
    {
        PruneCompletedSfxVoices();
    }

    public void StartPlayBGM(string name)
    {
        if (bgmSource == null || string.IsNullOrEmpty(name))
            return;

        if (audioResources.TryGetValue(name, out AudioClip cachedClip))
        {
            PlayBgmClip(cachedClip);
            return;
        }

        ResourcesManager.Instance.AddressablesLoadAsync<AudioClip>(_bgmKeyPrefix + name, clip =>
        {
            if (clip == null || bgmSource == null)
            {
                Debug.LogError($"Failed to load BGM '{name}'.");
                return;
            }

            audioResources[name] = clip;
            PlayBgmClip(clip);
        });
    }

    public void StartPlayBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null)
            return;

        PlayBgmClip(clip);
    }

    public void StartPlaySound(string name, bool isLoop, UnityAction<AudioSource> callback = null)
    {
        if (string.IsNullOrEmpty(name) || _sfxGameObject == null)
        {
            Debug.LogWarning("Cannot play an SFX before AudioManager is initialized or without a key.");
            return;
        }

        bool hasExplicitProfile = SfxPlaybackPolicy.TryGetProfile(name, out SfxProfile profile);
        if (!hasExplicitProfile)
        {
            profile = SfxPlaybackPolicy.GetProfileOrFallback(name);
            if (_warnedFallbackKeys.Add(name))
            {
                Debug.LogWarning($"SFX '{name}' has no playback profile; using the UI fallback group.");
            }
        }

        PruneCompletedSfxVoices();
        if (_pendingSfxProfiles.ContainsKey(name))
            return;

        ActiveSfxVoice activeVoice = FindActiveVoice(name);
        float secondsSinceLastTrigger = _lastSfxTriggerTimes.TryGetValue(name, out float lastTriggerTime)
            ? Time.unscaledTime - lastTriggerTime
            : float.MaxValue;
        SfxTriggerAction action = SfxPlaybackPolicy.DecideSameKeyTrigger(
            profile,
            activeVoice != null,
            secondsSinceLastTrigger);

        if (action == SfxTriggerAction.Ignore)
            return;

        if (action == SfxTriggerAction.Restart)
            RemoveActiveVoice(activeVoice, true);

        if (!TryMakeRoomFor(profile))
            return;

        if (audioResources.TryGetValue(name, out AudioClip cachedClip))
        {
            StartLoadedSfx(name, cachedClip, isLoop, profile, callback);
            return;
        }

        _pendingSfxProfiles[name] = profile;
        int loadGeneration = _sfxLoadGeneration;
        ResourcesManager.Instance.AddressablesLoadAsync<AudioClip>(_sfxKeyPrefix + name, clip =>
        {
            if (loadGeneration != _sfxLoadGeneration)
            {
                if (clip != null)
                    ResourcesManager.Instance.ReleaseAddressable(clip);
                return;
            }

            _pendingSfxProfiles.Remove(name);
            if (clip == null)
            {
                Debug.LogError($"Failed to load SFX '{name}'.");
                return;
            }

            audioResources[name] = clip;
            StartLoadedSfx(name, clip, isLoop, profile, callback);
        });
    }

    public void StartPlaySound(AudioClip clip, bool isLoop, UnityAction<AudioSource> callback = null)
    {
        if (clip == null || _sfxGameObject == null)
            return;

        SfxProfile profile = SfxPlaybackPolicy.GetProfileOrFallback(string.Empty);
        if (!TryMakeRoomFor(profile))
            return;

        StartLoadedSfx(null, clip, isLoop, profile, callback);
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
        if (bgmSource != null)
            bgmSource.Pause();
    }

    public void ResumePlayBGM()
    {
        if (bgmSource != null)
            bgmSource.UnPause();
    }

    public void SetBGMVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp(volume, 0f, 1f);
        if (_audioMixer != null &&
            !_audioMixer.SetFloat(BgmVolumeParameter, AudioVolumeUtility.NormalizedToDecibels(_bgmVolume)))
        {
            Debug.LogWarning($"Audio mixer parameter '{BgmVolumeParameter}' is unavailable.");
        }
    }

    public void SetSoundVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp(volume, 0f, 1f);
        if (_audioMixer != null &&
            !_audioMixer.SetFloat(SfxVolumeParameter, AudioVolumeUtility.NormalizedToDecibels(_sfxVolume)))
        {
            Debug.LogWarning($"Audio mixer parameter '{SfxVolumeParameter}' is unavailable.");
        }
    }

    public void StopPlaySound(AudioSource source)
    {
        for (int i = _activeSfxVoices.Count - 1; i >= 0; i--)
        {
            if (_activeSfxVoices[i].Source == source)
            {
                RemoveActiveVoice(_activeSfxVoices[i], true);
                return;
            }
        }
    }

    public void StopPlayAllSound()
    {
        _sfxLoadGeneration++;
        _pendingSfxProfiles.Clear();
        _lastSfxTriggerTimes.Clear();
        for (int i = _activeSfxVoices.Count - 1; i >= 0; i--)
            RemoveActiveVoice(_activeSfxVoices[i], true);
    }

    public void ReleaseAudioSources()
    {
        StopPlayAllSound();
        foreach (KeyValuePair<string, AudioClip> pair in audioResources)
            ResourcesManager.Instance.ReleaseAddressable(pair.Value);

        audioResources.Clear();
        _warnedFallbackKeys.Clear();
    }

    private void PlayBgmClip(AudioClip clip)
    {
        bgmSource.clip = clip;
        bgmSource.volume = 1f;
        bgmSource.outputAudioMixerGroup = _bgmMixerGroup;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    private void HandlAuidoObjectLoad(GameObject audioGameObject)
    {
        if (audioGameObject == null)
        {
            _initFailed = true;
            _audioRootLoad = true;
            return;
        }

        _audioRoot = audioGameObject;
        Transform bgmTransform = audioGameObject.transform.Find("BGMGameObject");
        Transform sfxTransform = audioGameObject.transform.Find("SFXGameObject");
        if (bgmTransform == null || sfxTransform == null)
        {
            Debug.LogError("Audio root is missing BGMGameObject or SFXGameObject.");
            _initFailed = true;
            _audioRootLoad = true;
            return;
        }

        _bgmGameObject = bgmTransform.gameObject;
        bgmSource = _bgmGameObject.GetComponent<AudioSource>();
        if (bgmSource == null)
            bgmSource = _bgmGameObject.AddComponent<AudioSource>();

        _sfxGameObject = sfxTransform.gameObject;
        _audioRootLoad = true;
    }

    private bool TryInitializeMixer()
    {
        _audioMixer = Resources.Load<AudioMixer>(GameAudioMixerResourcePath);
        if (_audioMixer == null)
        {
            Debug.LogError($"Audio mixer was not found at Resources/{GameAudioMixerResourcePath}.");
            return false;
        }

        _bgmMixerGroup = FindMixerGroup("BGM");
        _sfxMixerGroup = FindMixerGroup("SFX");
        _uiMixerGroup = FindMixerGroup("UI");
        _interactionMixerGroup = FindMixerGroup("Interaction");
        _resultMixerGroup = FindMixerGroup("Result");
        if (_bgmMixerGroup == null || _sfxMixerGroup == null || _uiMixerGroup == null ||
            _interactionMixerGroup == null || _resultMixerGroup == null)
        {
            Debug.LogError("Game Audio mixer does not contain the required BGM/SFX category groups.");
            return false;
        }

        bgmSource.outputAudioMixerGroup = _bgmMixerGroup;
        bgmSource.volume = 1f;
        return true;
    }

    private AudioMixerGroup FindMixerGroup(string groupName)
    {
        AudioMixerGroup[] matches = _audioMixer.FindMatchingGroups(groupName);
        for (int i = 0; i < matches.Length; i++)
        {
            if (matches[i].name == groupName)
                return matches[i];
        }

        return null;
    }

    private void StartLoadedSfx(
        string key,
        AudioClip clip,
        bool isLoop,
        SfxProfile profile,
        UnityAction<AudioSource> callback)
    {
        if (clip == null || _sfxGameObject == null || !TryMakeRoomFor(profile))
            return;

        AudioSource source = _sfxGameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.clip = clip;
        source.volume = profile.Gain;
        source.loop = isLoop;
        source.outputAudioMixerGroup = GetMixerGroup(profile.Category);
        source.Play();

        float startedAt = Time.unscaledTime;
        _activeSfxVoices.Add(new ActiveSfxVoice
        {
            Key = key,
            Source = source,
            Profile = profile,
            StartedAt = startedAt
        });
        if (!string.IsNullOrEmpty(key))
            _lastSfxTriggerTimes[key] = startedAt;

        callback?.Invoke(source);
    }

    private AudioMixerGroup GetMixerGroup(SfxCategory category)
    {
        switch (category)
        {
            case SfxCategory.Interaction:
                return _interactionMixerGroup ?? _sfxMixerGroup;
            case SfxCategory.Result:
                return _resultMixerGroup ?? _sfxMixerGroup;
            default:
                return _uiMixerGroup ?? _sfxMixerGroup;
        }
    }

    private ActiveSfxVoice FindActiveVoice(string key)
    {
        for (int i = 0; i < _activeSfxVoices.Count; i++)
        {
            if (_activeSfxVoices[i].Key == key)
                return _activeSfxVoices[i];
        }

        return null;
    }

    private bool TryMakeRoomFor(SfxProfile incomingProfile)
    {
        PruneCompletedSfxVoices();
        if (_activeSfxVoices.Count + _pendingSfxProfiles.Count < MaxSfxVoices)
            return true;

        ActiveSfxVoice reclaimCandidate = null;
        for (int i = 0; i < _activeSfxVoices.Count; i++)
        {
            ActiveSfxVoice voice = _activeSfxVoices[i];
            if (voice.Source == null || !SfxPlaybackPolicy.CanReclaim(
                    voice.Profile.Category,
                    voice.Source.loop,
                    incomingProfile.Category))
            {
                continue;
            }

            if (reclaimCandidate == null || voice.StartedAt < reclaimCandidate.StartedAt)
                reclaimCandidate = voice;
        }

        if (reclaimCandidate == null)
            return false;

        RemoveActiveVoice(reclaimCandidate, true);
        return true;
    }

    private void PruneCompletedSfxVoices()
    {
        for (int i = _activeSfxVoices.Count - 1; i >= 0; i--)
        {
            AudioSource source = _activeSfxVoices[i].Source;
            if (source == null || !source.isPlaying)
                RemoveActiveVoice(_activeSfxVoices[i], false);
        }
    }

    private void RemoveActiveVoice(ActiveSfxVoice voice, bool stopSource)
    {
        if (voice == null)
            return;

        _activeSfxVoices.Remove(voice);
        if (voice.Source == null)
            return;

        if (stopSource)
            voice.Source.Stop();
        Destroy(voice.Source);
    }
}
