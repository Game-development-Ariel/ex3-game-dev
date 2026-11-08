using System.Collections;
using UnityEngine;

/// <summary>
/// Plays the music quietly under the level. It starts after a delay and fades in slowly,
/// so the first thing the player hears when waking up is breathing, not music.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    [SerializeField] private float startDelay = 9f;
    [SerializeField] private float fadeSeconds = 8f;
    [SerializeField] private float volume = 0.22f;

    private IEnumerator Start()
    {
        AudioSource music = GetComponent<AudioSource>();
        music.loop = true;
        music.volume = 0f;
        yield return new WaitForSeconds(startDelay);

        music.Play();
        for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
        {
            music.volume = Mathf.Lerp(0f, volume, t / fadeSeconds);
            yield return null;
        }
        music.volume = volume;
    }
}
