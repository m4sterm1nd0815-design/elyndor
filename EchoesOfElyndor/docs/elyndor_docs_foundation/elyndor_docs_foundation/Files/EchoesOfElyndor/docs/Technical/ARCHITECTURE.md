# Technische Architektur

- Unity 6 und URP bleiben Basis.
- Lose gekoppelte Systeme, Events und Interfaces.
- ScriptableObjects für statische Definitionen.
- Runtime-Modelle für veränderliche Zustände.
- UI zeigt Modelle an; Gameplaylogik gehört nicht in Views.
- Keine neue Singleton-Struktur ohne dokumentierte Begründung.
- Stabile IDs sind Savegame-Verträge.

## Save-Bereiche
`inventory`, `equipment`, `quickslots`, `narrative`, `settings`, `world`

## Qualität
Jedes neue System braucht Fehlerbehandlung, Null-Prüfungen, Validator oder Test und Integrationsdokumentation.
