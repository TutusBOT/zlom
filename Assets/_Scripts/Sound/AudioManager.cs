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
}
