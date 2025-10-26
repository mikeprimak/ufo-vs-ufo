using UnityEngine;

/// <summary>
/// Tracks individual player statistics for end-of-match screen
/// Attached to each UFO to track their performance
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Combat Stats")]
    public int kills = 0;
    public int deaths = 0;
    public int shotsFired = 0;
    public int shotsHit = 0;

    [Header("Streak Tracking")]
    public int currentKillStreak = 0;
    public int longestKillStreak = 0;

    [Header("Damage Tracking")]
    public int damageDealt = 0;
    public int damageTaken = 0;

    [Header("Player Info")]
    public string playerName = "Player";
    public bool isHumanPlayer = true;

    /// <summary>
    /// Record a kill
    /// </summary>
    public void RecordKill()
    {
        kills++;
        currentKillStreak++;

        if (currentKillStreak > longestKillStreak)
        {
            longestKillStreak = currentKillStreak;
        }

        Debug.Log($"[STATS] {playerName} kill recorded. Total: {kills}, Streak: {currentKillStreak}");
    }

    /// <summary>
    /// Record a death
    /// </summary>
    public void RecordDeath()
    {
        deaths++;
        currentKillStreak = 0; // Reset streak on death

        Debug.Log($"[STATS] {playerName} death recorded. Total: {deaths}");
    }

    /// <summary>
    /// Record a shot fired
    /// </summary>
    public void RecordShotFired()
    {
        shotsFired++;
    }

    /// <summary>
    /// Record a shot that hit a target
    /// </summary>
    public void RecordShotHit()
    {
        shotsHit++;
    }

    /// <summary>
    /// Record damage dealt to another player
    /// </summary>
    public void RecordDamageDealt(int amount)
    {
        damageDealt += amount;
    }

    /// <summary>
    /// Record damage taken from another player
    /// </summary>
    public void RecordDamageTaken(int amount)
    {
        damageTaken += amount;
    }

    /// <summary>
    /// Calculate accuracy percentage
    /// </summary>
    public float GetAccuracy()
    {
        if (shotsFired == 0) return 0f;
        return (float)shotsHit / (float)shotsFired * 100f;
    }

    /// <summary>
    /// Calculate K/D ratio
    /// </summary>
    public float GetKDRatio()
    {
        if (deaths == 0) return kills; // Avoid division by zero
        return (float)kills / (float)deaths;
    }

    /// <summary>
    /// Get summary string for debug display
    /// </summary>
    public string GetSummary()
    {
        return $"{playerName}: {kills}K/{deaths}D, Streak: {longestKillStreak}, Accuracy: {GetAccuracy():F1}%";
    }
}
