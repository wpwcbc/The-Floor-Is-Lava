using UnityEngine;

public class GlobalVariableTemp: MonoBehaviour, VariableCellStatesEditInterface
{
	public static GlobalVariableTemp instance;
	private VariableCellStates cellStates = new VariableCellStates();

	void Awake()
	{
		instance = this;
	}

	public CellState GetGrid(Vector2Int worldIndex)
	{
		return cellStates.GetGrid(worldIndex);
	}

	public void SetGrid(Vector2Int worldIndex, CellState cell)
	{
		cellStates.SetGrid(worldIndex, cell);
	}

	public void InitGrids(int grid_width, int grid_height)
	{
		cellStates.InitGrids(grid_width, grid_height);
	}
}
