using UnityEngine;

/// <summary>
/// Debug script to detect controller inputs
/// Shows in Console what buttons/axes your controller is using
/// </summary>
public class ControllerDebug : MonoBehaviour
{
    void Update()
    {
        // Check all 20 joystick buttons
        for (int i = 0; i < 20; i++)
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton0 + i))
            {
                Debug.Log($"BUTTON PRESSED: Button {i}");
            }
        }

        // Check named axes (these are setup by default in Unity)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (Mathf.Abs(horizontal) > 0.3f)
            Debug.Log($"Horizontal Axis: {horizontal:F2}");

        if (Mathf.Abs(vertical) > 0.3f)
            Debug.Log($"Vertical Axis: {vertical:F2}");

        // Check Fire buttons
        if (Input.GetButtonDown("Fire1"))
            Debug.Log("Fire1 pressed (usually controller button 0)");

        if (Input.GetButtonDown("Fire2"))
            Debug.Log("Fire2 pressed (usually controller button 1)");

        if (Input.GetButtonDown("Fire3"))
            Debug.Log("Fire3 pressed (usually controller button 2)");

        if (Input.GetButtonDown("Jump"))
            Debug.Log("Jump pressed (usually controller button 3)");
    }
}
