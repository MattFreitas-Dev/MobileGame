using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class UI_Manager : MonoBehaviour
{
    public AudioMixer musicMixer;
    public AudioMixer sFXMixer;

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
