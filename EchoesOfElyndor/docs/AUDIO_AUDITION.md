# Audio-Hörprobe

Stand: 15. August 2026
Status: **HUMAN QA OPEN**

Alle Töne des Slice sind nach Benennung und gemessener Länge ausgewählt.
**Gehört hat sie niemand.** Diese Liste existiert, damit das in wenigen Minuten
nachzuholen ist, statt das Projekt nach Sounds abzusuchen.

Kein Clip wird allein aufgrund theoretischer Überlegung getauscht. Was hier
steht, ist die beabsichtigte Wirkung — die Entscheidung fällt am Ohr.

## So kommt man am schnellsten hin

Alle Zuweisungen liegen auf **einer** Komponente:
`Finsterwald` → `ElyndorExperience` → `SfxLibrary`.

Dort lässt sich jeder Clip direkt im Inspector anspielen.

## Kampf — Wurzelstreifer (Lichtung, ca. `-26 / -17`)

| Spielereignis | Clip | Stelle im Slice | Gewünschte Wirkung |
|---|---|---|---|
| **Telegraph vor dem Sprungbiss** | `creak1` (0,661 s) | Sobald der Gegner ansetzt | Der wichtigste Ton des Kampfes. Er läuft gegen 0,7 s Ansatz und soll **enden, wenn der Biss landet**. Muss auch hörbar sein, wenn der Gegner hinter einem Baum steht. Holz unter Last, kein Tierlaut. |
| Leichter Treffer am Gegner | `impactWood_light_000` | Jeder leichte Schlag | Rinde splittert. Trocken, kurz. |
| Schwerer Treffer / Stagger | `impactWood_heavy_000` | Schwerer Schlag | Deutlich schwerer als der leichte — er ist der hörbare Teil des 0,8-s-Straucheln. |
| Geblockter Treffer | `impactSoft_medium_000` (0,118 s) | Block hält | **Gedämpft.** Muss sich klar vom ungeblockten unterscheiden, sonst lehrt der Block nichts. |
| Ungeblockter Treffer | `impactPunch_medium_000` | Treffer kommt durch | Hart. Der Unterschied zum Block ist die eigentliche Information. |
| Niederlage | `impactSoft_heavy_000` (0,505 s) | Gegner besiegt | Beruhigung, kein Einschlag. Ausdrücklich **kein** zusätzlicher Trefferton — der tödliche Schlag spielt nur diesen. |

## Rätsel — Die geteilte Brücke (ca. `2 / -1`)

| Spielereignis | Clip | Stelle im Slice | Gewünschte Wirkung |
|---|---|---|---|
| **Ankerstellung trägt** | `creak3` (0,338 s) | Seilbock prüfen oder Stamm freigeben bei richtiger Stellung | Klarer Holz-/Seilton. Kurz und bejahend. Muss sich vom Telegraph-Creak unterscheiden lassen — beide sind Creaks, aber in verschiedenen Zusammenhängen. **Falls sie zu ähnlich klingen, ist das der wichtigste Tausch.** |
| **Ankerstellung trägt nicht** | `PLACEHOLDER_Stein_Reiben.wav` (0,45 s) | Jede falsche Prüfung, jedes Verkanten | Dumpfes Reiben von Stein. **Erzeugter Platzhalter** — der Bestand kennt Aufschläge auf Stein, aber kein Reiben; ein Aufschlag läse sich als „etwas ist zerbrochen" statt „das trägt nicht". Darf nie verraten, *welcher* Anker falsch steht — tut er per Konstruktion auch nicht. |

## Regeneration (nach Rätsel **und** Erinnerung)

| Spielereignis | Clip | Stelle im Slice | Gewünschte Wirkung |
|---|---|---|---|
| Der Wald antwortet | `PLACEHOLDER_Wald_Antwort.wav` (1,6 s) | Einmalig, wenn beide Bedingungen erfüllt sind | **Leise** — Lautstärke 0,55, niedriger als alles andere. Der Wald antwortet, er kündigt sich nicht an. Ein lauter Ton machte aus einer Beobachtung eine Belohnung. Zwei ruhige Teiltöne, langsam auf und ab. |

## Bestand aus früheren Paketen

Unverändert und nicht Teil dieser Prüfung: Narration (`bookFlip1`), Memory
Start/Ende (`metalLatch`, `bookOpen`), Spielerangriffe (`knifeSlice`, `chop`),
Übungspuppe, Inventar, Schritte.

## Was fehlt

- **Ambience.** Kein Wald-, Wasser- oder Nachtklang. Die Regeneration soll
  laut Plan „einzelne Tier-/Waldgeräusche zurückkehren" lassen — dafür gibt es
  im Bestand nichts.
- **Link.** Sein Ruf ist im Code vorgesehen (`LinkCompanion.AnyCalled`), hat
  aber noch keinen Clip. Bewusst: eine Eule, die falsch klingt, ist schlimmer
  als eine stille.

## Lizenz

Alle nicht erzeugten Clips stammen aus den vorhandenen Kenney-Packs
(`kenney_impact-sounds`, `kenney_rpg-audio`), **CC0**, je eigene `License.txt`
im Pack. Die beiden Platzhalter sind im Projekt erzeugt und tragen es im Namen.
