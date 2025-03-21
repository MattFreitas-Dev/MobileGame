using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // Para reiniciar o jogo

public class TileDestroyer : MonoBehaviour
{
	public Tilemap tilemap;

	public enum PowerType { Single, Explosion }
	public PowerType currentPower = PowerType.Single;

	public Button buttonSingle;
	public Button buttonExplosion;
	public TMP_Text ammoSingleText;
	public TMP_Text ammoExplosionText;
	public GameObject gameOverPanel; // Painel de Game Over
	public GameObject successPanel; // Painel de Sucesso

	[SerializeField] private int ammoSingle = 3;
	[SerializeField] private int ammoExplosion = 1;

	void Start()
	{
		buttonSingle.onClick.AddListener(SetSinglePower);
		buttonExplosion.onClick.AddListener(SetExplosionPower);

		gameOverPanel.SetActive(false);
		successPanel.SetActive(false); // Oculta o painel de sucesso no início
		UpdateAmmoUI();
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
			DestroyTiles(centerPosition);
			ammoSingle--;
		}
		else if (currentPower == PowerType.Explosion && ammoExplosion > 0)
		{
			DestroyTiles(centerPosition);
			ammoExplosion--;
		}

		UpdateAmmoUI();
		CheckGameOverOrSuccess(); // Agora verifica se o jogo acabou ou se foi um sucesso
	}

	void DestroyTiles(Vector3Int centerPosition)
	{
		Vector3Int[] directions;

		if (currentPower == PowerType.Single)
		{
			directions = new Vector3Int[] { Vector3Int.zero };
		}
		else if (currentPower == PowerType.Explosion)
		{
			directions = new Vector3Int[] {
				Vector3Int.zero, new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
				new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
			};
		}
		else return;

		foreach (Vector3Int direction in directions)
		{
			Vector3Int targetPosition = centerPosition + direction;
			if (tilemap.HasTile(targetPosition))
			{
				tilemap.SetTile(targetPosition, null);
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

	void UpdateAmmoUI()
	{
		ammoSingleText.text = $"Single: {ammoSingle}";
		ammoExplosionText.text = $"Explosion: {ammoExplosion}";

		buttonSingle.interactable = ammoSingle > 0;
		buttonExplosion.interactable = ammoExplosion > 0;
	}

	void CheckGameOverOrSuccess()
	{
		if (AllTilesDestroyed())
		{
			Success();
		}
		else if (ammoSingle == 0 && ammoExplosion == 0 && AnyTileLeft())
		{
			GameOver();
		}
	}

	bool AnyTileLeft()
	{
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
		return !AnyTileLeft(); // Se não houver mais tiles, então o jogador venceu!
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

	public void NextLevel()
	{
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex+1);
	}
}
