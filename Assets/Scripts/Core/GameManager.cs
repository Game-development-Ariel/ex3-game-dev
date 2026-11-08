using System;
using UnityEngine;

/// <summary>
/// Keeps the player's values for the whole game: health, water, captures and whether the player carries the bottle.
/// The HUD shows the health as "hope", like in the design: being caught takes hope, memories give it back.
/// There is only one GameManager (a singleton), and it survives scene changes (DontDestroyOnLoad),
/// so the end screen can show the values from the level.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>The one GameManager of the game. Other scripts reach it through here.</summary>
    public static GameManager Instance { get; private set; }

    [Header("Rules")]
    [SerializeField] private int maxHealth = 100;

    [Tooltip("Health at the start. In the tutorial the hero wakes up weak, so memories have room to fill it")]
    [SerializeField] private int startHealth = 100;

    [Tooltip("Health lost every time a camera catches the player")]
    [SerializeField] private int damagePerCapture = 25;

    [SerializeField] private int startWater = 50;

    [Tooltip("Water gained by bringing the bottle back to the mattress")]
    [SerializeField] private int waterPerBottle = 25;

    [Tooltip("Health never drops below this. Above 0 means the player can't lose (used in the tutorial)")]
    [SerializeField] private int minHealth = 0;

    [Header("Scenes")]
    [SerializeField] private string levelScene = "Tutorial";
    [SerializeField] private string endScene = "EndScreen";

    public int MaxHealth => maxHealth;
    public int Health { get; private set; }
    public int Water { get; private set; }
    public int Captures { get; private set; }
    public bool HasBottle { get; private set; }

    /// <summary>True if the last level ended with a win, false if with a game over.</summary>
    public bool Won { get; private set; }

    /// <summary>How many seconds the last level took.</summary>
    public float LevelSeconds { get; private set; }

    /// <summary>Happens when a camera catches the player. The HUD listens to it to flash and shake.</summary>
    public event Action Captured;

    /// <summary>Happens when the player picks up the bottle.</summary>
    public event Action BottlePicked;

    private void Awake()
    {
        // The level scene has a GameManager. When the level loads again, the old one already exists,
        // so the new copy removes itself and the values are kept.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResetValues();
    }

    public void PickUpBottle()
    {
        HasBottle = true;
        BottlePicked?.Invoke(); // ?. calls it only if someone listens.
    }

    /// <summary>Called by a memory the player found. Gives back hope, up to the maximum.</summary>
    public void AddHope(int amount)
    {
        Health = Mathf.Min(maxHealth, Health + amount);
    }

    /// <summary>Called by a camera that caught the player. The player drops the bottle and loses health.</summary>
    public void RegisterCapture()
    {
        Captures++;
        HasBottle = false;
        Health = Mathf.Max(minHealth, Health - damagePerCapture);
        Captured?.Invoke();
        if (Health == 0)
        {
            EndLevel(false);
        }
    }

    /// <summary>Called when the player reaches the mattress. Wins only if the player carries the bottle.</summary>
    public void DeliverBottle()
    {
        if (!HasBottle)
        {
            return;
        }
        HasBottle = false;
        Water += waterPerBottle;
        EndLevel(true);
    }

    /// <summary>Called by the end screen's button. Starts the level again with fresh values.</summary>
    public void PlayAgain()
    {
        ResetValues();
        ScreenFader.FadeAndLoad(levelScene);
    }

    private void EndLevel(bool won)
    {
        Won = won;
        LevelSeconds = Time.timeSinceLevelLoad;
        ScreenFader.FadeAndLoad(endScene);
    }

    private void ResetValues()
    {
        Health = Mathf.Min(startHealth, maxHealth);
        Water = startWater;
        Captures = 0;
        HasBottle = false;
    }
}
