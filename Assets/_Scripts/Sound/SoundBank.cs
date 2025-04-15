using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundBank", menuName = "Audio/Sound Bank")]
public class SoundBank : ScriptableObject
{
    [System.Serializable]
    public class SoundEntry
    {
        public string id;
        public AudioClip[] clips;
        public float volume = 1.0f;
        public float pitch = 1.0f;

        [Range(0, 1)]
        public float spatialBlend = 1.0f;
        public float minDistance = 1f;
        public float maxDistance = 50f;
        public bool randomizePitch = true;
        public Vector2 pitchRange = new Vector2(0.9f, 1.1f);
    }

    public List<SoundEntry> sounds = new List<SoundEntry>();
    private Dictionary<string, SoundEntry> _soundLookup;

    public AudioClip GetSound(string id)
    {
        if (_soundLookup == null)
        {
            InitializeLookup();
        }

        if (_soundLookup.TryGetValue(id, out SoundEntry entry))
        {
            // Return random clip from array if available
            if (entry.clips != null && entry.clips.Length > 0)
            {
                return entry.clips[Random.Range(0, entry.clips.Length)];
            }
        }

        Debug.LogWarning($"Sound ID not found: {id}");
        return null;
    }

    public SoundEntry GetSoundEntry(string id)
    {
        if (_soundLookup == null)
        {
            InitializeLookup();
        }

        if (_soundLookup.TryGetValue(id, out SoundEntry entry))
        {
            return entry;
        }

        return null;
    }

    public string GetRandomSoundId()
    {
        if (_soundLookup == null)
        {
            InitializeLookup();
        }

        if (_soundLookup.Count > 0)
        {
            List<string> allSoundIds = new List<string>(_soundLookup.Keys);
            return allSoundIds[Random.Range(0, allSoundIds.Count)];
        }

        Debug.LogWarning("Sound bank is empty!");
        return null;
    }

    private void InitializeLookup()
    {
        _soundLookup = new Dictionary<string, SoundEntry>();

        foreach (var sound in sounds)
        {
            if (!string.IsNullOrEmpty(sound.id))
            {
                _soundLookup[sound.id] = sound;
            }
        }
    }
}
