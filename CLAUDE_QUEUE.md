\# Claude GitHub Queue



Repository: m4sterm1nd0815-design/elyndor



\## Arbeitsablauf



Bearbeite immer höchstens ein offenes Issue mit dem Label `claude-ready`.



1\. Lies zuerst:

&#x20;  - `AGENTS.md`

&#x20;  - das vollständige Issue

&#x20;  - alle im Issue genannten Projektdokumente



2\. Prüfe vor Beginn:

&#x20;  - kein anderes Issue trägt `claude-in-progress`

&#x20;  - kein offener PR bearbeitet denselben Scope

&#x20;  - Basis ist der aktuelle `origin/developer`



3\. Übernimm das Issue:

&#x20;  - füge `claude-in-progress` hinzu

&#x20;  - entferne `claude-ready`

&#x20;  - kommentiere kurz, dass die Bearbeitung begonnen wurde



4\. Implementierung:

&#x20;  - eigener Feature-Branch von `origin/developer`

&#x20;  - ausschließlich der freigegebene Scope

&#x20;  - nie direkt auf `developer` oder `main`

&#x20;  - keine ungefragten Szenen-, ProjectSettings- oder Fremdänderungen

&#x20;  - keine offenen lokalen Änderungen überschreiben

&#x20;  - Meshy nur verwenden, wenn das Issue dies ausdrücklich erlaubt



5\. Abschluss:

&#x20;  - alle im Issue verlangten Prüfungen ausführen

&#x20;  - Draft-PR gegen `developer` erstellen

&#x20;  - Bericht ins Issue schreiben:

&#x20;    - Branch

&#x20;    - Commit

&#x20;    - PR

&#x20;    - geänderte Dateien

&#x20;    - Umsetzung

&#x20;    - Testergebnisse

&#x20;    - bekannte Grenzen

&#x20;    - manuelle Prüfungen



6\. Status:

&#x20;  - `claude-in-progress` entfernen

&#x20;  - `claude-review` hinzufügen

&#x20;  - bei visueller Prüfung zusätzlich `manual-unity-test`

&#x20;  - bei Blockade `claude-blocked` hinzufügen und stoppen



7\. Danach:

&#x20;  - kein weiteres Issue beginnen

&#x20;  - keinen PR selbst mergen

