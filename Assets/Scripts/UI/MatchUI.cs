using UnityEngine;
using TMPro;

/// <summary>
/// Manages match UI: countdown (3,2,1), death message, winner message
/// </summary>
public class MatchUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The GameManager to monitor")]
    public GameManager gameManager;

    [Tooltip("The player UFO (to check if player died)")]
    public GameObject playerUFO;

    [Header("UI Text")]
    [Tooltip("Center screen text for countdown and messages")]
    public TextMeshProUGUI centerText;

    [Header("Messages")]
    [Tooltip("Message shown when player dies")]
    public string deathMessage = "You ded.";

    [Tooltip("Message shown when player wins")]
    public string winMessage = "Winner";

    [Tooltip("Message shown when someone else wins")]
    public string loseMessage = "";

    private UFOHealth playerHealth;
    private bool playerDied = false;
    private bool matchEnded = false;

    void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        if (playerUFO != null)
        {
            playerHealth = playerUFO.GetComponent<UFOHealth>();
        }

        if (centerText != null)
        {
            centerText.text = "";
        }
    }

    void Update()
    {
        if (centerText == null || gameManager == null)
            return;

        GameManager.GameState state = gameManager.GetState();

        // Handle countdown during Starting state
        if (state == GameManager.GameState.Starting)
        {
            float countdown = gameManager.GetCountdownTime();
            int countdownInt = Mathf.CeilToInt(countdown);

            if (countdownInt >= 1)
            {
                centerText.text = countdownInt.ToString();
            }
            else
            {
                centerText.text = ""; // Clear text when countdown reaches 0
            }
        }
        // Handle in-progress state - check if player died
        else if (state == GameManager.GameState.InProgress)
        {
            if (!playerDied && playerHealth != null && playerHealth.IsDead())
            {
                playerDied = true;
                centerText.text = deathMessage;
            }
            else if (!playerDied)
            {
                // Ensure text is cleared during normal gameplay
                centerText.text = "";
            }
        }
        // Handle match over state - show winner/loser message
        else if (state == GameManager.GameState.MatchOver && !matchEnded)
        {
            matchEnded = true;

            GameObject winner = gameManager.GetWinner();

            // If player is the winner, show win message
            if (winner == playerUFO)
            {
                centerText.text = winMessage;
            }
            // If player already died, keep death message
            else if (playerDied)
            {
                // Keep "You ded." message
            }
            // Player lost but didn't die yet (shouldn't happen, but handle it)
            else
            {
                centerText.text = loseMessage;
            }
        }
    }
}
