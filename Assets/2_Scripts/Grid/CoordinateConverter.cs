using UnityEngine;


public abstract class CoordinateConverter
{
	/// <summary>
	/// Converts a grid cell position to its world position at the bottom-left corner of the cell.
	/// </summary>
	/// <param name="cellPosition">The grid coordinates (x, y) of the cell</param>
	/// <param name="gridSize">The total dimensions of the grid (width, height)</param>
	/// <param name="cellSize">The size of each cell in world units</param>
	/// <param name="cellSpacing">The spacing between cells in world units</param>
	/// <param name="origin">The origin point of the grid in world space</param>
	/// <returns>The world position at the corner of the cell</returns>
	public abstract Vector3 GridToWorld(Vector2Int cellPosition, Vector2Int gridSize, Vector3 cellSize, Vector3 cellSpacing, Vector3 origin);
	
	/// <summary>
	/// Converts a grid cell position to its world position at the center of the cell.
	/// This is typically used for placing objects/sprites in cells.
	/// </summary>
	/// <param name="cellPosition">The grid coordinates (x, y) of the cell</param>
	/// <param name="gridSize">The total dimensions of the grid (width, height)</param>
	/// <param name="cellSize">The size of each cell in world units</param>
	/// <param name="cellSpacing">The spacing between cells in world units</param>
	/// <param name="origin">The origin point of the grid in world space</param>
	/// <returns>The world position at the center of the cell</returns>
	public abstract Vector3 GridToWorldCenter(Vector2Int cellPosition, Vector2Int gridSize, Vector3 cellSize, Vector3 cellSpacing, Vector3 origin);
	
	/// <summary>
	/// Converts a world position to grid coordinates, determining which cell contains the position.
	/// Used for input handling (e.g., "which cell did the player click on?").
	/// </summary>
	/// <param name="worldPosition">The position in world space</param>
	/// <param name="gridSize">The total dimensions of the grid (width, height)</param>
	/// <param name="cellSize">The size of each cell in world units</param>
	/// <param name="cellSpacing">The spacing between cells in world units</param>
	/// <param name="origin">The origin point of the grid in world space</param>
	/// <returns>The grid coordinates of the cell containing the world position</returns>
	public abstract Vector2Int WorldToGrid(Vector3 worldPosition, Vector2Int gridSize, Vector3 cellSize, Vector3 cellSpacing, Vector3 origin);
	
	/// <summary>
	/// The forward direction for this coordinate system (used for 3D orientation).
	/// </summary>
	public abstract Vector3 Forward { get;}
}

public class VerticalConvertor : CoordinateConverter
{
	public override Vector3 GridToWorld(Vector2Int cellPosition, Vector2Int gridSize, Vector3 cellSize, Vector3 cellSpacing, Vector3 origin)
	{
		// Calculate the total size of the grid including spacing
		float totalGridWidth = (gridSize.x * cellSize.x) + ((gridSize.x - 1) * cellSpacing.x);
		float totalGridHeight = (gridSize.y * cellSize.y) + ((gridSize.y - 1) * cellSpacing.y);
		
		// Calculate offset to center the grid at the origin
		float sizeOffsetX = -totalGridWidth / 2f;
		float sizeOffsetY = -totalGridHeight / 2f;
		
		// Calculate cell position with spacing
		float posX = cellPosition.x * (cellSize.x + cellSpacing.x);
		float posY = cellPosition.y * (cellSize.y + cellSpacing.y);
		
		return new Vector3(
			origin.x + posX + sizeOffsetX,
			origin.y + posY + sizeOffsetY,
			0
		);
	}
	
	public override Vector3 GridToWorldCenter(Vector2Int cellPosition, Vector2Int gridSize, Vector3 cellSize, Vector3 cellSpacing, Vector3 origin)
	{
		// Calculate the total size of the grid including spacing
		float totalGridWidth = (gridSize.x * cellSize.x) + ((gridSize.x - 1) * cellSpacing.x);
		float totalGridHeight = (gridSize.y * cellSize.y) + ((gridSize.y - 1) * cellSpacing.y);
		
		// Calculate offset to center the grid at the origin
		float sizeOffsetX = -totalGridWidth / 2f + cellSize.x * 0.5f;
		float sizeOffsetY = -totalGridHeight / 2f + cellSize.y * 0.5f;
		
		// Calculate cell position with spacing
		float posX = cellPosition.x * (cellSize.x + cellSpacing.x);
		float posY = cellPosition.y * (cellSize.y + cellSpacing.y);
		
		return new Vector3(
			origin.x + posX + sizeOffsetX,
			origin.y + posY + sizeOffsetY,
			0
		);
	}
	
	public override Vector2Int WorldToGrid(Vector3 worldPosition, Vector2Int gridSize, Vector3 cellSize, Vector3 cellSpacing, Vector3 origin)
	{
		// Remove origin offset
		worldPosition -= origin;
		
		// Calculate the centering offset that was applied in GridToWorld
		float totalGridWidth = (gridSize.x * cellSize.x) + ((gridSize.x - 1) * cellSpacing.x);
		float totalGridHeight = (gridSize.y * cellSize.y) + ((gridSize.y - 1) * cellSpacing.y);
		float sizeOffsetX = -totalGridWidth / 2f;
		float sizeOffsetY = -totalGridHeight / 2f;
		
		// Remove the centering offset to get back to grid-relative coordinates
		worldPosition.x -= sizeOffsetX;
		worldPosition.y -= sizeOffsetY;
		
		// Convert to grid coordinates
		int x = Mathf.FloorToInt(worldPosition.x / (cellSize.x + cellSpacing.x));
		int y = Mathf.FloorToInt(worldPosition.y / (cellSize.y + cellSpacing.y));
		
		return new Vector2Int(x, y);
	}

	public override Vector3 Forward => Vector3.forward;
}

public class HorizontalConvertor : CoordinateConverter
{
	public override Vector3 GridToWorld(Vector2Int cellPosition, Vector2Int gridSize, Vector3 cellSize, Vector3 cellSpacing, Vector3 origin)
	{
		// Calculate the total size of the grid including spacing
		float totalGridWidth = (gridSize.x * cellSize.x) + ((gridSize.x - 1) * cellSpacing.x);
		float totalGridDepth = (gridSize.y * cellSize.z) + ((gridSize.y - 1) * cellSpacing.z);
		
		// Calculate offset to center the grid at the origin
		float sizeOffsetX = -totalGridWidth / 2f;
		float sizeOffsetZ = -totalGridDepth / 2f;
		
		// Calculate cell position with spacing (y becomes z for horizontal)
		float posX = cellPosition.x * (cellSize.x + cellSpacing.x);
		float posZ = cellPosition.y * (cellSize.z + cellSpacing.z);
		
		return new Vector3(
			origin.x + posX + sizeOffsetX,
			origin.y,
			origin.z + posZ + sizeOffsetZ
		);
	}

	public override Vector3 GridToWorldCenter(Vector2Int cellPosition, Vector2Int gridSize, Vector3 cellSize, Vector3 cellSpacing, Vector3 origin)
	{
		// Calculate the total size of the grid including spacing
		float totalGridWidth = (gridSize.x * cellSize.x) + ((gridSize.x - 1) * cellSpacing.x);
		float totalGridDepth = (gridSize.y * cellSize.z) + ((gridSize.y - 1) * cellSpacing.z);
		
		// Calculate offset to center the grid at the origin
		float sizeOffsetX = -totalGridWidth / 2f + cellSize.x * 0.5f;
		float sizeOffsetZ = -totalGridDepth / 2f + cellSize.z * 0.5f;
		
		// Calculate cell position with spacing
		float posX = cellPosition.x * (cellSize.x + cellSpacing.x);
		float posZ = cellPosition.y * (cellSize.z + cellSpacing.z);
		
		return new Vector3(
			origin.x + posX + sizeOffsetX,
			origin.y,
			origin.z + posZ + sizeOffsetZ
		);
	}

	public override Vector2Int WorldToGrid(Vector3 worldPosition, Vector2Int gridSize, Vector3 cellSize, Vector3 cellSpacing, Vector3 origin)
	{
		// Remove origin offset
		worldPosition -= origin;
		
		// Calculate the centering offset that was applied in GridToWorld
		float totalGridWidth = (gridSize.x * cellSize.x) + ((gridSize.x - 1) * cellSpacing.x);
		float totalGridDepth = (gridSize.y * cellSize.z) + ((gridSize.y - 1) * cellSpacing.z);
		float sizeOffsetX = -totalGridWidth / 2f;
		float sizeOffsetZ = -totalGridDepth / 2f;
		
		// Remove the centering offset to get back to grid-relative coordinates
		worldPosition.x -= sizeOffsetX;
		worldPosition.z -= sizeOffsetZ;
		
		// Convert to grid coordinates (using x and z)
		int x = Mathf.FloorToInt(worldPosition.x / (cellSize.x + cellSpacing.x));
		int y = Mathf.FloorToInt(worldPosition.z / (cellSize.z + cellSpacing.z));
		
		return new Vector2Int(x, y);
	}

	public override Vector3 Forward => -Vector3.up;
}