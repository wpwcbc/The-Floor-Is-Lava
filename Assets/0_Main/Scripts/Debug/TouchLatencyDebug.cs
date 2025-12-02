using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TouchLatencyDebug : MonoBehaviour
{
    private double _lastDeltaSeconds;
    private double _lastTouchTime;
    private double _lastRealtime;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        TouchSimulation.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        TouchSimulation.Disable();
    }

    private void Update()
    {
        if (Touch.activeTouches.Count > 0)
        {
            Touch t = Touch.activeTouches[0];

            _lastTouchTime = t.time;                     // same timeline as Time.realtimeSinceStartup
            _lastRealtime = Time.realtimeSinceStartup;   // use the matching clock
            _lastDeltaSeconds = Time.realtimeSinceStartup - t.time;
        }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10.0f, 10.0f, 600.0f, 25.0f),
            "Touch latency (realtime - touch.time): " + _lastDeltaSeconds.ToString("F4") + " s");

        GUI.Label(new Rect(10.0f, 35.0f, 600.0f, 25.0f),
            "Touch.time: " + _lastTouchTime.ToString("F3") + " | realtime: " + _lastRealtime.ToString("F3"));
    }
}
