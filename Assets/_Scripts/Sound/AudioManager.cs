using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Range(0f, 1f)]
    [SerializeField]
    private float globalVolume = 1.0f;

    [SerializeField]
    private SoundBank soundBank;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Play by sound ID
    public void PlaySound(string soundId, Vector3 position, float volumeScale = 1.0f)
    {
        AudioClip clip = soundBank.GetSound(soundId);
        if (clip != null)
        {
            PlaySoundAtPosition(clip, position, volumeScale);
        }
    }

    // Direct play with clip
    public void PlaySoundAtPosition(
        AudioClip clip,
        Vector3 position,
        float volume = 1.0f,
        float pitch = 1.0f,
        float spatialBlend = 1.0f
    )
    {
        if (clip == null)
            return;

        // Create a temporary GameObject for sound
        GameObject tempAudio = new GameObject("TempAudio_" + clip.name);
        tempAudio.transform.position = position;

        AudioSource source = tempAudio.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume * globalVolume;
        source.pitch = pitch;
        source.spatialBlend = spatialBlend;
        source.playOnAwake = false;

        source.Play();

        // Destroy after sound finishes playing
        Destroy(tempAudio, clip.length + 0.1f);

        // Notify AI system about sound
        float range = source.maxDistance;
        Sound sound = new Sound(position, range);
        Sounds.MakeSound(sound);
    }

    public void PlayLoopOnExistingSource(
        AudioSource source,
        // AudioClip clip,
        float volumeScale = 1.0f
    )
    {
        if (source == null)
            return;

        // source.clip = clip;
        source.volume = globalVolume * volumeScale;
        source.loop = true;
        source.spatialBlend = 1.0f;

        source.Play();

        // Notify AI system about sound
        float range = source.maxDistance;
        Sound sound = new Sound(source.transform.position, range);
        Sounds.MakeSound(sound);
    }

    public void StopLoopOnExistingSource(AudioSource source)
    {
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
    }

    public void PlayLocalSound(
        AudioClip clip,
        float volume = 1.0f,
        float pitch = 1.0f,
        bool loop = false,
        float spatialBlend = 0f
    )
    {
        if (clip == null)
            return;

        GameObject tempAudio = new GameObject("LocalAudio_" + clip.name);
        tempAudio.transform.parent = Camera.main.transform;

        AudioSource source = tempAudio.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume * globalVolume;
        source.pitch = pitch;
        source.spatialBlend = spatialBlend;
        source.playOnAwake = false;
        source.loop = loop;

        source.Play();

        if (!loop)
        {
            Destroy(tempAudio, clip.length + 0.1f);
        }
    }
}
