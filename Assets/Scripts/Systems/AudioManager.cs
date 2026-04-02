using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] AudioSource bgmSource;
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioSource ambientSource;

    float _bgmVolume = 0.5f;
    float _sfxVolume = 0.7f;
    float _masterVolume = 1f;

    readonly Dictionary<string, AudioClip> _clipCache = new();
    Coroutine _fadeCoroutine;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource == null) bgmSource = CreateSource("BGM", true);
        if (sfxSource == null) sfxSource = CreateSource("SFX", false);
        if (ambientSource == null) ambientSource = CreateSource("Ambient", true);

        LoadVolumeSettings();
    }

    AudioSource CreateSource(string name, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.loop = loop;
        src.playOnAwake = false;
        return src;
    }

    AudioClip GetClip(string path)
    {
        if (_clipCache.TryGetValue(path, out var cached)) return cached;
        var clip = Resources.Load<AudioClip>(path);
        if (clip != null) _clipCache[path] = clip;
        return clip;
    }

    public void PlayBGM(string clipName, float fadeTime = 1f)
    {
        var clip = GetClip($"Audio/BGM/{clipName}");
        if (clip == null) { Debug.LogWarning($"[AudioManager] BGM not found: {clipName}"); return; }
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(CrossfadeBGM(clip, fadeTime));
    }

    public void StopBGM(float fadeTime = 0.5f)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeOut(bgmSource, fadeTime));
    }

    IEnumerator CrossfadeBGM(AudioClip newClip, float fadeTime)
    {
        float halfFade = fadeTime * 0.5f;
        float startVol = bgmSource.volume;

        for (float t = 0; t < halfFade; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(startVol, 0f, t / halfFade);
            yield return null;
        }

        bgmSource.clip = newClip;
        bgmSource.Play();
        float targetVol = _bgmVolume * _masterVolume;

        for (float t = 0; t < halfFade; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0f, targetVol, t / halfFade);
            yield return null;
        }
        bgmSource.volume = targetVol;
    }

    IEnumerator FadeOut(AudioSource source, float fadeTime)
    {
        float startVol = source.volume;
        for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
            yield return null;
        }
        source.Stop();
        source.volume = startVol;
    }

    public void PlaySFX(string clipName, float pitchVariation = 0f)
    {
        var clip = GetClip($"Audio/SFX/{clipName}");
        if (clip == null) return;
        if (pitchVariation > 0f)
            sfxSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        else
            sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(clip, _sfxVolume * _masterVolume);
    }

    public void PlaySFXAt(string clipName, Vector2 worldPos)
    {
        var clip = GetClip($"Audio/SFX/{clipName}");
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, worldPos, _sfxVolume * _masterVolume);
    }

    public void PlayAmbient(string clipName, float fadeTime = 1f)
    {
        var clip = GetClip($"Audio/Ambient/{clipName}");
        if (clip == null) return;
        if (ambientSource.clip == clip && ambientSource.isPlaying) return;
        ambientSource.clip = clip;
        ambientSource.volume = 0.3f * _masterVolume;
        ambientSource.Play();
    }

    public void StopAmbient() { ambientSource.Stop(); }

    public void SetBGMVolume(float vol)
    {
        _bgmVolume = Mathf.Clamp01(vol);
        bgmSource.volume = _bgmVolume * _masterVolume;
        PlayerPrefs.SetFloat("bgm_volume", _bgmVolume);
    }

    public void SetSFXVolume(float vol)
    {
        _sfxVolume = Mathf.Clamp01(vol);
        PlayerPrefs.SetFloat("sfx_volume", _sfxVolume);
    }

    public void SetMasterVolume(float vol)
    {
        _masterVolume = Mathf.Clamp01(vol);
        bgmSource.volume = _bgmVolume * _masterVolume;
        ambientSource.volume = 0.3f * _masterVolume;
        PlayerPrefs.SetFloat("master_volume", _masterVolume);
    }

    public float BGMVolume => _bgmVolume;
    public float SFXVolume => _sfxVolume;
    public float MasterVolume => _masterVolume;

    void LoadVolumeSettings()
    {
        _bgmVolume = PlayerPrefs.GetFloat("bgm_volume", 0.5f);
        _sfxVolume = PlayerPrefs.GetFloat("sfx_volume", 0.7f);
        _masterVolume = PlayerPrefs.GetFloat("master_volume", 1f);
        bgmSource.volume = _bgmVolume * _masterVolume;
        ambientSource.volume = 0.3f * _masterVolume;
    }
}
