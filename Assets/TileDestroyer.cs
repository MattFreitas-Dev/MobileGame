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
	public Image lShapeIndicator;
	private int lShapeIndex = 0;
	private Vector3 startTouchWorldPos;
	private Vector3 touchStartWorldPos;
	private Vector3Int touchStartTilePos;

	private bool isDragging = false;

	public enum PowerType { Single, Explosion, Line, Pierce, LShape }
	public PowerType currentPower = PowerType.Single;	

	public Button buttonSingle;
	public Button buttonExplosion;
	public Button buttonLine;
	public Button buttonPierce;
	public Button buttonLShape;

	public TMP_Text ammoSingleText;
	public TMP_Text ammoExplosionText;
	public TMP_Text ammoLineText;
	public TMP_Text ammoPierceText;
	public TMP_Text ammoLShapeText;

	public GameObject gameOverPanel;
	public GameObject successPanel;

	[SerializeField] private int ammoSingle = 3;
	[SerializeField] private int ammoExplosion = 1;
	[SerializeField] private int ammoLine = 2;
	[SerializeField] private int ammoPierce = 2;
	[SerializeField] private int ammoLShape = 2;

	// AUDIO
	[SerializeField] private AudioSource audioSource;
	[SerializeField] private AudioClip singleSound;
	[SerializeField] private AudioClip explosionSound;
	[SerializeField] private AudioClip lineSound;
	[SerializeField] private AudioClip pierceSound;
	[SerializeField] private AudioClip lShapeSound;

	private Dictionary<Vector3Int, int> resistantTilesHealth = new Dictionary<Vector3Int, int>();

	private readonly float[] lShapeRotations = { 0f, 90f, 180f, 270f };


	void Start()
	{		
		buttonSingle.onClick.AddListener(SetSinglePower);
		buttonExplosion.onClick.AddListener(SetExplosionPower);
		buttonLine.onClick.AddListener(SetLinePower);
		buttonPierce.onClick.AddListener(SetPiercePower);
		buttonLShape.onClick.AddListener(SetLShapePower);

		//buttonLShape.onClick.AddListener(RotateLShape);
		RandomizeLShapeDirection();


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

			if (currentPower == PowerType.Line)
			{
				touchStartWorldPos = worldPos;
				touchStartTilePos = tilePos;
				isDragging = true;
			}
			else
			{
				TryDestroyTiles(tilePos);
			}
		}

		if (Input.GetMouseButtonUp(0) && isDragging && currentPower == PowerType.Line)
		{
			Vector3 endWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 dragDirection = endWorldPos - touchStartWorldPos;

			TryDestroyLineWithDirection(touchStartTilePos, dragDirection);
			isDragging = false;
		}
#endif

#if UNITY_ANDROID || UNITY_IOS
		if (Input.touchCount > 0)
		{
			Touch touch = Input.GetTouch(0);
			Vector3 worldPos = Camera.main.ScreenToWorldPoint(touch.position);
			Vector3Int tilePos = tilemap.WorldToCell(worldPos);

			if (touch.phase == TouchPhase.Began)
			{
				if (currentPower == PowerType.Line)
				{
					touchStartWorldPos = worldPos;
					touchStartTilePos = tilePos;
					isDragging = true;
				}
				else
				{
					TryDestroyTiles(tilePos);
				}
			}
			else if (touch.phase == TouchPhase.Ended && isDragging && currentPower == PowerType.Line)
			{
				Vector3 endWorldPos = Camera.main.ScreenToWorldPoint(touch.position);
				Vector2 dragDirection = endWorldPos - touchStartWorldPos;

				TryDestroyLineWithDirection(touchStartTilePos, dragDirection);
				isDragging = false;
			}
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
			audioSource.PlayOneShot(singleSound);
		}
		else if (currentPower == PowerType.Explosion && ammoExplosion > 0)
		{
			DestroyTiles(centerPosition, new Vector3Int[] {	Vector3Int.zero, Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down });
			ammoExplosion--;
			audioSource.PlayOneShot(explosionSound);
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
			audioSource.PlayOneShot(pierceSound);
		}
		else if (currentPower == PowerType.LShape && ammoLShape > 0)
		{
			// Define os padrões de "L"

			Vector3Int[][] lPatterns = new Vector3Int[][]
			{
				new Vector3Int[] { Vector3Int.zero, Vector3Int.up, Vector3Int.right },   // 0° (Cima, Direita)
				new Vector3Int[] { Vector3Int.zero, Vector3Int.up, Vector3Int.left },    // 90° (Cima, Esquerda)
				new Vector3Int[] { Vector3Int.zero, Vector3Int.down, Vector3Int.left },  // 180° (Baixo, Esquerda)
				new Vector3Int[] { Vector3Int.zero, Vector3Int.down, Vector3Int.right }  // 270° (Baixo, Direita)
			};

			Vector3Int[] selectedPattern = lPatterns[lShapeIndex]; // Usa a rotação correta

			DestroyTiles(centerPosition, selectedPattern);
			ammoLShape--;
			audioSource.PlayOneShot(lShapeSound);
			RandomizeLShapeDirection();
		}

		UpdateAmmoUI();
		CheckGameOverOrSuccess();
	}
	void TryDestroyLineWithDirection(Vector3Int startPosition, Vector2 dragDirection)
	{
		if (currentPower != PowerType.Line || ammoLine <= 0)
			return;

		if (!tilemap.HasTile(startPosition) && !resistantTilesHealth.ContainsKey(startPosition))
			return;

		Vector3Int dir;

		// Decide a direção principal do arrasto
		if (Mathf.Abs(dragDirection.x) > Mathf.Abs(dragDirection.y))
		{
			// Horizontal
			dir = (dragDirection.x > 0) ? Vector3Int.right : Vector3Int.left;
		}
		else
		{
			// Vertical
			dir = (dragDirection.y > 0) ? Vector3Int.up : Vector3Int.down;
		}

		// Destroi os 3 tiles a partir da ponta
		Vector3Int[] directions = new Vector3Int[]
		{
		Vector3Int.zero,           // Ponta (onde clicou)
        dir,                       // Próximo tile
        dir * 2                    // Segundo tile
		};

		DestroyTiles(startPosition, directions);
		ammoLine--;
		audioSource.PlayOneShot(lineSound);

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

	public void SetLShapePower()
	{
		if (ammoLShape > 0)
			currentPower = PowerType.LShape;
	}

	void UpdateAmmoUI()
	{
		ammoSingleText.text = $"Single: {ammoSingle}";
		ammoExplosionText.text = $"Explosion: {ammoExplosion}";
		ammoLineText.text = $"Line: {ammoLine}";
		ammoPierceText.text = $"Pierce: {ammoPierce}";
		ammoLShapeText.text = $"L-Shape: {ammoLShape}";

		buttonSingle.interactable = ammoSingle > 0;
		buttonExplosion.interactable = ammoExplosion > 0;
		buttonLine.interactable = ammoLine > 0;
		buttonPierce.interactable = ammoPierce > 0;
		buttonLShape.interactable = ammoLShape > 0;
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
	//public void RotateLShape() // rotate the icon image representing the L shape
	//{
	//	lShapeIndex = (lShapeIndex + 1) % lShapeRotations.Length; // Alterna entre 0,1,2,3
	//	lShapeIndicator.rectTransform.rotation = Quaternion.Euler(0, 0, lShapeRotations[lShapeIndex]);
	//}

	private void RandomizeLShapeDirection()
	{
		lShapeIndex = Random.Range(0, 4); // Sorteia um número entre 0 e 3
		lShapeIndicator.rectTransform.rotation = Quaternion.Euler(0, 0, lShapeRotations[lShapeIndex]);
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
