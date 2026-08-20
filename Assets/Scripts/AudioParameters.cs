using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioParameters", menuName = "Configs/Audio Parameters")]
public class AudioParameters : ScriptableObject
{
    [Header("Sounds")]
    [SerializeField] private AudioSfxClipEntry[] _sounds = Array.Empty<AudioSfxClipEntry>();

    [Header("Music")]
    [SerializeField] private AudioMusicClipEntry[] _music = Array.Empty<AudioMusicClipEntry>();

    public bool TryGetSound(AudioSfxType type, out AudioClip clip)
    {
        return TryGetClip(_sounds, type, out clip);
    }

    public bool TryGetMusic(AudioMusicType type, out AudioClip clip)
    {
        return TryGetClip(_music, type, out clip);
    }

    private static bool TryGetClip(AudioSfxClipEntry[] entries, AudioSfxType type, out AudioClip clip)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].SfxType == type)
            {
                clip = entries[i].Clip;
                return clip != null;
            }
        }

        clip = null;
        return false;
    }

    private static bool TryGetClip(AudioMusicClipEntry[] entries, AudioMusicType type, out AudioClip clip)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].MusicType == type)
            {
                clip = entries[i].Clip;
                return clip != null;
            }
        }

        clip = null;
        return false;
    }
}

[Serializable]
public struct AudioSfxClipEntry
{
    [SerializeField] private AudioSfxType _sfxType;
    [SerializeField] private AudioClip _clip;

    public AudioSfxType SfxType => _sfxType;
    public AudioClip Clip => _clip;
}

[Serializable]
public struct AudioMusicClipEntry
{
    [SerializeField] private AudioMusicType _musicType;
    [SerializeField] private AudioClip _clip;

    public AudioMusicType MusicType => _musicType;
    public AudioClip Clip => _clip;
}

public enum AudioSfxType
{
    PlayerShoot,
    EnemyShoot,
    EnemyHit,
    DestructibleHit,
    Explosion,
    Victory,
    Defeat
}

public enum AudioMusicType
{
    MainMenu,
    Gameplay
}
