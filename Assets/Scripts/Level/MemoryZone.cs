using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A memory: a small thing in the world (a note, marks on a wall) that reminds the hero of home.
/// The first time the player steps on it, the memory is written in the middle of the screen,
/// a few soft piano notes play, and hope grows. From the design: memories fill the hope meter.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MemoryZone : MonoBehaviour
{
    private const int CanvasSortingOrder = 30;

    [TextArea]
    [SerializeField] private string memory;

    [Tooltip("Hope gained from this memory")]
    [SerializeField] private int hope = 15;

    [SerializeField] private float showSeconds = 5f;

    // True while any memory is on the screen. Two memories found close together are shown one after the other.
    private static bool isShowing;

    private bool found;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (found || other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }
        found = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddHope(hope);
        }
        Sfx.Play("Memory", 0.9f);
        StartCoroutine(ShowMemory());
    }

    private IEnumerator ShowMemory()
    {
        while (isShowing)
        {
            yield return null;
        }
        isShowing = true;

        Canvas canvas = UiFactory.CreateCanvas("Memory", CanvasSortingOrder);
        var group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        Text text = UiFactory.CreateTitle(canvas.transform, "Memory", new Vector2(0.5f, 0.5f), new Vector2(1300f, 200f), 46, UiTheme.Hope);
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchoredPosition = new Vector2(0f, 170f);
        text.font = UiTheme.ItalicFont;
        text.text = memory;

        yield return UiAnim.Fade(group, 1f, 1.5f);
        yield return new WaitForSeconds(showSeconds);
        yield return UiAnim.Fade(group, 0f, 2f);
        isShowing = false;
        Destroy(canvas.gameObject);
        Destroy(gameObject);
    }
}
