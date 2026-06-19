enum Rohstoff
{
    Holz,
    Lehm,
    Korn,
    Wolle,
    Erz
}

enum Gebaeude
{
    Keine,
    Siedlung,
    Stadt
}

enum Hafen
{
    Keiner,
    Allgemein,
    Spezial
}

class BoardNode
{
    public Rohstoff Rohstoff { get; set; }
    public int Zahlenchip { get; set; }
    public Spieler? Besitzer { get; set; }
    public Gebaeude Gebaeude { get; set; }
    public Hafen Hafen { get; set; }
}

class Spieler
{
    public required string Name { get; set; }
    public int Siegpunkte { get; set; }
    public int OffeneStrassen { get; set; }
    public int GebauteSiedlungen { get; set; }
    public int GebauteStaedte { get; set; }
    public Hafen BesterHafen { get; set; }
    public Dictionary<Rohstoff, int> Rohstoffe { get; } = new();
}

class StrengthResult
{
    public double Siegpunkte { get; set; }
    public double Produktion { get; set; }
    public double Baupotenzial { get; set; }
    public double Expansion { get; set; }
    public double Hafenpotenzial { get; set; }
    public double Tauschwert { get; set; }
    public double Gesamtstaerke { get; set; }
}
