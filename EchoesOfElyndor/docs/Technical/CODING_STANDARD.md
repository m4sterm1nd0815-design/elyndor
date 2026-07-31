# Coding Standard

- PascalCase für Klassen und öffentliche Mitglieder.
- camelCase für private Felder.
- Inspector-Felder privat mit `[SerializeField]`.
- Eine Hauptklasse pro Datei.
- Kurze Methoden, keine Magic Numbers, keine leeren Catch-Blöcke.
- Keine teuren Suchen oder Allokationen in `Update`.
- Editor-Code nur in Editor-Ordnern.
- `main` bleibt stabil; Entwicklung auf Feature-Branches.
- Vor Commit: `git diff --check`, Unity-Kompilierung und relevante Tests.

## KI-Aufträge
Immer relevante Dokumente, konkrete Dateien, kleine Aufgabe, erlaubte Änderungen, Tests und Commit-Nachricht nennen.
