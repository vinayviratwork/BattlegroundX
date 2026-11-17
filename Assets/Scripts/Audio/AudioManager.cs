using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Clips — assign in Inspector")]
    [SerializeField] private AudioClip shotClip;
    [SerializeField] private AudioClip reloadClip;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private AudioClip zoneWarningClip;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private AudioClip deathClip;

    [Header("Volume")]
    [Range(0,1)] [SerializeField] private float sfxVolume  = 0.8f;
    [Range(0,1)] [SerializeField] private float stepVolume = 0.4f;

    private AudioSource _src;
    private AudioSource _loopSrc;

    
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _src     = gameObject.AddComponent<AudioSource>();
        _loopSrc = gameObject.AddComponent<AudioSource>();
        _loopSrc.loop = true;
    }

    
    public void PlayShot()         => Play(shotClip,         sfxVolume);
    public void PlayReload()       => Play(reloadClip,       sfxVolume);
    public void PlayHit()          => Play(hitClip,          sfxVolume);
    public void PlayFootstep()     => Play(footstepClip,     stepVolume);
    public void PlayPickup()       => Play(pickupClip,       sfxVolume);
    public void PlayDeath()        => Play(deathClip,        sfxVolume);

    public void StartZoneWarning()
    {
        if (zoneWarningClip == null || (_loopSrc.isPlaying && _loopSrc.clip == zoneWarningClip)) return;
        _loopSrc.clip = zoneWarningClip;
        _loopSrc.volume = sfxVolume;
        _loopSrc.Play();
    }

    public void StopZoneWarning()
    {
        if (_loopSrc.isPlaying) _loopSrc.Stop();
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip == null || _src == null) return;
        _src.PlayOneShot(clip, volume);
    }
}