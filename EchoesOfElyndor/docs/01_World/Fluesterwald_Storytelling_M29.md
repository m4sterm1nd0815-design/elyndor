# Flüsterwald — Environmental Storytelling (Sprint M2.9)

Stand: 22.07.2026. Alle Elemente werden deterministisch vom
`WhisperingForestSceneBuilder` erzeugt (Menü: *Elyndor → Setup →
Fluesterwald-Testszene erstellen*). Koordinaten sind Welt-XZ.

## Leitidee

Der Wald soll nicht „schön" wirken, sondern eine Frage stellen: *Wer war
hier — und warum ist niemand mehr da?* Jedes Element erzählt dieselbe
Geschichte aus einem anderen Blickwinkel: Es gab Menschen im Flüsterwald,
sie sind fort, und der Wald erinnert sich (Kernthema Memory Watch).

## Regionen

| Region | Ort | Identität |
|---|---|---|
| Startbereich | (0, −54) | Offene Wiese, Schild, sanfter Einstieg |
| Dichter Urwald | Kreis um (−48, −40), r≈18 | Eng stehende knorrige Bäume und Kiefern, dunkelste Stelle des Waldes |
| Birkenhain | (−16, −31) | Heller Kontrast im Nebel, weiße Stämme |
| Ruhige Lichtung | (−26, −16) | Großer alter Baum, Blumen, Licht |
| Bachlauf | West→Ost über die Karte | Uferzone mit Steinen, Farnen, Birken |
| Uralte Baumriesen | Kreis um (−14, 16), r≈13 | Alle Bäume wachsen bis zu 45 % höher, Zentrum ist der „Uralte Wächter" |
| Überwucherte Ruine | (−40, 28) | Tempelmauern, durchwachsener Baum, Inschrift |
| Moosiger Felsenbereich | (50, −24) | Gestapelte Felsformation, Farne, Pilze |
| Aussichtspunkt | (48, 44) | Höhenzug, toter Silhouettenbaum, Blick über den Wald |

## Landmarken & Storytelling-Elemente

- **Uralter Wächter** (−14, 16): Riesiger TwistedTree (~19 m) mit
  freiliegendem „Wurzelwerk" aus Steinplatten und Farnen. Untersuchbar.
  Vom Nordufer-Weg aus sichtbar → Blickführung nach Nordwesten.
- **Tempelruine** (−40, 28): Halb eingestürzte Mauern aus dem
  Modular-Temple-Kit (moos- und steingrau), versunkene Bodenplatten,
  stehender + gestürzter Pfeiler, Baum wächst mitten hindurch.
  Verwitterte Inschrift (untersuchbar) bleibt erhalten.
- **Vergessener Schrein** (−34.3, 16.5, direkt am versteckten Pfad):
  Wandnische mit Kieselhalbkreis und Blumen. Text-Hook: *„Manche sind
  frischer, als sie sein dürften."* → jemand besucht den Schrein noch.
- **Verlassener Rastplatz** (5, −25, vom Hauptweg sichtbar):
  Kalte Feuerstelle, Sitzstämme, nie verbranntes Feuerholz. Text-Hook:
  Die Rastenden wollten zurückkommen — kamen aber nie.
- **Überwucherte Mauer** (17, −11.5, am Uferweg): Drei Ruinen-Mauerstücke
  ohne Haus und Straße, von Büschen und Farnen zurückerobert. Untersuchbar.
- **Moosige Felsformation** (50, −24): Zwei aufeinander gestapelte
  Großfelsen + Ring kleinerer Felsen. Text-Hook: Rillen im Stein, „zu
  regelmäßig, um Zufall zu sein".
- **Verstreute Pfeilerfragmente** (−44, −20), (22, −32), (18, 30):
  Stehender Pfeilerstumpf + gestürztes Segment + versunkene Bodenplatte,
  jeweils mit Bewuchs. Deuten an: der ganze Wald war einmal besiedelt.
- **Gestürzte Baumriesen** (−24, −38), (26, 22): Liegende Totbäume (~13 m)
  mit Pilzreihen und Farn am modernden Holz.
- **Weg-Engstellen** bei (−8, −32), (26, −6), (36, 12): Felsen rücken mit
  Collidern an den Weg — der Wald wird kurz eng, dann öffnet er sich wieder.

Bestehende Landmarken davor: zerstörte Brücke + Memory Site, Steinkreis,
Baumstamm-Querung, Furt, Wegmarkierungen, Aussichtspunkt-Schild.

## Blickführung

- Start → Wegschild → Gabelungsfels → (links) Lichtung mit Großbaum /
  (rechts) Brücke.
- Rastplatz liegt 5–6 m neben dem Hauptweg → erster „Was ist das?"-Moment.
- Uferweg: überwucherte Mauer südlich, Engstelle bei der Furt.
- Nordufer-Weg: Uralter Wächter überragt den Bestand nach Nordwesten.
- Versteckter Pfad: Schrein als Belohnung fürs Abbiegen.
- Aussichtspunkt: toter Baum als Silhouette, Blick zurück über alles.

## Verwendete Assets

- **Natur Pack**: Basisvegetation, Felsen, Kiesel, Pilze, Blumen
- **Ultimate Stylized Nature** (`USN:`): Normal-/Ahorn-/Kiefern-/Birkenbäume, Büsche, Stauden, Großfelsen
- **Textured Stylized Trees** (`TST:`): Birken, texturierte Laub-/Totbäume (auch als gestürzte Riesen)
- **Modular Temple** (`TEMPLE:`, OBJ): Ruinenmauern, Pfeiler, Bodenplatten, Schrein-Nische — einheitlich mit `Proto_Stone`/`Proto_MossyStone` eingefärbt
- Neue Prototyp-Materialien: `Proto_MossyStone`, `Proto_Charred`

## Freigehaltene Flächen (Gameplay-Reserve)

Baumfreie Zonen um: Startbereich, Lichtung, Memory Site, Brücke,
Baumstamm-Querung, Furt, Steinkreis, Ruine, Aussichtspunkt sowie 5 m um
jede neue Landmarke. Dort ist Platz für NPCs, weitere Memory Sites,
Rätsel und Sammelobjekte.

## Mögliche Quest-/Story-Anker (später)

1. **Schrein**: Wer legt die frischen Blumen hin? → NPC-Spur / Memory Site
2. **Rastplatz**: Was geschah mit den Rastenden? → Echo-Szene
3. **Felsformation**: Die Rillen im Stein → Rätsel (Memory Watch zeigt das Muster)
4. **Pfeilerfragmente**: Karte der alten Siedlung rekonstruieren (Sammelaufgabe)
5. **Uralter Wächter**: Zentrum einer großen Erinnerung (Haupt-Story-Beat)

## Performance-Notizen

- Alles statisch markiert (Static Batching); keine LOD-Gruppen im
  Prototyp — bei Bedarf später via Editor-Skript nachrüstbar.
- Collider nur wo nötig: Baumstämme (Kapseln), Ruinenmauern/Pfeiler/
  Engstellen-Felsen (Box über Bounds), begehbare Querungen. Deko
  (Gras, Blumen, Pilze, Kiesel, Bodenplatten) ohne Collider.
- Vegetation in Clustern statt Gleichverteilung; Zonen bleiben frei.
