# Blender-Asset-Pipeline

Stand: 16. August 2026

Dies ist die verbindliche Beschreibung, wie ein 3D-Asset in Elyndor entsteht,
von der Idee bis zu dem Prefab, das jemand in eine Szene zieht. Sie gilt für
alles, was ab heute neu gebaut wird.

Sie ersetzt `../ASSET_PIPELINE.md` nicht. Jenes Dokument regelt **Beschaffung**
— Quellen, Lizenzen, Herkunftsnachweise. Dieses hier regelt **Herstellung**.
Wo beide etwas zum selben Punkt sagen, gilt der strengere Wert.

---

## Werkzeugstand

| Werkzeug | Version | Anmerkung |
|---|---|---|
| Blender | **5.1.2** (`blender-v5.1-release`) | |
| Blender MCP | stdio-Server `blender-mcp`, benutzerweit registriert | steuert die laufende Blender-Instanz |
| Unity | **6000.4.5f1** | URP 17.4.0 |
| Exportformat | **FBX 7400 binär** | siehe Formatentscheidung |

**Blender MCP** heißt: Blender läuft als normales Programm, und ein Agent kann
Python darin ausführen. Das ist mächtig und entsprechend gefährlich — die
laufende Sitzung ist die eines Menschen. Regeln dafür stehen unten unter
*Regeln für den Zugriff über MCP*.

---

## Die Kette

```
CONCEPT → BLOCKOUT → BLENDER → NAMING → SCALE → PIVOT → UV/MATERIAL
        → EXPORT → UNITY IMPORT → PREFAB → QA → PRODUCTION
```

### CONCEPT
Bild oder Beschreibung, gegen die später abgenommen wird. Ohne Referenz gibt es
keine Abnahme, sondern nur Geschmack. Für Gegner und Figuren ist das ein
Creative Gate — der Game Director entscheidet, nicht die Produktion.

### BLOCKOUT
Erst Proportion und Silhouette, im Zweifel als Primitiv direkt in Unity. Ein
Blockout, das sich im Spiel falsch anfühlt, wird nicht durch Topologie besser.

### BLENDER
Modellieren. Ziel ist die niedrigste Dreieckszahl, bei der die Silhouette noch
liest — nicht die höchste, die das Budget hergibt.

### NAMING
Vergeben, bevor exportiert wird. Umbenennen nach dem Import kostet in Unity
Referenzen.

### SCALE
1 Blender-Meter = 1 Unity-Meter. Objektskalierung ist **angewendet** (1,1,1),
Rotation ebenfalls (0,0,0). Ein Objekt mit nicht angewendeter Skalierung
exportiert scheinbar richtig und verhält sich in Unity falsch, sobald es ein
Kind bekommt.

### PIVOT
Am Boden, mittig, bei (0,0,0). Ein Objekt mit Pivot im Schwerpunkt versinkt beim
Absetzen zur Hälfte im Terrain, und jeder, der es platziert, korrigiert das von
Hand — jedes Mal neu.

### UV/MATERIAL
UV0 vorhanden, ohne Überlappung, in 0..1. Auch ein untexturiertes Objekt bekommt
UVs: sie nachzuliefern heißt, das Modell erneut durch die Kette zu schicken.
Das Blender-Material ist **Vorlage**, kein Lieferbestandteil (siehe unten).

### EXPORT
Nach dem Exportstandard weiter unten. Keine Handeinstellungen im Dialog.

### UNITY IMPORT
Importeinstellungen gehören ins Skript, nicht in den Inspector. Was jemand im
Inspector klickt, steht nirgends geschrieben und ist beim nächsten Asset wieder
weg.

### PREFAB
Das Prefab ist das Lieferprodukt. Material und Collider entstehen **hier**, nicht
in der importierten Datei. Modelle werden nie direkt in Szenen gezogen.

### QA
`Elyndor/QA/Validate Model Imports` bzw. der Batchlauf. Ein Asset gilt als
fertig, wenn der Validator es kennt und grün meldet.

### PRODUCTION
Erst jetzt in eine Szene.

---

## Formatentscheidung: FBX

FBX, nicht glTF, nicht `.blend` als Asset.

- Unity importiert FBX ohne Zusatzsoftware. Ein `.blend` unter `Assets/`
  verlangt eine Blender-Installation auf **jedem** Rechner, der das Projekt
  öffnet, und importiert bei jedem Refresh neu.
- Der vorhandene Bestand ist FBX. Ein zweites Format hieße zwei Importwege.
- glTF wäre für Materialien besser — aber wir liefern gar keine Materialien aus
  der Datei aus (siehe unten), womit der Vorteil entfällt.

Die `.blend` ist **Quelldatei** und liegt außerhalb von `Assets/`.

---

## Exportstandard

Gemessen am 16.08.2026 mit Blender 5.1.2 → Unity 6000.4.5f1.

```python
bpy.ops.export_scene.fbx(
    filepath=...,
    use_selection=False,
    object_types={'MESH'},
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL',   # entscheidend, siehe unten
    global_scale=1.0,
    bake_space_transform=True,             # nur fuer statische Meshes
    axis_forward='-Z',
    axis_up='Y',
    use_mesh_modifiers=True,
    mesh_smooth_type='FACE',
    use_tspace=False,
    use_custom_props=False,
    add_leaf_bones=False,
    bake_anim=False,
    path_mode='AUTO',
    embed_textures=False,
)
```

### Die Skalierungsfalle

`apply_scale_options` entscheidet, wohin die Einheitenumrechnung geschrieben
wird. FBX zählt intern in Zentimetern; das Feld `UnitScaleFactor` sagt, wie viele
Zentimeter eine Dateieinheit ist.

| Einstellung | `UnitScaleFactor` in der Datei | 1-m-Objekt kommt in Unity an als |
|---|---:|---|
| `FBX_SCALE_NONE` | 1.0 | **0,01 m** |
| `FBX_SCALE_ALL` | 100.0 | **1,00 m** |
| `FBX_SCALE_UNITS` | 100.0 | 1,00 m |

`FBX_SCALE_NONE` ist der Vorgabewert und damit der Fehler, den man bekommt, wenn
man nichts entscheidet. Der Testfels kam beim ersten Durchlauf mit **0,007 m**
statt 1,00 m an — Faktor 100 zu klein.

Das ist der Fehler, der am teuersten wird, wenn man ihn nicht am ersten Asset
findet: Er sieht im Blender-Viewport richtig aus, im Unity-Inspector richtig aus
(Scale Factor 1, Convert Units an), und fällt erst auf, wenn jemand das Objekt
neben eine Figur stellt. Wer ihn dort bemerkt, korrigiert ihn typischerweise
über die Transform-Skalierung im Prefab — und dann stimmen Collider, Physik und
jede weitere Größenaussage im Projekt nicht mehr.

**Nie mit einer Prefab-Skalierung korrigieren. Immer neu exportieren.**

### Die Achsenfalle

Blender ist Z-oben mit −Y nach vorn, Unity ist Y-oben mit +Z nach vorn.
`axis_forward='-Z', axis_up='Y'` schreibt die Umrechnung in die Datei — aber
als **Rotation der Wurzel**, nicht in die Meshdaten. Gemessen kam der Testfels
so an:

```
Importwurzel roh: rot=(270.02, 0.00, 0.00) scale=(1.00, 1.00, 1.00)
```

Das Objekt steht damit richtig: Höhe 1,00 m auf Y, Unterkante bei y=0. Aber die
Wurzel trägt eine Drehung, die niemand dort haben will — und `270.02` statt
`270` zeigt, dass in dieser Zahl bereits gerundet wurde. Wer das Prefab später
dreht, dreht um eine schiefe Achse; wer die lokale Ausrichtung eines Kindes
ausliest, bekommt sie um 90° verdreht.

Der naheliegende Griff auf der Unity-Seite ist `bakeAxisConversion = true` am
ModelImporter. **Er löst das Problem nicht.** Gemessen:

| `bakeAxisConversion` | Rotation der Importwurzel |
|---|---|
| aus | (270.02, 0, 0) |
| an | (89.98, 0, 0) |

Unity dreht Meshdaten **und** Wurzel um dieselben 180° weiter. Das Objekt steht
in beiden Fällen richtig, die Wurzel ist in beiden Fällen schief. Die Option
verschiebt das Problem, statt es zu lösen.

**Lösung: `bake_space_transform=True` beim Export.** Blender rechnet die
Achsdrehung dann in die Meshdaten hinein, und die Datei enthält bereits
Y-oben-Geometrie. Die Importwurzel bleibt bei (0,0,0).

Die Quelldatei bleibt davon unberührt: Das Objekt steht in Blender weiterhin
aufrecht auf Z, mit Rotation (0,0,0) und Scale (1,1,1). Umgerechnet wird nur der
Export.

**Einschränkung.** Blender kennzeichnet `bake_space_transform` als
experimentell; problematisch ist sie vor allem für Rigs und Animationen, weil
sie Objekt- und Armature-Raum auseinanderzieht. Für **statische Meshes mit
angewendeten Transformationen** — also für alles, was diese Pipeline heute baut
— ist sie unauffällig und belegt.

`NOCH ZU ENTSCHEIDEN`: Wie gerigte Assets (Wurzelstreifer, Link) mit den Achsen
umgehen. Das ist am ersten gerigten Asset zu messen, nicht vorher zu raten.

**Gegenprobe im Validator:** Wurzel **und** alle Kinder müssen im Prefab
Rotation (0,0,0) und Scale (1,1,1) haben. Die Wurzel wird ausdrücklich
mitgeprüft — sie ist genau der Ort, an dem eine nicht umgerechnete Achse landet.

### Normalen

`mesh_smooth_type='FACE'` schreibt Glättungsgruppen in die Datei, Unity liest sie
mit `importNormals = Import`. Lässt man Unity die Normalen berechnen, glättet es
eine bewusst facettierte Oberfläche stillschweigend weg.

---

## Unity-Importstandard

| Einstellung | Wert | Grund |
|---|---|---|
| Scale Factor | 1 | jede Abweichung macht Größenangaben wertlos |
| Convert Units | an | rechnet `UnitScaleFactor` korrekt um |
| Normals | Import | Glättung kommt aus der Datei |
| Tangents | None | ohne Normal Map ungenutzt |
| Material Creation Mode | **None** | siehe unten |
| Import Animation / Cameras / Lights / BlendShapes | aus | statische Objekte bringen davon nichts mit |
| Read/Write | aus | spart die doppelte Meshkopie im Speicher |
| Mesh Compression | Off | verlustbehaftet; erst mit Profilingnachweis |
| Generate Lightmap UVs | aus, bis eine Szene backt | |

### Die Materialfalle

Blender schreibt sein Material in die FBX, aber ohne Shader. Unity legt daraus
ein Material des **eingebauten Standard-Shaders** an. Das Projekt rendert mit
URP — dort ist dieses Material magenta. Der Import meldet nichts; man sieht es
erst in der Szene.

Der naheliegende Weg dagegen — `materialLocation = External` plus `AddRemap` —
ist in Unity 6000.4 abgekündigt:

```
MaterialLocation.External is obsolete.
External Material Location is no longer supported.
```

Der Import lief trotzdem durch und legte zusätzlich stillschweigend einen
zweiten Materialordner an. Ein abgekündigter Pfad, der noch funktioniert, ist
technische Schuld mit Ablaufdatum.

**Regel:** `materialImportMode = None`. Aus der Datei kommt kein Material.
Das URP-Material wird in Unity angelegt und **am Prefab** zugewiesen. Die
Blender-Werte (Base Color, Roughness, Metallic) sind Vorlage und werden im
Buildskript festgehalten, damit die Herkunft der Zahlen nachvollziehbar bleibt.

---

## Namenskonvention

| Was | Muster | Beispiel |
|---|---|---|
| Modell (FBX) | `ELY_<Domäne>_<Name>[_<Variante>]` | `ELY_Test_Rock_A.fbx` |
| Mesh in Blender | wie das Modell, Suffix `_Mesh` | `ELY_Test_Rock_A_Mesh` |
| Material | `M_<Assetname>[_<Slot>]` | `M_ELY_Test_Rock_A.mat` |
| Textur | `T_<Assetname>_<Kanal>` | `T_ELY_Test_Rock_A_BC` |
| Prefab | wie das Modell | `ELY_Test_Rock_A.prefab` |
| Quelldatei | `<ASSETNAME>.blend` | `ELYNDOR_MCP_PIPELINE_TEST.blend` |

Domänen: `Env`, `Prop`, `Puzzle`, `Enemy`, `Char`, `Test`.

Der Bestand aus der Meshy-Zeit (`Meshy_AI_*`) wird **nicht** rückwirkend
umbenannt. Umbenennen bricht in Unity jede Referenz, die auf den alten Namen
zeigt; der Gewinn wäre kosmetisch. Wer eines dieser Assets ohnehin neu baut,
benennt es dabei mit um.

---

## Ordnerstruktur

```text
Art_Source/<AssetName>/                       # ausserhalb von Assets/
    <ASSETNAME>.blend

Assets/_Elyndor/Art/<Domäne>/<RegionOderSystem>/<AssetName>/
    <AssetName>.fbx
    M_<AssetName>.mat
    <AssetName>.prefab
    Textures/
```

`Art_Source/` liegt bewusst außerhalb von `Assets/`: Unity würde eine `.blend`
sonst bei jedem Refresh durch Blender importieren, und eine Quelldatei ist kein
Spielasset.

`.blend` und `.fbx` sind in `.gitattributes` als `binary` markiert. Eine als Text
behandelte Binärdatei überlebt eine Zeilenendenumwandlung nicht, und der Schaden
fiele erst beim Import auf.

---

## Regeln für finale Assets

Ein Asset ist fertig, wenn **alles** davon gilt:

1. Scale angewendet (1,1,1), Rotation angewendet (0,0,0).
2. Pivot am Boden, mittig, bei (0,0,0).
3. Größe in Metern stimmt mit der Konzeptangabe überein.
4. Geometrie wasserdicht manifold, keine losen Vertices oder Kanten, keine
   entarteten Flächen, keine Innenflächen.
5. Normalen zeigen nach außen und sind normiert.
6. UV0 vorhanden, in 0..1, ohne unbeabsichtigte Überlappung.
7. Dreiecksbudget eingehalten (siehe `../ASSET_PIPELINE.md`).
8. Materialslots im Budget, jedes Material URP.
9. Keine fehlenden Verweise, keine fehlenden Skripte.
10. Collider bewusst gewählt: Primitiv vor MeshCollider. Ein konvexer
    MeshCollider ist in Unity auf 255 Dreiecke begrenzt.
11. Rig und Animationsclips nur, wenn das Asset sie wirklich braucht.
12. Im `ModelImportValidator` eingetragen und grün.

Punkt 12 ist kein Formalismus: Ein Asset, das kein Werkzeug prüft, ist ein Asset,
dessen Zustand niemand kennt, sobald jemand anderes es anfasst.

---

## Regeln für KI-generierte Assets

Gilt für Meshy, Hyper3D/Rodin, Hunyuan3D und alles Vergleichbare.

- **Generierung ist ein Freigabepunkt, kein Arbeitsschritt.** Der Bedarf wird
  benannt, das Paket freigegeben, erst dann wird generiert.
- **Kostenpflichtige Dienste erfordern eine ausdrückliche Freigabe pro Lauf.**
  Guthaben und erwarteten Verbrauch vorher prüfen. Kein Agent startet einen
  bezahlten Lauf selbstständig.
- Maximale Versuchszahl je Asset steht in `../FINSTERWALD_ASSET_REQUIREMENTS.md`
  und ist eine harte Kostenobergrenze.
- Prompt, Modell, Generierungs-ID, Datum und die eingeräumten Nutzungsrechte
  werden protokolliert. Der API-Key nie.
- Das Ergebnis ist **Rohmaterial**, kein Asset. Es läuft ohne Ausnahme durch
  dieselbe Kette: Blender → Aufräumen → Retopologie falls nötig → Scale/Pivot →
  Export → Import → Prefab → QA.
- Typische Fehler generierter Modelle, die dabei zu prüfen sind: Pivot im
  Schwerpunkt, willkürlicher Maßstab, nicht angewendete Transformationen,
  Normalen nach innen, überlappende UVs, ein Vielfaches des nötigen Polycounts,
  Materialslots ohne Inhalt.
- Wenn die Nutzungsrechte nicht eindeutig sind, wird das Asset nicht verwendet.
  Unklar heißt nein.

---

## Regeln für externe Assets und Lizenzen

Es gilt `../ASSET_PIPELINE.md` und `../EXTERNAL_ASSET_POLICY.md`. Ergänzend:

- Über Blender MCP **nichts** aus Poly Haven, Sketchfab, Hyper3D oder Hunyuan
  laden. Diese Dienste sind über die MCP-Brücke erreichbar; die Erreichbarkeit
  ist keine Freigabe. Beschaffung läuft über den Beschaffungsablauf, damit
  Herkunft und Lizenz belegt sind.
- Ein kostenloser Download ist keine Lizenzfreigabe.
- Jedes externe Asset braucht das Herkunftsmanifest aus `../ASSET_PIPELINE.md`
  (ID, Name, Autor, URL, Lizenz, Abrufdatum, Dateien, Änderungen, Einsatz).
- Unveränderte Fremdpakete bleiben unter `Assets/ThirdParty/`. Was wir daraus
  ableiten, liegt unter `_Elyndor` und nennt seine Herkunft.
- Bei unklarer Lizenz wird nicht importiert, sondern gefragt.

---

## Regeln für den Zugriff über MCP

Blender MCP führt beliebigen Python-Code in einer **laufenden Blender-Sitzung**
aus. Das ist die Sitzung eines Menschen.

- Vor dem ersten schreibenden Schritt den Zustand prüfen: Dateipfad, geänderte
  Daten, vorhandene Objekte.
- Eine bestehende Datei wird nie überschrieben. Neue Arbeit kommt in eine neue
  Datei.
- Vor einem Schritt, der die Sitzung leert (`read_homefile`), wird der aktuelle
  Stand mit `save_as_mainfile(copy=True)` gesichert. `copy=True` schreibt die
  Sicherung, ohne den Dateipfad der Sitzung zu ändern.
- Kein Zugriff auf Assets, die nicht zum eigenen Auftrag gehören.
- Bevorzugt `bmesh` statt `bpy.ops`: Operatoren hängen am UI-Kontext und
  verhalten sich je nach aktivem Bereich unterschiedlich.
- Zufall wird mit festem Seed gesetzt, damit ein Asset reproduzierbar bleibt.

---

## Automatisierte QA

`ModelImportValidator` (`Elyndor/QA/Validate Model Imports`) prüft die
eingetragenen Modelle. Er prüft ausdrücklich **nicht** den gesamten Bestand: Die
Modelle aus der Meshy-Zeit sind vor diesem Standard entstanden, und ein Gate, das
am ersten Tag rot ist, wird ignoriert statt befolgt. Die Liste wächst pro Asset,
das den Standard nachweislich erfüllt.

### Was heute automatisch geprüft wird

| Prüfung | Art | Wo gemessen |
|---|---|---|
| Scale Factor / Convert Units | Gate | ModelImporter |
| Rotation und Scale der Hierarchie | Gate | Prefab-Instanz |
| Pivot an der Unterkante | Gate | Renderer-Bounds |
| Höhe im erwarteten Bereich | Gate | Renderer-Bounds |
| Dreiecksbudget | Gate | Mesh |
| Normalen vorhanden und normiert | Gate | Mesh |
| UV0 vorhanden | Gate | Mesh |
| Materialslots im Budget | Gate | Prefab |
| Material ist URP | Gate | Prefab |
| Leere Materialslots | Gate | Prefab |
| Unaufgelöste Importverweise | Gate | ModelImporter |
| Fehlende Skripte | Gate | Prefab |
| Rig/Clips bei statischen Objekten | Gate | Modell + Prefab |
| Dateigröße | Gate | Datei |
| UV-Wertebereich | Bericht | Mesh |
| Collider und MeshCollider-Tauglichkeit | Bericht | Prefab |

### Was bewusst noch nicht automatisiert ist

- **UV-Überlappung.** Sinnvoll, aber nur mit einer echten Flächenberechnung
  aussagekräftig. Eine billige Näherung würde falsch Alarm schlagen und damit
  denselben Schaden anrichten wie ein rotes Gate am ersten Tag.
- **LOD-Pflicht.** Es gibt noch kein Asset mit LODs und kein belastbares
  Performancebudget. Eine Regel ohne Zielhardware wäre geraten.
- **Fehlende Texturen.** Der Validator erkennt unaufgelöste *Importverweise*.
  Eine nicht zugewiesene Textur ist davon nicht unterscheidbar, solange kein
  Materialstandard sagt, welche Kanäle Pflicht sind.
- **Lizenz-/Herkunftsmetadaten.** Das Herkunftsmanifest steht heute in
  Markdown. Automatisch prüfbar wird es erst, wenn es maschinenlesbar neben dem
  Asset liegt. Das ist der nächste sinnvolle Schritt und bewusst noch nicht
  gebaut — solange nur ein Asset diesem Standard folgt, prüfte das Werkzeug sich
  selbst.
- **Silhouette und Stil.** Nicht automatisierbar. Creative Gate.

---

## Verwandte Dokumente

- `../ASSET_PIPELINE.md` — Beschaffung, Lizenzen, Herkunftsmanifest
- `../EXTERNAL_ASSET_POLICY.md` — Umgang mit Fremdmaterial
- `../FINSTERWALD_ASSET_REQUIREMENTS.md` — Bedarf je Asset, Budgets, Freigaben
- `QA_GATES.md` — wie geprüft wird
- `CODING_STANDARD.md` — für die Editor-Werkzeuge
