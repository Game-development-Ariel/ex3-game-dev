using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The screen after the level: a win or a game over, with the values the GameManager kept from the level,
/// and a button to play again. The UI is created here in code, when the scene starts.
/// A VerticalLayoutGroup stacks the parts one under the other, so they never overlap,
/// whatever the screen size or the length of the texts.
/// </summary>
public class EndScreen : MonoBehaviour
{
    private const int CanvasSortingOrder = 0;

    [SerializeField] private int titleFontSize = 110;
    [SerializeField] private int messageFontSize = 52;
    [SerializeField] private int statsFontSize = 44;
    [SerializeField] private int buttonFontSize = 52;

    [Tooltip("Space between the parts, in pixels of the reference screen")]
    [SerializeField] private float spacing = 50f;

    [SerializeField] private Vector2 buttonSize = new Vector2(480f, 130f);
    [SerializeField] private Color winColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color loseColor = new Color(1f, 0.3f, 0.25f);
    [SerializeField] private Color statsColor = new Color(0.8f, 0.8f, 0.8f);
    [SerializeField] private Color buttonColor = new Color(1f, 0.85f, 0.3f);

    // Wide enough for the longest line. The height is set by the layout to fit the text.
    private const float TextWidth = 1600f;

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    private void Start()
    {
        UiFactory.EnsureEventSystem();
        Canvas canvas = UiFactory.CreateCanvas("End Canvas", CanvasSortingOrder);
        Transform column = CreateColumn(canvas.transform);
        GameManager game = GameManager.Instance;

        Text title = AddText(column, "Title", titleFontSize);
        Text message = AddText(column, "Message", messageFontSize);

        if (game == null)
        {
            // Happens only when this scene is played directly in the editor, without the level before it.
            title.text = "End Screen";
            message.text = "Start from the Tutorial scene to see the results.";
            return;
        }

        title.text = game.Won ? "Day 1 survived" : "Game over";
        title.color = game.Won ? winColor : loseColor;
        message.text = game.Won
            ? "You brought the water back to your cell."
            : "The guards caught you too many times.";

        Text stats = AddText(column, "Stats", statsFontSize);
        stats.color = statsColor;
        stats.text = $"Health {game.Health}/{game.MaxHealth}\nWater {game.Water}\nCaught {game.Captures} times\nTime {game.LevelSeconds:0} seconds";

        Button again = UiFactory.CreateButton(column, "Play again", Center, buttonSize, buttonFontSize, buttonColor);
        LayoutElement buttonLayout = again.gameObject.AddComponent<LayoutElement>();
        buttonLayout.preferredHeight = buttonSize.y;
        again.onClick.AddListener(game.PlayAgain);
    }

    /// <summary>A full-screen object that stacks its children vertically, centered on the screen.</summary>
    private Transform CreateColumn(Transform parent)
    {
        var columnObject = new GameObject("Column", typeof(RectTransform), typeof(VerticalLayoutGroup));
        columnObject.transform.SetParent(parent, false);

        RectTransform rect = columnObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = columnObject.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = spacing;

        // The layout sets each child's height to fit its content, but keeps the widths we chose.
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        return columnObject.transform;
    }

    private Text AddText(Transform column, string name, int fontSize)
    {
        Text text = UiFactory.CreateText(column, name, Center, new Vector2(TextWidth, 0f), fontSize, TextAnchor.MiddleCenter);
        return text;
    }
}
