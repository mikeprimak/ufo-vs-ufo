using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages game state, match flow, win conditions, and player statistics
/// Detects when only 1 UFO remains and declares winner
/// Tracks kills, deaths, and stats for end-of-match screen
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    [Tooltip("Current state of the game")]
    public GameState currentState = GameState.WaitingToStart;

    [Header("Match Settings")]
    [Tooltip("Delay before match starts (seconds)")]
    public float startDelay = 3f;

    [Tooltip("How long to wait after match ends before cleanup (seconds)")]
    public float endDelay = 5f;

    [Header("Player Tracking")]
    [Tooltip("All UFOs in the match (auto-populated)")]
    public List<GameObject> allPlayers = new List<GameObject>();

    [Header("Start Screen")]
    [Tooltip("Start screen UI (optional - will be shown before match starts)")]
    public GameObject startScreenUI;

    [Header("Victory Screen")]
    [Tooltip("Victory screen UI (optional - will be shown when match ends)")]
    public GameObject victoryScreenUI;

    private float stateTimer = 0f;
    private GameObject winner = null;
    private List<PlayerStats> allPlayerStats = new List<PlayerStats>();

    public enum GameState
    {
        WaitingToStart,
        Starting,
        InProgress,
        MatchOver
    }

    void Start()
    {
        // Find all UFOs in the scene with "Player" tag
        GameObject[] foundPlayers = GameObject.FindGameObjectsWithTag("Player");
        allPlayers.AddRange(foundPlayers);

        Debug.Log($"[GAME MANAGER] Found {allPlayers.Count} players in match");

        // Ensure all players have PlayerStats component
        foreach (GameObject player in allPlayers)
        {
            if (player != null)
            {
                PlayerStats stats = player.GetComponent<PlayerStats>();
                if (stats == null)
                {
                    stats = player.AddComponent<PlayerStats>();
                    // Auto-assign names (Player vs AI_1, AI_2, etc.)
                    stats.playerName = player.name;
                    stats.isHumanPlayer = player.name.Contains("Player");
                }
                allPlayerStats.Add(stats);
            }
        }

        // Hide victory screen at start
        if (victoryScreenUI != null)
        {
            victoryScreenUI.SetActive(false);
        }

        // Show start screen if available
        if (startScreenUI != null)
        {
            startScreenUI.SetActive(true);
        }

        // Disable movement for all UFOs until match starts
        DisableAllUFOMovement();

        // Stay in WaitingToStart until StartMatch() is called
        currentState = GameState.WaitingToStart;
    }

    void Update()
    {
        switch (currentState)
        {
            case GameState.Starting:
                UpdateStarting();
                break;

            case GameState.InProgress:
                UpdateInProgress();
                break;

            case GameState.MatchOver:
                UpdateMatchOver();
                break;
        }
    }

    void UpdateStarting()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            // Re-enable movement for all UFOs
            EnableAllUFOMovement();

            // Start the match
            currentState = GameState.InProgress;
            Debug.Log("[GAME MANAGER] Match started!");
        }
    }

    void UpdateInProgress()
    {
        // Count how many UFOs are still alive
        int aliveCount = 0;
        GameObject lastAlive = null;

        foreach (GameObject player in allPlayers)
        {
            if (player != null)
            {
                UFOHealth health = player.GetComponent<UFOHealth>();
                if (health != null && !health.IsDead())
                {
                    aliveCount++;
                    lastAlive = player;
                }
            }
        }

        // Check win condition: only 1 UFO remaining
        if (aliveCount <= 1)
        {
            winner = lastAlive;
            currentState = GameState.MatchOver;
            stateTimer = endDelay;

            if (winner != null)
            {
                Debug.Log($"[GAME MANAGER] *** MATCH OVER! *** Winner: {winner.name}");
            }
            else
            {
                Debug.Log("[GAME MANAGER] *** MATCH OVER! *** No survivors (draw)");
            }
            Debug.Log($"[GAME MANAGER] Victory screen will show in {endDelay} seconds...");
        }
    }

    void UpdateMatchOver()
    {
        stateTimer -= Time.deltaTime;

        // Show victory screen AFTER the delay (so "Winner" text shows first)
        if (stateTimer <= 0f)
        {
            // Show victory screen after delay
            if (victoryScreenUI != null && !victoryScreenUI.activeSelf)
            {
                Debug.Log("[GAME MANAGER] *** SHOWING VICTORY SCREEN NOW ***");
                ShowVictoryScreen();
            }
            else if (victoryScreenUI == null)
            {
                Debug.LogError("[GAME MANAGER] Victory screen UI is NULL! Cannot show victory screen.");
            }

            // Match cleanup/restart logic goes here
            // TODO: Add restart logic, return to menu, etc.
        }
        else
        {
            // Show countdown every second
            int secondsRemaining = Mathf.CeilToInt(stateTimer);
            if (Time.frameCount % 60 == 0) // Roughly once per second
            {
                Debug.Log($"[GAME MANAGER] Victory screen in {secondsRemaining} seconds...");
            }
        }
    }

    /// <summary>
    /// Show the victory screen with match statistics
    /// </summary>
    void ShowVictoryScreen()
    {
        if (victoryScreenUI == null) return;

        victoryScreenUI.SetActive(true);

        // Sort players by kills (descending)
        var sortedPlayers = allPlayerStats.OrderByDescending(p => p.kills).ToList();

        // Find the VictoryScreenUI component and populate it
        VictoryScreenUI victoryUI = victoryScreenUI.GetComponent<VictoryScreenUI>();
        if (victoryUI != null)
        {
            victoryUI.DisplayResults(sortedPlayers, winner);
        }
        else
        {
            // Fallback: Just log stats to console
            Debug.Log("=== MATCH OVER ===");
            Debug.Log($"Winner: {(winner != null ? winner.name : "None")}");
            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                Debug.Log($"{i + 1}. {sortedPlayers[i].GetSummary()}");
            }
        }
    }

    /// <summary>
    /// Start the match (called by StartScreenUI)
    /// </summary>
    public void StartMatch()
    {
        if (currentState != GameState.WaitingToStart)
        {
            Debug.LogWarning("[GAME MANAGER] StartMatch called but not in WaitingToStart state");
            return;
        }

        Debug.Log("[GAME MANAGER] Starting match countdown");

        // Begin countdown
        currentState = GameState.Starting;
        stateTimer = startDelay;
    }

    /// <summary>
    /// Get the current game state
    /// </summary>
    public GameState GetState()
    {
        return currentState;
    }

    /// <summary>
    /// Get the winner (only valid when state is MatchOver)
    /// </summary>
    public GameObject GetWinner()
    {
        return winner;
    }

    /// <summary>
    /// Get countdown time remaining (during Starting state)
    /// </summary>
    public float GetCountdownTime()
    {
        return stateTimer;
    }

    /// <summary>
    /// Check if match is currently in progress
    /// </summary>
    public bool IsMatchInProgress()
    {
        return currentState == GameState.InProgress;
    }

    /// <summary>
    /// Disable movement for all UFOs (used during countdown)
    /// </summary>
    void DisableAllUFOMovement()
    {
        foreach (GameObject player in allPlayers)
        {
            if (player != null)
            {
                UFOController controller = player.GetComponent<UFOController>();
                if (controller != null)
                {
                    controller.movementEnabled = false;
                }
            }
        }
    }

    /// <summary>
    /// Enable movement for all UFOs (used when match starts)
    /// </summary>
    void EnableAllUFOMovement()
    {
        foreach (GameObject player in allPlayers)
        {
            if (player != null)
            {
                UFOController controller = player.GetComponent<UFOController>();
                if (controller != null)
                {
                    controller.movementEnabled = true;
                }
            }
        }
    }

    /// <summary>
    /// Record a kill for a player (call from projectiles/weapons)
    /// </summary>
    public void RecordKill(GameObject killer)
    {
        if (killer == null) return;

        PlayerStats stats = killer.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.RecordKill();
        }
    }

    /// <summary>
    /// Record a death for a player (call from UFOHealth)
    /// </summary>
    public void RecordDeath(GameObject victim)
    {
        if (victim == null) return;

        PlayerStats stats = victim.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.RecordDeath();
        }
    }

    /// <summary>
    /// Get all player stats (for UI or external systems)
    /// </summary>
    public List<PlayerStats> GetAllPlayerStats()
    {
        return allPlayerStats;
    }
}
