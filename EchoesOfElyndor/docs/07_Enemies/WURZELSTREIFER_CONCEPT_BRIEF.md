# Wurzelstreifer — Konzeptentwurf für das finale Asset

Stand: 15. August 2026
Status: `FREIGEGEBEN` als Art-Direction-Vorgabe.

> **Nachtrag 16.08.2026 — das finale Modell steht.** Es ist im Haus in Blender
> gebaut worden, nicht über Meshy; keiner der drei freigegebenen Meshy-Versuche
> wurde verbraucht. Der technische Stand steht in
> `../Technical/WURZELSTREIFER_ASSET.md`: Quelldatei, Exportweg, Unity-Pfade,
> gemessene Maße, Rig- und Animationsumfang.
>
> Das Asset ist **technisch integriert** und steht auf der Kleinen Lichtung.
> Die **finale künstlerische Abnahme ist erteilt** (16.08.2026, Game Director:
> „passt erstmal so"): Die In-Game-Darstellung ist für den gegenwärtigen
> Vertical-Slice-Stand freigegeben, Detailgrad, Materialfarben und Türkiston
> brauchen dafür keine Änderung. Die Freigabe gilt dem Stand auf Merge-Commit
> `08beca9`. Sie ist eine menschliche Entscheidung; kein grünes Gate im Projekt
> hat sie belegt. Was dieser Entwurf beschreibt, bleibt die Vorgabe.
>
> Der Blockout mit dem Quaternius-Wolf bleibt als Werkzeug erhalten
> (`Elyndor/Setup/Wurzelstreifer-Blockout bauen`); in der Szene steht er nicht
> mehr.

Dieser Entwurf beschreibt, wie der Wurzelstreifer am Ende aussehen soll. Er
entsteht am Ende von P1.5, weil das Gameplay zuerst mit einem Blockout
überzeugen sollte: Proportion, Timing und Hitboxen sind jetzt gemessen und
nicht mehr geraten. Erst damit lohnt sich ein Hero-Asset.

**Der Blockout ist nicht die Art Direction.** In der Szene steht derzeit der
vorhandene Quaternius-Wolf (CC0) — ausschließlich für Größe, Bewegung,
Hitboxen, Telegraph, Kamera, Encounter-Abstand und Timing.

---

## 1. Was er ist

Ein kleines Waldtier, dessen Ortsgedächtnis von wiederholten Fluchtspuren
überlagert wurde. Rinde und türkise Bruchlinien sind an ihm **gewachsen**,
nicht angelegt.

Er ist kein Dämon, kein Untoter, keine Rüstung und kein Monster. Er ist ein
Tier, dessen Erinnerung beschädigt wurde.

Der erste Blick soll erzeugen:

> „Mit diesem Wesen stimmt etwas nicht."

und ausdrücklich nicht:

> „Hier kommt das nächste Monster."

---

## 2. Silhouette und Proportion

Nur die **Bewegungslogik** ist grob tierartig lesbar. Er ist kein Wolf mit
Baumtextur.

| Maß | Ziel |
|---|---|
| Schulterhöhe | 0,85–0,95 m |
| Körperlänge | 1,6–1,8 m |
| Vorderkörper | etwas kräftiger und höher als der Hinterkörper |
| Silhouette | asymmetrisch — links und rechts nicht spiegelgleich |

**Merkmale:**

- niedriger, schneller Vierbeiner
- schlanke Beine, aber von Wurzelsträngen umwachsen
- Rücken aus unregelmäßigen Rindenplatten
- keine normale Fell-Silhouette
- kein klassischer Wolfsschwanz; hinten ein geteilter, wurzelartiger Fortsatz
- Kopf langgezogen, aber nicht eindeutig Wolf oder Hund
- Gesicht teilweise von Rinde geschlossen

**Ausdrücklich nicht:** dämonisch, blutig, untot, generisches Monster,
überladen mit Hörnern, WoW-artige Panzerplatten, Sci-Fi, Neon-Hologramm.

### Ansichten

**Vorderansicht.** Schmaler als erwartet — die Masse liegt in der Tiefe, nicht
in der Breite. Der Kopf sitzt tief zwischen den Schultern. Die
Gesichtsrinde ist von vorn am deutlichsten: eine geschlossene, unregelmäßige
Fläche, aus der nur ein Auge oder eine Andeutung davon hervorsieht. Die
Asymmetrie muss hier zuerst auffallen — eine Schulter höher als die andere.

**Seitenansicht.** Die Leitansicht. Die Rückenlinie fällt vom kräftigen
Vorderkörper zum leichteren Hinterkörper ab. Rindenplatten liegen wie
verschobene Schuppen darauf, nie in einem Raster. Der geteilte Wurzelfortsatz
hinten hängt tief und schleift beinahe. Die Beine sind erkennbar Beine, aber
von Wurzelsträngen umwickelt, die an den Gelenken dichter werden.

**Rückansicht.** Der geteilte Fortsatz ist das Erkennungsmerkmal: zwei
ungleich lange Wurzelstränge, nicht symmetrisch. Der Hinterkörper wirkt
schmal und leicht — der Gegner sieht von hinten fliehend aus, selbst wenn er
steht. Das unterstützt seinen Rückzug erzählerisch.

---

## 3. Material und Farbe

**Grundmaterial:**

- dunkles, feuchtes Holz
- graubraune Rinde
- einzelne natürliche Fell- und Faserreste
- moosige, gedeckte Akzente

**Memory-Schäden: Türkis, sparsam.**

Türkis darf den Gegner **nicht dauerhaft beleuchten**. Im Normalzustand sind
nur feine Risse zu sehen. Stärker sichtbar wird es ausschließlich bei:

| Anlass | Intensität |
|---|---|
| Ruhe | feine Linien, kein Leuchten |
| Wahrnehmung / Alarm | schwach |
| Telegraph | ansteigend bis deutlich |
| Treffer | kurzer heller Blitz |
| Heavy Stagger | starkes Aufbrechen der Linien |
| Niederlage / Beruhigung | erlischt langsam, Risse schließen sich |

Damit bleibt Türkis bedeutungsvoll.

### Was das finale Asset dafür liefern muss

**Eine Emissionsmaske auf den Bruchlinien.** Der Blockout hat keine — deshalb
steht seine Grundhelligkeit auf 0. Ohne Maske würde jede Grundhelligkeit den
ganzen Körper gleichmäßig einfärben, also genau das, was oben ausgeschlossen
ist. Die feinen Risse des Normalzustands sind erst mit dem finalen Asset
darstellbar; die Ansteuerung dafür steht bereits
(`WurzelstreiferFeedback`, schreibt `_EmissionColor` über einen
MaterialPropertyBlock).

---

## 4. VFX

Nur einfache Gameplay-VFX. Kein VFX-System.

- kleine Rinden- und Holzpartikel bei Treffer *(im Blockout vorhanden)*
- kurze türkise Resonanzverstärkung während des Telegraphs *(vorhanden)*
- sehr dezente bodennahe Erinnerungsschliere beim Sprungangriff *(offen)*
- stärkeres Aufbrechen der türkisen Linien beim Heavy Stagger *(vorhanden,
  wird mit der Maske erst richtig sichtbar)*
- Niederlage ohne Blut

Keine Explosion, keine Magiekreise, keine Dauer-Partikelwolke, keine
Hologrammoptik.

---

## 5. Rig und Animation

Generic-Rig, Vierbeiner. Root Motion wird **nicht** verwendet — die Bewegung
kommt aus `EnemyMovement`; der Animator wird von `EnemyAnimationDriver`
ausgewählt und enthält bewusst keine Übergangsbedingungen.

Der Telegraph ist wichtiger als perfekte Animation.

| Zustand | Anforderung | Blockout heute |
|---|---|---|
| Idle / Lauschen | ruhiges Atmen, Ohren-/Kopfbewegung | `Idle` |
| Walk / Trot | Fortbewegung | `Walk` |
| Seitlicher Schritt | **fehlt** — eigener Strafe-Zyklus nötig | ersatzweise `Walk` |
| Attack Telegraph | **0,7 s** klar lesbares Absenken der Schulter | ersatzweise `Idle_2_HeadLow` |
| Kurzer Sprungbiss | schnell, mit sichtbarem Absprung | `Attack` |
| Light Hit Reaction | **0,18 s** Zucken | `Idle_HitReact_Left` |
| Heavy Stagger | **0,8 s** — eigenes Straucheln nötig | ersatzweise `Idle_HitReact_Right` |
| Rückzug | Wegbewegung, gesenkter Kopf | ersatzweise `Gallop` |
| Niederlage / Beruhigung | Haltung sinkt, kein Sterbekrampf, kein Blut | `Death` |

**Zwei Zustände brauchen im finalen Asset einen eigenen Clip:** der seitliche
Schritt und das schwere Straucheln. Beide greifen heute auf einen ähnlichen
Clip zurück; das ist für einen Blockout tragbar, aber nicht für den fertigen
Gegner — gerade das Straucheln ist die sichtbare Belohnung für einen schweren
Angriff.

Der Telegraph verdient einen eigenen Clip statt der geliehenen
Kopf-tief-Pose: Die Schulter soll sich sichtbar laden, damit die 0,7 s auch
ohne die türkise Verstärkung lesbar bleiben.

---

## 6. Collider und Hitboxen

Der Blockout hat gemessene Werte, die übernommen werden können:

| Größe | Wert | Begründung |
|---|---|---|
| Körper-Collider | `CharacterController`, Höhe 0,9 m, Radius 0,45 m, Mitte y 0,45 m | flach genug, dass Aren über ihn hinwegsieht |
| Step Offset | 0,35 m | Wurzeln und Steine der Lichtung |
| Slope Limit | 50° | Uferböschung |
| Trefferreichweite | 2,2 m | eine Rolle trägt rund 4 m und verlässt sie sicher |
| Halteabstand | 1,8 m | knapp innerhalb der Reichweite |

Keine komplexen Mesh-Collider. Für einzelne Trefferzonen (Kopf, Rücken) gibt
es noch keinen Bedarf: der Damage Contract kennt bisher nur `Normal` und
`Blocked`.

---

## 7. Größenvergleich mit Aren

Aren ist ein aufrechter Mensch; der Wurzelstreifer reicht ihm etwa bis zur
Hüfte. Er ist **niedrig und lang**, nicht klein und gedrungen.

```
        Aren                    Wurzelstreifer
         ▲  ~1,80 m
         │                       ▲  0,85–0,95 m Schulter
         │                       │
         │              ┌────────┴────────────┐
         │              │   1,6–1,8 m lang    │
        ─┴──────────────┴─────────────────────┴──
```

Er muss im hohen Gras der Lichtung noch erkennbar sein — die Rückenlinie
liegt bewusst über der Grashöhe.

---

## 8. Was der Blockout bereits belegt hat

Diese Werte sind gemessen, nicht angenommen, und sollen das finale Asset
binden:

- 0,7 s Telegraph ist lang genug, um darauf zu antworten
- 1,2 s Erholung ist ein echtes Gegenfenster
- 2,2 m Reichweite lässt sich durch eine Rolle sicher verlassen
- 40 LP und 10 Schaden ergeben einen Kampf, der Unterricht bleibt
- 12 m Bindung an die Lichtung hält die Begegnung an ihrem Ort

---

## 9. Offen

Stand 16.08.2026, nach dem Bau des finalen Modells und der Art-Freigabe.

**Erledigt:**

- ~~Finale künstlerische Abnahme des Modells~~ — am 16.08.2026 vom Game
  Director erteilt („passt erstmal so"), für den gegenwärtigen
  Vertical-Slice-Stand auf Merge-Commit `08beca9`. Detailgrad, Materialfarben
  und Türkiston brauchen für diesen Stand keine Änderung.
- ~~Beauftragung des finalen Modells~~ — eigene Modellierung in Blender,
  ohne Meshy-Credits.
- ~~Emissionsmaske für die Bruchlinien~~ — gelöst über einen zweiten
  Materialslot statt über eine Textur. Begründung in
  `../Technical/WURZELSTREIFER_ASSET.md`; ob es dabei bleibt, entscheidet sich,
  sobald ein Texturstandard steht.
- ~~Eigene Clips für seitlichen Schritt, Telegraph und schweres Straucheln~~ —
  alle drei liegen als eigene Clips vor, mit den hier festgelegten Dauern
  0,7 s und 0,8 s.

**Weiterhin offen:**

- Bodennahe Erinnerungsschliere beim Sprungangriff.
- Audio: Lauschen, Telegraph, Biss, Treffer, Beruhigung.
- Ein Vorwärtsgang wäre animiert vorhanden (`Trab`), wird aber von keinem
  Zustand gewählt — die Zustandsmaschine kennt heute keinen Vorwärtsschritt.
  Ihn zu verdrahten ist eine Gameplay-Entscheidung, keine Assetfrage.

Siehe `FINSTERWALD_ENCOUNTER_PLAN.md` für die Rolle im Encounter und
`FINSTERWALD_ASSET_REQUIREMENTS.md` für den Assetbedarf des Slice.
