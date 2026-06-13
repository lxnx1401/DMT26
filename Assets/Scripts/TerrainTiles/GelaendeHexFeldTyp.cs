/// <summary>
/// Gameplay-Bedeutung eines generierten Hex-Feldes.
///
/// Diese Namen orientieren sich an den klassischen Catan-Rohstoffgruppen.
/// Das Enum verhindert, dass Strassen, Siedlungen, Produktion, UI oder
/// Speicherdaten die Bedeutung eines Feldes aus Material oder Farbe erraten
/// muessen.
/// </summary>
public enum GelaendeHexFeldTyp
{
    /// <summary>Holz-Rohstofffeld.</summary>
    Wald,

    /// <summary>Schaf-/Weide-Rohstofffeld.</summary>
    Weide,

    /// <summary>Getreide-/Acker-Rohstofffeld.</summary>
    Acker,

    /// <summary>Lehm-/Huegel-Rohstofffeld.</summary>
    Huegel,

    /// <summary>Erz-/Gebirge-Rohstofffeld.</summary>
    Gebirge,

    /// <summary>Wueste produziert keinen Rohstoff.</summary>
    Wueste
}

/// <summary>
/// Legt fest, wie generierte Felder ihren GelaendeHexFeldTyp erhalten.
/// </summary>
public enum GelaendeHexFeldVergabeModus
{
    /// <summary>
    /// Baut dasselbe 19-Felder-Muster nach, das in Gameplay.unity serialisiert ist.
    /// </summary>
    GameplaySzenenMuster,

    /// <summary>
    /// Nutzt die Standardverteilung 4/4/4/3/3/1 in Generator-Reihenfolge.
    /// Das ist nuetzlich, wenn das axiale Muster geaendert wird und kein exaktes
    /// Szenenmuster existiert.
    /// </summary>
    CatanZaehlliste
}
