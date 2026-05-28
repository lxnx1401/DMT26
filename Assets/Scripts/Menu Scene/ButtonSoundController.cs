using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class ButtonSoundController : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Audio-Clips")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    private AudioSource audioSource;
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        
        // AudioSource automatisch hinzufügen oder finden
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    // Wird aufgerufen, wenn die Maus über den Button fährt
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable && hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }

    // Wird aufgerufen, wenn der Button angeklickt wird
    public void OnPointerClick(PointerEventData eventData)
    {
        if (button.interactable && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}
