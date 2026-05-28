using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("UI Canvases")]
    public GameObject mainMenuCanvas;
    public GameObject settingsCanvas;

    [Header("Scenename für Play")]
    public string gameplaySceneName = "GameplayScene";

    // 1. Spiel starten / Szene wechseln
    public void PlayGame()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    // 2. Einstellungen öffnen
    public void OpenSettings()
    {
        if (mainMenuCanvas != null && settingsCanvas != null)
        {
            mainMenuCanvas.SetActive(false);
            settingsCanvas.SetActive(true);
        }
    }

    // 3. Zurück zum Hauptmenü (aus den Einstellungen)
    public void CloseSettings()
    {
        if (mainMenuCanvas != null && settingsCanvas != null)
        {
            mainMenuCanvas.SetActive(true);
            settingsCanvas.SetActive(false);
        }
    }

    // 4. Spiel beenden
    public void QuitGame()
    {
        Debug.Log("Spiel wird beendet..."); // Sichtbar im Editor-Fenster
        Application.Quit();
    }
}
