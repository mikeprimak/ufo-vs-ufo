using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages game state, match flow, and win conditions
/// Detects when only 1 UFO remains and declares winner
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

    private float stateTimer = 0f;
    private GameObject winner = null;

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

        // Disable movement for all UFOs during countdown
        DisableAllUFOMovement();

        // Start countdown
        currentState = GameState.Starting;
        stateTimer = startDelay;
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
                Debug.Log($"[GAME MANAGER] MATCH OVER! Winner: {winner.name}");
            }
            else
            {
                Debug.Log("[GAME MANAGER] MATCH OVER! No survivors (draw)");
            }
        }
    }

    void UpdateMatchOver()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            // Match cleanup/restart logic goes here
            Debug.Log("[GAME MANAGER] Match ended, cleanup time");
            // TODO: Add restart logic, return to menu, etc.
        }
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
}
