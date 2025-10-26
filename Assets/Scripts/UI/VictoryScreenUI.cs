using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Displays end-of-match statistics and victory screen
/// Shows ranked players, individual stats, and MVP awards
/// </summary>
public class VictoryScreenUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Title text (e.g., 'VICTORY!' or 'MATCH OVER')")]
    public TextMeshProUGUI titleText;

    [Tooltip("Main results panel displaying player rankings")]
    public TextMeshProUGUI resultsText;

    [Tooltip("Stats panel showing MVP and highlights")]
    public TextMeshProUGUI statsText;

    [Tooltip("Rematch button (optional)")]
    public Button rematchButton;

    [Tooltip("Main menu button (optional)")]
    public Button mainMenuButton;

    /// <summary>
    /// Display match results with ranked players and stats
    /// </summary>
    public void DisplayResults(List<PlayerStats> sortedPlayers, GameObject winner)
    {
        // Update title
        if (titleText != null)
        {
            if (winner != null)
            {
                PlayerStats winnerStats = winner.GetComponent<PlayerStats>();
                if (winnerStats != null && winnerStats.isHumanPlayer)
                {
                    titleText.text = "VICTORY!";
                    titleText.color = new Color(0.2f, 1f, 0.2f); // Green
                }
                else
                {
                    titleText.text = "DEFEAT";
                    titleText.color = new Color(1f, 0.3f, 0.3f); // Red
                }
            }
            else
            {
                titleText.text = "MATCH OVER";
                titleText.color = Color.white;
            }
        }

        // Build results string with ranked players
        if (resultsText != null && sortedPlayers != null)
        {
            string results = "FINAL STANDINGS:\n\n";

            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                PlayerStats player = sortedPlayers[i];
                string placement = GetPlacementString(i + 1);

                // Color code human player vs AI
                string colorStart = player.isHumanPlayer ? "<color=#00FFFF>" : "<color=#FFA500>";
                string colorEnd = "</color>";

                results += $"{placement} {colorStart}{player.playerName}{colorEnd}\n";
                results += $"    {player.kills} Kills  |  {player.deaths} Deaths  |  K/D: {player.GetKDRatio():F2}\n\n";
            }

            resultsText.text = results;
        }

        // Build stats/highlights string
        if (statsText != null && sortedPlayers != null)
        {
            string stats = "MATCH HIGHLIGHTS:\n\n";

            // MVP (most kills)
            PlayerStats mvp = sortedPlayers.FirstOrDefault();
            if (mvp != null)
            {
                stats += $"<color=#FFD700>MVP:</color> {mvp.playerName} ({mvp.kills} kills)\n\n";
            }

            // Longest kill streak
            PlayerStats bestStreak = sortedPlayers.OrderByDescending(p => p.longestKillStreak).FirstOrDefault();
            if (bestStreak != null && bestStreak.longestKillStreak > 0)
            {
                stats += $"<color=#FF4500>Longest Kill Streak:</color> {bestStreak.playerName} ({bestStreak.longestKillStreak})\n\n";
            }

            // Best accuracy
            PlayerStats mostAccurate = sortedPlayers.Where(p => p.shotsFired > 0).OrderByDescending(p => p.GetAccuracy()).FirstOrDefault();
            if (mostAccurate != null && mostAccurate.shotsFired > 0)
            {
                stats += $"<color=#00FF00>Best Accuracy:</color> {mostAccurate.playerName} ({mostAccurate.GetAccuracy():F1}%)\n\n";
            }

            // Most damage dealt
            PlayerStats mostDamage = sortedPlayers.OrderByDescending(p => p.damageDealt).FirstOrDefault();
            if (mostDamage != null && mostDamage.damageDealt > 0)
            {
                stats += $"<color=#FF1493>Most Damage:</color> {mostDamage.playerName} ({mostDamage.damageDealt} HP)\n\n";
            }

            statsText.text = stats;
        }

        Debug.Log("[VICTORY SCREEN] Results displayed");
    }

    /// <summary>
    /// Get placement string with appropriate suffix
    /// </summary>
    string GetPlacementString(int placement)
    {
        switch (placement)
        {
            case 1:
                return "1st";
            case 2:
                return "2nd";
            case 3:
                return "3rd";
            default:
                return placement + "th";
        }
    }

    /// <summary>
    /// Handle rematch button click
    /// </summary>
    public void OnRematchClicked()
    {
        Debug.Log("[VICTORY SCREEN] Rematch requested");
        // TODO: Reload current scene or restart match
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    /// <summary>
    /// Handle main menu button click
    /// </summary>
    public void OnMainMenuClicked()
    {
        Debug.Log("[VICTORY SCREEN] Return to main menu requested");
        // TODO: Load main menu scene
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    void Start()
    {
        // Wire up button events if buttons are assigned
        if (rematchButton != null)
        {
            rematchButton.onClick.AddListener(OnRematchClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }
}
