using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "FireAmmo/Explosion")]
public class FireExplosionEffect : FireAmmoEffect
{
	public override List<Vector3Int> GetAffectedTiles(Vector3Int center, Tilemap vineTilemap)
	{
		List<Vector3Int> result = new List<Vector3Int>();

		// 3x3, ignorando o centro
		for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				if (x == 0 && y == 0) continue;
				Vector3Int pos = center + new Vector3Int(x, y, 0);
				if (vineTilemap.HasTile(pos))
					result.Add(pos);
			}
		}

		return result;
	}
}
