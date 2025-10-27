using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the start screen shown when the game first opens
/// Displays title and start button before the match begins
/// </summary>
public class StartScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Start Game button")]
    public Button startButton;

    [Tooltip("Optional title text")]
    public TMPro.TextMeshProUGUI titleText;

    [Tooltip("Optional instruction text for controller/keyboard input")]
    public TMPro.TextMeshProUGUI instructionText;

    private GameManager gameManager;

    void Start()
    {
        // Find the GameManager
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("[START SCREEN] Could not find GameManager!");
        }

        // Set up button listener
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
        else
        {
            Debug.LogError("[START SCREEN] Start button reference is NULL! Please assign it in Inspector.");
        }

        // Set title if available
        if (titleText != null)
        {
            titleText.text = "UFO vs UFO";
        }

        // Set instruction text if available
        if (instructionText != null)
        {
            instructionText.text = "Press A To Begin";
        }
    }

    void Update()
    {
        // Check for gamepad input to start the game
        // Button 0 = A button (Xbox), Button 7 = Start button
        if (Input.GetButtonDown("Jump") || // Space bar or gamepad A button
            Input.GetKeyDown(KeyCode.Return) || // Enter key
            Input.GetKeyDown(KeyCode.JoystickButton0) || // A button (Xbox) / Cross (PS)
            Input.GetKeyDown(KeyCode.JoystickButton7)) // Start button
        {
            OnStartButtonClicked();
        }
    }

    void OnStartButtonClicked()
    {
        // Tell GameManager to start the match
        if (gameManager != null)
        {
            gameManager.StartMatch();
        }
        else
        {
            Debug.LogError("[START SCREEN] Cannot start match - GameManager is null!");
        }

        // Hide the start screen
        gameObject.SetActive(false);
    }
}
