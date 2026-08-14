# Finsterwald – Technische Arbeitspakete

## Status

Gate 0 ist seit dem 2. August 2026 abgeschlossen; die Blockade entfaellt.
Mehrere Pakete sind inzwischen ganz oder teilweise umgesetzt - der reale
Stand je Paket steht in `FINSTERWALD_VERTICAL_SLICE_STATUS.md` und geht
diesem Dokument vor. Dieses Dokument plant Arbeit; es startet keine Implementierung. Jedes Paket erhält nach Freigabe einen eigenen Feature-Branch gegen `developer`. Szenenänderungen sind ausschließlich in ausdrücklich dafür vorgesehenen Integrationspaketen zulässig.

## Gemeinsame Regeln

- Basis ist jeweils der aktuelle, freigegebene `developer`.
- Keine parallelen Änderungen an demselben zentralen Player-, HUD-, Inventory- oder Szenenbestand.
- Automatisch mindestens: Unity-Batch-Kompilierung, vorhandene relevante Validatoren, `git diff --check`, Missing-Script-Prüfung und Prüfung doppelter zentraler Komponenten.
- Manuelle Unity-Prüfungen sind Abnahmepflicht und werden nicht durch Batch-Erfolg ersetzt.
- Lore- oder Designabweichungen werden vor Implementierung als `OFFEN` oder `VORSCHLAG` entschieden.

## P1.1 Movement und Kamera

- **Branch:** `feature/finsterwald-movement-camera`
- **Scope:** bestehende Bewegung und Kamera für Waldwege, Steigungen, Brücke und enge Ruinen kalibrieren; Eingaben zentralisieren, ohne neue Traversalmechanik.
- **Betroffene Systeme:** `PlayerInputReader`, `PlayerMovement`, `CameraFollow`, Input Actions.
- **Gesperrt:** Combat, Vitals, Inventory, HUD, Quickslots, Memory, Finsterwald-Szene.
- **Abhängigkeiten:** keine offenen; Gate 0 ist abgeschlossen.
- **Automatisch:** Batch-Kompilierung; EditMode-Tests für Eingabezustände; Diff-/Komponentenprüfung.
- **Manuell:** Tastatur/Maus und Gamepad; Hang, Kollision, Rollen, Kameraclipping, 15 Minuten Dauerlauf.
- **Abnahme:** stabile 60-Hz-unabhängige Bewegung, keine verlorenen Eingaben, lesbare Kamera an allen Blockout-Profilen.

## P1.2 Vitals- und Ausdaueranbindung

- **Branch:** `feature/finsterwald-vitals-stamina`
- **Scope:** Sprinten, Rollen, Blocken und Angriffe an vorhandene `PlayerVitals`-Ausdauer anbinden; Regeneration und Nullzustände definieren.
- **Betroffene Systeme:** PlayerVitals, Movement, Combat-Verträge, bestehende HUD-Bindung.
- **Gesperrt:** Inventory, Quickslots, Memory, Gegner, Szenen.
- **Abhängigkeiten:** P1.1.
- **Automatisch:** Unit-/EditMode-Tests für Kosten, Regeneration, Unterbrechung und Grenzwerte; Batch-Kompilierung.
- **Manuell:** HUD-Synchronität, Spam-Eingaben, Ausdauer null, Pause/Fokusverlust.
- **Abnahme:** keine Aktion umgeht Kosten; UI und Laufzeitwert stimmen überein; keine negativen Werte.

## P1.3 Kampfbasis

- **Branch:** `feature/finsterwald-combat-foundation`
- **Scope:** leichte/schwere Angriffe, Block und Treffervertrag auf zentralen Input und Vitals ausrichten; klare Telegraph-/Trefferfenster vorbereiten.
- **Betroffene Systeme:** PlayerCombat, IDamageable, Waffen-Prefab, Vitals, Input.
- **Gesperrt:** Gegner-KI, Inventory-Inhalte, Quickslots, Memory, HUD-Layout, Szenen.
- **Abhängigkeiten:** P1.1, P1.2.
- **Automatisch:** Tests für Angriffscooldown, Ausdauerkosten, Blockschaden und genau einen Treffer pro Fenster; Batch-Kompilierung.
- **Manuell:** Controllergefühl, Reichweite, Blickrichtung, Blockfeedback, alte Klinge.
- **Abnahme:** deterministische Treffer, kein Mehrfachschaden durch Collider, vollständige Steuerbarkeit nach Animationen.

## P1.4 Gegner-KI-Grundlage

- **Branch:** `feature/finsterwald-enemy-ai`
- **Scope:** Wahrnehmung, Zustände Idle/Investigate/Chase/Attack/Recover/Return/Stagger/Defeated, Leash und Debugsicht.
- **Betroffene Systeme:** neue Enemy-Core-Komponenten, Navigation, Damage-Verträge.
- **Gesperrt:** Player-Steuerung, Inventory, Memory, HUD/Quickslots, Finsterwald-Szene.
- **Abhängigkeiten:** P1.3.
- **Automatisch:** Zustandsmaschinen-Tests; Sicht-/Hörgrenzen; Leash und verlorenes Ziel; Batch-Kompilierung.
- **Manuell:** Hindernisse, mehrere Gegner, Rand der Nav-Fläche, Performance.
- **Abnahme:** keine Endlosschleifen oder Angriffe außerhalb Reichweite; Rückkehr und Stagger reproduzierbar.

## P1.5 Erster Gegnertyp – Wurzelstreifer

- **Branch:** `feature/finsterwald-root-strider`
- **Scope:** Wurzelstreifer mit Blockout-Modell, Biss/Sprung, Trefferreaktion, Audio-/VFX-Hooks und Datenprofil.
- **Betroffene Systeme:** Enemy AI, Combat, Gegnerdaten, Blockout-Prefab.
- **Gesperrt:** zweiter Gegnertyp, Mini-Boss, Player/HUD/Inventory/Memory, Hauptszene.
- **Abhängigkeiten:** P1.4; Encounter-Plan.
- **Automatisch:** Datenvalidierung, Angriffs- und Cooldowntests, Batch-Kompilierung, Prefab-Missing-Script-Prüfung.
- **Manuell:** Solo-Encounter auf isolierter Testfläche, Lesbarkeit ohne Audio, 30 Wiederholungen ohne Softlock.
- **Abnahme:** Werte im vorläufigen Rahmen; beide Angriffe erkennbar und vermeidbar; Niederlage beendet Aggression sauber.

## P1.6 Encounter-Erweiterung und Namenloser Hüter

- **Branch:** `feature/finsterwald-encounter-roster`
- **Scope:** Echohüter und Abschlussgegner auf derselben KI-Basis; Phasen-/Flächenkontrolle; Blockout-Balancing.
- **Betroffene Systeme:** Enemy AI, Combat, Gegnerdaten, Blockout-Prefabs.
- **Gesperrt:** Player/HUD/Inventory/Memory, finale Szene, finale Art-Assets.
- **Abhängigkeiten:** P1.5.
- **Automatisch:** Zustands-, Phasen-, Schadens- und Datenvalidierung; Batch-Kompilierung.
- **Manuell:** Einzel- und Mischbegegnung, Boss-Dauer 3–5 Minuten, Kamera/Telegraphen, nicht-tödliche visuelle Auflösung.
- **Abnahme:** Rollen unterscheiden sich klar; Abschluss ist fordernder, aber ohne Grind oder unlesbare Ketten bestehenbar.

## P1.7 Memory-Watch-Rätsel

- **Branch:** `feature/finsterwald-memory-bridge-puzzle`
- **Scope:** Zustandsmaschine, Watch-Beobachtung, drei Anker, reversible Fehlversuche und Brückenfreigabe gemäß Rätseldokument; zunächst isolierte Testszene.
- **Betroffene Systeme:** MemorySite/Echo/Session, Interaction, Watch-UI, Puzzle-Daten/Prefabs.
- **Gesperrt:** Combat, Inventory, Quickslots, PlayerVitals, Finsterwald-Hauptszene bis Integrationsschritt.
- **Abhängigkeiten:** P1.1; Memory-Puzzle-Plan.
- **Automatisch:** Zustands-/Speichertests und alle dokumentierten Puzzle-Testfälle; Batch-Kompilierung.
- **Manuell:** Lösung ohne Text, Fehlreihenfolgen, UI-Kontrast, Gamepad, Abbruch/Wiederaufnahme.
- **Abnahme:** beobachtbar und logisch lösbar; Watch gibt keine Lösung vor; kein Softlock.

## P1.8 Link-Begleiter

- **Branch:** `feature/finsterwald-link-companion`
- **Scope:** Sitzpunkte, diskrete Ortswechsel, Blick/Ruf und sichere Fallbackpositionen; keine Navigation durch freie Welt erforderlich.
- **Betroffene Systeme:** neue Link-Komponenten, Trigger, Audio-/Animationshooks.
- **Gesperrt:** Questmarker, Dialogsystem, Player/Combat/Memory-Logik, Hauptszene bis Integration.
- **Abhängigkeiten:** P1.1; freigegebene Link-Lore.
- **Automatisch:** Zustands- und Fallbacktests; Batch-Kompilierung; Prefabvalidierung.
- **Manuell:** Link bleibt sichtbar, blockiert nicht, verrät Puzzlelösung nicht, funktioniert nach Reload.
- **Abnahme:** Begleiterpräsenz unterstützt Aufmerksamkeit ohne Zwangsführung oder Sprache.

## P1.9 Lore- und Narrationsintegration

- **Branch:** `feature/finsterwald-lore-flow`
- **Scope:** freigegebene Kurztexte, Memory-Site-Fragmente, optionale Umweltuntersuchungen, Untertitel- und Lokalisierungsschlüssel.
- **Betroffene Systeme:** Memory, Examinable/Interaction, Narrationsdaten, UI-Text.
- **Gesperrt:** neue kanonische Fakten, Combat/Inventory/Quickslots, Layout-Grundstruktur.
- **Abhängigkeiten:** P1.7, P1.8; Lore-Freigabe aller OFFEN-Punkte.
- **Automatisch:** fehlende/duplizierte Schlüssel, leere Texte, Datenreferenzen, Batch-Kompilierung.
- **Manuell:** Reihenfolge, Überspringen, Untertiteltempo, erneute Interaktion, Widerspruchsreview.
- **Abnahme:** Spieler erhält den geplanten Wissensstand; keine zurückgehaltene Information wird vorweggenommen.

## P1.10 Regionsregeneration

- **Branch:** `feature/finsterwald-region-regeneration`
- **Scope:** datengetriebener Vorher-/Nachher-Zustand für Licht, Vegetation, Wasser, Audio und Wegöffnung; keine globale Weltsimulation.
- **Betroffene Systeme:** neue Region-State-Komponente, Save-Hooks, Environment-Prefabs, Audio/VFX-Hooks.
- **Gesperrt:** bestehende Player-/Combat-/Inventory-/HUD-Systeme; andere Regionen.
- **Abhängigkeiten:** P1.7, P1.9.
- **Automatisch:** Zustandswechsel einmalig/idempotent, Reload-Persistenz, Referenzvalidierung, Batch-Kompilierung.
- **Manuell:** Veränderung klar aber performant; Collider bleiben konsistent; Rückkehr in Region.
- **Abnahme:** Waldveränderung ist vom Weg aus sichtbar und beeinflusst keine unbeteiligten Systeme.

## P1.11 Audio und VFX

- **Branch:** `feature/finsterwald-audio-vfx`
- **Scope:** Feedback für Watch, Puzzle, Gegner, Treffer, Link und Regeneration; Lautheits-/Overdraw-Budgets.
- **Betroffene Systeme:** AudioMixer/AudioSources, VFX-Prefabs, Event-Hooks.
- **Gesperrt:** Gameplayregeln, Balancing, Loretexte, HUD-Layout, Szenengeometrie.
- **Abhängigkeiten:** P1.5–P1.10; freigegebene Assets/Lizenzen.
- **Automatisch:** fehlende Clips/Materialien, Pooling-/Referenzprüfung, Batch-Kompilierung.
- **Manuell:** Stereo/Kopfhörer, Lautheit, Telegraphen mit/ohne Bild, reduzierte Effekte.
- **Abnahme:** alle kritischen Zustände besitzen mindestens zwei lesbare Kanäle; keine störenden Peaks oder Daueremissionen.

## P1.12 Level- und Art-Polishing

- **Branch:** `feature/finsterwald-level-art-polish`
- **Scope:** Haupt- und Nebenpfad in `Finsterwald.unity`, Encounterflächen, Puzzle- und Regenerationszustände, Sichtlinien, Set Dressing und Performance.
- **Betroffene Systeme:** Finsterwald-Szene, freigegebene Environment-/Puzzle-/Encounter-Prefabs, Beleuchtung/NavMesh.
- **Gesperrt:** Player-, Combat-, Vitals-, Memory-Core-, Inventory-, HUD- und Quickslot-Skripte; andere Szenen.
- **Abhängigkeiten:** P1.6–P1.11; Assetfreigaben.
- **Automatisch:** World-Validator, Missing Scripts, doppelte zentrale Komponenten, Nav-/Referenzprüfung, Batch-Kompilierung, Diff-Check.
- **Manuell:** vollständiger 30–45-Minuten-Pfad, Sackgassen, Orientierung, Collision, Beleuchtung, Zielhardwareprofil.
- **Abnahme:** kritischer Pfad vollständig, optionaler Fund erreichbar, keine Blocker/Leaks, visuelle Führung ohne Marker.

## P1.13 Save- und Checkpoint-Grundlage

- **Branch:** `feature/finsterwald-checkpoints`
- **Scope:** minimale persistente Zustände für Start/Rucksack, Puzzle, Memory Site, Regeneration und Regionsabschluss; versionierbares Datenformat.
- **Betroffene Systeme:** Save-Service neu, Player-/Puzzle-/Region-State-Adapter, Checkpoint-Daten.
- **Gesperrt:** vollständiges Profil-/Slot-Menü, Cloud Save, Inventory-Neudesign, Szenenlayout.
- **Abhängigkeiten:** P1.7, P1.10; Zustandsverträge von P1.12.
- **Automatisch:** Roundtrip, fehlende/alte Daten, atomarer Schreibfehler, jeder Checkpointzustand; Batch-Kompilierung.
- **Manuell:** Neustart an jedem Checkpoint, Tod/Retry, beschädigte Testdatei, keine doppelten Belohnungen.
- **Abnahme:** Fortschritt ist nach Neustart konsistent; sichere Defaults verhindern Softlocks.

## P1.14 Integrationstest und Slice-Abnahme

- **Branch:** `feature/finsterwald-vertical-slice-integration`
- **Scope:** freigegebene Pakete zusammenführen, ausschließlich Integrationsfehler beheben, Balancing messen und Gate-1-Abnahmeevidenz sammeln.
- **Betroffene Systeme:** gesamter freigegebener Finsterwald-Slice.
- **Gesperrt:** neue Features, neue Lore, andere Regionen, Crafting, expansive Systeme.
- **Abhängigkeiten:** P1.1–P1.13.
- **Automatisch:** vollständige Batch-Kompilierung, alle Tests/Validatoren, Missing Scripts, Duplikate, Diff-Check, Save-Roundtrip.
- **Manuell:** mindestens drei komplette Läufe (kritischer Pfad, Nebenfund, Fehler-/Reloadpfad), Maus/Tastatur und Gamepad, 30–45-Minuten-Zeitmessung.
- **Abnahme:** keine Blocker; Hauptpfad 36–41 Minuten Zielwert, mit Nebenfund 41–45; alle Pflichtbeats funktionieren; bekannte Restpunkte dokumentiert.

## Abhängigkeitsfolge

`P1.1 → P1.2 → P1.3 → P1.4 → P1.5 → P1.6`

`P1.1 → P1.7 → P1.9 → P1.10`

`P1.1 → P1.8 → P1.9`

`P1.6–P1.10 → P1.11 → P1.12 → P1.13 → P1.14`

Parallelität ist nur zulässig, wenn die in den Paketen gesperrten Systeme und Dateien nicht überlappen. Die finale Szenenintegration bleibt P1.12 vorbehalten.
