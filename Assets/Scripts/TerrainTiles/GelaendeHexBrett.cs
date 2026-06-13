using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Erzeugt ein vollstaendiges Brett aus GelaendeHexFeld-Objekten.
///
/// Standardmaessig entsteht ein mathematisch regelmaessiges axiales Hex-Raster.
/// Das alte "Hexxagon"-Objekt kann optional noch als Quelle fuer Mittelpunkte
/// dienen. Fuer das eigentliche Brett sollte aber das axiale Raster unten
/// verwendet werden.
/// </summary>
public sealed class GelaendeHexBrett : MonoBehaviour
{
    /// <summary>
    /// Ein Knoten ist eine logische Hex-Ecke. Dort koennen spaeter Siedlungen
    /// oder Staedte sitzen.
    /// </summary>
    [System.Serializable]
    public sealed class HexEckenKnoten
    {
        public int Id;
        public Vector3 WeltPosition;
        public Vector2Int QuantisiertePosition;
        public List<Vector2Int> AngrenzendeFelder = new();
    }

    /// <summary>
    /// Eine Kante verbindet zwei Hex-Ecken. Dort koennen spaeter Strassen
    /// platziert werden.
    /// </summary>
    [System.Serializable]
    public sealed class HexStrassenKante
    {
        public int Id;
        public int EckeAId;
        public int EckeBId;
        public Vector3 WeltStart;
        public Vector3 WeltEnde;
        public Vector3 WeltMittelpunkt;
        public List<Vector2Int> AngrenzendeFelder = new();
    }

    /// <summary>
    /// Dauerhafte Beschreibung eines Feldes. Diese Daten bleiben stabil, auch
    /// wenn das sichtbare Mesh neu erzeugt wird.
    /// </summary>
    [System.Serializable]
    public sealed class HexFeldDaten
    {
        public int Id;
        public Vector2Int AxialKoordinaten;
        public Vector3 WeltMitte;
        public GelaendeHexFeldTyp FeldTyp;
        public GelaendeHexFeld LaufzeitFeld;
    }

    /// <summary>
    /// UnityEvent-Wrapper, damit Feld-Events im Inspector verbunden werden koennen.
    /// </summary>
    [System.Serializable]
    public sealed class HexFeldDatenEvent : UnityEvent<HexFeldDaten>
    {
    }

    /// <summary>
    /// UnityEvent fuer Auswahlwechsel: erster Parameter = vorheriges Feld,
    /// zweiter Parameter = aktuelles Feld.
    /// </summary>
    [System.Serializable]
    public sealed class HexFeldAuswahlGeaendertEvent : UnityEvent<HexFeldDaten, HexFeldDaten>
    {
    }

    /// <summary>
    /// Ordnet einem GelaendeHexFeldTyp eine sichtbare Overlay-Farbe zu.
    /// </summary>
    [System.Serializable]
    private sealed class FeldTypDarstellung
    {
        public GelaendeHexFeldTyp feldTyp;
        public Color farbe;

        [Tooltip(
            "Optionale Referenz auf das alte Gameplay-Material. " +
            "Das projizierte Feld nutzt trotzdem den transparenten Overlay-Shader.")]
        public Material referenzMaterial = null;

        public FeldTypDarstellung(
            GelaendeHexFeldTyp feldTyp,
            Color farbe)
        {
            this.feldTyp = feldTyp;
            this.farbe = farbe;
        }
    }

    [Header("Szenen-Referenzen")]
    [SerializeField] private Terrain gelaende;
    [SerializeField] private Transform altesSpriteBrett;
    [SerializeField] private Camera eingabeKamera;
    [SerializeField] private Material feldMaterial = null;

    [Header("Erzeugung")]
    // Wenn kein altes Sprite-Brett verwendet wird, erzeugt der Generator ein
    // eigenes axiales Hex-Raster mit diesem Radius.
    [SerializeField] private bool mittelpunkteAusAltemBrettLesen = false;
    [SerializeField] private bool altesSpriteBrettVerstecken = true;
    [SerializeField] private int ersatzBrettRadius = 2;

    [Tooltip("Abstand vom Mittelpunkt eines Hex-Feldes zu einer seiner Ecken.")]
    [SerializeField] private float feldRadius = 34.5f;

    [Tooltip(
        "Wie weit das sichtbare Feld-Mesh vom logischen Hexagon nach innen gerueckt wird. " +
        "Das logische Hexagon definiert weiterhin Strassen und Siedlungsknoten.")]
    [SerializeField] private float feldAbstand = 5f;

    [SerializeField] private int meshUnterteilungen = 5;
    [SerializeField] private float hoehenVersatz = 0.08f;

    [Tooltip(
        "Strassen und Siedlungsknoten nutzen das logische Hexagon ohne Padding. " +
        "Dieser Versatz haelt ihre Debug-Positionen ueber dem Gelaende.")]
    [SerializeField] private float graphHoehenVersatz = 0.16f;

    [Tooltip("Dreht das gesamte Brett um die Y-Achse.")]
    [SerializeField] private float brettRotationGrad = 73.885f;

    [Tooltip("Fuer spitze Hexagone 30 verwenden. Fuer flache Hexagone 0 verwenden.")]
    [SerializeField] private float feldEckenRotationGrad = 30f;

    [Tooltip("Legt fest, wie den erzeugten Feld-Mittelpunkten Feldtypen zugewiesen werden.")]
    [SerializeField] private GelaendeHexFeldVergabeModus feldTypVergabeModus =
        GelaendeHexFeldVergabeModus.GameplaySzenenMuster;

    [Tooltip("Sichtbare Farben pro Rohstofftyp. Diese Farben werden pro Feld an den transparenten Overlay-Shader uebergeben.")]
    [SerializeField] private FeldTypDarstellung[] feldTypDarstellungen =
    {
        new(GelaendeHexFeldTyp.Wald, new Color(0.022f, 0.297f, 0.000f, 1f)),
        new(GelaendeHexFeldTyp.Weide, new Color(0.090f, 0.443f, 0.069f, 1f)),
        new(GelaendeHexFeldTyp.Acker, new Color(0.651f, 0.585f, 0.000f, 1f)),
        new(GelaendeHexFeldTyp.Huegel, new Color(0.608f, 0.397f, 0.336f, 1f)),
        new(GelaendeHexFeldTyp.Gebirge, new Color(0.220f, 0.220f, 0.220f, 1f)),
        new(GelaendeHexFeldTyp.Wueste, new Color(0.679f, 0.582f, 0.484f, 1f))
    };

    [SerializeField] private string generierterStammName = "Generierte Gelaende-Hex-Felder";

    [Header("Maus-Hover")]
    [SerializeField] private LayerMask raycastMaske = ~0;
    [SerializeField] private Color normaleFeldFarbe = new(0.18f, 0.78f, 0.95f, 1f);

    [Header("Auswahl")]
    [SerializeField] private bool feldMitLinksklickAuswaehlen = true;
    [SerializeField] private bool auswahlBeiLeerklickLoeschen = true;
    [SerializeField] private bool ausgewaehltesFeldBeiZweitemKlickAbwaehlen = true;
    [SerializeField] private Color auswahlFarbe = new(1f, 0.92f, 0.18f, 1f);

    [Header("Feld-Events")]
    // Diese UnityEvents sind fuer den Inspector. Darunter gibt es zusaetzlich
    // normale C#-Events, die Scripts sauber abonnieren und abmelden koennen.
    [SerializeField] private HexFeldDatenEvent beiFeldErzeugt = new();
    [SerializeField] private HexFeldDatenEvent beiFeldBetreten = new();
    [SerializeField] private HexFeldDatenEvent beiFeldVerlassen = new();
    [SerializeField] private HexFeldDatenEvent beiFeldAusgewaehlt = new();
    [SerializeField] private HexFeldDatenEvent beiFeldAbgewaehlt = new();
    [SerializeField] private HexFeldAuswahlGeaendertEvent beiFeldauswahlGeaendert = new();

    [Header("Catan-Graph-Debug")]
    // Die Gizmos zeigen den logischen Graphen, nicht die sichtbaren gepaddeten
    // Mesh-Raender. So sieht man, wo Siedlungen und Strassen spaeter liegen.
    [SerializeField] private bool graphGizmosZeichnen = true;
    [SerializeField] private float graphZusammenfuehrToleranz = 0.05f;
    [SerializeField] private float graphKnotenGizmoRadius = 1.2f;
    [SerializeField] private Color graphKnotenGizmoFarbe = new(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private Color graphStrassenGizmoFarbe = new(0.15f, 0.95f, 0.35f, 1f);

    private readonly List<GelaendeHexFeld> generierteFelder = new();
    private readonly List<HexFeldDaten> felder = new();
    private readonly List<HexEckenKnoten> eckenKnoten = new();
    private readonly List<HexStrassenKante> strassenKanten = new();

    // Schnelle Nachschlagetabellen. Ohne sie muessten wir bei jedem Klick oder
    // jeder Graph-Erzeugung lineare Suchlaeufe ueber alle Felder/Knoten machen.
    private readonly Dictionary<Vector2Int, HexFeldDaten> feldNachKoordinaten = new();
    private readonly Dictionary<Vector2Int, int> eckenKnotenNachPosition = new();
    private readonly Dictionary<StrassenKantenSchluessel, int> strassenKanteNachEcken = new();

    private GelaendeHexFeld feldUnterMaus;
    private GelaendeHexFeld ausgewaehltesFeld;
    private HexFeldDaten ausgewaehlteFeldDaten;
    private bool zeigerVonFremdemColliderBlockiert;
    private Transform generierterStamm;

    public IReadOnlyList<HexFeldDaten> Felder => felder;
    public IReadOnlyList<HexEckenKnoten> EckenKnoten => eckenKnoten;
    public IReadOnlyList<HexStrassenKante> StrassenKanten => strassenKanten;
    public HexFeldDaten FeldUnterMaus => LeseFelddatenOderNull(feldUnterMaus);
    public GelaendeHexFeld FeldUnterMausKomponente => feldUnterMaus;
    public HexFeldDaten AusgewaehltesFeld => ausgewaehlteFeldDaten;
    public GelaendeHexFeld AusgewaehlteFeldKomponente => ausgewaehltesFeld;
    public HexFeldDatenEvent BeiFeldErzeugt => beiFeldErzeugt;
    public HexFeldDatenEvent BeiFeldBetreten => beiFeldBetreten;
    public HexFeldDatenEvent BeiFeldVerlassen => beiFeldVerlassen;
    public HexFeldDatenEvent BeiFeldAusgewaehlt => beiFeldAusgewaehlt;
    public HexFeldDatenEvent BeiFeldAbgewaehlt => beiFeldAbgewaehlt;
    public HexFeldAuswahlGeaendertEvent BeiFeldauswahlGeaendert => beiFeldauswahlGeaendert;

    // Diese C#-Events sind der bevorzugte Weg fuer Gameplay-Scripts. Sie sind
    // typisiert, testbarer als Inspector-Verbindungen und lassen sich in
    // OnEnable/OnDisable kontrolliert abonnieren.
    public event Action<HexFeldDaten> FeldErzeugt;
    public event Action<HexFeldDaten> FeldBetreten;
    public event Action<HexFeldDaten> FeldVerlassen;
    public event Action<HexFeldDaten> FeldAusgewaehlt;
    public event Action<HexFeldDaten> FeldAbgewaehlt;
    public event Action<HexFeldDaten, HexFeldDaten> FeldauswahlGeaendert;

    private void Start()
    {
        FehlendeReferenzenAufloesen();
        GeneriereBrett();
    }

    private void Update()
    {
        // Hover und Klickauswahl laufen pro Frame, weil sie vom aktuellen
        // Mauszeiger abhaengen. Die teure Mesh-Erzeugung passiert dagegen nur
        // beim Generieren des Bretts.
        AktualisiereFeldUnterMaus();
        AktualisiereFeldauswahlEingabe();
    }

    /// <summary>
    /// Erlaubt es, das Brett ueber das Komponenten-Menue im Inspector neu zu
    /// bauen, ohne dafuer in den Play Mode wechseln zu muessen.
    /// </summary>
    [ContextMenu("Projiziertes Brett neu bauen")]
    public void GeneriereBrett()
    {
        FehlendeReferenzenAufloesen();
        LoescheGenerierteFelder();

        // Die Reihenfolge ist wichtig:
        // 1. Mittelpunkte bestimmen
        // 2. reine Felddaten aufbauen
        // 3. logischen Catan-Graphen berechnen
        // 4. sichtbare Mesh-Objekte erzeugen
        List<FeldMittelpunkt> mittelpunkte = SammleFeldMittelpunkte();
        BaueFeldDaten(mittelpunkte);
        BaueCatanGraph(mittelpunkte);
        generierterStamm = ErzeugeGeneriertenStamm();

        for (int i = 0; i < felder.Count; i++)
        {
            ErzeugeFeld(felder[i]);
        }

        if (altesSpriteBrett != null)
        {
            altesSpriteBrett.gameObject.SetActive(!altesSpriteBrettVerstecken);
        }
    }

    private void AktualisiereFeldUnterMaus()
    {
        if (eingabeKamera == null || Mouse.current == null)
        {
            zeigerVonFremdemColliderBlockiert = false;
            SetzeFeldUnterMaus(null);
            return;
        }

        zeigerVonFremdemColliderBlockiert = false;
        Vector2 bildschirmPosition = Mouse.current.position.ReadValue();
        Ray zeigerStrahl = eingabeKamera.ScreenPointToRay(bildschirmPosition);

        // RaycastAll ist hier absichtlich besser als ein einzelner Raycast:
        // Wir koennen alle Treffer nach Entfernung sortieren und dann
        // entscheiden, ob ein Gebaeude den Blick auf das Feld blockiert.
        RaycastHit[] trefferListe = Physics.RaycastAll(
            zeigerStrahl,
            eingabeKamera.farClipPlane,
            raycastMaske);

        System.Array.Sort(
            trefferListe,
            (links, rechts) => links.distance.CompareTo(rechts.distance));

        GelaendeHexFeld naechstesFeldUnterMaus = null;

        foreach (RaycastHit treffer in trefferListe)
        {
            GelaendeHexFeld feld =
                treffer.collider.GetComponentInParent<GelaendeHexFeld>();

            if (feld != null)
            {
                naechstesFeldUnterMaus = feld;
                break;
            }

            // Terrain darf minimal vor oder hinter dem Feld liegen und blockiert
            // deshalb den Hover nicht. Jeder andere Collider, zum Beispiel ein
            // Gebaeude, blockiert ihn: Der Spieler zeigt dann auf dieses Objekt.
            if (treffer.collider is TerrainCollider)
            {
                continue;
            }

            zeigerVonFremdemColliderBlockiert = true;
            break;
        }

        SetzeFeldUnterMaus(naechstesFeldUnterMaus);
    }

    private void AktualisiereFeldauswahlEingabe()
    {
        if (!feldMitLinksklickAuswaehlen ||
            Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (feldUnterMaus != null)
        {
            WaehleOderLoescheFeldUnterMausAus();
            return;
        }

        // Ein Klick auf ein Gebaeude oder einen anderen Collider soll nicht
        // heimlich das Feld dahinter auswaehlen. Die aktuelle Auswahl bleibt
        // dabei ebenfalls erhalten.
        if (auswahlBeiLeerklickLoeschen && !zeigerVonFremdemColliderBlockiert)
        {
            AuswahlLeeren();
        }
    }

    private void SetzeFeldUnterMaus(GelaendeHexFeld naechstesFeldUnterMaus)
    {
        // Der Wechsel wird nur verarbeitet, wenn sich das Feld wirklich
        // geaendert hat. Dadurch feuern Betreten/Verlassen-Events nicht
        // dauerhaft, waehrend die Maus auf demselben Feld steht.
        if (feldUnterMaus == naechstesFeldUnterMaus)
        {
            return;
        }

        if (feldUnterMaus != null)
        {
            feldUnterMaus.SetzeHover(false);
            MeldeFeldVerlassen(feldUnterMaus);
        }

        feldUnterMaus = naechstesFeldUnterMaus;

        if (feldUnterMaus != null)
        {
            feldUnterMaus.SetzeHover(true);
            MeldeFeldBetreten(feldUnterMaus);
        }
    }

    private void WaehleOderLoescheFeldUnterMausAus()
    {
        // Zweiter Klick auf dasselbe Feld kann optional als "abwaehlen" dienen.
        // Das fuehlt sich fuer Brettspiel-Editoren meist natuerlicher an.
        if (feldUnterMaus == ausgewaehltesFeld &&
            ausgewaehltesFeldBeiZweitemKlickAbwaehlen)
        {
            AuswahlLeeren();
            return;
        }

        VersucheFeldAuszuwaehlen(feldUnterMaus.AxialKoordinaten);
    }

    private void ErzeugeFeld(HexFeldDaten felddaten)
    {
        // Das sichtbare GameObject ist nur die Darstellung. Die eigentlichen
        // Spielinformationen bleiben in HexFeldDaten und koennen auch ohne Mesh
        // ausgewertet werden.
        GameObject feldObjekt = new(
            $"{felddaten.FeldTyp} Feld {felddaten.Id:00} ({felddaten.AxialKoordinaten.x}, {felddaten.AxialKoordinaten.y})");

        feldObjekt.layer = gameObject.layer;
        feldObjekt.transform.SetParent(generierterStamm, false);

        Vector3 projiziertePosition = ProjiziereAufGelaende(felddaten.WeltMitte);

        // Wichtige Korrektur: Die Mittelpunkt-Positionen werden in
        // AxialZuWeltVersatz() bereits um brettRotationGrad gedreht. Das
        // Feld-Mesh selbst braucht dieselbe Brett-Rotation, sonst bilden die
        // Mittelpunkte zwar ein gedrehtes Hex-Raster, die Meshes passen aber
        // optisch nicht zueinander.
        Quaternion feldRotation = Quaternion.Euler(0f, brettRotationGrad, 0f);

        feldObjekt.transform.SetPositionAndRotation(projiziertePosition, feldRotation);

        GelaendeHexFeld feld = feldObjekt.AddComponent<GelaendeHexFeld>();
        Color feldFarbe = LeseFeldTypFarbe(felddaten.FeldTyp);

        feld.Konfigurieren(
            felddaten.Id,
            felddaten.AxialKoordinaten,
            felddaten.FeldTyp,
            gelaende,
            feldMaterial,
            LeseSichtbarenFeldRadius(),
            meshUnterteilungen,
            hoehenVersatz,
            feldEckenRotationGrad,
            feldFarbe,
            LeseHoverFarbe(feldFarbe),
            LeseKantenFarbe(feldFarbe),
            auswahlFarbe);

        felddaten.LaufzeitFeld = feld;
        generierteFelder.Add(feld);
        MeldeFeldErzeugt(felddaten);
    }

    public bool VersucheFeldAuszuwaehlen(Vector2Int axialKoordinaten)
    {
        if (!VersucheFeldZuLesen(axialKoordinaten, out HexFeldDaten felddaten))
        {
            return false;
        }

        WaehleFeldAus(felddaten);
        return true;
    }

    public void WaehleFeldAus(HexFeldDaten felddaten)
    {
        if (felddaten == null)
        {
            AuswahlLeeren();
            return;
        }

        if (felddaten == ausgewaehlteFeldDaten)
        {
            return;
        }

        HexFeldDaten vorherigeFelddaten = ausgewaehlteFeldDaten;
        AuswahlLeerenOhneGeaendertEvent();

        // Erst nach dem Abwaehlen des alten Feldes wird das neue Feld gesetzt.
        // Dadurch bleibt die Event-Reihenfolge eindeutig:
        // FeldAbgewaehlt -> FeldAusgewaehlt -> FeldauswahlGeaendert.
        ausgewaehlteFeldDaten = felddaten;
        ausgewaehltesFeld = felddaten != null ? felddaten.LaufzeitFeld : null;

        if (ausgewaehltesFeld != null)
        {
            ausgewaehltesFeld.SetzeAusgewaehlt(true);
        }

        MeldeFeldAusgewaehlt(felddaten);
        MeldeFeldauswahlGeaendert(vorherigeFelddaten, ausgewaehlteFeldDaten);
    }

    public void AuswahlLeeren()
    {
        if (ausgewaehlteFeldDaten == null && ausgewaehltesFeld == null)
        {
            return;
        }

        HexFeldDaten vorherigeFelddaten = ausgewaehlteFeldDaten;
        AuswahlLeerenOhneGeaendertEvent();
        MeldeFeldauswahlGeaendert(vorherigeFelddaten, null);
    }

    private void AuswahlLeerenOhneGeaendertEvent()
    {
        // Diese Hilfsmethode entfernt nur den lokalen Auswahlzustand. Das
        // uebergeordnete Auswahl-geaendert-Event wird vom Aufrufer gesendet,
        // damit es bei Feldwechseln genau einmal feuert.
        HexFeldDaten vorherigeFelddaten = ausgewaehlteFeldDaten;

        if (ausgewaehltesFeld != null)
        {
            ausgewaehltesFeld.SetzeAusgewaehlt(false);
        }

        ausgewaehltesFeld = null;
        ausgewaehlteFeldDaten = null;

        if (vorherigeFelddaten != null)
        {
            MeldeFeldAbgewaehlt(vorherigeFelddaten);
        }
    }

    public bool VersucheAusgewaehltesFeldZuLesen(out HexFeldDaten felddaten)
    {
        felddaten = ausgewaehlteFeldDaten;
        return felddaten != null;
    }

    public bool VersucheFeldZuLesen(
        Vector2Int axialKoordinaten,
        out HexFeldDaten felddaten)
    {
        return feldNachKoordinaten.TryGetValue(axialKoordinaten, out felddaten);
    }

    public bool VersucheFeldZuLesen(
        GelaendeHexFeld feld,
        out HexFeldDaten felddaten)
    {
        if (feld == null)
        {
            felddaten = null;
            return false;
        }

        return VersucheFeldZuLesen(feld.AxialKoordinaten, out felddaten);
    }

    public bool VersucheNaechstenEckenKnotenZuFinden(
        Vector3 weltPosition,
        float maximaleDistanz,
        out HexEckenKnoten naechsterKnoten)
    {
        // Quadratdistanzen vermeiden eine Quadratwurzel pro Knoten. Fuer
        // Vergleiche reicht die quadrierte Distanz voellig aus.
        naechsterKnoten = null;
        float besteQuadratDistanz = maximaleDistanz * maximaleDistanz;

        foreach (HexEckenKnoten knoten in eckenKnoten)
        {
            float quadratDistanz = (knoten.WeltPosition - weltPosition).sqrMagnitude;
            if (quadratDistanz <= besteQuadratDistanz)
            {
                besteQuadratDistanz = quadratDistanz;
                naechsterKnoten = knoten;
            }
        }

        return naechsterKnoten != null;
    }

    public bool VersucheNaechsteStrassenKanteZuFinden(
        Vector3 weltPosition,
        float maximaleDistanz,
        out HexStrassenKante naechsteKante)
    {
        naechsteKante = null;
        float besteQuadratDistanz = maximaleDistanz * maximaleDistanz;

        foreach (HexStrassenKante kante in strassenKanten)
        {
            Vector3 naechsterPunktAufStrasse = NaechsterPunktAufLinienSegment(
                kante.WeltStart,
                kante.WeltEnde,
                weltPosition);

            float quadratDistanz = (naechsterPunktAufStrasse - weltPosition).sqrMagnitude;
            if (quadratDistanz <= besteQuadratDistanz)
            {
                besteQuadratDistanz = quadratDistanz;
                naechsteKante = kante;
            }
        }

        return naechsteKante != null;
    }

    private List<FeldMittelpunkt> SammleFeldMittelpunkte()
    {
        // Der Generator kann zwei Quellen nutzen:
        // - altes Sprite-Brett als Uebergang von der bestehenden Szene
        // - eigenes axiales Raster fuer das neue prozedurale System
        if (mittelpunkteAusAltemBrettLesen &&
            altesSpriteBrett != null &&
            altesSpriteBrett.childCount > 0)
        {
            return SammleAlteFeldMittelpunkte();
        }

        return ErzeugeErsatzHexRaster();
    }

    private List<FeldMittelpunkt> SammleAlteFeldMittelpunkte()
    {
        List<FeldMittelpunkt> mittelpunkte = new();
        int index = 0;

        foreach (Transform kind in altesSpriteBrett)
        {
            // Das alte Brett platziert die Feld-Mittelpunkte bereits in der
            // Welt. Wir uebernehmen X/Z und tasten Y neu vom Terrain ab.
            mittelpunkte.Add(new FeldMittelpunkt(
                kind.position,
                new Vector2Int(index, 0),
                VersucheAltenFeldTypZuLesen(kind, out GelaendeHexFeldTyp feldTyp),
                feldTyp));

            index++;
        }

        return mittelpunkte;
    }

    private List<FeldMittelpunkt> ErzeugeErsatzHexRaster()
    {
        List<FeldMittelpunkt> mittelpunkte = new();

        int radiusInFeldern = Mathf.Max(0, ersatzBrettRadius);

        for (int q = -radiusInFeldern; q <= radiusInFeldern; q++)
        {
            int minR = Mathf.Max(-radiusInFeldern, -q - radiusInFeldern);
            int maxR = Mathf.Min(radiusInFeldern, -q + radiusInFeldern);

            for (int r = minR; r <= maxR; r++)
            {
                Vector3 versatz = AxialZuWeltVersatz(q, r);
                Vector3 weltPosition = transform.position + versatz;

                mittelpunkte.Add(new FeldMittelpunkt(
                    weltPosition,
                    new Vector2Int(q, r)));
            }
        }

        return mittelpunkte;
    }

    private Vector3 AxialZuWeltVersatz(int q, int r)
    {
        // Axiales Hex-Layout mit Spitze oben.
        //
        // Korrekte Formel von axialen Koordinaten zu Weltkoordinaten:
        //
        // x = radius * sqrt(3) * (q + r / 2)
        // z = radius * 3/2 * r
        //
        // Abstand zum naechsten Feld-Mittelpunkt:
        //
        // sqrt(3) * feldRadius
        //
        // Dabei bedeutet feldRadius:
        //
        // Mittelpunkt -> Ecke
        //
        // Nicht:
        //
        // Mittelpunkt -> Kante
        // Feld-Durchmesser
        // Sprite-Breite
        // Sprite-Hoehe

        float x = feldRadius * Mathf.Sqrt(3f) * (q + r * 0.5f);
        float z = feldRadius * 1.5f * r;

        Vector3 ungedrehterVersatz = new(x, 0f, z);

        Quaternion brettRotation = Quaternion.Euler(
            0f,
            brettRotationGrad,
            0f);

        return brettRotation * ungedrehterVersatz;
    }

    private void BaueFeldDaten(List<FeldMittelpunkt> mittelpunkte)
    {
        // Ab hier arbeitet das System mit eigenen Daten, nicht mehr direkt mit
        // Transform-Hierarchien. Das macht spaetere Spielregeln unabhaengiger
        // von der sichtbaren Darstellung.
        felder.Clear();
        feldNachKoordinaten.Clear();

        for (int i = 0; i < mittelpunkte.Count; i++)
        {
            FeldMittelpunkt mittelpunkt = mittelpunkte[i];
            GelaendeHexFeldTyp feldTyp = BestimmeFeldTyp(mittelpunkt, i);

            HexFeldDaten felddaten = new()
            {
                Id = i,
                AxialKoordinaten = mittelpunkt.AxialKoordinaten,
                WeltMitte = ProjiziereAufGelaende(mittelpunkt.WeltPosition),
                FeldTyp = feldTyp
            };

            felder.Add(felddaten);
            feldNachKoordinaten[felddaten.AxialKoordinaten] = felddaten;
        }
    }

    private GelaendeHexFeldTyp BestimmeFeldTyp(
        FeldMittelpunkt mittelpunkt,
        int index)
    {
        // Prioritaet:
        // 1. expliziter Typ aus einer alten Vorlage
        // 2. festes Gameplay-Szenenmuster
        // 3. generische Catan-Zaehlliste als Ersatz
        if (mittelpunkt.HatExplizitenTyp)
        {
            return mittelpunkt.FeldTyp;
        }

        if (feldTypVergabeModus == GelaendeHexFeldVergabeModus.GameplaySzenenMuster &&
            VersucheGameplaySzenenFeldTypZuLesen(mittelpunkt.AxialKoordinaten, out GelaendeHexFeldTyp szenenTyp))
        {
            return szenenTyp;
        }

        return LeseCatanZaehllistenFeldTyp(index);
    }

    private static bool VersucheGameplaySzenenFeldTypZuLesen(
        Vector2Int axialKoordinaten,
        out GelaendeHexFeldTyp feldTyp)
    {
        // Diese Tabelle ist das alte Gameplay.unity-Brett, umgerechnet von
        // lokalen Positionen in axiale Koordinaten. Die Anzahl entspricht der
        // klassischen Catan-Verteilung: 4 Wald, 4 Weide, 4 Acker, 3 Huegel,
        // 3 Gebirge, 1 Wueste.
        switch (axialKoordinaten.x, axialKoordinaten.y)
        {
            case (0, -2):
            case (1, 0):
            case (-2, 2):
                feldTyp = GelaendeHexFeldTyp.Gebirge;
                return true;

            case (2, -2):
            case (-1, 0):
            case (1, 1):
                feldTyp = GelaendeHexFeldTyp.Huegel;
                return true;

            case (-1, -1):
            case (1, -1):
            case (0, 2):
            case (-1, 1):
                feldTyp = GelaendeHexFeldTyp.Wald;
                return true;

            case (1, -2):
            case (0, 1):
            case (-2, 0):
            case (2, -1):
                feldTyp = GelaendeHexFeldTyp.Acker;
                return true;

            case (2, 0):
            case (0, -1):
            case (-1, 2):
            case (-2, 1):
                feldTyp = GelaendeHexFeldTyp.Weide;
                return true;

            case (0, 0):
                feldTyp = GelaendeHexFeldTyp.Wueste;
                return true;

            default:
                feldTyp = GelaendeHexFeldTyp.Wueste;
                return false;
        }
    }

    private static GelaendeHexFeldTyp LeseCatanZaehllistenFeldTyp(int index)
    {
        // Die Liste enthaelt exakt so viele Eintraege pro Typ, wie ein
        // klassisches 19-Felder-Catan-Brett braucht. Die Reihenfolge kann
        // spaeter gemischt werden, wenn zufaellige Bretter gewuenscht sind.
        GelaendeHexFeldTyp[] standardVerteilung =
        {
            GelaendeHexFeldTyp.Wald,
            GelaendeHexFeldTyp.Wald,
            GelaendeHexFeldTyp.Wald,
            GelaendeHexFeldTyp.Wald,
            GelaendeHexFeldTyp.Weide,
            GelaendeHexFeldTyp.Weide,
            GelaendeHexFeldTyp.Weide,
            GelaendeHexFeldTyp.Weide,
            GelaendeHexFeldTyp.Acker,
            GelaendeHexFeldTyp.Acker,
            GelaendeHexFeldTyp.Acker,
            GelaendeHexFeldTyp.Acker,
            GelaendeHexFeldTyp.Huegel,
            GelaendeHexFeldTyp.Huegel,
            GelaendeHexFeldTyp.Huegel,
            GelaendeHexFeldTyp.Gebirge,
            GelaendeHexFeldTyp.Gebirge,
            GelaendeHexFeldTyp.Gebirge,
            GelaendeHexFeldTyp.Wueste
        };

        return standardVerteilung[Mathf.Abs(index) % standardVerteilung.Length];
    }

    private bool VersucheFeldTypAusAltemMaterialZuLesen(
        Material material,
        out GelaendeHexFeldTyp feldTyp)
    {
        string materialName = material != null ?
            material.name.ToLowerInvariant() :
            string.Empty;

        if (materialName.Contains("wald"))
        {
            feldTyp = GelaendeHexFeldTyp.Wald;
            return true;
        }

        if (materialName.Contains("gras"))
        {
            feldTyp = GelaendeHexFeldTyp.Weide;
            return true;
        }

        if (materialName.Contains("weizen"))
        {
            feldTyp = GelaendeHexFeldTyp.Acker;
            return true;
        }

        if (materialName.Contains("ton"))
        {
            feldTyp = GelaendeHexFeldTyp.Huegel;
            return true;
        }

        if (materialName.Contains("new material") ||
            materialName.Contains("erz") ||
            materialName.Contains("ore"))
        {
            feldTyp = GelaendeHexFeldTyp.Gebirge;
            return true;
        }

        if (materialName.Contains("sand"))
        {
            feldTyp = GelaendeHexFeldTyp.Wueste;
            return true;
        }

        feldTyp = GelaendeHexFeldTyp.Wueste;
        return false;
    }

    private bool VersucheAltenFeldTypZuLesen(
        Transform altesFeld,
        out GelaendeHexFeldTyp feldTyp)
    {
        foreach (Renderer renderer in altesFeld.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (VersucheFeldTypAusAltemMaterialZuLesen(material, out feldTyp))
                {
                    return true;
                }
            }
        }

        feldTyp = GelaendeHexFeldTyp.Wueste;
        return false;
    }

    private Color LeseFeldTypFarbe(GelaendeHexFeldTyp feldTyp)
    {
        foreach (FeldTypDarstellung darstellung in feldTypDarstellungen)
        {
            if (darstellung != null && darstellung.feldTyp == feldTyp)
            {
                return darstellung.farbe;
            }
        }

        // Ersatzwert, falls die Darstellungsliste im Inspector geleert wurde.
        return normaleFeldFarbe;
    }

    private static Color LeseHoverFarbe(Color feldFarbe)
    {
        return Color.Lerp(feldFarbe, Color.white, 0.42f);
    }

    private static Color LeseKantenFarbe(Color feldFarbe)
    {
        return Color.Lerp(feldFarbe, Color.white, 0.25f);
    }

    private void BaueCatanGraph(List<FeldMittelpunkt> mittelpunkte)
    {
        // Der Graph basiert auf dem logischen, ungepaddeten Hexagon. Dadurch
        // liegen Strassen und Siedlungen zwischen den sichtbaren Feldern,
        // obwohl die Feld-Meshes selbst kleiner dargestellt werden.
        eckenKnoten.Clear();
        strassenKanten.Clear();
        eckenKnotenNachPosition.Clear();
        strassenKanteNachEcken.Clear();

        foreach (FeldMittelpunkt mittelpunkt in mittelpunkte)
        {
            int[] eckenIds = new int[6];

            // Zuerst werden alle sechs Ecken des Feldes gelesen oder erzeugt.
            // Benachbarte Felder teilen sich dieselben Knoten.
            for (int ecke = 0; ecke < eckenIds.Length; ecke++)
            {
                Vector3 eckenPosition = BerechneLogischeEckenPosition(
                    mittelpunkt.WeltPosition,
                    ecke);

                eckenIds[ecke] = LeseOderErzeugeEckenKnoten(
                    eckenPosition,
                    mittelpunkt.AxialKoordinaten);
            }

            // Danach werden zwischen aufeinanderfolgenden Ecken die sechs
            // Strassenkanten dieses Feldes erzeugt oder wiederverwendet.
            for (int ecke = 0; ecke < eckenIds.Length; ecke++)
            {
                int naechsteEcke = (ecke + 1) % eckenIds.Length;
                LeseOderErzeugeStrassenKante(
                    eckenIds[ecke],
                    eckenIds[naechsteEcke],
                    mittelpunkt.AxialKoordinaten);
            }
        }
    }

    private int LeseOderErzeugeEckenKnoten(
        Vector3 weltPosition,
        Vector2Int angrenzendesFeld)
    {
        Vector2Int positionsSchluessel = QuantisiereGraphPosition(weltPosition);

        // Wenn eine Ecke an derselben quantisierten Position bereits existiert,
        // nutzen wir sie wieder. So entstehen echte gemeinsame Graph-Knoten.
        if (!eckenKnotenNachPosition.TryGetValue(positionsSchluessel, out int knotenId))
        {
            knotenId = eckenKnoten.Count;

            eckenKnotenNachPosition.Add(positionsSchluessel, knotenId);
            eckenKnoten.Add(new HexEckenKnoten
            {
                Id = knotenId,
                WeltPosition = weltPosition,
                QuantisiertePosition = positionsSchluessel
            });
        }

        FuegeAngrenzendesFeldEinmalHinzu(eckenKnoten[knotenId].AngrenzendeFelder, angrenzendesFeld);
        return knotenId;
    }

    private int LeseOderErzeugeStrassenKante(
        int eckeAId,
        int eckeBId,
        Vector2Int angrenzendesFeld)
    {
        StrassenKantenSchluessel kantenSchluessel = new(eckeAId, eckeBId);

        // Der Schluessel sortiert Ecke A/B intern. Dadurch ist die Kante
        // A->B dieselbe wie B->A und wird nicht doppelt angelegt.
        if (!strassenKanteNachEcken.TryGetValue(kantenSchluessel, out int kantenId))
        {
            kantenId = strassenKanten.Count;

            HexEckenKnoten eckeA = eckenKnoten[kantenSchluessel.EckeAId];
            HexEckenKnoten eckeB = eckenKnoten[kantenSchluessel.EckeBId];
            Vector3 mittelpunkt = (eckeA.WeltPosition + eckeB.WeltPosition) * 0.5f;
            mittelpunkt.y = LeseGelaendeY(mittelpunkt) + graphHoehenVersatz;

            strassenKanteNachEcken.Add(kantenSchluessel, kantenId);
            strassenKanten.Add(new HexStrassenKante
            {
                Id = kantenId,
                EckeAId = kantenSchluessel.EckeAId,
                EckeBId = kantenSchluessel.EckeBId,
                WeltStart = eckeA.WeltPosition,
                WeltEnde = eckeB.WeltPosition,
                WeltMittelpunkt = mittelpunkt
            });
        }

        FuegeAngrenzendesFeldEinmalHinzu(strassenKanten[kantenId].AngrenzendeFelder, angrenzendesFeld);
        return kantenId;
    }

    private Vector3 BerechneLogischeEckenPosition(
        Vector3 feldMitte,
        int eckenIndex)
    {
        Vector2 lokaleEcke = LeseHexEcke2D(feldRadius, eckenIndex);
        Vector3 lokaleEcke3D = new(lokaleEcke.x, 0f, lokaleEcke.y);

        Quaternion brettRotation = Quaternion.Euler(
            0f,
            brettRotationGrad,
            0f);

        Vector3 weltPosition = feldMitte + brettRotation * lokaleEcke3D;
        weltPosition.y = LeseGelaendeY(weltPosition) + graphHoehenVersatz;

        return weltPosition;
    }

    private Vector2 LeseHexEcke2D(float radius, int eckenIndex)
    {
        float angle =
            (feldEckenRotationGrad + eckenIndex * 60f) *
            Mathf.Deg2Rad;

        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    private Vector3 ProjiziereAufGelaende(Vector3 weltPosition)
    {
        if (gelaende == null)
        {
            return weltPosition;
        }

        weltPosition.y = LeseGelaendeY(weltPosition) + hoehenVersatz;

        return weltPosition;
    }

    private float LeseGelaendeY(Vector3 weltPosition)
    {
        if (gelaende == null)
        {
            return weltPosition.y;
        }

        return gelaende.transform.position.y + gelaende.SampleHeight(weltPosition);
    }

    private float LeseSichtbarenFeldRadius()
    {
        // Das Padding ist nur sichtbar. Der logische Radius bleibt feldRadius,
        // damit der Catan-Graph weiterhin die echten Ecken und Kanten nutzt.
        float begrenztesPadding = Mathf.Clamp(feldAbstand, 0f, feldRadius * 0.9f);
        return Mathf.Max(0.01f, feldRadius - begrenztesPadding);
    }

    private Vector2Int QuantisiereGraphPosition(Vector3 weltPosition)
    {
        // Benachbarte Felder berechnen dieselbe Catan-Ecke ueber leicht
        // verschiedene Rechenwege. Die Quantisierung fuehrt kleine
        // Fliesskomma-Unterschiede zu einem stabilen Graph-Knoten zusammen.
        float toleranz = Mathf.Max(0.001f, graphZusammenfuehrToleranz);

        return new Vector2Int(
            Mathf.RoundToInt(weltPosition.x / toleranz),
            Mathf.RoundToInt(weltPosition.z / toleranz));
    }

    private Transform ErzeugeGeneriertenStamm()
    {
        GameObject stammObjekt = new(generierterStammName);
        stammObjekt.layer = gameObject.layer;

        stammObjekt.transform.SetParent(transform, false);

        return stammObjekt.transform;
    }

    private void LoescheGenerierteFelder()
    {
        // Vor dem Neuaufbau muessen Hover und Auswahl geleert werden, weil die
        // alten Laufzeit-GameObjects gleich zerstoert werden.
        SetzeFeldUnterMaus(null);
        AuswahlLeeren();
        generierteFelder.Clear();
        felder.Clear();
        eckenKnoten.Clear();
        strassenKanten.Clear();
        feldNachKoordinaten.Clear();
        eckenKnotenNachPosition.Clear();
        strassenKanteNachEcken.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform kind = transform.GetChild(i);

            if (kind.name == generierterStammName)
            {
                ZerstoereGeneriertesObjekt(kind.gameObject);
            }
        }
    }

    private HexFeldDaten LeseFelddatenOderNull(GelaendeHexFeld feld)
    {
        return VersucheFeldZuLesen(feld, out HexFeldDaten felddaten) ? felddaten : null;
    }

    private void MeldeFeldErzeugt(HexFeldDaten felddaten)
    {
        // Jedes Ereignis wird doppelt gemeldet:
        // - als C#-Event fuer Code
        // - als UnityEvent fuer Inspector-Verbindungen
        FeldErzeugt?.Invoke(felddaten);
        beiFeldErzeugt.Invoke(felddaten);
    }

    private void MeldeFeldBetreten(GelaendeHexFeld feld)
    {
        if (!VersucheFeldZuLesen(feld, out HexFeldDaten felddaten))
        {
            return;
        }

        FeldBetreten?.Invoke(felddaten);
        beiFeldBetreten.Invoke(felddaten);
    }

    private void MeldeFeldVerlassen(GelaendeHexFeld feld)
    {
        if (!VersucheFeldZuLesen(feld, out HexFeldDaten felddaten))
        {
            return;
        }

        FeldVerlassen?.Invoke(felddaten);
        beiFeldVerlassen.Invoke(felddaten);
    }

    private void MeldeFeldAusgewaehlt(HexFeldDaten felddaten)
    {
        FeldAusgewaehlt?.Invoke(felddaten);
        beiFeldAusgewaehlt.Invoke(felddaten);
    }

    private void MeldeFeldAbgewaehlt(HexFeldDaten felddaten)
    {
        FeldAbgewaehlt?.Invoke(felddaten);
        beiFeldAbgewaehlt.Invoke(felddaten);
    }

    private void MeldeFeldauswahlGeaendert(
        HexFeldDaten vorherigeFelddaten,
        HexFeldDaten aktuelleFelddaten)
    {
        FeldauswahlGeaendert?.Invoke(vorherigeFelddaten, aktuelleFelddaten);
        beiFeldauswahlGeaendert.Invoke(vorherigeFelddaten, aktuelleFelddaten);
    }

    private void FehlendeReferenzenAufloesen()
    {
        if (gelaende == null)
        {
            gelaende = FindFirstObjectByType<Terrain>();
        }

        if (altesSpriteBrett == null)
        {
            GameObject altesObjekt = GameObject.Find("Hexxagon");
            altesSpriteBrett = altesObjekt != null ? altesObjekt.transform : null;
        }

        if (eingabeKamera == null)
        {
            eingabeKamera = Camera.main;
        }
    }

    private static void ZerstoereGeneriertesObjekt(GameObject ziel)
    {
        if (Application.isPlaying)
        {
            Destroy(ziel);
        }
        else
        {
            DestroyImmediate(ziel);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!graphGizmosZeichnen)
        {
            return;
        }

        Gizmos.color = graphStrassenGizmoFarbe;
        foreach (HexStrassenKante kante in strassenKanten)
        {
            Gizmos.DrawLine(kante.WeltStart, kante.WeltEnde);
        }

        Gizmos.color = graphKnotenGizmoFarbe;
        foreach (HexEckenKnoten knoten in eckenKnoten)
        {
            Gizmos.DrawSphere(knoten.WeltPosition, graphKnotenGizmoRadius);
        }
    }

    private static void FuegeAngrenzendesFeldEinmalHinzu(
        List<Vector2Int> angrenzendeFelder,
        Vector2Int feld)
    {
        if (!angrenzendeFelder.Contains(feld))
        {
            angrenzendeFelder.Add(feld);
        }
    }

    private static Vector3 NaechsterPunktAufLinienSegment(
        Vector3 anfang,
        Vector3 ende,
        Vector3 punkt)
    {
        Vector3 anfangZuEnde = ende - anfang;
        float segmentLaengeQuadrat = anfangZuEnde.sqrMagnitude;

        if (segmentLaengeQuadrat <= Mathf.Epsilon)
        {
            return anfang;
        }

        float t = Vector3.Dot(punkt - anfang, anfangZuEnde) / segmentLaengeQuadrat;
        return anfang + anfangZuEnde * Mathf.Clamp01(t);
    }

    private readonly struct FeldMittelpunkt
    {
        public readonly Vector3 WeltPosition;
        public readonly Vector2Int AxialKoordinaten;
        public readonly bool HatExplizitenTyp;
        public readonly GelaendeHexFeldTyp FeldTyp;

        public FeldMittelpunkt(Vector3 weltPosition, Vector2Int axialKoordinaten)
        {
            WeltPosition = weltPosition;
            AxialKoordinaten = axialKoordinaten;
            HatExplizitenTyp = false;
            FeldTyp = GelaendeHexFeldTyp.Wueste;
        }

        public FeldMittelpunkt(
            Vector3 weltPosition,
            Vector2Int axialKoordinaten,
            bool hatExplizitenTyp,
            GelaendeHexFeldTyp feldTyp)
        {
            WeltPosition = weltPosition;
            AxialKoordinaten = axialKoordinaten;
            HatExplizitenTyp = hatExplizitenTyp;
            FeldTyp = feldTyp;
        }
    }

    private readonly struct StrassenKantenSchluessel
    {
        public readonly int EckeAId;
        public readonly int EckeBId;

        public StrassenKantenSchluessel(int ersteEckeId, int zweiteEckeId)
        {
            EckeAId = Mathf.Min(ersteEckeId, zweiteEckeId);
            EckeBId = Mathf.Max(ersteEckeId, zweiteEckeId);
        }

        public override bool Equals(object obj)
        {
            return obj is StrassenKantenSchluessel other &&
                   EckeAId == other.EckeAId &&
                   EckeBId == other.EckeBId;
        }

        public override int GetHashCode()
        {
            return (EckeAId * 397) ^ EckeBId;
        }
    }
}
