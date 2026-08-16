# Finsterwald – Asset Requirements

## Zweck und Beschaffungsstopp

Der Katalog beschreibt den Bedarf des Vertical Slice. Er autorisiert weder Downloads noch Meshy-Generierungen. Gate 0 ist abgeschlossen. Der Beschaffungsstopp bleibt dennoch bestehen: Assets werden erst nach ausdruecklicher Freigabe des jeweiligen Asset-Pakets importiert, erzeugt oder veraendert. Vor jedem späteren Import gelten `ASSET_PIPELINE.md`, Lizenznachweis, Quellenprotokoll und isolierte Prüfung.

Prioritäten: **P0** blockiert den spielbaren Ablauf, **P1** prägt Lesbarkeit und Identität, **P2** ist Polishing. Polycount-Angaben sind Zielwerte für sichtbare Dreiecke der höchsten Spiel-LOD.

## Bereits im Projekt vorhanden

| Asset | Zweck | Prio | Zielordner / Bestand | Stil | Polycount-Ziel | Collider / LOD | Quelle | Lizenzanforderung | Meshy-Versuche |
|---|---|---:|---|---|---:|---|---|---|---:|
| Finsterwald-Terrain und Routengerüst | Raum, Haupt- und Nebenpfade | P0 | `Assets/_Elyndor/Scenes/Finsterwald.unity` | malerisch-düstere Natur | Bestand | Terrain Collider; Sichtweiten prüfen | Projektbestand | Repository-Nachweis | 0 |
| Root Gate | visuelle Schwelle | P1 | `Assets/_Elyndor/Art/Meshy/RootGate/` und Prefab | verwachsene, asymmetrische Waldform | Bestand prüfen | vereinfachter Collider; LOD0-2 prüfen | vorhandener Meshy-Import | vorhandenen Generierungs-/Lizenznachweis erhalten | 0 |
| Aren-Modell | Spielerfigur | P0 | `Assets/_Elyndor/Art/Meshy/Aren/` | Elyndor-Charakterstil | Bestand prüfen | CharacterController; LOD OFFEN | vorhandener Meshy-Import | vorhandenen Nachweis erhalten | 0 |
| Mittelalterliche Schwerter | Blockout und erste Waffe | P0 | vorhandenes Medieval-Weapons-Paket; später kuratierter Prefab unter `Assets/_Elyndor/Prefabs/Items/Weapons/` | gebraucht, bodenständig | Bestand | Mesh-/Box-Collider; LOD nach Sichtgröße | Projektbestand, Drittanbieter | Lizenz im Asset-Manifest verifizieren | 0 |
| Wolf- und Hirschmodelle | Gegner-Blockout | P0 | vorhandenes Animal-Paket; Blockout-Prefabs unter `Assets/_Elyndor/Prefabs/Enemies/Blockout/` | nur Proportion/Timing | Bestand | Capsule/Primitive; keine komplexen Mesh-Collider | Projektbestand, Drittanbieter | Lizenz vor Nutzung verifizieren | 0 |
| Natur-, Ruinen- und Dorfmodule | Greybox/Set Dressing | P0/P1 | vorhandene ThirdParty-Pakete; kuratierte Prefabs unter `Assets/_Elyndor/Prefabs/Environment/` | gealtert, moosig, silhouettesicher | Bestand | statische vereinfachte Collider; LOD0-2 | Projektbestand | Paketlizenz und Attribution prüfen | 0 |
| Kenney UI/Audio-Bestand | Platzhalter für Icons und Feedback | P1 | vorhandene ThirdParty-Pakete; kuratierte Inhalte unter `Assets/_Elyndor/UI/` und `Assets/_Elyndor/Audio/` | klar, zurückhaltend | n. a. | n. a. | Kenney / Projektbestand | konkrete Paketlizenz dokumentieren | 0 |
| Memory-Site-, Bridge- und World-Prefabs | Puzzle-/Lore-Basis | P0 | vorhandene `_Elyndor`-Prefabs und Builder-Ausgabe | bestehende Finsterwald-Sprache | Bestand | Trigger getrennt von Physik; LOD prüfen | Projektbestand | Repository-intern | 0 |

## Später sinnvoll über CC0-Bibliotheken beschaffbar

Jeder Eintrag wird per Such-/Dry-Run-Pipeline bewertet, bevor etwas beschafft wird. „Quelle“ bezeichnet die bevorzugte Bibliothek, nicht eine bereits ausgewählte Datei.

| Asset | Zweck | Prio | Zielordner | Stil | Polycount-Ziel | Collider / LOD | Quelle | Lizenzanforderung | Meshy-Versuche |
|---|---|---:|---|---|---:|---|---|---|---:|
| Moos-, Rinde-, Schlamm- und Felsmaterialien | einheitliche Oberflächen | P1 | `Assets/_Elyndor/Art/External/Materials/Finsterwald/` | feucht, entsättigt, natürliche Variation | n. a. | n. a.; 1–2K Texturen, Kanalpackung prüfen | ambientCG oder Poly Haven | ausschließlich CC0, URL und Abrufdatum | 0 |
| Kleine Felsen und Totholz | Wegführung und Deckung | P1 | `Assets/_Elyndor/Art/External/Environment/Finsterwald/` | unregelmäßig, moosfähig | 500–4.000 | primitive Collider; LOD0-2 | Poly Haven | CC0 mit Quellnachweis | 0 |
| Waldlaub, Farne, Pilze | Regenerationsvergleich | P1 | `Assets/_Elyndor/Art/External/Vegetation/Finsterwald/` | lesbare Formen, keine Fotorealismus-Inseln | 100–2.000 je Cluster | meist ohne Collider; LOD/Crossfade | ambientCG / Poly Haven | CC0 mit Quellnachweis | 0 |
| Holzbalken, Seil- und Plankenmaterialien | Brückenrätsel | P0 | `Assets/_Elyndor/Art/External/Props/Bridge/` | verwittert, reparierbar lesbar | 300–2.500 | Box/Capsule; LOD0-1 | Poly Haven / ambientCG | CC0 mit Quellnachweis | 0 |
| Dezente UI-Symbole | Watch-Verfügbarkeit, Interaktion | P1 | `Assets/_Elyndor/UI/Icons/MemoryWatch/` | geometrisch, kontraststark, nicht diegetisch dominant | n. a. | n. a.; Vektor/PNG-Varianten | Kenney | CC0 bzw. konkrete Kenney-Lizenz dokumentieren | 0 |
| Wald-, Wasser- und Nacht-Ambiences | Zonen- und Regenerationsklang | P1 | `Assets/_Elyndor/Audio/Ambience/Finsterwald/` | organisch, loopfähig, ohne erkennbare Melodie | n. a. | n. a.; Loop-/Lautheitsprüfung | Poly Haven falls passend, sonst manuell | CC0; keine unklare Sample-Lizenz | 0 |

## Später individuell über Meshy zu erzeugen

Generierung erst nach schriftlicher Freigabe des jeweiligen Asset-Pakets. Die maximale Versuchszahl ist eine harte Kostenobergrenze; Blockout und Review erfolgen vor Textur- oder Variantenläufen.

> **Nachtrag 16.08.2026.** Der Wurzelstreifer ist stattdessen im Haus in Blender
> gebaut worden — siehe `Technical/WURZELSTREIFER_ASSET.md`. Das war keine
> Umgehung der Freigabe, sondern ihre Folge: eine Meshy-Generierung ist ein
> kostenpflichtiger Lauf und braucht eine ausdrückliche menschliche Freigabe je
> Lauf, die nicht vorlag. Eigene Modellierung nennt der Konzeptentwurf
> ausdrücklich als gleichwertige Alternative.
>
> Der Phase-1-Deckel von 13 Versuchen ist damit **unangetastet**; für die
> verbleibenden vier Assets stehen weiterhin alle 13 zur Verfügung.

| Asset | Zweck | Prio | Zielordner | Stil | Polycount-Ziel | Collider / LOD | Quelle | Lizenzanforderung | max. Meshy-Versuche |
|---|---|---:|---|---|---:|---|---|---|---:|
| ~~Wurzelstreifer~~ **erledigt, ohne Meshy** | normaler mobiler Gegner | P0 | **`Assets/_Elyndor/Art/Enemy/Finsterwald/ELY_Enemy_Wurzelstreifer/`** | niedriger Waldkörper, Holz/Fell, asymmetrisch, originell | **3.558 gemessen** (Budget 12k–20k) | CharacterController, keine Trefferzonen nötig; LOD offen | **im Haus in Blender 5.1.2** | projekteigen, Sidecar-Datei vorhanden | **0 von 3 verbraucht** |
| Echohüter | normaler Flächenkontroll-Gegner | P0 | `Assets/_Elyndor/Art/Meshy/Enemies/EchoWarden/` | verwachsene Stein-Wurzel-Silhouette, kein Humanoid-Abklatsch | 15k–24k | primitive Verbundcollider; LOD0-2 | Meshy, eigener Prompt/Concept Brief | kommerzielle Nutzungsrechte und Generierungsprotokoll | 3 |
| Namenloser Hüter | Abschlussgegner | P0 | `Assets/_Elyndor/Art/Meshy/Enemies/NamelessGuardian/` | hirschartige Lesbarkeit, gebrochene Geweihwurzel, nicht realweltlich | 25k–40k | Capsule/Body-Hitboxes; LOD0-3 | Meshy, eigener Prompt/Concept Brief | kommerzielle Nutzungsrechte und Generierungsprotokoll | 2 |
| Link-Spielmodell | sichtbarer Begleiter und Sitzanimationen | P1 | `Assets/_Elyndor/Art/Meshy/Characters/Link/` | kleine charakteristische Eule, klare Silhouette, Elyndor-Farbpalette | 10k–18k | einfacher Capsule Collider nur falls nötig; LOD0-2 | Meshy nach freigegebenem Concept Brief | kommerzielle Nutzungsrechte; keine geschützte Figur nachahmen | 3 |
| Memory-Watch-Nahmodell | Aktivierung und Detailshots | P1 | `Assets/_Elyndor/Art/Meshy/Props/MemoryWatch/` | handgebaut, kartografisch-mechanisch, gealtertes Metall/Glas | 8k–15k | kleiner Box Collider; LOD0-2 | Meshy nach freigegebenem Orthographic Brief | kommerzielle Nutzungsrechte und Generierungsprotokoll | 2 |

## Manuell noch erforderlich

| Asset | Zweck | Prio | Zielordner | Stil | Polycount-Ziel | Collider / LOD | Quelle | Lizenzanforderung | Meshy-Versuche |
|---|---|---:|---|---|---:|---|---|---|---:|
| Finsterwald Mood-/Color-Key | verbindliche Art-Referenz | P0 | `docs/Art/Finsterwald/` | Elyndor-Painterly, kühler Schaden zu warmen Lebensakzenten | n. a. | n. a. | interne Konzeptarbeit | Urheberschaft dokumentieren | 0 |
| Gegner Turnarounds und Scale Sheets | Modell- und Animationsfreigabe | P0 | `docs/Art/Finsterwald/Enemies/` | entsprechend Encounter Plan | n. a. | Maßstab/Hitbox eingezeichnet | interne Konzeptarbeit | nur eigene Referenzen | 0 |
| Gegner-Rigs und Animationen | vollständige Encounter | P0 | `Assets/_Elyndor/Animations/Enemies/Finsterwald/` | gewichtete, klar telegraphierte Bewegung | n. a. | Root Motion je Animation festlegen | manuell/Mixamo nur nach Lizenzprüfung | Quell- und Bearbeitungsrechte dokumentieren | 0 |
| Link-Rig und Animationen | sitzen, Kopfbewegung, Start, Flug, Landung, Ruf | P1 | `Assets/_Elyndor/Animations/Characters/Link/` | natürliche Eule mit gezielter Lesbarkeit | n. a. | Flug-Hülle statt Physikmesh | manuell | eigene Arbeit oder eindeutig lizenzierte Basis | 0 |
| Memory-Echo Shader/VFX | vergangene Beziehungen sichtbar machen | P0 | `Assets/_Elyndor/VFX/MemoryWatch/` | transluzent, fragmentiert, nicht holografisch-technisch | geringes Overdraw; Budget testen | keine Collider; LOD/Distanzabschaltung | manuell in Unity | Repository-intern | 0 |
| Gegner- und Regenerations-VFX | Telegraphen, Treffer, Reinigung | P1 | `Assets/_Elyndor/VFX/Finsterwald/` | Wurzelstaub, Sporen, Lichtadern; kein Gore | budgetabhängig | keine Collider; Pooling | manuell in Unity | Repository-intern | 0 |
| Puzzle-Interaktionsprops | Anker, Kerben, Seil, sichere Planken | P0 | `Assets/_Elyndor/Prefabs/Puzzles/FinsterwaldBridge/` | funktional lesbar, verwittert | 300–3.000 je Prop | primitive Collider; LOD0-1 | manuelle Modellierung aus CC0/Bestand | abgeleitete Quellen dokumentieren | 0 |
| Regenerationsvarianten | Vorher-/Nachher-Zustände | P0 | `Assets/_Elyndor/Prefabs/Environment/Finsterwald/Regeneration/` | gleiche Geometrie, gesundere Farbe/Vegetation/Licht | Bestand plus Varianten | identische Gameplay-Collider; LOD0-2 | manuelle Prefab-/Materialarbeit | Quellen der Basen erhalten | 0 |
| Audio-Set Gegner/Watch/Link | Gameplay-Lesbarkeit und Identität | P1 | `Assets/_Elyndor/Audio/SFX/Finsterwald/` | organisch, trocken, räumlich klar | n. a. | n. a.; Lautheit/Loop/Import prüfen | eigene Aufnahme oder lizenzierte CC0-Quellen | Einzelnachweise je Datei | 0 |
| Untertitel-, Icon- und Kontrastvarianten | Barrierefreiheit | P0 | `Assets/_Elyndor/UI/Finsterwald/` | bestehende HUD-Sprache, Form plus Farbe | n. a. | n. a. | manuell | Repository-intern | 0 |

## Abhängigkeiten und Entscheidungen

- Gegner-Concept-Briefs und Maßstab müssen vor Meshy-Freigabe stehen.
- Blockout mit vorhandenen Wolf-/Hirschmodellen validiert Timing und Hitboxen, nicht den finalen Look.
- **OFFEN:** Im Repository wurde keine verbindliche Finsterwald-Konzeptgrafik als Bilddatei identifiziert; vorhandene Text-/Builder-Vorgaben reichen für Planung, nicht für finale Art-Abnahme.
- **OFFEN:** Lizenzen aller bereits importierten ThirdParty-Pakete werden vor Weiterverwendung gegen das Asset-Manifest geprüft.
- **OFFEN:** Performancebudgets für Zielhardware, Texturauflösung und maximale gleichzeitige Gegner.
- **VORSCHLAG:** Für Phase 1 gilt ein Meshy-Gesamtdeckel von 13 Versuchen über alle fünf individuellen Assets; jede Nutzung benötigt eine eigene Freigabe.
