using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전역 사운드(SFX, BGM)와 볼륨 설정을 담당하는 싱글톤 매니저.
/// SFX는 AudioSource를 미리 풀링해두고(EffectPoolManager와 비슷한 방식) 재생 요청이 오면 비어있는
/// 소스를 꺼내 쓴다. 별도 프리팹/씬 세팅 없이 빈 GameObject에 이 스크립트만 붙이면 바로 동작한다
/// (SFX 풀, BGM용 AudioSource 모두 Awake에서 자동 생성).
/// 마스터/BGM/SFX 볼륨은 PlayerPrefs에 영구 저장되어 다음 실행에도 유지된다(골드 저장과 동일한 방식).
/// 무기 공격 사운드는 WeaponData.ResolvedAttackSounds를 PlayRandomSfx에 넘기는 식으로 사용한다
/// (PlayerAttack 참고). 피격음/UI음 등 다른 SFX도 같은 PlaySfx/PlayRandomSfx로 재생하면 된다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("BGM")]
    [Tooltip("비워두면 자동으로 전용 AudioSource를 하나 생성해서 사용한다.")]
    [SerializeField] private AudioSource bgmSource;

    [Header("SFX Pool")]
    [Tooltip("동시에 재생 가능한 SFX 개수. 무기 공격음처럼 짧고 빈번한 효과음 기준으로 넉넉하게 잡아둔다.")]
    [SerializeField] private int sfxPoolSize = 16;

    [Tooltip("한 프레임에 실제로 재생(PlaySfx)할 수 있는 효과음 최대 회수. 몬스터 대량 사망·픽업 대량 흡수처럼 같은 소리가 한 프레임에 수십 번 겹쳐 요청되면 CPU 부하는 물론이고 소리도 지저분해지므로, 이 값을 넘는 요청은 생략한다.")]
    [SerializeField, Min(1)] private int maxSfxPlaysPerFrame = 6;

    private int sfxPlaysRemainingThisFrame;

    [Header("Volume (0~1, PlayerPrefs에 영구 저장)")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    private const string MasterVolumeKey = "HDY_MasterVolume";
    private const string BgmVolumeKey = "HDY_BgmVolume";
    private const string SfxVolumeKey = "HDY_SfxVolume";

    private readonly List<AudioSource> sfxPool = new List<AudioSource>();
    private int nextSfxIndex = 0;

    public float MasterVolume => masterVolume;
    public float BgmVolume => bgmVolume;
    public float SfxVolume => sfxVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureBgmSource();
        BuildSfxPool();
        LoadVolumeSettings();
    }

    private void Update()
    {
        sfxPlaysRemainingThisFrame = Mathf.Max(1, maxSfxPlaysPerFrame);
    }

    private void EnsureBgmSource()
    {
        if (bgmSource != null) return;

        GameObject bgmGo = new GameObject("BgmSource");
        bgmGo.transform.SetParent(transform);
        bgmSource = bgmGo.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
    }

    private void BuildSfxPool()
    {
        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject sfxGo = new GameObject($"SfxSource_{i}");
            sfxGo.transform.SetParent(transform);
            AudioSource source = sfxGo.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            sfxPool.Add(source);
        }
    }

    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, masterVolume);
        bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, bgmVolume);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);
        ApplyBgmVolume();
    }

    /// <summary>클립 하나를 재생한다. volumeScale은 무기별 attackSoundVolume처럼 개별 보정치로 곱해진다.</summary>
    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxPool.Count == 0) return;

        // 한 프레임에 너무 많은 효과음 재생이 몰리면(대량 사망/픽업 대량 흡수 등) 이후 요청은 조용히 생략한다
        // (어차피 대량 동일 효과음은 겹쳐져도 응우 소음으로만 들려 체감 손실도 적다).
        if (sfxPlaysRemainingThisFrame <= 0)
        {
            return;
        }

        sfxPlaysRemainingThisFrame--;

        AudioSource source = GetAvailableSfxSource();
        source.pitch = 1f;
        source.volume = Mathf.Clamp01(masterVolume * sfxVolume * volumeScale);
        source.PlayOneShot(clip);
    }

    /// <summary>배열에서 무작위로 하나를 골라 재생한다. WeaponData.ResolvedAttackSounds에 사용한다.</summary>
    public void PlayRandomSfx(AudioClip[] clips, float volumeScale = 1f)
    {
        if (clips == null || clips.Length == 0) return;
        PlaySfx(clips[Random.Range(0, clips.Length)], volumeScale);
    }

    public void PlayBgm(AudioClip clip, bool loop = true)
    {
        if (bgmSource == null || clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        ApplyBgmVolume();
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyBgmVolume();
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.Save();
    }

    public void SetBgmVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        ApplyBgmVolume();
        PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
    }

    private void ApplyBgmVolume()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = Mathf.Clamp01(masterVolume * bgmVolume);
        }
    }

    private AudioSource GetAvailableSfxSource()
    {
        foreach (var source in sfxPool)
        {
            if (!source.isPlaying) return source;
        }

        AudioSource fallback = sfxPool[nextSfxIndex];
        nextSfxIndex = (nextSfxIndex + 1) % sfxPool.Count;
        return fallback;
    }
}
