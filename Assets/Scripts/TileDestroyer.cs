using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // Para reiniciar o jogo
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;

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
	public RectTransform highlightImage;


	[SerializeField] private int ammoSingle = 3;
	[SerializeField] private int ammoExplosion = 1;
	[SerializeField] private int ammoLine = 2;
	[SerializeField] private int ammoPierce = 2;
	[SerializeField] private int ammoLShape = 2;

	// AUDIO
	[Header("Sound Effects")]
	[SerializeField] private AudioSource audioSource;
	[SerializeField] private AudioClip singleSound;
	[SerializeField] private AudioClip explosionSound;
	[SerializeField] private AudioClip lineSound;
	[SerializeField] private AudioClip pierceSound;
	[SerializeField] private AudioClip lShapeSound;

	// VFX
	[Header("Visual Effects")]
	public GameObject singleEffect;
	public GameObject explosionEffect;
	public GameObject lineEffect;
	public GameObject pierceEffect;
	public GameObject lShapeEffect;


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
			if (EventSystem.current.IsPointerOverGameObject())
				return; // Está clicando na UI, então ignora.

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
				if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
					return;

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


	//METHODS

	void TryDestroyTiles(Vector3Int centerPosition)
	{
		if (!tilemap.HasTile(centerPosition))
			return;

		if (currentPower == PowerType.Single && ammoSingle > 0)
		{
			StartCoroutine(DestroyTilesWithDelay(centerPosition, new Vector3Int[] { Vector3Int.zero }));

			ammoSingle--;
			audioSource.PlayOneShot(singleSound);
		}
		else if (currentPower == PowerType.Explosion && ammoExplosion > 0)
		{
			StartCoroutine(DestroyTilesWithDelay(centerPosition, new Vector3Int[] {	Vector3Int.zero, Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down }));
			ammoExplosion--;
			audioSource.PlayOneShot(explosionSound);
		}
		else if (currentPower == PowerType.Line && ammoLine > 0)
		{
			StartCoroutine(DestroyTilesWithDelay(centerPosition, new Vector3Int[] {	Vector3Int.zero, Vector3Int.right, Vector3Int.left }));
			ammoLine--;
		}
		else if (currentPower == PowerType.Pierce && ammoPierce > 0)
		{
			StartCoroutine(DestroyTilesWithDelay(centerPosition, new Vector3Int[] { Vector3Int.zero, Vector3Int.zero, Vector3Int.zero }));
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

			StartCoroutine(DestroyTilesWithDelay(centerPosition, selectedPattern));
			ammoLShape--;
			audioSource.PlayOneShot(lShapeSound);
			RandomizeLShapeDirection();
		}

		UpdateAmmoUI();
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

		StartCoroutine(DestroyTilesWithDelay(startPosition, directions));
		ammoLine--;
		audioSource.PlayOneShot(lineSound);

		UpdateAmmoUI();
	}



	IEnumerator DestroyTilesWithDelay(Vector3Int centerPosition, Vector3Int[] directions, float delay = 0.3f)
	{
		foreach (Vector3Int direction in directions)
		{
			Vector3Int targetPosition = centerPosition + direction;
			PlayEffect(GetEffectForCurrentPower(), targetPosition);
		}

		yield return new WaitForSeconds(delay);

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

		UpdateAmmoUI();
		CheckGameOverOrSuccess();
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
		{
			currentPower = PowerType.Single;
			MoveHighlightToButton(buttonSingle);
		}
	}

	public void SetExplosionPower()
	{
		if (ammoExplosion > 0)
		{
			currentPower = PowerType.Explosion;
			MoveHighlightToButton(buttonExplosion);
		}
	}
	public void SetLinePower()
	{
		if (ammoLine > 0)
		{
			currentPower = PowerType.Line;
			MoveHighlightToButton(buttonLine);
		}
	}

	public void SetPiercePower()
	{
		if (ammoPierce > 0)
		{
			currentPower = PowerType.Pierce;
			MoveHighlightToButton(buttonPierce);
		}
	}

	public void SetLShapePower()
	{
		if (ammoLShape > 0)
		{
			currentPower = PowerType.LShape;
			MoveHighlightToButton(buttonLShape);
		}
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
	private void MoveHighlightToButton(Button selectedButton)
	{
		highlightImage.position = selectedButton.transform.position;
	}

	private void RandomizeLShapeDirection()
	{
		lShapeIndex = Random.Range(0, 4); // Sorteia um número entre 0 e 3
		lShapeIndicator.rectTransform.rotation = Quaternion.Euler(0, 0, lShapeRotations[lShapeIndex]);
	}

	// VFX
	void PlayEffect(GameObject effectPrefab, Vector3Int position)
	{
		if (effectPrefab == null) return;

		Vector3 worldPos = tilemap.CellToWorld(position) + tilemap.cellSize / 2;
		Instantiate(effectPrefab, worldPos, Quaternion.identity);
		GameObject effect = effectPrefab;
		//Destroy(effectPrefab, 1f);
	}
	GameObject GetEffectForCurrentPower()
	{
		switch (currentPower)
		{
			case PowerType.Single: return singleEffect;
			case PowerType.Explosion: return explosionEffect;
			case PowerType.Line: return lineEffect;
			case PowerType.Pierce: return pierceEffect;
			case PowerType.LShape: return lShapeEffect;
			default: return null;
		}
	}
	
	IEnumerator PlayEffectCascade(GameObject effectPrefab, Vector3Int[] tilePositions, float delay = 0.2f)
	{
		foreach (var tilePos in tilePositions)
		{
			Vector3 worldPos = tilemap.CellToWorld(tilePos) + tilemap.cellSize / 2f;
			var instance = Instantiate(effectPrefab, worldPos, Quaternion.identity);
			//Destroy(instance, 2f); // Destrói o efeito após 2 segundos
			yield return new WaitForSeconds(delay);
		}
	}
	IEnumerator PlayLShapeEffectCascade(GameObject effectPrefab, Vector3Int[] tilePositions, float delay = 0.1f)
	{
		if (tilePositions.Length < 3)
		{
			// Fallback se por algum motivo tiver menos que 3 tiles
			yield return PlayEffectCascade(effectPrefab, tilePositions, delay);
			yield break;
		}

		// Primeiro tile
		Vector3 worldPos = tilemap.CellToWorld(tilePositions[0]) + tilemap.cellSize / 2f;
		var firstEffect = Instantiate(effectPrefab, worldPos, Quaternion.identity);
		//Destroy(firstEffect, 2f);

		yield return new WaitForSeconds(delay);

		// Segundo e terceiro ao mesmo tempo
		for (int i = 1; i < tilePositions.Length; i++)
		{
			Vector3 pos = tilemap.CellToWorld(tilePositions[i]) + tilemap.cellSize / 2f;
			var fx = Instantiate(effectPrefab, pos, Quaternion.identity);
			//Destroy(fx, 2f);
		}
	}

	//  END OF VFX

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
	void CheckGameOverOrSuccess()
	{
		if (AllTilesDestroyed())
		{
			Success();
		}
		else if (ammoSingle == 0 && ammoExplosion == 0 && ammoLine == 0 && ammoPierce == 0 && ammoLShape == 0 && AnyTileLeft())
		{
			GameOver();
		}
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
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
	}
}
