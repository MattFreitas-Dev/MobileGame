using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;
using static TileDestroyer;

public class TileDestroyer : MonoBehaviour
{
	public Tilemap tilemap;
	public Tilemap resistantTilemap;
	public Tilemap vineTilemap;

	public int resistantTilesInicialHealth = 1;

	// Botões e textos para munições normais
	public Button buttonSingle, buttonExplosion, buttonLine, buttonPierce, buttonLShape;
	public TMP_Text ammoSingleText, ammoExplosionText, ammoLineText, ammoPierceText, ammoLShapeText;

	// Botões e textos para munições de fogo
	public Button buttonFireSingle, buttonFireExplosion, buttonFireLine;
	public TMP_Text ammoFireSingleText, ammoFireExplosionText, ammoFireLineText;

	public GameObject gameOverPanel, successPanel;
	public RectTransform highlightImage;
	public Image lShapeIndicator;

	public GameObject singleEffect, explosionEffect, lineEffect, pierceEffect, lShapeEffect;
	public GameObject fireSingleEffect, fireExplosionEffect, fireLineEffect;

	[Header("Sound Effects")]
	public AudioSource audioSource;
	public AudioClip singleSound, explosionSound, lineSound, pierceSound, lShapeSound;
	public AudioClip fireSound;

	public enum PowerType { Single, Explosion, Line, Pierce, LShape }
	public enum FirePowerType { FireSingle, FireExplosion, FireLine }

	private PowerType? currentPower = PowerType.Single;
	private FirePowerType? currentFirePower = null;
	[SerializeField]
	private int ammoSingle = 3, ammoExplosion = 1, ammoLine = 2, ammoPierce = 2, ammoLShape = 2, ammoFireSingle = 2, ammoFireExplosion = 1, ammoFireLine = 1;

	private Dictionary<Vector3Int, int> resistantTilesHealth = new Dictionary<Vector3Int, int>();
	private Vector3 startTouchWorldPos;
	private Vector3Int touchStartTilePos;
	private bool isDragging = false;
	private int lShapeIndex = 0;
	private readonly float[] lShapeRotations = { 0f, 90f, 180f, 270f };

	void Start()
	{
		// Botões de munição normal
		buttonSingle.onClick.AddListener(() => SelectPower(PowerType.Single));
		buttonExplosion.onClick.AddListener(() => SelectPower(PowerType.Explosion));
		buttonLine.onClick.AddListener(() => SelectPower(PowerType.Line));
		buttonPierce.onClick.AddListener(() => SelectPower(PowerType.Pierce));
		buttonLShape.onClick.AddListener(() => SelectPower(PowerType.LShape));

		// Botões de munição de chama
		buttonFireSingle.onClick.AddListener(() => SelectFirePower(FirePowerType.FireSingle));
		buttonFireExplosion.onClick.AddListener(() => SelectFirePower(FirePowerType.FireExplosion));
		buttonFireLine.onClick.AddListener(() => SelectFirePower(FirePowerType.FireLine));

		RandomizeLShapeDirection();
		UpdateAmmoUI();
		InitializeResistantTiles();
	}

	void Update()
	{
		HandleInput();
	}

	void SelectPower(PowerType power)
	{
		if (GetAmmo(power) > 0)
		{
			currentPower = power;
			currentFirePower = null;
			MoveHighlightToButton(GetButtonForPower(power));
		}
	}

	void SelectFirePower(FirePowerType firePower)
	{
		if (GetAmmo(firePower) > 0)
		{
			currentFirePower = firePower;
			currentPower = null;
			MoveHighlightToButton(GetButtonForFirePower(firePower));
		}
	}

	void HandleInput()
	{
		if (Input.GetMouseButtonDown(0))
		{
			// Bloqueia clique em UI
			if (EventSystem.current.IsPointerOverGameObject()) return;

			// Converte para posição no mundo
			Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			worldPos.z = 0f;

			// Converte para posição da célula do tilemap
			Vector3Int tilePos = tilemap.WorldToCell(worldPos);

			// Verifica se há tile nessa posição
			if (tilemap.HasTile(tilePos))
			{
				if (currentPower == PowerType.Line || currentFirePower == FirePowerType.FireLine)
				{
					startTouchWorldPos = worldPos;
					touchStartTilePos = tilePos;
					isDragging = true;
				}
				else
				{
					TryDestroyTiles(tilePos);
				}
			}
		}

		if (Input.GetMouseButtonUp(0) && isDragging)
		{
			Vector3 endWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			endWorldPos.z = 0f;

			Vector2 dragDir = endWorldPos - startTouchWorldPos;

			TryDestroyLine(touchStartTilePos, dragDir);
			isDragging = false;
		}
	}




	void TryDestroyTiles(Vector3Int center)
	{
		if (currentPower != null)
		{
			PowerType power = currentPower.Value;
			if (GetAmmo(power) <= 0) return;

			var positions = GetAffectedTiles(power, center);
			StartCoroutine(DestroyTilesWithDelay(positions, false));
			UseAmmo(power);
		}
		else if (currentFirePower != null)
		{
			FirePowerType firePower = currentFirePower.Value;
			if (GetAmmo(firePower) <= 0) return;

			var positions = GetAffectedTiles(firePower, center);
			StartCoroutine(DestroyTilesWithDelay(positions, true));
			UseAmmo(firePower);
		}
		UpdateAmmoUI();
	}

	void TryDestroyLine(Vector3Int start, Vector2 dir)
	{
		Vector3Int mainDir = Mathf.Abs(dir.x) > Mathf.Abs(dir.y)
			? (dir.x > 0 ? Vector3Int.right : Vector3Int.left)
			: (dir.y > 0 ? Vector3Int.up : Vector3Int.down);

		Vector3Int[] linePattern = new[] { Vector3Int.zero, mainDir, mainDir * 2 };

		if (currentPower == PowerType.Line && ammoLine > 0)
		{
			StartCoroutine(DestroyTilesWithDelay(GetTilePositions(start, linePattern), false));
			ammoLine--;
			audioSource.PlayOneShot(lineSound);
		}
		else if (currentFirePower == FirePowerType.FireLine && ammoFireLine > 0)
		{
			StartCoroutine(DestroyTilesWithDelay(GetTilePositions(start, linePattern), true));
			ammoFireLine--;
			audioSource.PlayOneShot(fireSound);
		}
		UpdateAmmoUI();
	}

	Vector3Int[] GetTilePositions(Vector3Int origin, Vector3Int[] pattern)
	{
		Vector3Int[] result = new Vector3Int[pattern.Length];
		for (int i = 0; i < pattern.Length; i++)
			result[i] = origin + pattern[i];
		return result;
	}

	IEnumerator DestroyTilesWithDelay(Vector3Int[] positions, bool isFire)
	{
		foreach (var pos in positions)
		{

			PlayEffect(GetEffect(isFire), pos);
		}

		yield return new WaitForSeconds(0.3f);

		foreach (var pos in positions)
		{
			if (isFire)
			{
				if (vineTilemap.HasTile(pos))
					vineTilemap.SetTile(pos, null);
			}
			else
			{
				if (resistantTilesHealth.ContainsKey(pos))
				{
					// Impede destruição se houver vine
					if (!vineTilemap.HasTile(pos))
					{
						resistantTilesHealth[pos]--;
						if (resistantTilesHealth[pos] <= 0)
						{
							resistantTilemap.SetTile(pos, null);
							resistantTilesHealth.Remove(pos);
						}
					}
				}
				else if (tilemap.HasTile(pos))
				{
					if (!vineTilemap.HasTile(pos))
					{
						tilemap.SetTile(pos, null);
					}
				}
			}
		}

		CheckGameOverOrSuccess();
	}

	GameObject GetEffect(bool isFire)
	{
		if (isFire && currentFirePower != null)
		{
			switch (currentFirePower)
			{
				case FirePowerType.FireSingle: return fireSingleEffect;
				case FirePowerType.FireExplosion: return fireExplosionEffect;
				case FirePowerType.FireLine: return fireLineEffect;
			}
		}
		else if (!isFire && currentPower != null)
		{
			switch (currentPower)
			{
				case PowerType.Single: return singleEffect;
				case PowerType.Explosion: return explosionEffect;
				case PowerType.Line: return lineEffect;
				case PowerType.Pierce: return pierceEffect;
				case PowerType.LShape: return lShapeEffect;
			}
		}
		return null;
	}
	[System.Serializable]
	public class FireAmmo
	{
		public FireAmmoEffect effect;
		public int count;
	}
	public List<FireAmmo> fireAmmoInventory;
	Vector3Int[] GetAffectedTiles(PowerType power, Vector3Int center)
	{
		switch (power)
		{
			case PowerType.Single: return new[] { center };
			case PowerType.Explosion: return new[] { center, center + Vector3Int.up, center + Vector3Int.down, center + Vector3Int.left, center + Vector3Int.right };
			case PowerType.Pierce: return new[] { center, center };
			case PowerType.LShape:
				Vector3Int[][] patterns = {
					new[] { Vector3Int.zero, Vector3Int.up, Vector3Int.right },
					new[] { Vector3Int.zero, Vector3Int.up, Vector3Int.left },
					new[] { Vector3Int.zero, Vector3Int.down, Vector3Int.left },
					new[] { Vector3Int.zero, Vector3Int.down, Vector3Int.right }
				};
				return GetTilePositions(center, patterns[lShapeIndex]);
			default: return new[] { center };
		}
	}

	Vector3Int[] GetAffectedTiles(FirePowerType firePower, Vector3Int center)
	{
		switch (firePower)
		{
			case FirePowerType.FireSingle: return new[] { center };
			case FirePowerType.FireExplosion:
				List<Vector3Int> explosion = new();
				for (int x = -1; x <= 1; x++)
					for (int y = -1; y <= 1; y++)
						if (!(x == 0 && y == 0))
							explosion.Add(center + new Vector3Int(x, y, 0));
				return explosion.ToArray();
			case FirePowerType.FireLine: return new[] { center, center + Vector3Int.up, center + Vector3Int.down };
			default: return new[] { center };
		}
	}

	void PlayEffect(GameObject effectPrefab, Vector3Int pos)
	{
		if (effectPrefab == null) return;
		Vector3 world = tilemap.CellToWorld(pos) + tilemap.cellSize / 2;
		Instantiate(effectPrefab, world, Quaternion.identity);
	}

	void InitializeResistantTiles()
	{
		BoundsInt bounds = resistantTilemap.cellBounds;
		foreach (var pos in bounds.allPositionsWithin)
		{
			if (resistantTilemap.HasTile(pos))
				resistantTilesHealth[pos] = resistantTilesInicialHealth;
		}
	}

	void MoveHighlightToButton(Button btn)
	{
		RectTransform btnRect = btn.GetComponent<RectTransform>();
		RectTransform highlightRect = highlightImage;

		// Copia a posição
		highlightRect.position = btnRect.position;

		// Ajusta o tamanho adicionando +10 pixels de margem em largura e altura (5px cada lado)
		highlightRect.sizeDelta = btnRect.sizeDelta + new Vector2(20f, 10f);
	}


	void RandomizeLShapeDirection()
	{
		lShapeIndex = Random.Range(0, 4);
		lShapeIndicator.rectTransform.rotation = Quaternion.Euler(0, 0, lShapeRotations[lShapeIndex]);
	}

	void UpdateAmmoUI()
	{
		ammoSingleText.text = $"Single: {ammoSingle}";
		ammoExplosionText.text = $"Explosion: {ammoExplosion}";
		ammoLineText.text = $"Line: {ammoLine}";
		ammoPierceText.text = $"Pierce: {ammoPierce}";
		ammoLShapeText.text = $"L: {ammoLShape}";

		ammoFireSingleText.text = $"Fire: {ammoFireSingle}";
		ammoFireExplosionText.text = $"Fire Explosion: {ammoFireExplosion}";
		ammoFireLineText.text = $"Flamethrower: {ammoFireLine}";

		buttonSingle.interactable = ammoSingle > 0;
		buttonExplosion.interactable = ammoExplosion > 0;
		buttonLine.interactable = ammoLine > 0;
		buttonPierce.interactable = ammoPierce > 0;
		buttonLShape.interactable = ammoLShape > 0;

		buttonFireSingle.interactable = ammoFireSingle > 0;
		buttonFireExplosion.interactable = ammoFireExplosion > 0;
		buttonFireLine.interactable = ammoFireLine > 0;
	}

	int GetAmmo(PowerType power) => power switch
	{
		PowerType.Single => ammoSingle,
		PowerType.Explosion => ammoExplosion,
		PowerType.Line => ammoLine,
		PowerType.Pierce => ammoPierce,
		PowerType.LShape => ammoLShape,
		_ => 0
	};

	int GetAmmo(FirePowerType power) => power switch
	{
		FirePowerType.FireSingle => ammoFireSingle,
		FirePowerType.FireExplosion => ammoFireExplosion,
		FirePowerType.FireLine => ammoFireLine,
		_ => 0
	};

	void UseAmmo(PowerType power)
	{
		switch (power)
		{
			case PowerType.Single: ammoSingle--; audioSource.PlayOneShot(singleSound); break;
			case PowerType.Explosion: ammoExplosion--; audioSource.PlayOneShot(explosionSound); break;
			case PowerType.Line: ammoLine--; audioSource.PlayOneShot(lineSound); break;
			case PowerType.Pierce: ammoPierce--; audioSource.PlayOneShot(pierceSound); break;
			case PowerType.LShape: ammoLShape--; audioSource.PlayOneShot(lShapeSound); RandomizeLShapeDirection(); break;
		}
	}

	void UseAmmo(FirePowerType power)
	{
		switch (power)
		{
			case FirePowerType.FireSingle: ammoFireSingle--; break;
			case FirePowerType.FireExplosion: ammoFireExplosion--; break;
			case FirePowerType.FireLine: ammoFireLine--; break;
		}
		audioSource.PlayOneShot(fireSound);
	}

	bool AnyTileLeft()
	{
		foreach (var pos in tilemap.cellBounds.allPositionsWithin)
		{
			if (tilemap.HasTile(pos) || resistantTilemap.HasTile(pos) || vineTilemap.HasTile(pos))
				return true;

		}
		return false;
	}
	bool AnyVineTileLeft()
	{
		foreach (var pos in vineTilemap.cellBounds.allPositionsWithin)
		{
			if (vineTilemap.HasTile(pos))
				return true;
		}
		return false;
	}


	//void CheckFireAmmoGameOver()
	//{
	//	bool hasVineTiles = false;

	//	foreach (var pos in vineTilemap.cellBounds.allPositionsWithin)
	//	{
	//		if (vineTilemap.HasTile(pos))
	//		{
	//			hasVineTiles = true;
	//			break;
	//		}
	//	}

	//	if (hasVineTiles && ammoFireSingle == 0 && ammoFireExplosion == 0 && ammoFireLine == 0)
	//	{
	//		Debug.Log("Game Over: Sem munição de fogo e ainda há tiles VINE.");
	//		gameOverPanel.SetActive(true);
	//	}
	//}



	void CheckGameOverOrSuccess()
	{
		//CheckFireAmmoGameOver();
		if (!AnyTileLeft())
		{
			successPanel.SetActive(true);
		}
		else if (ammoFireSingle == 0 && ammoFireExplosion == 0 && ammoFireLine == 0 && AnyVineTileLeft())
		{
			gameOverPanel.SetActive(true);
		}
		else if (			
			ammoSingle == 0 && ammoExplosion == 0 && ammoLine == 0 &&
			ammoPierce == 0 && ammoLShape == 0
		)
		{
			gameOverPanel.SetActive(true);
		}
	}

	public void RestartGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	public void NextLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

	Button GetButtonForPower(PowerType power) => power switch
	{
		PowerType.Single => buttonSingle,
		PowerType.Explosion => buttonExplosion,
		PowerType.Line => buttonLine,
		PowerType.Pierce => buttonPierce,
		PowerType.LShape => buttonLShape,
		_ => null
	};

	Button GetButtonForFirePower(FirePowerType power) => power switch
	{
		FirePowerType.FireSingle => buttonFireSingle,
		FirePowerType.FireExplosion => buttonFireExplosion,
		FirePowerType.FireLine => buttonFireLine,
		_ => null
	};
}
