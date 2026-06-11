using UnityEngine;
using UnityEngine.InputSystem; 

public class CanvasCameraZoom : MonoBehaviour
{
    [Header("Zoom Einstellungen")]
    public float zoomSpeed = 0.5f;      // Geschwindigkeit des Zooms
    public float minZoom = -5f;         // Maximal herangezoomt (Abweichung vom Startpunkt)
    public float maxZoom = 20f;         // Maximal herausgezoomt (Abweichung vom Startpunkt)

    private Transform cameraTransform;
    private Vector3 startPosition;      // Speichert die Ursprungsposition des Canvas
    private float currentZoomOffset = 0f; // Aktuelle Zoom-Entfernung

    void Start()
    {
        // Sucht die Main Camera, die sich unter dem Canvas befindet
        Camera mainCam = GetComponentInChildren<Camera>();
        
        if (mainCam != null)
        {
            cameraTransform = mainCam.transform;
        }
        else
        {
            Debug.LogError("Keine Kamera unter dem Canvas gefunden! Liegt das Skript auf dem Canvas?");
        }

        // Wir merken uns, wo das Canvas am Anfang stand
        startPosition = transform.position;
    }

    void Update()
    {
        if (cameraTransform == null) return;

        // Mausrad-Input abfragen (Neues Input System)
        float scrollInput = Mouse.current.scroll.ReadValue().y;

        if (scrollInput != 0)
        {
            float normalizedScroll = Mathf.Sign(scrollInput);

            // Wir verändern unseren Zoom-Wert basierend auf dem Input
            // Hinweis: Ein Plus nähert uns an, ein Minus entfernt uns (je nach Blickrichtung)
            currentZoomOffset += normalizedScroll * zoomSpeed;

            // Hier setzen wir die strikten Grenzen (Min und Max)
            currentZoomOffset = Mathf.Clamp(currentZoomOffset, minZoom, maxZoom);

            // Wir berechnen die neue Position ausgehend vom Startpunkt entlang der Kamerablickrichtung
            Vector3 zoomDirection = cameraTransform.forward;
            transform.position = startPosition + (zoomDirection * currentZoomOffset);
        }
    }
}