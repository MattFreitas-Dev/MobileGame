using UnityEngine;
using UnityEngine.Tilemaps;

public class MobilePlayerScript : MonoBehaviour
{
	public Tilemap tilemap;

	void Update()
	{
		if (Input.touchCount > 0) // Se houver toque na tela
		{
			Touch touch = Input.GetTouch(0); // Pega o primeiro toque
			if (touch.phase == TouchPhase.Began) // Apenas quando o toque começar
			{
				Vector3 touchWorldPos = Camera.main.ScreenToWorldPoint(touch.position);
				Vector3Int tilePosition = tilemap.WorldToCell(touchWorldPos);

				DestroyTileAndNeighbors(tilePosition);
			}
		}
	}

	void DestroyTileAndNeighbors(Vector3Int centerPosition)
	{
		Vector3Int[] directions = {
			Vector3Int.zero, new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
			new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
		};

		foreach (Vector3Int direction in directions)
		{
			Vector3Int targetPosition = centerPosition + direction;
			if (tilemap.HasTile(targetPosition))
			{
				tilemap.SetTile(targetPosition, null);
			}
		}
	}
}
