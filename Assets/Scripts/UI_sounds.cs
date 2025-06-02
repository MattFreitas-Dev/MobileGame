using UnityEngine;

public class UI_sounds : MonoBehaviour
{
	public AudioSource audioSource;
	public AudioClip buttonSFX;
    public AudioClip menuSFX;

    public void Menu_Sound()
    {
        audioSource.clip = menuSFX;
        audioSource.Play();
    }

    public void Button_Sound()
    {
        audioSource.clip = buttonSFX;
        audioSource.Play();
    }
}
