using UnityEngine;
using System.Collections.Generic;

public enum SoundType
{
    Shoot_Pistol,
    Load_Pistol,
    Shoot_Spreadshooter,
    Load_Spreadshooter,
    Shoot_SMG,
    Load_SMG,
    Dash,
    EnemyHit,
    EnemyDeath,
    LoopRewind,
    LoopStart,
    Win
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [System.Serializable]
    public struct SoundClip
    {
        public SoundType type;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    [Header("Audio Source Pool")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("Clips")]
    public List<SoundClip> sounds;

    private Dictionary<SoundType, SoundClip> soundDict;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        soundDict = new Dictionary<SoundType, SoundClip>();
        foreach (var s in sounds)
        {
            if (!soundDict.ContainsKey(s.type))
                soundDict.Add(s.type, s);
        }
    }

    public void PlaySound(SoundType type)
    {
        if (soundDict.ContainsKey(type))
        {
            SoundClip s = soundDict[type];
            sfxSource.pitch = Random.Range(0.9f, 1.1f);
            sfxSource.PlayOneShot(s.clip, s.volume);
        }
        else
        {
            Debug.LogWarning($"Sound {type} not found!");
        }
    }

    public void PlayMusic(AudioClip musicClip)
    {
        musicSource.clip = musicClip;
        musicSource.loop = true;
        musicSource.Play();
    }
}