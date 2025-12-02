using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class MousePointCollector : MonoBehaviour, IPointCollector
{
    private readonly List<TouchPoint> _collected = new(); // Reusable to lower allocation rate.

    public List<TouchPoint> Collect()
    {
        _collected.Clear();

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            var point = new TouchPoint(
                id: 0,
                position: mousePos,
                source: TouchSource.Mouse
            );

            _collected.Add(point);

        }

        return _collected;
    }
}
