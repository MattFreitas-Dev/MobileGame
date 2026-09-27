using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class FireAmmoEffect : ScriptableObject
{
	public abstract List<Vector3Int> GetAffectedTiles(Vector3Int center, Tilemap vineTilemap);
}
