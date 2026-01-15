using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class VariableCellStates : NetworkVariableBase
{
	public List<CellState> cellStates = new List<CellState>();
	public int grid_width;
	public int grid_height;

	public delegate void OnChangedDelegate(int changedIndex);
	public event OnChangedDelegate OnValueChanged;


	public void InitGrids(int grid_width, int grid_height)
	{
		this.grid_width = grid_width;
		this.grid_height = grid_height;
		for (int i = 0; i < grid_width * grid_height; i++)
		{
			cellStates.Add(new CellState());
		}
		this.SetDirty(true);
		OnValueChanged?.Invoke(-1);
	}


	public void SetGrid(Vector2Int worldIndex, CellState cell)
	{
		if (!cellStates[worldIndex.x + grid_width * worldIndex.y].Equals(cell))
		{
			cellStates[worldIndex.x + grid_width * worldIndex.y] = cell;
			this.SetDirty(true);
			OnValueChanged?.Invoke(worldIndex.x + grid_width * worldIndex.y);
		}
	}

	public CellState GetGrid(Vector2Int worldIndex)
	{
		return cellStates[worldIndex.x + grid_width * worldIndex.y];
	}



	public override void WriteField(FastBufferWriter writer)
	{
		writer.WriteValueSafe(cellStates.Count);
		foreach (var dataEntry in cellStates)
		{
			writer.WriteValueSafe(dataEntry.Role);
			writer.WriteValueSafe(dataEntry.Color);
			writer.WriteValueSafe(dataEntry.HasEffectTint);
			writer.WriteValueSafe(dataEntry.EffectTint);
		}
	}

	public override void ReadField(FastBufferReader reader)
	{
		var itemsToUpdate = (int)0;
		reader.ReadValueSafe(out itemsToUpdate);
		cellStates.Clear();
		for (int i = 0; i < itemsToUpdate; i++)
		{
			var newEntry = new CellState();
			reader.ReadValueSafe(out newEntry.Role);
			reader.ReadValueSafe(out newEntry.Color);
			reader.ReadValueSafe(out newEntry.HasEffectTint);
			reader.ReadValueSafe(out newEntry.EffectTint);
			cellStates.Add(newEntry);
		}
	}

	public override void WriteDelta(FastBufferWriter writer)
	{
		WriteField(writer);
	}

	public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
	{
		ReadField(reader);
	}
}
public interface VariableCellStatesEditInterface
{
	void InitGrids(int grid_width, int grid_height);
	void SetGrid(Vector2Int worldIndex, CellState cell);
	CellState GetGrid(Vector2Int worldIndex);
}
