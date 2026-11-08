using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the player's values from the GameManager in the top-left corner, and what to do next.
/// The text is created here in code, when the game starts.
/// </summary>
public class StatusHud : MonoBehaviour
{
    // Higher than the minimap's canvas, so the HUD is drawn on top.
    private const int CanvasSortingOrder = 10;

    [SerializeField] private int fontSize = 40;
    [SerializeField] private Vector2 margin = new Vector2(30f, 30f);
    [SerializeField] private Vector2 size = new Vector2(1100f, 160f);

    private Text text;

    // The values shown now, so the text is rebuilt only when one of them changes.
    private int shownHealth = -1;
    private int shownCaptures = -1;
    private bool shownHasBottle;

    private void Start()
    {
        Canvas canvas = UiFactory.CreateCanvas("HUD Canvas", CanvasSortingOrder);
        Vector2 topLeft = new Vector2(0f, 1f);
        text = UiFactory.CreateText(canvas.transform, "Status", topLeft, size, fontSize, TextAnchor.UpperLeft);
        text.rectTransform.anchoredPosition = new Vector2(margin.x, -margin.y);
    }

    private void Update()
    {
        GameManager game = GameManager.Instance;
        if (game == null || text == null)
        {
            return;
        }

        if (game.Health == shownHealth && game.Captures == shownCaptures && game.HasBottle == shownHasBottle)
        {
            return;
        }

        shownHealth = game.Health;
        shownCaptures = game.Captures;
        shownHasBottle = game.HasBottle;

        string goal = game.HasBottle
            ? "You have the water. Bring it back to the mattress."
            : "Find the water bottle in the storage room. Stay out of the camera's light.";
        text.text = $"Health {game.Health}/{game.MaxHealth}    Caught {game.Captures} times\n{goal}";
    }
}
