using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

/// <summary>
/// An on-screen joystick for phones, in the bottom-left corner. It is created only on touch screens.
/// OnScreenStick moves the knob with the finger and reports it as the left stick of a virtual gamepad,
/// which PlayerController already reads, so the player code doesn't change at all.
/// </summary>
public class MobileJoystick : MonoBehaviour
{
    private const int CanvasSortingOrder = 15;
    private const float RingSize = 300f;
    private const float KnobSize = 130f;
    private const float Margin = 60f;

    // How far the knob can move from the center, in UI pixels.
    private const float KnobRange = 100f;

    /// <summary>True when the game runs on a phone or a touch screen.</summary>
    public static bool UsesTouch => Application.isMobilePlatform || Touchscreen.current != null;

    private void Start()
    {
        if (!UsesTouch)
        {
            return;
        }

        UiFactory.EnsureEventSystem();
        Canvas canvas = UiFactory.CreateCanvas("Joystick Canvas", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);

        Image ring = UiFactory.CreateImage(canvas.transform, "Joystick", UiTheme.Sprite("JoystickRing"), Color.white);
        UiFactory.Place(ring.rectTransform, Vector2.zero, new Vector2(Margin, Margin), Vector2.one * RingSize);

        Image knob = UiFactory.CreateImage(ring.transform, "Knob", UiTheme.Sprite("JoystickKnob"), Color.white);
        UiFactory.Place(knob.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * KnobSize);
        knob.raycastTarget = true; // The stick needs the touches.

        // The object is off while the stick is set up: OnScreenStick needs its control path before it starts.
        knob.gameObject.SetActive(false);
        var stick = knob.gameObject.AddComponent<OnScreenStick>();
        stick.controlPath = "<Gamepad>/leftStick";
        stick.movementRange = KnobRange;
        knob.gameObject.SetActive(true);
    }
}
