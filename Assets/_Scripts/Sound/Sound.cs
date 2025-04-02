using UnityEngine;

public class Sound
{
    public enum SoundType
    {
        Default = -1, // Initialize to -1 to indicate invalid sound type
    }

    public Sound(Vector3 _pos, float _range)
    {
        pos = _pos;
        range = _range;
    }

    public readonly Vector3 pos;
    public readonly float range;
    public SoundType soundType;
}
