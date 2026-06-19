# CatanStrengthPrototype

## Voraussetzungen

- .NET 8 SDK

## Start

```bash
dotnet run
```

## Dateien

- `Program.cs`: startet die zwei Runden und gibt die Ergebnisse aus.
- `Models.cs`: enthält Spieler, Board-Felder und Enums.
- `TestScenario.cs`: baut ein kleines hartcodiertes Testszenario.
- `StrengthCalculator.cs`: berechnet Teilwerte und Gesamtstärke.

## Programmablauf

Das Programm berechnet zuerst die Spielerstärken im Ausgangszustand. Nach einer kurzen Pause mit ENTER wird ein veränderter Spielzustand geladen und mit derselben Logik erneut bewertet.

## Bekannte Einschränkungen

Das Spielfeld ist nur ein logisches Testmodell als `BoardNode?[,,] Board`. Es bildet die echte hexagonale CATAN-Geometrie, legale Bauplätze, Räuber, Entwicklungskarten und genaue Hafenpositionen nicht vollständig ab.

Der `StrengthCalculator` ist bewusst unabhängig gehalten und könnte später von Unity oder dem Hauptspiel mit echten Spieldaten aufgerufen werden.
