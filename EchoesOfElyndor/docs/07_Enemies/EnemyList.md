# Enemy List

Stand: 16. August 2026

Übersicht der Gegner. Die Rolle im Encounter steht in
`FINSTERWALD_ENCOUNTER_PLAN.md`, die gemeinsame Technik in
`ARCHITECTURE.md`.

| Gegner | Rolle | Stand | Entwurf |
|---|---|---|---|
| **Wurzelstreifer** | Mobiler Nahkampf-Lehrer; erster echter Gegner | **Umgesetzt (P1.5)** — finales Asset in der Szene, künstlerisch freigegeben (16.08.2026, für den Vertical-Slice-Stand) | [Konzeptentwurf](WURZELSTREIFER_CONCEPT_BRIEF.md) |
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

**Modell:** seit dem 16.08.2026 das eigene Asset
`ELY_Enemy_Wurzelstreifer` aus Blender (PR #53, Merge-Commit `08beca9`); der
technische Stand steht in `../Technical/WURZELSTREIFER_ASSET.md`. Der
Quaternius-Wolf (CC0) war der Blockout davor und hat **nie** die Art Direction
definiert; siehe Konzeptentwurf. Die künstlerische Freigabe für den
gegenwärtigen Vertical-Slice-Stand liegt vor.
