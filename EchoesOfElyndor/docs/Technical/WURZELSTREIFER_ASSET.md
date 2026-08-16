# Wurzelstreifer — technischer Assetstand

Stand: 16. August 2026

Dies ist die technische Beschreibung des fertigen Wurzelstreifer-Assets: woher
es kommt, wie es entsteht, wo es liegt und was an ihm gemessen ist.

Die gestalterische Vorgabe steht in `../07_Enemies/WURZELSTREIFER_CONCEPT_BRIEF.md`
(Status `FREIGEGEBEN`). Der Herstellungsstandard steht in
`BLENDER_ASSET_PIPELINE.md`. Wo dieses Dokument etwas anderes sagt als eines
von beiden, ist dieses Dokument falsch.

> **Freigabestand:** Das Asset ist **technisch integriert**. Die finale
> künstlerische Abnahme steht **aus** und ist ein menschliches Gate. Kein Wert
> in diesem Dokument und kein grünes Gate im Projekt behauptet etwas über die
> gestalterische Qualität — sie ist nicht automatisch prüfbar und wurde nicht
> geprüft.

---

## Warum dieser Gegner

Der Wurzelstreifer ist der einzige Gegner des Projekts, für den eine
**freigegebene** visuelle Vorgabe vorliegt. `FINSTERWALD_ASSET_REQUIREMENTS.md`
führt ihn als P0 und als ersten von drei Gegnern; `WURZELSTREIFER_CONCEPT_BRIEF.md`
ist der einzige Concept Brief mit Status `FREIGEGEBEN`, mit Maßen,
Materialregeln, Rig- und Animationsanforderungen sowie gemessenen
Collider-Werten. Echohüter und Namenloser Hüter haben keinen solchen Brief.

Er steht außerdem bereits im Vertical Slice: `WurzelstreiferBuilder` setzt seit
P1.5 einen Blockout auf die Kleine Lichtung, und der Konzeptentwurf ist
ausdrücklich als dessen Nachfolger geschrieben.

---

## Quelle und Exportweg

| Was | Wo |
|---|---|
| Aufbauskript | `Art_Source/Wurzelstreifer/build_wurzelstreifer.py` |
| Blender-Datei | `Art_Source/Wurzelstreifer/ELY_ENEMY_WURZELSTREIFER.blend` |
| Blender | 5.1.2, gesteuert über die Blender-MCP-Brücke |
| Export | FBX 7400 binär, `bake_space_transform=True` |

**Die Quelle ist das Skript, nicht die `.blend`.** Die `.blend` ist sein
Ergebnis und wird bei jedem Lauf neu geschrieben. Wer das Modell ändern will,
ändert das Skript; wer die `.blend` von Hand ändert, verliert die Änderung beim
nächsten Lauf.

Das ist die Umkehrung des sonst Üblichen und hat einen Grund: Jede Zahl des
Konzeptentwurfs — Schulterhöhe, Körperlänge, Telegraph-Dauer — steht im Skript
als benannte Konstante. In einer `.blend` stünde sie als Vertexposition, und
niemand könnte hinterher sagen, ob 0,89 m Absicht oder Zufall war.

Reproduzierbarkeit: fester Seed (`20260816`), keine Zufallszahl ohne ihn, keine
Handgriffe im Viewport. Zwei Läufe ergeben dasselbe Mesh.

### Erneut bauen

Blender starten (mit Oberfläche — der Aufbau braucht einen 3D-Viewport für das
UV-Auspacken), die MCP-Brücke verbinden, dann:

```python
PATH = r"<Projekt>/Art_Source/Wurzelstreifer/build_wurzelstreifer.py"
ns = {"__name__": "ely_build"}
exec(compile(open(PATH, encoding="utf-8").read(), PATH, "exec"), ns)

ns["build_all"](
    blend_path=r"<Projekt>/Art_Source/Wurzelstreifer/ELY_ENEMY_WURZELSTREIFER.blend",
    fbx_paths={"produktion": (
        r"<Projekt>/Assets/_Elyndor/Art/Enemy/Finsterwald/"
        r"ELY_Enemy_Wurzelstreifer/ELY_Enemy_Wurzelstreifer.fbx", True)})
```

Danach in Unity `Elyndor/Art/Wurzelstreifer-Asset aufbauen`, dann
`Elyndor/Finsterwald/Erste Begegnung platzieren`.

---

## Unity-Zielpfade

```text
Assets/_Elyndor/Art/Enemy/Finsterwald/ELY_Enemy_Wurzelstreifer/
    ELY_Enemy_Wurzelstreifer.fbx
    ELY_Enemy_Wurzelstreifer.fbx.provenance.json
    ELY_Enemy_Wurzelstreifer.controller
    M_ELY_Enemy_Wurzelstreifer.mat
    M_ELY_Enemy_Wurzelstreifer_Risse.mat

Assets/_Elyndor/Art/Enemies/Wurzelstreifer/
    M_Wurzelstreifer_Rindenstaub.mat      # Blockout und fertiges Asset teilen es

Assets/_Elyndor/Prefabs/Enemies/
    Wurzelstreifer.prefab                 # das Lieferprodukt
```

**Abweichung von `FINSTERWALD_ASSET_REQUIREMENTS.md`.** Dort steht als
Zielordner `Assets/_Elyndor/Art/Meshy/Enemies/RootStrider/`. Dieser Pfad
unterstellt eine Meshy-Generierung. Das Asset ist stattdessen im Haus in
Blender entstanden, und damit gilt die Ordnerstruktur aus
`BLENDER_ASSET_PIPELINE.md`: `Art/<Domäne>/<Region>/<AssetName>/`. Ein
`Meshy`-Ordner für ein Asset, das nie durch Meshy gelaufen ist, wäre eine
falsche Herkunftsangabe im Dateipfad.

Von den **drei** für den Wurzelstreifer freigegebenen Meshy-Versuchen wurde
**keiner** verbraucht.

---

## Gemessene Werte

Alle Zahlen aus dem `ModelImportValidator` am 16.08.2026, nicht aus der
Planung.

| Größe | Vorgabe | Gemessen |
|---|---|---|
| Schulterhöhe (höchster Punkt) | 0,85–0,95 m | **0,93 m** |
| Körperlänge über alles | 1,6–1,8 m | **1,76 m** |
| Breite | „schmaler als erwartet" | 0,42 m |
| Pivot | am Boden, mittig, (0,0,0) | Unterkante y = 0,00 |
| Blickrichtung | Unity +Z | Becken→Kopf = (0, 0, +0,62) |
| Dreiecke | ≤ 20.000 | **3.558** |
| Vertices (Unity, nach UV-Trennung) | — | 4.099 |
| Materialslots am Modell | ≤ 2 | 2 |
| Dateigröße | ≤ 2 MiB | 1.626.268 Bytes |
| UV0 | in 0..1 | u 0,006–0,994, v 0,006–0,993 |
| Normalen | vorhanden, normiert | 4.099, alle normiert |
| Knochen | 24 | 24, Wurzel `Wurzel` |
| Avatar | gültig | `ELY_Enemy_WurzelstreiferAvatar`, valid |
| Clips | 11 | 11 |

### Zum Dreiecksbudget

`FINSTERWALD_ASSET_REQUIREMENTS.md` nennt 12.000–20.000 Dreiecke. Das Modell
hat 3.558 und liegt damit deutlich **darunter**, nicht darüber.

Das ist kein verfehltes Ziel. Die Budgetangabe ist ein Zielwert für ein
generiertes Asset, das typischerweise mit einem Vielfachen des nötigen
Polycounts ankommt. Der Herstellungsstandard sagt dagegen: „Ziel ist die
niedrigste Dreieckszahl, bei der die Silhouette noch liest — nicht die höchste,
die das Budget hergibt." Ein handgebautes Modell fängt unten an. Der Validator
führt die 20.000 als Obergrenze, nicht als Sollwert.

Wo die Silhouette nach der Art-Abnahme mehr Auflösung braucht, ist Luft.

---

## Aufbau des Modells

Der Körper ist ein einziges Loft über einer durchgehenden Rücken- und
Kopfkurve. Rumpf und Kopf sind bewusst **nicht** zwei Volumen: Rindenplatten,
Bruchlinien und Beinansätze sitzen alle auf derselben Flächenparametrisierung,
und an einer Naht zwischen zwei Volumen säße ausgerechnet die Gesichtsrinde.

| Teil | Aufbau |
|---|---|
| Rumpf und Kopf | Loft, 34 Ringe × 18 Segmente, Ringe senkrecht zur Achse |
| Beine | Röhren durch Hüfte, Gelenk, Pfote; an den Gelenken dicker |
| Wurzelstränge | zwei gegenläufige Wicklungen je Bein |
| Geteilter Fortsatz | zwei ungleich lange, seitlich getrennte Stränge |
| Rindenplatten | 20 aufliegende Schalen, keine zwei gleich groß, kein Raster |
| Auge | eine flache Kuppel, rechts, genau eine |
| Bruchlinien | 16 aufliegende Bänder, 2 mm über der Oberfläche |

Asymmetrie ist gebaut, nicht behauptet: die rechte Schulter ist um 24 mm
angehoben, die linke Hüfte um 13 mm, und die vier Beine haben leicht
verschiedene Stärken und Ansatzpunkte.

---

## Materialien und die türkise Resonanz

Zwei Materialslots am Modell:

| Slot | Material | Emission |
|---|---|---|
| 0 | `M_ELY_Enemy_Wurzelstreifer` (Rinde) | **aus** |
| 1 | `M_ELY_Enemy_Wurzelstreifer_Risse` | **an**, Startwert schwarz |

**Warum zwei Slots und keine Emissionstextur.** Der Konzeptentwurf verlangt
eine „Emissionsmaske auf den Bruchlinien", damit Türkis nicht den ganzen Körper
einfärbt. `WurzelstreiferFeedback` schreibt `_EmissionColor` über einen
`MaterialPropertyBlock` auf den Renderer — und ein Property Block gilt für
**alle** Slots.

Eine Texturmaske wäre der klassische Weg. Sie bräuchte einen Texturstandard,
den das Projekt nicht hat: `BLENDER_ASSET_PIPELINE.md` führt „Fehlende
Texturen" ausdrücklich als nicht automatisierbar, „solange kein Materialstandard
sagt, welche Kanäle Pflicht sind".

Der zweite Materialslot erreicht dasselbe mit dem, was heute dokumentiert ist:
Am Rindenmaterial ist das Keyword `_EMISSION` **aus**, und URP ignoriert die
Farbe aus dem Property Block deshalb dort vollständig. Am Rissmaterial ist es
an. Es leuchten nur die Linien.

`NOCH ZU ENTSCHEIDEN`: ob es bei der Geometrielösung bleibt. Sie kostet 98
zusätzliche Flächen und erzwingt einen zweiten Drawcall; eine Maske wäre
billiger, sobald ein Texturstandard steht. Die Entscheidung gehört an den
Punkt, an dem dieser Standard entsteht, nicht hierher.

---

## Rig

Generic, Vierbeiner, 24 Knochen. Kein Humanoid: ein Vierbeiner hat in Unitys
Muskelschema keine Entsprechung, und der Konzeptentwurf legt Generic fest.

```text
Wurzel                          (Ursprung, am Boden, zeigt nach vorn)
├── Becken                      (reicht bis ans Rumpfende)
│   ├── Oberbein_HL/HR → Unterbein_HL/HR → Pfote_HL/HR
│   ├── Fortsatz_A_01 → _02 → _03
│   └── Fortsatz_B_01 → _02
└── Ruecken → Brust → Hals → Kopf → Schnauze
    └── Oberbein_VL/VR → Unterbein_VL/VR → Pfote_VL/VR
```

`V`/`H` sind vorn und hinten, `L`/`R` links und rechts aus Sicht des Tieres.

**Root Motion wird nicht verwendet.** Die Bewegung kommt aus `EnemyMovement`;
keine Animation verschiebt den Wurzelknochen waagerecht. Täte sie es, liefe das
Modell gegen seinen eigenen Transform und die Füße würden rutschen.

**Das Becken reicht bis ans Rumpfende** und nicht nur bis zur Hüfte. In der
ersten Fassung endete es bei der Hüfte; für die letzten 28 cm Rumpf waren dann
die Glieder des Wurzelfortsatzes die nächsten Knochen, und ein schwingender
Fortsatz verformte die Kruppe mit.

### Aufliegende Schalen

Rindenplatten, Augenkuppel und Bruchlinien sind eigene Schalen, die auf der
Rumpfhaut liegen, ohne mit ihr verbunden zu sein. Automatische Gewichte
(Bone Heat) behandeln sie eigenständig — und weil ihre Vertices ein paar
Millimeter weiter außen liegen, fällt die Gewichtung anders aus als bei der
Haut darunter. Beim ersten Krümmen des Rückens klaffte zwischen Platte und
Körper ein Loch.

Die **423** Vertices dieser Schalen übernehmen deshalb nach dem Häuten die
Gewichte ihres nächstgelegenen Hautvertex. Danach bewegen sie sich exakt wie
ihr Untergrund. Ohne Gewicht bleiben am Ende **0** Vertices.

---

## Animationen

Elf Clips, 50 Bilder je Sekunde.

**Warum 50 fps.** Der Konzeptentwurf legt drei Dauern auf die
Hundertstelsekunde fest: 0,7 s Telegraph, 0,18 s Flinch, 0,8 s Straucheln. Bei
50 fps sind das glatt 35, 9 und 40 Bilder. Bei 30 fps wäre der Flinch 5,4
Bilder lang, also entweder 0,167 s oder 0,200 s — eine gemessene Vorgabe, die
schon beim Anlegen der Datei verfehlt würde.

| Clip | Dauer | Schleife | Animator-Zustand |
|---|---:|---|---|
| `Idle` | 2,40 s | ja | `Idle` |
| `Lauschen` | 2,00 s | ja | `Lauschen` |
| `Schritt` | 1,00 s | ja | `Schritt` |
| `Trab` | 1,00 s | ja | — *(ungenutzt)* |
| `Lauf` | 0,60 s | ja | `Lauf` |
| `Telegraph` | 0,70 s | nein | `Telegraph` |
| `Sprungbiss` | 0,50 s | nein | `Sprungbiss` |
| `Flinch` | 0,18 s | nein | `Flinch` |
| `Stagger` | 0,80 s | nein | `Stagger` |
| `Flucht` | 0,70 s | ja | `Flucht` |
| `Niederlage` | 1,40 s | nein | `Niederlage` |

Der Animator-Controller enthält nur Zustände mit Clips und **keine Übergänge**.
Die Auswahl trifft `EnemyAnimationDriver` anhand der Zustandsmaschine; ein
Bedingungsnetz wäre eine zweite, nur im Editorfenster nachlesbare
Zustandsmaschine.

### Der seitliche Schritt

Der Konzeptentwurf führt „seitlicher Schritt" als eigenen Clip auf, den der
Blockout nicht hatte und ersatzweise mit `Walk` bediente.

Im Controller gibt `EnemyAnimationDriver.SelectState` den Zustand `Schritt`
**ausschließlich** beim Umkreisen zurück; der Vorwärtslauf läuft über `Lauf`.
`Schritt` bekommt deshalb den Seitwärtszyklus. Das ist kein Umbau der
Zustandsmaschine, sondern die Auflösung dessen, was der Zustand ohnehin tut —
am Code wurde dafür nichts geändert.

`Trab` ist der im Entwurf ebenfalls genannte Vorwärtsgang. Er wird
mitgeliefert, aber von keinem Zustand gewählt: einen Vorwärtsschritt kennt die
Zustandsmaschine heute nicht. Ihn zu verdrahten hieße, einen Zustand
hinzuzufügen, und das wäre eine Gameplay-Entscheidung.

---

## Verwendete Referenzen

**Keine externen.** Weder Concept-Art-Bilddatei noch Fremdmodell noch
generatives Modell noch kostenpflichtiger Dienst.

Das ist keine Sparsamkeit, sondern der Befund:
`FINSTERWALD_ASSET_REQUIREMENTS.md` hält fest, dass im Repository **keine
verbindliche Finsterwald-Konzeptgrafik als Bilddatei** existiert, und
`docs/Art/Finsterwald/Enemies/` mit den geforderten Turnarounds und Scale
Sheets gibt es nicht. Die einzige verbindliche Vorgabe ist der Text des
Konzeptentwurfs, und gegen dessen Zahlen und Beschreibungen ist gebaut worden.

Der Quaternius-Wolf (CC0) aus dem Blockout wurde **nicht** als Vorlage benutzt.
Er hat weder Geometrie noch Animation beigesteuert; er stand für Größe,
Timing und Hitboxen, und genau diese Werte sind aus ihm in den Konzeptentwurf
und von dort ins Modell gewandert.

---

## Offene visuelle Entscheidungen

Diese Punkte sind **menschliche Gates**. Kein Werkzeug im Projekt kann sie
beantworten, und kein grünes Gate behauptet, sie seien beantwortet.

- **Finale Art-Abnahme der Silhouette.** Liest der erste Blick als „mit diesem
  Wesen stimmt etwas nicht" und nicht als „hier kommt das nächste Monster"?
  Das ist die Kernfrage des Konzeptentwurfs und die einzige, die zählt.
- **Auflösung und Detailgrad.** 3.558 Dreiecke sind eine bewusst niedrige
  Untergrenze. Ob Kopf, Rindenplatten oder Wurzelstränge mehr Auflösung
  brauchen, entscheidet der Blick, nicht das Budget.
- **Farbwerte.** Base Color und Roughness beider Materialien stammen aus dem
  Blender-Aufbau und sind Vorschläge, keine Freigabe.
- **Der Türkiston** `(0.16, 0.72, 0.68)` steht als Vorgabewert in
  `WurzelstreiferFeedback` und wurde übernommen, nicht geprüft.
- **Ob die Bruchlinien als Geometrie bleiben** oder auf eine Texturmaske
  wechseln, sobald ein Texturstandard steht.
- **Die bodennahe Erinnerungsschliere beim Sprungangriff** aus Abschnitt 4 des
  Entwurfs fehlt weiterhin. Sie ist ein VFX und kein Modellteil.
- **Audio** — Lauschen, Telegraph, Biss, Treffer, Beruhigung — fehlt
  unverändert.

---

## Verwandte Dokumente

- `../07_Enemies/WURZELSTREIFER_CONCEPT_BRIEF.md` — die gestalterische Vorgabe
- `BLENDER_ASSET_PIPELINE.md` — Herstellungsstandard, Export, Import, QA
- `../FINSTERWALD_ASSET_REQUIREMENTS.md` — Assetbedarf des Slice
- `../FINSTERWALD_ENCOUNTER_PLAN.md` — Rolle im Encounter
- `QA_GATES.md` — wie geprüft wird
