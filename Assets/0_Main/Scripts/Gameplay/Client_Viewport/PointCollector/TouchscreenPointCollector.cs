using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TouchscreenPointCollector : MonoBehaviour, IPointCollector
{
    // Reusable buffer to avoid allocations every frame.
    private readonly List<TouchPoint> _collected = new List<TouchPoint>();

    private void OnEnable()
    {
        // Enable EnhancedTouch once. Safe to call multiple times.
        if (!EnhancedTouchSupport.enabled)
        {
            EnhancedTouchSupport.Enable();
        }
    }

    private void OnDisable()
    {
        // Paired with Enable; if this is your only user, this is fine.
        if (EnhancedTouchSupport.enabled)
        {
            EnhancedTouchSupport.Disable();
        }
    }

    public List<TouchPoint> Collect()
    {
        _collected.Clear();

        // Touch.activeTouches is the high-level multi-touch API from the new Input System.
        // It already tracks fingers and IDs for you. :contentReference[oaicite:1]{index=1}
        foreach (Touch touch in Touch.activeTouches)
        {
            // Ignore touches that are not currently down (Ended, Canceled).
            if (!touch.isInProgress)
            {
                continue;
            }

            int id = touch.finger.index;      // Stable index per finger as long as it is on screen.
            Vector2 position = touch.screenPosition; // Screen-space position in pixels.

            Debug.Log($"[TouchscreenPointCollector] id: {id}");

            TouchPoint point = new TouchPoint(
                id: id,
                position: position,
                source: TouchSource.Touchscreen
            );

            _collected.Add(point);
        }

        return _collected;
    }
}
