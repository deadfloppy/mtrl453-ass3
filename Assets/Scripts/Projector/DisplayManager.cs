using UnityEngine;

public class DisplayManager : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"Total displays detected: {Display.displays.Length}");
        
        // Activate all available displays
        for (int i = 1; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
            Debug.Log($"Activated display {i}: {Display.displays[i].renderingWidth}x{Display.displays[i].renderingHeight}");
        }
        
        if (Display.displays.Length < 2)
        {
            Debug.LogWarning("Second display not detected! Connect projector via HDMI.");
        }
    }
}