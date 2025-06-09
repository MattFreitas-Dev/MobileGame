using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "FireAmmo/Line")]
public class FireLineEffect : FireAmmoEffect
{
	public int length = 3;

	public override List<Vector3Int> GetAffectedTiles(Vector3Int center, Tilemap vineTilemap)
	{
		List<Vector3Int> result = new List<Vector3Int>();

		for (int i = -length / 2; i <= length / 2; i++)
		{
			Vector3Int pos = center + new Vector3Int(i, 0, 0);
			if (vineTilemap.HasTile(pos))
				result.Add(pos);
		}

		return result;
	}
}
