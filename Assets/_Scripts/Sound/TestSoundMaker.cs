using UnityEngine;

public class TestSoundMaker : MonoBehaviour
{
    [SerializeField]
    private AudioSource source = null;
    public float range = 25f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlaySound();
        }
    }

    private void PlaySound()
    {
        if (source.isPlaying)
        {
            return;
        }

        source.Play();

        Sound sound = new Sound(transform.position, range);
        Debug.Log($"Sound created at position: {sound.pos}, with range: {sound.range}");

        Sounds.MakeSound(sound);
    }
}
