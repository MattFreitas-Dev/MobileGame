using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // Para reiniciar o jogo
using System.Collections.Generic;

public class TileDestroyer : MonoBehaviour
{
	public Tilemap tilemap;
	public Tilemap resistantTilemap;
	public int resistantTilesInicialHealth;

	public enum PowerType { Single, Explosion, Line, Pierce }
	public PowerType currentPower = PowerType.Single;	

	public Button buttonSingle;
	public Button buttonExplosion;
	public Button buttonLine;
	public Button buttonPierce;

	public TMP_Text ammoSingleText;
	public TMP_Text ammoExplosionText;
	public TMP_Text ammoLineText;
	public TMP_Text ammoPierceText;

	public GameObject gameOverPanel;
	public GameObject successPanel;

	[SerializeField] private int ammoSingle = 3;
	[SerializeField] private int ammoExplosion = 1;
	[SerializeField] private int ammoLine = 2;
	[SerializeField] private int ammoPierce = 2;

	private Dictionary<Vector3Int, int> resistantTilesHealth = new Dictionary<Vector3Int, int>();

	void Start()
	{
		buttonSingle.onClick.AddListener(SetSinglePower);
		buttonExplosion.onClick.AddListener(SetExplosionPower);
		buttonLine.onClick.AddListener(SetLinePower);
		buttonPierce.onClick.AddListener(SetPiercePower);

		gameOverPanel.SetActive(false);
		successPanel.SetActive(false);
		UpdateAmmoUI();
		InitializeResistantTiles();
	}

	void Update()
	{
		HandleInput();
	}

	void HandleInput()
	{
#if UNITY_EDITOR || UNITY_STANDALONE
		if (Input.GetMouseButtonDown(0))
		{
			Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector3Int tilePos = tilemap.WorldToCell(worldPos);
			TryDestroyTiles(tilePos);
		}
#endif

#if UNITY_ANDROID || UNITY_IOS
		if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
		{
			Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);
			Vector3Int tilePos = tilemap.WorldToCell(worldPos);
			TryDestroyTiles(tilePos);
		}
#endif
	}
	
	void TryDestroyTiles(Vector3Int centerPosition)
	{
		if (!tilemap.HasTile(centerPosition))
			return;

		if (currentPower == PowerType.Single && ammoSingle > 0)
		{
			DestroyTiles(centerPosition, new Vector3Int[] { Vector3Int.zero });
			ammoSingle--;
		}
		else if (currentPower == PowerType.Explosion && ammoExplosion > 0)
		{
			DestroyTiles(centerPosition, new Vector3Int[] {	Vector3Int.zero, Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down });
			ammoExplosion--;
		}
		else if (currentPower == PowerType.Line && ammoLine > 0)
		{
			DestroyTiles(centerPosition, new Vector3Int[] {	Vector3Int.zero, Vector3Int.right, Vector3Int.left });
			ammoLine--;
		}
		else if (currentPower == PowerType.Pierce && ammoPierce > 0)
		{
			DestroyTiles(centerPosition, new Vector3Int[] { Vector3Int.zero, Vector3Int.zero, Vector3Int.zero });
			ammoPierce--;
		}

		UpdateAmmoUI();
		CheckGameOverOrSuccess();
	}

	void DestroyTiles(Vector3Int centerPosition, Vector3Int[] directions)
	{
		foreach (Vector3Int direction in directions)
		{
			Vector3Int targetPosition = centerPosition + direction;
			if (resistantTilesHealth.ContainsKey(targetPosition))
			{
				resistantTilesHealth[targetPosition]--;
				if (resistantTilesHealth[targetPosition] <= 0)
				{
					resistantTilemap.SetTile(targetPosition, null);
					resistantTilesHealth.Remove(targetPosition);
				}
			}
			else if (tilemap.HasTile(targetPosition))
			{
				tilemap.SetTile(targetPosition, null);
			}
		}
	}

	void InitializeResistantTiles()
	{
		BoundsInt bounds = resistantTilemap.cellBounds;
		foreach (Vector3Int pos in bounds.allPositionsWithin)
		{
			if (resistantTilemap.HasTile(pos))
			{
				resistantTilesHealth[pos] = resistantTilesInicialHealth;
			}
		}
	}

	public void SetSinglePower()
	{
		if (ammoSingle > 0)
			currentPower = PowerType.Single;
	}

	public void SetExplosionPower()
	{
		if (ammoExplosion > 0)
			currentPower = PowerType.Explosion;
	}
	public void SetLinePower()
	{
		if (ammoLine > 0)
			currentPower = PowerType.Line;
	}

	public void SetPiercePower()
	{
		if (ammoPierce > 0)
			currentPower = PowerType.Pierce;
	}


	void UpdateAmmoUI()
	{
		ammoSingleText.text = $"Single: {ammoSingle}";
		ammoExplosionText.text = $"Explosion: {ammoExplosion}";
		ammoLineText.text = $"Line: {ammoLine}";
		ammoPierceText.text = $"Pierce: {ammoPierce}";

		buttonSingle.interactable = ammoSingle > 0;
		buttonExplosion.interactable = ammoExplosion > 0;
		buttonLine.interactable = ammoLine > 0;
		buttonPierce.interactable = ammoPierce > 0;
	}

	void CheckGameOverOrSuccess()
	{
		if (AllTilesDestroyed())
		{
			Success();
		}
		else if (ammoSingle == 0 && ammoExplosion == 0 && ammoLine == 0 && ammoPierce == 0 && AnyTileLeft())
		{
			GameOver();
		}
	}

	bool AnyTileLeft()
	{
		if (resistantTilesHealth.Count > 0)
			return true;

		BoundsInt bounds = tilemap.cellBounds;
		foreach (Vector3Int pos in bounds.allPositionsWithin)
		{
			if (tilemap.HasTile(pos))
			{
				return true; // Ainda há tiles no mapa
			}
		}
		return false; // Todos os tiles foram destruídos
	}

	bool AllTilesDestroyed()
	{
		return !AnyTileLeft();
	}

	void GameOver()
	{
		gameOverPanel.SetActive(true);
	}

	void Success()
	{
		successPanel.SetActive(true);
	}

	public void RestartGame()
	{
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}
}
