using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioParameters _parameters;
    [SerializeField] private AudioSource _soundSource;
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioMusicType _musicType = AudioMusicType.MainMenu;
    [SerializeField] private bool _playMusicOnStart = true;
    [SerializeField] private float _enemyShootCooldown = 0.12f;

    private float _lastEnemyShootTime = -999f;

    private void Awake()
    {
        EnsureSources();
        Global.RegisterAudioManager(this);
    }

    private void Start()
    {
        if (_playMusicOnStart)
            PlayMusic(_musicType);
    }

    private void OnDestroy()
    {
        Global.UnregisterAudioManager(this);
    }

    public void PlaySound(AudioSfxType type)
    {
        if (_parameters == null || _soundSource == null)
            return;

        if (_parameters.TryGetSound(type, out AudioClip clip))
            _soundSource.PlayOneShot(clip);
    }

    public void PlayEnemyShoot()
    {
        if (Time.unscaledTime - _lastEnemyShootTime < _enemyShootCooldown)
            return;

        _lastEnemyShootTime = Time.unscaledTime;
        PlaySound(AudioSfxType.EnemyShoot);
    }

    public void PlayMusic(AudioMusicType type)
    {
        if (_parameters == null || _musicSource == null)
            return;

        if (_parameters.TryGetMusic(type, out AudioClip clip) == false)
            return;

        if (_musicSource.clip == clip && _musicSource.isPlaying)
            return;

        _musicSource.clip = clip;
        _musicSource.loop = true;
        _musicSource.Play();
    }

    private void EnsureSources()
    {
        if (_soundSource == null)
            _soundSource = gameObject.AddComponent<AudioSource>();

        if (_musicSource == null)
            _musicSource = gameObject.AddComponent<AudioSource>();

        _soundSource.playOnAwake = false;
        _musicSource.playOnAwake = false;
        _musicSource.loop = true;
    }
}
