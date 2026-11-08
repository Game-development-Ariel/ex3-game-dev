using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves the player around the map in a top-down view.
/// The movement goes through the Rigidbody2D, so the tunnel walls can stop the player.
/// The body (sprite + flashlight) turns to the walking direction,
/// so the flashlight shows what the character is looking at.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // Angle 0 in Unity points right, but the player sprite is drawn facing up.
    private const float SpriteForwardOffsetDegrees = -90f;

    // Input weaker than this counts as "not moving", so a small gamepad drift doesn't turn the body.
    private const float MinInputToTurn = 0.01f;

    [Tooltip("Movement speed, in world units per second")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("How fast the body turns, in degrees per second")]
    [SerializeField] private float turnSpeed = 720f;

    [Tooltip("The child object that turns: the sprite and the flashlight")]
    [SerializeField] private Transform body;

    [Tooltip("The movement direction. Arrows, WASD and the left gamepad stick by default")]
    [SerializeField]
    private InputAction moveAction = new InputAction(type: InputActionType.Value, expectedControlType: nameof(Vector2));

    private Rigidbody2D rb;

    // The last input we read. Update reads it and FixedUpdate uses it.
    private Vector2 moveInput;

    /// <summary>
    /// Adds the default keys, so the action works without setting it in the Inspector.
    /// Unity runs this in the editor when the component is loaded or changed.
    /// </summary>
    private void OnValidate()
    {
        if (moveAction.bindings.Count == 0)
        {
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddBinding("<Gamepad>/leftStick");
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // An InputAction reads nothing until it is enabled.
    private void OnEnable()
    {
        moveAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
    }

    /// <summary>Reads the input every frame and turns the body toward it.</summary>
    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        TurnBodyTowardMovement();
    }

    /// <summary>Moves the player. Physics changes belong in FixedUpdate.</summary>
    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    /// <summary>Turns the body a little each frame toward the walking direction.</summary>
    private void TurnBodyTowardMovement()
    {
        bool isMoving = moveInput.sqrMagnitude > MinInputToTurn;
        if (body == null || !isMoving)
        {
            return; // When the player stops, the body keeps facing the last direction.
        }

        // Atan2 gives the angle of the direction vector, in radians.
        float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg + SpriteForwardOffsetDegrees;
        Quaternion facing = Quaternion.Euler(0f, 0f, angle);

        // RotateTowards never passes the goal, so the turn is smooth and stops exactly on it.
        body.rotation = Quaternion.RotateTowards(body.rotation, facing, turnSpeed * Time.deltaTime);
    }
}
