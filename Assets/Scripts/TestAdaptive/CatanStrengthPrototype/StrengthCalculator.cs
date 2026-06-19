class StrengthCalculator
{
    private const double MaxProduktion = 2.0;
    private const double MaxBaupotenzial = 12.0;
    private const double MaxExpansion = 12.0;
    private const double MaxHafenpotenzial = 0.8;
    private const double MaxTauschwert = 30.0;

    private const double GewichtStrasse = 1.0;
    private const double GewichtSiedlung = 2.5;
    private const double GewichtStadt = 4.0;
    private const double GewichtEntwicklungskarte = 1.5;

    private readonly Dictionary<Rohstoff, int> rohstoffWerte = new()
    {
        [Rohstoff.Holz] = 2,
        [Rohstoff.Lehm] = 2,
        [Rohstoff.Korn] = 4,
        [Rohstoff.Wolle] = 2,
        [Rohstoff.Erz] = 4
    };

    public StrengthResult Berechne(Spieler spieler, BoardNode?[,,] board)
    {
        double v = Normalisiere(spieler.Siegpunkte, 10);
        double p = Normalisiere(BerechneProduktion(spieler, board), MaxProduktion);
        double b = Normalisiere(BerechneBaupotenzial(spieler), MaxBaupotenzial);
        double e = Normalisiere(BerechneExpansion(spieler), MaxExpansion);
        double h = Normalisiere(BerechneHafenpotenzial(spieler, board), MaxHafenpotenzial);
        double t = Normalisiere(BerechneTauschwert(spieler), MaxTauschwert);

        return new StrengthResult
        {
            Siegpunkte = v,
            Produktion = p,
            Baupotenzial = b,
            Expansion = e,
            Hafenpotenzial = h,
            Tauschwert = t,
            Gesamtstaerke = 0.35 * v + 0.25 * p + 0.15 * b + 0.10 * e + 0.05 * h + 0.10 * t
        };
    }

    private double BerechneProduktion(Spieler spieler, BoardNode?[,,] board)
    {
        double summe = 0;

        foreach (BoardNode? feld in board)
        {
            if (feld?.Besitzer != spieler || feld.Gebaeude == Gebaeude.Keine || feld.Zahlenchip == 7)
            {
                continue;
            }

            int multiplikator = feld.Gebaeude == Gebaeude.Stadt ? 2 : 1;
            double wahrscheinlichkeit = (6 - Math.Abs(7 - feld.Zahlenchip)) / 36.0;
            summe += multiplikator * wahrscheinlichkeit * rohstoffWerte[feld.Rohstoff];
        }

        return summe;
    }

    private double BerechneBaupotenzial(Spieler spieler)
    {
        int holz = Karten(spieler, Rohstoff.Holz);
        int lehm = Karten(spieler, Rohstoff.Lehm);
        int korn = Karten(spieler, Rohstoff.Korn);
        int wolle = Karten(spieler, Rohstoff.Wolle);
        int erz = Karten(spieler, Rohstoff.Erz);

        double strasse = Math.Min(holz, lehm);
        double siedlung = Minimum(holz, lehm, korn, wolle);
        double stadt = Math.Min(erz / 3.0, korn / 2.0);
        double entwicklungskarte = Minimum(erz, korn, wolle);

        return GewichtStrasse * strasse
            + GewichtSiedlung * siedlung
            + GewichtStadt * stadt
            + GewichtEntwicklungskarte * entwicklungskarte;
    }

    private double BerechneExpansion(Spieler spieler)
    {
        int verfuegbareSiedlungen = Math.Max(0, 5 - spieler.GebauteSiedlungen);
        int verfuegbareStaedte = Math.Max(0, 4 - spieler.GebauteStaedte);
        int moeglicheStadtUpgrades = Math.Min(spieler.GebauteSiedlungen, verfuegbareStaedte);
        const double bauplatzQualitaet = 2.5;

        return spieler.OffeneStrassen * 1.5
            + verfuegbareSiedlungen
            + verfuegbareStaedte * 0.5
            + moeglicheStadtUpgrades * 1.5
            + bauplatzQualitaet;
    }

    private double BerechneHafenpotenzial(Spieler spieler, BoardNode?[,,] board)
    {
        double besteRohstoffProduktion = 0;

        foreach (Rohstoff rohstoff in Enum.GetValues<Rohstoff>())
        {
            double produktion = BerechneProduktionFuerRohstoff(spieler, board, rohstoff);
            besteRohstoffProduktion = Math.Max(besteRohstoffProduktion, produktion);
        }

        return besteRohstoffProduktion / Tauschverhaeltnis(spieler.BesterHafen);
    }

    private double BerechneProduktionFuerRohstoff(Spieler spieler, BoardNode?[,,] board, Rohstoff rohstoff)
    {
        double summe = 0;

        foreach (BoardNode? feld in board)
        {
            if (feld?.Besitzer != spieler || feld.Rohstoff != rohstoff || feld.Gebaeude == Gebaeude.Keine || feld.Zahlenchip == 7)
            {
                continue;
            }

            int multiplikator = feld.Gebaeude == Gebaeude.Stadt ? 2 : 1;
            double wahrscheinlichkeit = (6 - Math.Abs(7 - feld.Zahlenchip)) / 36.0;
            summe += multiplikator * wahrscheinlichkeit * rohstoffWerte[rohstoff];
        }

        return summe;
    }

    private double BerechneTauschwert(Spieler spieler)
    {
        return spieler.Rohstoffe.Sum(karten => karten.Value * rohstoffWerte[karten.Key]);
    }

    private static double Normalisiere(double value, double maxValue)
    {
        return Math.Min(1.0, value / maxValue);
    }

    private static int Karten(Spieler spieler, Rohstoff rohstoff)
    {
        return spieler.Rohstoffe.TryGetValue(rohstoff, out int anzahl) ? anzahl : 0;
    }

    private static double Minimum(params double[] werte)
    {
        return werte.Min();
    }

    private static int Tauschverhaeltnis(Hafen hafen)
    {
        return hafen switch
        {
            Hafen.Spezial => 2,
            Hafen.Allgemein => 3,
            _ => 4
        };
    }
}
