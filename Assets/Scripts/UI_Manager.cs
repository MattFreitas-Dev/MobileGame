using System.Data.Common;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.DebugUI;

public class UI_Manager : MonoBehaviour
{
    public AudioMixer musicMixer;
    public AudioMixer sFXMixer;
	public float initialMusicVolume = -25f;
	public float initialSFXVolume = -25f;
	private void Awake()
	{
		
	}
	private void Start()
	{
		musicMixer.SetFloat("MusicVolume", initialMusicVolume);
		sFXMixer.SetFloat("SFXVolume", initialSFXVolume);
	}

	public void MusicVolume(float volume)
    {
        musicMixer.SetFloat("MusicVolume", volume);
    }
	public void SFXVolume(float volume)
	{
		sFXMixer.SetFloat("SFXVolume", volume);
	}

	public void StartGame()
    {
        SceneManager.LoadScene(1);		
    }

    public void QuitToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    public void QuitGame()
    {
		Application.Quit();
	}
}
