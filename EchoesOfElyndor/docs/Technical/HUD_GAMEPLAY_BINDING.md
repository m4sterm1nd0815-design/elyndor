# HUD Gameplay Binding

## Enthalten

- `PlayerVitals` als laufende Quelle für Leben, Ausdauer und Erinnerung
- eventbasierte Anbindung an den bestehenden `HudVitalsSource`
- acht Laufzeit-Quickslots
- Auswahl über das bestehende Unity-EventSystem
- Nutzung per Submit/Controller oder Mausklick
- Mengenanzeige und Verbrauch von Items
- drei Prototype-Items zum direkten Testen
- QA- und Debug-Menüs

## Installation in der Szene

1. Unity kompilieren lassen.
2. Finsterwald-Szene öffnen.
3. `Elyndor > UI > Bind HUD to Gameplay` ausführen.
4. Szene speichern.
5. Play Mode starten.
6. `Elyndor > QA > Validate HUD Gameplay Binding` ausführen.

## Debug-Menüs

- `Elyndor > Debug > HUD > Take 25 Damage`
- `Elyndor > Debug > HUD > Spend 30 Stamina`
- `Elyndor > Debug > HUD > Gain 25 Memory`
- `Elyndor > Debug > HUD > Reset Vitals`

## Quickslot-Test

Die ersten drei Slots werden beim ersten Binden einmalig gefüllt:

1. Heiltrank
2. Ausdauertrank
3. Erinnerungssplitter

Ein Slot wird über das EventSystem ausgewählt. Submit beziehungsweise ein
Mausklick benutzt das Item. Die Menge wird verringert und der HUD-Balken
aktualisiert sich automatisch.

## Spätere Gameplay-Anbindung

Kampf-, Bewegungs- und Erinnerungsmechaniken können die öffentlichen Methoden
von `PlayerVitals` aufrufen, beispielsweise:

```csharp
playerVitals.TakeDamage(20f);
playerVitals.TrySpendStamina(15f);
playerVitals.ApplyMemory(10f);
```

Die UI muss dabei nicht direkt angesprochen werden.
