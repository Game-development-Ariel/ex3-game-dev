using UnityEngine;

/// <summary>
/// Plays short sound effects from anywhere: Sfx.Play("Water").
/// The sounds are in Assets/Resources/Audio. One hidden AudioSource plays them all,
/// and it survives scene changes, so a sound doesn't cut off when the scene loads.
/// </summary>
public static class Sfx
{
    private static AudioSource source;

    public static void Play(string name, float volume = 1f)
    {
        if (source == null)
        {
            var player = new GameObject("Sound Effects");
            Object.DontDestroyOnLoad(player);
            source = player.AddComponent<AudioSource>();
        }

        AudioClip clip = Resources.Load<AudioClip>("Audio/" + name);
        if (clip != null)
        {
            source.PlayOneShot(clip, volume);
        }
    }
}
