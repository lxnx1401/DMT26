# Mathematische Formeln
Spielerstärke = Siegpunkte + Produktionspotenzial + Baupotenzial + Expansionspotenzial + Hafenwert + Tauschwert

## Produktionspotenzial
P_i = Σ (g_h * p(n_h) * r_h)
P_i = Summe aller eigenen Felder:
Ressourcen_Multiplikator * ZahlenWahrscheinlichkeit * RohstoffWert

Ressourcen_Multiplikator also ein Dorf = 1 und eine Stadt = 2

RohstoffWert = Wertigkeit_RohstoffElement * Häufigkeit_Karte
Wertigkeit_RohstoffElement = Summe_(Anzahl_Im_Bauelement.I * Wertigkeit_Von_Bauelement.I)
Häufigkeit_Karte = Summe_(Wahrscheinlichkeiten_EinzelFeld)

## Baupotenzial
B_i = Wertigkeit_Straße * StraßenPotenzial + Wertigkeit_Siedlung * SiedlungsPotenzial 
        + Wertigkeit_Stadt * StadtPotenzial + Wertigkeit_Entwicklungskarte * EntwicklungskartenPotenzial

Straßenpotenzial = Anzahl_Holz + Anzahl_Lehm
Siedlungspotenzial = Anzahl_Holz + Anzahl_Lehm + Anzahl_Korn + Anzahl_Schaf
StadtPotenzial = Anzahl_Erz / 3 + Anzahl_Korn / 2
EntwicklungskartenPotenzial = Anzahl_Erz + Anzahl_Korn + Anzahl_Schaf

## Expansionspotenzial
E_i = 0.4 * (OffeneStraßen - Verfuegbare_Siedlungen) + 0.2 * Summe_(Qualität_Bauplätze) + 0.4 * (Gebaute_Siedlungen - Verfuegbare_Städte)

Qualität_Bauplätze = ZahlenWahrscheinlichkeit * RohstoffWert

## Hafenwert
H_i = Summe_(Produktionsstärke_Rohstoff / Anzahl benötigter Rohstoffe)
        - Für jeden Rohstoff wird die Produktionsstärke berechnet dividert durch die Anzahl an Rohstoffen nötig für den billigsten Tausch

Produktionsstärke_Rohstoff = Summe_(Wahrscheinlichkeit_Einzelfeld * Ressourcen_Multiplikator)

## Tauschwert
T_i = Summe_(RohstoffWert * Anzahl_RohstoffImDeck)