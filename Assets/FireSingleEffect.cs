using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "FireAmmo/Single")]
public class FireSingleEffect : FireAmmoEffect
{
	public override List<Vector3Int> GetAffectedTiles(Vector3Int center, Tilemap vineTilemap)
	{
		if (vineTilemap.HasTile(center))
			return new List<Vector3Int> { center };
		return new List<Vector3Int>();
	}
}
