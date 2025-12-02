using UnityEngine;

public interface ITouchCell
{
    int Size { get; set; } // In number of cells. E.g. Size 2 = 2x2.

    Vector2Int Position { get; } // In grid index

    bool IsTouched { get; }

    void SetIsTouched(bool isTouched);

    bool IsSensitive { get; set; }
    bool IsForbidden { get; set; }
}
