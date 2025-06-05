using UnityEngine;
using UnityEngine.Tilemaps;

public class TileGenerator : MonoBehaviour
{
	public Tilemap tilemap;         // Referência ao Tilemap
	public TileBase destructibleTile;  // Tile que será usado
	public Vector2Int mapSize = new Vector2Int(10, 10);  // Tamanho do mapa (em células)

	public void GenerateTiles(int totalTiles)
	{
		// Limpa o tilemap antes de gerar
		tilemap.ClearAllTiles();

		int tilesPlaced = 0;
		int attempts = 0;
		int maxAttempts = totalTiles * 10;  // Evita loop infinito

		while (tilesPlaced < totalTiles && attempts < maxAttempts)
		{
			attempts++;

			// Gera uma posição aleatória dentro do mapa
			Vector3Int randomPos = new Vector3Int(
				Random.Range(-mapSize.x / 2, mapSize.x / 2),
				Random.Range(-mapSize.y / 2, mapSize.y / 2),
				0
			);

			// Só coloca o tile se ainda não tiver nada
			if (!tilemap.HasTile(randomPos))
			{
				tilemap.SetTile(randomPos, destructibleTile);
				tilesPlaced++;
			}
		}

		Debug.Log($"Tiles gerados: {tilesPlaced} em {attempts} tentativas");
	}
}
