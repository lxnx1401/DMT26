using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Baut ein sichtbares Hex-Feld als echtes Mesh.
///
/// Die wichtige Idee: Das Feld ist kein flaches Sprite mehr. Jeder Mesh-Punkt
/// wird gegen die Terrain-Hoehe abgetastet. Dadurch folgt das Feld Huegeln und
/// Taelern, statt ueber dem Terrain zu schweben oder es zu schneiden.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public sealed class GelaendeHexFeld : MonoBehaviour
{
    // Diese Namen muessen exakt zu den Properties im Shader passen. Wir cachen
    // die IDs, damit Unity die String-Namen nicht bei jedem Frame neu suchen muss.
    private static readonly int HoverId = Shader.PropertyToID("_Hover");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int HoverColorId = Shader.PropertyToID("_HoverColor");
    private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
    private static readonly int SelectedId = Shader.PropertyToID("_Selected");
    private static readonly int SelectedColorId = Shader.PropertyToID("_SelectedColor");

    // Die Listen werden beim Neubau wiederverwendet. Das vermeidet unnoetige
    // Speicherallokationen, wenn das Brett im Inspector mehrfach neu gebaut wird.
    private readonly List<Vector3> meshPunkte = new();
    private readonly List<Vector2> uvKoordinaten = new();
    private readonly List<int> dreiecke = new();

    private MeshFilter meshFilterKomponente;
    private MeshRenderer meshRendererKomponente;
    private MeshCollider meshColliderKomponente;
    private MaterialPropertyBlock materialEigenschaften;
    private Mesh generiertesMesh;

    private Terrain zielGelaende;
    private float radius = 1f;
    private int unterteilungen = 4;
    private float hoehenVersatz = 0.05f;
    private float hexRotationGrad = 30f;
    private Color feldFarbe = Color.white;
    private Color hoverFarbe = Color.white;
    private Color kantenFarbe = Color.white;
    private Color auswahlFarbe = Color.white;
    private bool istUnterMaus;
    private bool istAusgewaehlt;

    // Diese Werte sind bewusst nur lesbar. Andere Systeme sollen Felder
    // identifizieren koennen, aber die Datenhoheit bleibt beim Brett-Generator.
    public int FeldIndex { get; private set; }
    public Vector2Int AxialKoordinaten { get; private set; }
    public GelaendeHexFeldTyp FeldTyp { get; private set; }
    public bool IstAusgewaehlt => istAusgewaehlt;

    /// <summary>
    /// Wird vom Brett-Generator direkt nach dem Erzeugen des GameObjects
    /// aufgerufen. So bleibt die Feld-Erzeugung an einer zentralen Stelle.
    /// </summary>
    public void Konfigurieren(
        int feldIndex,
        Vector2Int axialKoordinaten,
        GelaendeHexFeldTyp feldTyp,
        Terrain gelaende,
        Material material,
        float feldRadius,
        int meshUnterteilungen,
        float gelaendeHoehenVersatz,
        float rotationGrad,
        Color farbe,
        Color hoverFarbton,
        Color kantenFarbton,
        Color auswahlFarbton)
    {
        // Konfigurieren sammelt alle Eingabedaten, die dieses Feld zum Bauen
        // und Darstellen braucht. Danach kann das Objekt unabhaengig rendern.
        FeldIndex = feldIndex;
        AxialKoordinaten = axialKoordinaten;
        FeldTyp = feldTyp;
        zielGelaende = gelaende;
        radius = Mathf.Max(0.01f, feldRadius);
        unterteilungen = Mathf.Max(1, meshUnterteilungen);
        hoehenVersatz = gelaendeHoehenVersatz;
        hexRotationGrad = rotationGrad;
        feldFarbe = farbe;
        hoverFarbe = hoverFarbton;
        kantenFarbe = kantenFarbton;
        auswahlFarbe = auswahlFarbton;

        KomponentenMerken();

        if (material != null)
        {
            meshRendererKomponente.sharedMaterial = material;
        }

        MeshNeuBauen();
        DarstellungAnwenden();
    }

    public void SetzeHover(bool aktiv)
    {
        // Frueh abbrechen spart Material-Updates, wenn der Zustand gleich bleibt.
        if (istUnterMaus == aktiv)
        {
            return;
        }

        istUnterMaus = aktiv;
        DarstellungAnwenden();
    }

    public void SetzeAusgewaehlt(bool aktiv)
    {
        // Auswahl ist nur ein Darstellungszustand. Die eigentliche Spiellogik
        // liegt im Brett, das auch die passenden Events ausloest.
        if (istAusgewaehlt == aktiv)
        {
            return;
        }

        istAusgewaehlt = aktiv;
        DarstellungAnwenden();
    }

    public void MeshNeuBauen()
    {
        KomponentenMerken();
        MeshDatenLeeren();

        // Das Hexagon besteht aus sechs dreieckigen Segmenten. Jedes Segment
        // wird weiter unterteilt, damit genug Mesh-Punkte fuer unebenes Terrain
        // vorhanden sind.
        for (int seite = 0; seite < 6; seite++)
        {
            HexSeiteBauen(seite);
        }

        if (generiertesMesh == null)
        {
            // Das Mesh wird pro Feld erzeugt, weil jedes Feld andere Terrain-
            // Hoehen haben kann und deshalb eigene Vertex-Positionen braucht.
            generiertesMesh = new Mesh
            {
                name = $"GelaendeHexFeld_{FeldIndex:00}"
            };
        }
        else
        {
            generiertesMesh.Clear();
        }

        generiertesMesh.SetVertices(meshPunkte);
        generiertesMesh.SetUVs(0, uvKoordinaten);
        generiertesMesh.SetTriangles(dreiecke, 0);
        generiertesMesh.RecalculateBounds();
        generiertesMesh.RecalculateNormals();

        meshFilterKomponente.sharedMesh = generiertesMesh;

        // MeshCollider erkennt direkte Mesh-Aenderungen nicht immer. Durch das
        // kurze Leeren baut Unity den Collider aus den neuen Mesh-Daten neu auf.
        meshColliderKomponente.sharedMesh = null;
        meshColliderKomponente.sharedMesh = generiertesMesh;
    }

    private void HexSeiteBauen(int seite)
    {
        // Ein Hexagon laesst sich gut als sechs Dreiecksfaecher denken:
        // Mittelpunkt -> Ecke A -> Ecke B. Jede Zeile im Faecher hat einen
        // Punkt mehr als die vorherige und bildet dadurch ein kleines Raster.
        Vector2 eckeA = HexEckeLesen(seite);
        Vector2 eckeB = HexEckeLesen(seite + 1);
        List<List<int>> zeilenIndices = new();

        for (int zeile = 0; zeile <= unterteilungen; zeile++)
        {
            float zeilenAnteil = zeile / (float)unterteilungen;
            List<int> aktuelleZeile = new();

            for (int spalte = 0; spalte <= zeile; spalte++)
            {
                float spaltenAnteil = zeile == 0 ? 0f : spalte / (float)zeile;

                Vector2 von = eckeA * zeilenAnteil;
                Vector2 bis = eckeB * zeilenAnteil;
                Vector2 lokalesXZ = Vector2.Lerp(von, bis, spaltenAnteil);

                aktuelleZeile.Add(ProjiziertenVertexHinzufuegen(lokalesXZ));
            }

            zeilenIndices.Add(aktuelleZeile);
        }

        for (int zeile = 0; zeile < unterteilungen; zeile++)
        {
            List<int> aktuelleZeile = zeilenIndices[zeile];
            List<int> naechsteZeile = zeilenIndices[zeile + 1];

            for (int spalte = 0; spalte <= zeile; spalte++)
            {
                // Dreieck von der aktuellen Zeile zur naechsten Zeile.
                DreieckHinzufuegen(aktuelleZeile[spalte], naechsteZeile[spalte + 1], naechsteZeile[spalte]);

                // Zweites Dreieck im Viereck zwischen zwei Zeilen. Die erste
                // Zeile enthaelt nur den Mittelpunkt und braucht es deshalb nicht.
                if (spalte < zeile)
                {
                    DreieckHinzufuegen(aktuelleZeile[spalte], aktuelleZeile[spalte + 1], naechsteZeile[spalte + 1]);
                }
            }
        }
    }

    private int ProjiziertenVertexHinzufuegen(Vector2 lokalesXZ)
    {
        // Zuerst wird die lokale XZ-Position in Weltkoordinaten transformiert.
        // Erst dort kann Terrain.SampleHeight korrekt abgefragt werden.
        Vector3 lokalePosition = new(lokalesXZ.x, 0f, lokalesXZ.y);
        Vector3 weltPosition = transform.TransformPoint(lokalePosition);
        weltPosition.y = LeseGelaendeY(weltPosition) + hoehenVersatz;

        int index = meshPunkte.Count;
        meshPunkte.Add(transform.InverseTransformPoint(weltPosition));

        // Die UV-Koordinaten liegen um 0 herum. Der Shader nutzt das, um die
        // Mitte transparent und den Rand sichtbarer zu machen.
        uvKoordinaten.Add(lokalesXZ / radius);

        return index;
    }

    private float LeseGelaendeY(Vector3 weltPosition)
    {
        if (zielGelaende == null)
        {
            return weltPosition.y;
        }

        return zielGelaende.transform.position.y + zielGelaende.SampleHeight(weltPosition);
    }

    private Vector2 HexEckeLesen(int eckenIndex)
    {
        // Die sechs Ecken liegen jeweils 60 Grad auseinander. Die zusaetzliche
        // Rotation entscheidet, ob das Hexagon mit Spitze oder flacher Kante oben steht.
        float angle = (hexRotationGrad + eckenIndex * 60f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    private void DreieckHinzufuegen(int ersterIndex, int zweiterIndex, int dritterIndex)
    {
        dreiecke.Add(ersterIndex);
        dreiecke.Add(zweiterIndex);
        dreiecke.Add(dritterIndex);
    }

    private void DarstellungAnwenden()
    {
        KomponentenMerken();

        // MaterialPropertyBlock setzt Werte nur fuer diesen Renderer. So koennen
        // alle Felder dasselbe Material teilen und trotzdem eigene Farben,
        // Hover- und Auswahlzustaende haben.
        materialEigenschaften ??= new MaterialPropertyBlock();
        meshRendererKomponente.GetPropertyBlock(materialEigenschaften);
        materialEigenschaften.SetFloat(HoverId, istUnterMaus ? 1f : 0f);
        materialEigenschaften.SetFloat(SelectedId, istAusgewaehlt ? 1f : 0f);
        materialEigenschaften.SetColor(ColorId, feldFarbe);
        materialEigenschaften.SetColor(HoverColorId, hoverFarbe);
        materialEigenschaften.SetColor(EdgeColorId, kantenFarbe);
        materialEigenschaften.SetColor(SelectedColorId, auswahlFarbe);
        meshRendererKomponente.SetPropertyBlock(materialEigenschaften);
    }

    private void KomponentenMerken()
    {
        meshFilterKomponente ??= GetComponent<MeshFilter>();
        meshRendererKomponente ??= GetComponent<MeshRenderer>();
        meshColliderKomponente ??= GetComponent<MeshCollider>();
    }

    private void MeshDatenLeeren()
    {
        meshPunkte.Clear();
        uvKoordinaten.Clear();
        dreiecke.Clear();
    }
}
