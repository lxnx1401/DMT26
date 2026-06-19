using System.Globalization;

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

StrengthCalculator calculator = new();

var rundeEins = TestScenario.ErstelleAusgangszustand();
Dictionary<string, StrengthResult> ergebnisseRundeEins = ZeigeRunde(1, rundeEins.Board, rundeEins.Spieler, calculator);

Console.WriteLine();
Console.WriteLine("Weiter mit ENTER: In Runde 2 wird eine Siedlung zur Stadt und die Rohstoffe ändern sich.");
Console.ReadLine();

var rundeZwei = TestScenario.ErstelleRundeZwei();
ZeigeRunde(2, rundeZwei.Board, rundeZwei.Spieler, calculator, ergebnisseRundeEins);

static Dictionary<string, StrengthResult> ZeigeRunde(
    int runde,
    BoardNode?[,,] board,
    List<Spieler> spieler,
    StrengthCalculator calculator,
    Dictionary<string, StrengthResult>? vorherigeErgebnisse = null)
{
    Dictionary<string, StrengthResult> ergebnisse = new();

    foreach (Spieler person in spieler)
    {
        ergebnisse[person.Name] = calculator.Berechne(person, board);
    }

    double durchschnitt = ergebnisse.Values.Average(e => e.Gesamtstaerke);
    double standardabweichung = Math.Sqrt(ergebnisse.Values.Average(e => Math.Pow(e.Gesamtstaerke - durchschnitt, 2)));

    Console.WriteLine();
    Console.WriteLine($"=== RUNDE {runde} ===");
    Console.WriteLine();

    foreach (Spieler person in spieler)
    {
        StrengthResult result = ergebnisse[person.Name];
        double veraenderung = vorherigeErgebnisse is not null && vorherigeErgebnisse.TryGetValue(person.Name, out StrengthResult? vorher)
            ? result.Gesamtstaerke - vorher.Gesamtstaerke
            : 0;

        ZeigeSpieler(person, result, veraenderung, result.Gesamtstaerke - durchschnitt, runde > 1);
    }

    Console.WriteLine($"Durchschnitt:          {durchschnitt:0.00}");
    Console.WriteLine($"Standardabweichung:    {standardabweichung:0.00}");

    return ergebnisse;
}

static void ZeigeSpieler(Spieler spieler, StrengthResult result, double veraenderung, double abstand, bool zeigeVeraenderung)
{
    Console.WriteLine($"Spieler: {spieler.Name}");
    Console.WriteLine($"Siegpunkte:           {result.Siegpunkte:0.00}");
    Console.WriteLine($"Produktion:           {result.Produktion:0.00}");
    Console.WriteLine($"Baupotenzial:         {result.Baupotenzial:0.00}");
    Console.WriteLine($"Expansion:            {result.Expansion:0.00}");
    Console.WriteLine($"Hafenpotenzial:       {result.Hafenpotenzial:0.00}");
    Console.WriteLine($"Tauschwert:           {result.Tauschwert:0.00}");
    Console.WriteLine("--------------------------------");
    Console.WriteLine($"Gesamtstärke:         {result.Gesamtstaerke:0.00}");

    if (zeigeVeraenderung)
    {
        Console.WriteLine($"Veränderung:          {veraenderung:+0.00;-0.00; 0.00}");
    }

    Console.WriteLine($"Abstand Durchschnitt: {abstand:+0.00;-0.00; 0.00}");
    Console.WriteLine();
}
