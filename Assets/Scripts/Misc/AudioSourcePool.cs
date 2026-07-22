using System.Collections.Generic;
using UnityEngine;

public class AudioSourcePool : MonoBehaviour
{
    private static AudioSourcePool instance;
    private static int maxAudioSources;
    private const int STARTING_SIZE = 12;
    private List<AudioSource> audioSources = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        maxAudioSources = 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject obj = new GameObject("AudioSourcePool");
        instance = obj.AddComponent<AudioSourcePool>();
    }

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
        maxAudioSources = AudioSettings.GetConfiguration().numRealVoices;

        if (audioSources.Count == 0)
        {
            for (int i = 0; i < STARTING_SIZE; i++)
            {
                AudioSource audioSource = gameObject.AddComponent<AudioSource>();
                audioSources.Add(audioSource);
            }
        }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void Play(AudioClip audioClip, float volume = 1.0f, float pitch = 1.0f, float pan = 0f)
    {
        if (!audioClip) return;

        AudioSource audioSource = instance.GetAvailableAudioSource();

        if (!audioSource)
        {
            if (instance.audioSources.Count >= maxAudioSources) return;

            audioSource = instance.gameObject.AddComponent<AudioSource>();
            instance.audioSources.Add(audioSource);
        }

        audioSource.volume = Random.Range(volume - 0.1f, volume + 0.1f);
        audioSource.pitch = Random.Range(pitch - 0.1f, pitch + 0.1f);
        audioSource.panStereo = Mathf.Clamp(pan, -1f, 1f);
        audioSource.clip = audioClip;
        audioSource.Play();
    }

    public static void Play(AudioClip[] audioClips, float volume = 1.0f, float pitch = 1.0f, float pan = 0f)
    {
        if (audioClips.Length > 0)
        {
            Play(audioClips[Random.Range(0, audioClips.Length)], volume, pitch, pan);
        }
    }

    private AudioSource GetAvailableAudioSource()
    {
        foreach (AudioSource audioSource in audioSources)
        {
            if (!audioSource.isPlaying) return audioSource;
        }

        return null;
    }
}