# Finsterwald Encounter Plan

Stand: 2. August 2026
Status: `VORSCHLAG`
Aktualisiert: 14. August 2026 - Gate 0 ist abgeschlossen; die Blockade
entfaellt. Die Gegner-Grundlage ist inzwischen integriert, siehe
`FINSTERWALD_VERTICAL_SLICE_STATUS.md`.

## Designregeln

Die Gegner sind eigenstaendige Elyndor-Entwuerfe. Sie entstehen aus
beschaedigten Ortsgedaechtnissen und verwirrtem Schutzverhalten, nicht aus
einer austauschbaren Daemonenarmee. Silhouetten sind handgemacht, warm-mystisch
und naturgebunden. Namen, Aussehen und Verhalten kopieren keine bekannte oder
geschuetzte Figur.

Balancing-Werte sind vorlaeufig fuer 100 Spielerleben, 10 leichten und 25
schweren Spielerschaden. Ziel ist Lesbarkeit, nicht hohe Lebenspunkte.

**Blockregel (projektweit, entschieden am 15.08.2026):** Block reduziert
eingehenden Gesundheitsschaden um 70 %; 30 % kommen durch. Er negiert
bewusst nicht vollstaendig, damit Rolle, Block und Abstand unterschiedliche
Kosten haben. Einzelheiten in `04_Gameplay/Combat.md`.

## Normaler Gegnertyp A: Wurzelstreifer

| Aspekt | Definition |
|---|---|
| Rolle | Mobiler Nahkampf-Lehrer; isoliert gut lesbar, in Paaren leichter Flanker. |
| Weltbezug | Ein kleines Waldtier wurde von wiederholten Fluchtspuren ueberlagert. Rinde und tuerkise Bruchlinien wirken gewachsen, nicht gepanzert. |
| Wahrnehmung | 9 m Sichtkegel, 5 m Geraeuschradius, 2 s Verdachtsphase; verliert Aren nach 4 s ohne Sicht. |
| Muster | Beobachten -> seitlicher Schritt -> 0,7 s Schulter-Telegraph -> kurzer Sprungbiss; nach Fehlschlag 1,2 s offen. Unter 30 % Leben einmaliger Rueckzug, kein Spam. |
| Trefferreaktion | Leicht: 0,18 s Flinch mit kurzer Unterbrechung ausser waehrend Sprung. Schwer: Stagger 0,8 s und sichtbare Rindenpartikel. |
| Werte | 35-45 LP; 8-12 Schaden; maximal 2 aktive Angreifer. |
| Spawn | Ein Exemplar auf der Lichtung; spaeter zwei am regenerierten Nordpfad, mit ausreichend Ausweichraum. |
| Animation | Idle/Lauschen, Walk, Trot, Strafe, Telegraph, Sprungbiss, Hit L/R, Heavy Stagger, Rueckzug, Beruhigung/Niederlage. |
| Modelle/VFX | `ENTSCHIEDEN` (15.08.2026): vorhandener Quaternius-Wolf (CC0) nur als Blockout — umgesetzt. Finales Modell noch nicht beauftragt; Vorgabe in `07_Enemies/WURZELSTREIFER_CONCEPT_BRIEF.md`. Tuerkise Rissglut und Rindenstaub umgesetzt; bodennahe Erinnerungsschliere offen. |
| Bindung | `ENTSCHIEDEN`: 12 m um die Lichtung. Darueber kehrt er zurueck und nimmt bis dahin kein Ziel wahr. |
| Stand | **Umgesetzt in P1.5.** Werte im Prefab `Prefabs/Enemies/Blockout/Wurzelstreifer_Blockout.prefab`, Profil im Code unter `Scripts/Enemies/Wurzelstreifer.cs`. |
| Technik | Perception, State Machine, NavMesh/Steering, IDamageable-Erweiterung, PlayerVitals-Schaden, Hitstop/Feedback, Spawn- und Resetlogik. |

## Normaler Gegnertyp B: Echohueter

| Aspekt | Definition |
|---|---|
| Rolle | Langsamer Raumkontrolleur; zwingt zu Abstandswahl, Block oder seitlichem Ausweichen. |
| Weltbezug | Eine knorrige, menschenferne Gestalt aus Wurzelholz und schwebenden Steinsplittern wiederholt eine vergessene Wachroute. Keine untote Person. |
| Wahrnehmung | 11 m Sichtkegel, reagiert stark auf Kampflaerm; 1,5 s Aktivierungsphase; kehrt zu einem festen Erinnerungsweg zurueck. |
| Muster | Zwei langsame Schritte -> breiter 1,0-s-Wurzelschwung; alternativ 1,2-s-Aufladung einer geraden Bodenspur bis 6 m. Nach beiden 1,4 s offen. Keine Zielkorrektur nach 60 % Telegraph. |
| Trefferreaktion | Leichte Treffer erzeugen Feedback ohne Dauerschleife; schwere Treffer waehrend Aufladung brechen die Bodenspur und staggern 1,0 s. |
| Werte | 65-80 LP; Schwung 14-18, Bodenspur 12-16 Schaden; einzeln oder mit genau einem Streifer. |
| Spawn | Erste Begegnung nach der Memory Site in einer breiten Wegschleife; zweites Exemplar als Vorhut des Prozessionstors. |
| Animation | Dormant/Aufrichten, Patrol, Turn, Wind-up, Sweep, Ground Cast, Hit, Broken Cast, Stagger, Settle. |
| Modelle/VFX | Eigenes stilisiertes Wurzel-/Steinmodell; Proto-Ruinensteine duerfen Blockout liefern. Wurzelspur, Staubring, tuerkise Nachbilder. |
| Technik | Root-Motion-Entscheidung `OFFEN`, gerichtete Hazard-Spur, schwere Angriffsunterbrechung, Animator-Events, Arena-Leash. |

## Staerkerer Gegner: Namenloser Hueter

`VORSCHLAG`: ein alter, hirschartiger Waldhueter am Forgotten Processional
Gate. Er ist kein boeses Tier und kein obligatorisch getoeteter Boss. Der
Spieler beruhigt die zerrissene Erinnerung durch Kampf; die Niederlage endet
mit Loesung der feindlichen Resonanz.

| Aspekt | Definition |
|---|---|
| Rolle | Abschlusspruefung aller gelernten Kampfregeln, 3-5 Minuten. |
| Wahrnehmung | Scripted Awareness erst nach Betreten der Arena; klare Grenze, kein Angriff durch Geometrie. |
| Phase 1 | Doppelhieb mit Geweih/Wurzelarmen, gerader Ansturm, lange Erholung nach Kollision oder Ausweichen. |
| Phase 2 | Ab 55 % LP einmaliger Resonanzimpuls, dann eine langsame Echo-Kopie des letzten Ansturms. Original und Echo sind durch Form und Ton unterscheidbar; maximal ein Echo. |
| Gegenfenster | Block reduziert Schaden um 70 % und negiert ihn nicht (`ENTSCHIEDEN` 15.08.2026, gilt projektweit); Rolle vermeidet; schwerer Angriff nach Ansturm erzeugt grossen Stagger. |
| Trefferreaktion | Kleine Hit-Reaktion nur ausserhalb Super-Armor; schwerer Stagger 1,3 s; drei klar sichtbare Resonanz-Bruchstufen. |
| Werte | 160-190 LP; Standard 16-20, Ansturm 24-30, Echo 10-14 Schaden. Keine Heilung oder zufaellige Phase. |
| Ort | Forgotten Processional Gate mit Blick auf Root Gate/Aussicht; Arena 16-20 m, keine Kamera- oder Baumkronenblockade. |
| Ende | Bei 0 LP nicht blutig sterben: Haltung sinkt, Risse schliessen sich, Partikel ziehen zur Memory Site. `VORSCHLAG`: spaetere friedliche Silhouette bleibt. |
| Modell | Vorhandener Quaternius-Stag als lizenzierter Blockout; individuelles Meshy-Hero-Asset fuer finale Elyndor-Silhouette, maximal 2 Versuche. |

## Encounter-Reihenfolge

1. Lichtung: 1 Wurzelstreifer, sicherer Lehrraum.
2. Nordufer nach Memory Site: 1 Echohueter, Bodenspur ohne weitere Gegner.
3. Regenerierter Pfad: 2 Wurzelstreifer mit versetzten Aktivierungszonen.
4. Torvorhof: 1 Echohueter + 1 spaet eintretender Wurzelstreifer; niemals drei
   gleichzeitige Angreifer.
5. Prozessionstor: Namenloser Hueter allein.

## Benoetigte gemeinsame Technik

- zentrale Combat-Actions statt direkter Device-Abfragen;
- `EnemyVitals`, Damage Context, Trefferteams und Unverwundbarkeitsfenster;
- PlayerVitals-Schaden, Tod/Reset-Entscheidung und Checkpoint-Rueckkehr;
- Wahrnehmung mit Sicht, Geraeusch, Verdacht und Leash;
- gemeinsame Enemy-State-Machine und datengesteuerte Balancing-Profile;
- Animation Events fuer Trefferfenster, VFX und Audio;
- reproduzierbare Spawner mit stabilen IDs und Save-Zustand;
- Kamera-/Silhouettenabnahme in allen Arenen.

## Testkriterien

- Jede Attacke ist mindestens 0,6 s vorher durch Bewegung und Ton lesbar.
- Kein Gegner trifft ausserhalb seines sichtbaren Hitvolumens.
- Block, Rolle und Distanz sind jeweils mindestens einmal valide Antworten.
- Schwere Angriffe unterbrechen nur dokumentierte Fenster.
- Gegner verfolgen Aren nicht aus Arena/Region oder durch Hindernisse.
- Maximal zwei normale Gegner greifen gleichzeitig aktiv an.
- Kein normaler Kampf dauert bei korrektem Spiel laenger als 90 s; der Hueter
  dauert 3-5 Minuten und hat keine Leerlaufphase ueber 8 s.
- Controller und Maus/Tastatur bestehen identische Kampfszenarien.
- 30/60/120-FPS-Vergleich zeigt keine abweichenden Trefferfenster.
- Niederlage, Checkpoint-Reset, Save/Load und Rueckkehr hinterlassen keine
  doppelten Gegner oder verlorenen Weltzustaende.
