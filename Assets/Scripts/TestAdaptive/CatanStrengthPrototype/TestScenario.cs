static class TestScenario
{
    public static (BoardNode?[,,] Board, List<Spieler> Spieler) ErstelleAusgangszustand()
    {
        Spieler rot = new()
        {
            Name = "Rot",
            Siegpunkte = 3,
            OffeneStrassen = 2,
            GebauteSiedlungen = 3,
            GebauteStaedte = 0,
            BesterHafen = Hafen.Allgemein
        };

        Spieler blau = new()
        {
            Name = "Blau",
            Siegpunkte = 4,
            OffeneStrassen = 1,
            GebauteSiedlungen = 2,
            GebauteStaedte = 1,
            BesterHafen = Hafen.Keiner
        };

        SetzeRohstoffe(rot, 2, 2, 1, 1, 0);
        SetzeRohstoffe(blau, 1, 0, 3, 1, 2);

        BoardNode?[,,] board = ErstelleBoard(rot, blau);
        return (board, [rot, blau]);
    }

    public static (BoardNode?[,,] Board, List<Spieler> Spieler) ErstelleRundeZwei()
    {
        var (board, spieler) = ErstelleAusgangszustand();
        Spieler rot = spieler[0];
        Spieler blau = spieler[1];

        rot.Siegpunkte = 4;
        rot.OffeneStrassen = 3;
        rot.GebauteSiedlungen = 2;
        rot.GebauteStaedte = 1;
        rot.Rohstoffe[Rohstoff.Korn] += 2;
        rot.Rohstoffe[Rohstoff.Erz] += 3;

        blau.Siegpunkte = 5;
        blau.Rohstoffe[Rohstoff.Holz] += 1;
        blau.Rohstoffe[Rohstoff.Wolle] += 2;

        board[0, 0, 0]!.Gebaeude = Gebaeude.Stadt;

        return (board, spieler);
    }

    private static BoardNode?[,,] ErstelleBoard(Spieler rot, Spieler blau)
    {
        BoardNode?[,,] board = new BoardNode?[2, 3, 2];

        board[0, 0, 0] = Feld(Rohstoff.Holz, 6, rot, Gebaeude.Siedlung, Hafen.Allgemein);
        board[0, 1, 0] = Feld(Rohstoff.Lehm, 5, rot, Gebaeude.Siedlung, Hafen.Keiner);
        board[1, 0, 0] = Feld(Rohstoff.Korn, 8, rot, Gebaeude.Siedlung, Hafen.Keiner);

        board[0, 2, 0] = Feld(Rohstoff.Erz, 9, blau, Gebaeude.Stadt, Hafen.Keiner);
        board[1, 1, 0] = Feld(Rohstoff.Korn, 4, blau, Gebaeude.Siedlung, Hafen.Keiner);
        board[1, 2, 1] = Feld(Rohstoff.Wolle, 10, blau, Gebaeude.Siedlung, Hafen.Spezial);

        board[0, 0, 1] = Feld(Rohstoff.Lehm, 3, null, Gebaeude.Keine, Hafen.Keiner);
        board[1, 0, 1] = Feld(Rohstoff.Holz, 11, null, Gebaeude.Keine, Hafen.Keiner);

        return board;
    }

    private static BoardNode Feld(Rohstoff rohstoff, int zahlenchip, Spieler? besitzer, Gebaeude gebaeude, Hafen hafen)
    {
        return new BoardNode
        {
            Rohstoff = rohstoff,
            Zahlenchip = zahlenchip,
            Besitzer = besitzer,
            Gebaeude = gebaeude,
            Hafen = hafen
        };
    }

    private static void SetzeRohstoffe(Spieler spieler, int holz, int lehm, int korn, int wolle, int erz)
    {
        spieler.Rohstoffe[Rohstoff.Holz] = holz;
        spieler.Rohstoffe[Rohstoff.Lehm] = lehm;
        spieler.Rohstoffe[Rohstoff.Korn] = korn;
        spieler.Rohstoffe[Rohstoff.Wolle] = wolle;
        spieler.Rohstoffe[Rohstoff.Erz] = erz;
    }
}
