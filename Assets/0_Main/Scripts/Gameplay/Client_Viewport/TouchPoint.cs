using UnityEngine;

public enum TouchSource
{
    Sensor,
    Mouse,
    Touchscreen
}

public struct TouchPoint
{
    public int Id;          // e.g. sensor contact id; 0 for mouse
    public Vector2 Position; // In game space (e.g. pixels or normalized [0,1])
    public TouchSource Source;

    public TouchPoint(int id, Vector2 position, TouchSource source)
    {
        Id = id;
        Position = position;
        Source = source;
    }
}

