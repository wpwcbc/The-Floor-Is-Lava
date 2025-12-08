using UnityEngine;

public interface ITouchCell
{
    int Size { get; set; }
    Vector2Int Position { get; }
    bool IsTouched { get; }
    void SetIsTouched(bool isTouched);

    CellRole role { get; }
    void SetRole(CellRole role);

    CellColor color { get; }
    void SetColor(CellColor color);

    event System.Action<ITouchCell> Touched;
    event System.Action<ITouchCell> Untouched;

    // oldRole, newRole
    event System.Action<ITouchCell, CellRole, CellRole> RoleChanged;
}

