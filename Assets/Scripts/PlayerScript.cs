using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerScript : MonoBehaviour
{
	public Tilemap tilemap;

	void Update()
	{
		if (Input.GetMouseButtonDown(0)) // Clique esquerdo do mouse
		{
			Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector3Int tilePosition = tilemap.WorldToCell(mouseWorldPos);

			//if (mainTilemap.HasTile(tilePosition)) // Verifica se há um tile na posição
			//{
			//	mainTilemap.SetTile(tilePosition, null); // Remove o tile
			//}
			CrossDestroy(tilePosition);
		}
	}

    void SingleDestroy(Vector3Int tilePosition)
    {
		if (tilemap.HasTile(tilePosition)) // Verifica se há um tile na posição
		{
			tilemap.SetTile(tilePosition, null); // Remove o tile
		}
	}
	void CrossDestroy(Vector3Int centerPosition)
    {
        // Lista de offsets para tiles adjacentes (cima, baixo, esquerda, direita)
        Vector3Int[] directions = {
            Vector3Int.zero,         // O próprio tile clicado
            new Vector3Int(1, 0, 0), // Direita
            new Vector3Int(-1, 0, 0), // Esquerda
            new Vector3Int(0, 1, 0), // Cima
            new Vector3Int(0, -1, 0) // Baixo
        };

        foreach (Vector3Int direction in directions)
        {
            Vector3Int targetPosition = centerPosition + direction;
            if (tilemap.HasTile(targetPosition))
            {
                tilemap.SetTile(targetPosition, null); // Remove o tile
            }
        }
    }
}
