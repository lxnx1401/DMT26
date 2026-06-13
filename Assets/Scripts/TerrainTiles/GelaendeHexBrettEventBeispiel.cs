using UnityEngine;

/// <summary>
/// Beispielsystem, das auf Events von GelaendeHexBrett reagiert.
///
/// Dieses Script ist absichtlich getrennt von GelaendeHexFeld. Generierte
/// Feld-Objekte koennen zur Laufzeit neu aufgebaut werden. Gameplay-Code sollte
/// deshalb auf das Brett hoeren und HexFeldDaten verwenden, statt Referenzen auf
/// temporaere Mesh-Objekte zu speichern.
/// </summary>
[DisallowMultipleComponent]
public sealed class GelaendeHexBrettEventBeispiel : MonoBehaviour
{
    [SerializeField] private GelaendeHexBrett brett = null;

    [Header("Beispiel-Logging")]
    [SerializeField] private bool erzeugteFelderLoggen = false;
    [SerializeField] private bool hoverFelderLoggen = false;
    [SerializeField] private bool ausgewaehlteFelderLoggen = true;
    [SerializeField] private bool auswahlWechselLoggen = true;

    private void Awake()
    {
        // Awake sucht nur die Referenz. Abonniert wird erst in OnEnable, weil
        // Unity Komponenten mehrfach aktivieren/deaktivieren kann.
        BrettReferenzAufloesen();
    }

    private void OnEnable()
    {
        BrettReferenzAufloesen();

        if (brett == null)
        {
            Debug.LogWarning(
                "GelaendeHexBrettEventBeispiel braucht eine GelaendeHexBrett-Referenz.",
                this);
            return;
        }

        // Hier abonnieren wir die Events. OnEnable laeuft vor Start(), daher
        // erwischt dieses Beispiel auch FeldErzeugt-Events des Brett-Generators.
        brett.FeldErzeugt += BeiFeldErzeugt;
        brett.FeldBetreten += BeiFeldBetreten;
        brett.FeldVerlassen += BeiFeldVerlassen;
        brett.FeldAusgewaehlt += BeiFeldAusgewaehlt;
        brett.FeldAbgewaehlt += BeiFeldAbgewaehlt;
        brett.FeldauswahlGeaendert += BeiFeldauswahlGeaendert;
    }

    private void OnDisable()
    {
        if (brett == null)
        {
            return;
        }

        // Immer abmelden. Sonst koennte diese Komponente nach Disable oder
        // Destroy weiterhin Callbacks erhalten.
        brett.FeldErzeugt -= BeiFeldErzeugt;
        brett.FeldBetreten -= BeiFeldBetreten;
        brett.FeldVerlassen -= BeiFeldVerlassen;
        brett.FeldAusgewaehlt -= BeiFeldAusgewaehlt;
        brett.FeldAbgewaehlt -= BeiFeldAbgewaehlt;
        brett.FeldauswahlGeaendert -= BeiFeldauswahlGeaendert;
    }

    private void BrettReferenzAufloesen()
    {
        // Fuer einfache Tests kann dieses Beispiel auf demselben GameObject wie
        // das Brett liegen. Dann muss im Inspector nichts manuell gesetzt werden.
        if (brett == null)
        {
            brett = GetComponent<GelaendeHexBrett>();
        }
    }

    private void BeiFeldErzeugt(GelaendeHexBrett.HexFeldDaten feld)
    {
        // FeldErzeugt ist gut fuer Systeme, die direkt nach dem Brettaufbau
        // Markierungen, Marker oder weitere Komponenten vorbereiten muessen.
        if (!erzeugteFelderLoggen)
        {
            return;
        }

        Debug.Log($"[Feld-Beispiel] Erzeugt: {FeldFormatieren(feld)}", this);
    }

    private void BeiFeldBetreten(GelaendeHexBrett.HexFeldDaten feld)
    {
        // Hover-Events eignen sich fuer UI-Vorschauen. Dauerhafte Spiellogik
        // sollte erst bei Auswahl oder Klick ausgefuehrt werden.
        if (!hoverFelderLoggen)
        {
            return;
        }

        Debug.Log($"[Feld-Beispiel] Betreten: {FeldFormatieren(feld)}", this);
    }

    private void BeiFeldVerlassen(GelaendeHexBrett.HexFeldDaten feld)
    {
        if (!hoverFelderLoggen)
        {
            return;
        }

        Debug.Log($"[Feld-Beispiel] Verlassen: {FeldFormatieren(feld)}", this);
    }

    private void BeiFeldAusgewaehlt(GelaendeHexBrett.HexFeldDaten feld)
    {
        if (!ausgewaehlteFelderLoggen)
        {
            return;
        }

        Debug.Log($"[Feld-Beispiel] Ausgewaehlt: {FeldFormatieren(feld)}", this);

        // Hier wuerde echte Spiellogik starten:
        // - Feld-Infopanel oeffnen
        // - moegliche Strassen-/Siedlungsplaetze anzeigen
        // - Bauvorschau starten
        // - feld.FeldTyp fuer Rohstoffregeln auswerten
    }

    private void BeiFeldAbgewaehlt(GelaendeHexBrett.HexFeldDaten feld)
    {
        if (!ausgewaehlteFelderLoggen)
        {
            return;
        }

        Debug.Log($"[Feld-Beispiel] Abgewaehlt: {FeldFormatieren(feld)}", this);
    }

    private void BeiFeldauswahlGeaendert(
        GelaendeHexBrett.HexFeldDaten vorherigesFeld,
        GelaendeHexBrett.HexFeldDaten aktuellesFeld)
    {
        // Dieses Event ist praktisch, wenn ein System sowohl das alte als auch
        // das neue Feld kennen muss, zum Beispiel um alte Highlights zu entfernen.
        if (!auswahlWechselLoggen)
        {
            return;
        }

        Debug.Log(
            $"[Feld-Beispiel] Auswahl geaendert: {FeldFormatieren(vorherigesFeld)} -> {FeldFormatieren(aktuellesFeld)}",
            this);
    }

    private static string FeldFormatieren(GelaendeHexBrett.HexFeldDaten feld)
    {
        if (feld == null)
        {
            return "kein Feld";
        }

        return $"{feld.FeldTyp}-Feld #{feld.Id} bei axial {feld.AxialKoordinaten}";
    }
}
