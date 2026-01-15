using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class CellState : INetworkSerializable, IEquatable<CellState>
{
    public CellRole Role;
    public CellColor Color;
    public bool HasEffectTint;
    public Color EffectTint;

    public static CellState Default
    {
        get
        {
            CellState state = new CellState();
            state.Role = CellRole.None;
            state.Color = CellColor.Black; // Assumes you already have Black; adjust if needed.
            state.HasEffectTint = false;
            state.EffectTint = new Color();
            return state;
        }
    }

    public bool Equals(CellState other)
    {
        return Role == other.Role && Color == other.Color && HasEffectTint == other.HasEffectTint && EffectTint == other.EffectTint;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Role);
        serializer.SerializeValue(ref Color);
        serializer.SerializeValue(ref HasEffectTint);
        serializer.SerializeValue(ref EffectTint);
    }
}