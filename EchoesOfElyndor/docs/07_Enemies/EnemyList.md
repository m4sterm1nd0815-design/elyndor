# Enemy List

Stand: 15. August 2026

Übersicht der Gegner. Die Rolle im Encounter steht in
`FINSTERWALD_ENCOUNTER_PLAN.md`, die gemeinsame Technik in
`ARCHITECTURE.md`.

| Gegner | Rolle | Stand | Entwurf |
|---|---|---|---|
| **Wurzelstreifer** | Mobiler Nahkampf-Lehrer; erster echter Gegner | **Umgesetzt (P1.5)** — Blockout in der Szene, finales Asset offen | [Konzeptentwurf](WURZELSTREIFER_CONCEPT_BRIEF.md) |
| Echohüter | Langsamer Raumkontrolleur | Geplant (P1.6) | — |
| Namenloser Hüter | Abschlussprüfung des Slice | Geplant | — |

## Wurzelstreifer — Kurzprofil

Ein kleines Waldtier, dessen Ortsgedächtnis von wiederholten Fluchtspuren
überlagert wurde. Kein Monster, sondern ein beschädigtes Tier.

| Aspekt | Wert |
|---|---|
| Lebenspunkte | 40 |
| Schaden | 10 |
| Sicht / Gehör | 9 m Kegel (120°) / 5 m ohne Sichtlinie |
| Verdachtsphase | 2 s |
| Vergisst Aren nach | 4 s |
| Telegraph | 0,7 s |
| Erholung nach dem Biss | 1,2 s |
| Bisstreichweite | 2,2 m |
| Flinch / Stagger | 0,18 s / 0,8 s |
| Rückzug | einmalig unter 30 % Leben |
| Bindung an die Lichtung | 12 m |

Alle Werte sind vorläufige Balancingwerte. Sie liegen als Konstanten in
`Scripts/Enemies/Wurzelstreifer.cs` und werden von dort auf die
Gegnergrundlage gelegt — nicht nur im Prefab, damit sie ohne Unity nachlesbar
und testbar bleiben.

**Modell:** derzeit der vorhandene Quaternius-Wolf (CC0) als reiner Blockout.
Er definiert **nicht** die Art Direction; siehe Konzeptentwurf.
