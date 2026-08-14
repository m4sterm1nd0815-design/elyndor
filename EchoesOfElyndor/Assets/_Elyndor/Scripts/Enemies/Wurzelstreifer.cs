using UnityEngine;

namespace Elyndor.Enemies
{
    /// <summary>
    /// Der erste Gegnertyp des Finsterwalds: ein kleines Waldtier, dessen
    /// Ortsgedaechtnis von wiederholten Fluchtspuren ueberlagert wurde. Rinde
    /// und tuerkise Bruchlinien sind gewachsen, nicht angelegt — er ist kein
    /// Monster, sondern ein beschaedigtes Tier.
    ///
    /// Spielerisch ist er der mobile Nahkampf-Lehrer: er greift langsam genug
    /// an, dass Rolle, Block und Abstand jeweils einmal die richtige Antwort
    /// sein koennen, und bestraft nur, wer gar nicht antwortet.
    ///
    /// Diese Komponente traegt keine eigene Logik. Sie legt die Werte des
    /// Encounter-Plans auf die gemeinsame Gegnergrundlage — an einer Stelle,
    /// nachlesbar und testbar. Die Alternative waere gewesen, sie nur im
    /// Prefab zu hinterlegen; dann haette kein Test und kein Leser sie ohne
    /// Unity sehen koennen, und ein zur Laufzeit gebauter Wurzelstreifer
    /// haette sich anders verhalten als der aus der Szene.
    ///
    /// Alle Zahlen sind vorlaeufige Balancingwerte nach der Konvention des
    /// Slice-Plans: 100 Spielerleben, 10 leichter und 25 schwerer Schaden.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    [RequireComponent(typeof(EnemyPerception))]
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(EnemyAttack))]
    [RequireComponent(typeof(EnemyHitReaction))]
    public sealed class Wurzelstreifer : MonoBehaviour
    {
        // --- Wahrnehmung (Encounter-Plan) ---------------------------------

        /// <summary>9 m Sichtkegel.</summary>
        public const float SightRadius = 9f;

        /// <summary>
        /// Oeffnung des Sichtkegels. Der Encounter-Plan nennt die Reichweite,
        /// aber keinen Winkel; 120 Grad ist ein vorlaeufiger Wert, der ein
        /// Anschleichen von hinten erlaubt, ohne den Gegner blind zu machen.
        /// </summary>
        public const float SightAngle = 120f;

        /// <summary>5 m Geraeuschradius — wirkt ohne Sicht und ohne Winkel.</summary>
        public const float HearingRadius = 5f;

        /// <summary>2 s Verdachtsphase vor der ersten Handlung.</summary>
        public const float SuspicionDuration = 2f;

        /// <summary>Verliert Aren nach 4 s ohne Wahrnehmung.</summary>
        public const float LoseTargetDelay = 4f;

        // --- Kampfmuster ---------------------------------------------------

        /// <summary>0,7 s klar lesbarer Schulter-Telegraph vor dem Sprungbiss.</summary>
        public const float TelegraphDuration = 0.7f;

        /// <summary>1,2 s offenes Gegenfenster nach dem Sprungbiss.</summary>
        public const float RecoveryDuration = 1.2f;

        /// <summary>
        /// Abstand zwischen zwei Sprungbissen. Telegraph und Erholung fuellen
        /// davon 1,9 s; der Rest ist die sichtbare Beobachtungs- und
        /// Umkreisphase, in der der Spieler die Initiative hat.
        /// </summary>
        public const float AttackCooldown = 2.6f;

        /// <summary>
        /// Reichweite des Sprungbisses. Eine Rolle traegt rund 4 m und
        /// verlaesst sie damit sicher.
        /// </summary>
        public const float AttackRange = 2.2f;

        // --- Werte ---------------------------------------------------------

        /// <summary>35-45 LP laut Plan; die Mitte.</summary>
        public const float MaxHealth = 40f;

        /// <summary>8-12 Schaden laut Plan; die Mitte.</summary>
        public const float AttackDamage = 10f;

        /// <summary>0,18 s Zusammenzucken nach einem leichten Treffer.</summary>
        public const float LightFlinchDuration = 0.18f;

        /// <summary>0,8 s Straucheln nach einem schweren Treffer.</summary>
        public const float HeavyStaggerDuration = 0.8f;

        /// <summary>Einmaliger Rueckzug unter 30 % Leben.</summary>
        public const float RetreatHealthThreshold01 = 0.3f;

        /// <summary>Dauer des Loesens. Vorlaeufig, nicht aus dem Plan.</summary>
        public const float RetreatDuration = 2.5f;

        // --- Bewegung ------------------------------------------------------

        /// <summary>
        /// Etwas langsamer als Arens Gehtempo von 5 m/s. Damit ist Abstand
        /// eine echte Antwort, ohne dass Weglaufen den Kampf beendet: der
        /// Spieler gewinnt Raum, aber nur langsam.
        /// </summary>
        public const float MoveSpeed = 4.8f;

        /// <summary>Halteabstand knapp innerhalb der Angriffsreichweite.</summary>
        public const float StopDistance = 1.8f;

        /// <summary>Tempo des seitlichen Schritts zwischen den Angriffen.</summary>
        public const float StrafeSpeed = 2.4f;

        /// <summary>Sekunden bis zum Seitenwechsel des Umkreisens.</summary>
        public const float StrafeInterval = 1.4f;

        /// <summary>Beim Loesen etwas schneller als beim Verfolgen.</summary>
        public const float RetreatSpeed = 5.4f;

        // --- Bindung an die Begegnung --------------------------------------

        /// <summary>
        /// Grenze um die Lichtung. Weiter folgt der Wurzelstreifer nicht: die
        /// Begegnung ist ein Ort, kein Verfolgungsrennen. Der Wert deckt die
        /// Lichtung mit etwas Rand ab.
        /// </summary>
        public const float LeashRadius = 12f;

        /// <summary>Abstand, ab dem die Rueckkehr als abgeschlossen gilt.</summary>
        public const float LeashReturnTolerance = 1.5f;

        [Tooltip("Werte beim Start auf die Gegnergrundlage legen. Aus lassen, " +
                 "wenn ein Exemplar bewusst abweichend eingestellt werden soll.")]
        [SerializeField] private bool applyOnAwake = true;

        private void Awake()
        {
            if (applyOnAwake)
            {
                Apply();
            }
        }

        /// <summary>
        /// Legt das gesamte Profil auf die vorhandenen Komponenten. Fehlende
        /// Komponenten werden uebergangen — ein unvollstaendig gebauter
        /// Wurzelstreifer soll keine roten Meldungen erzeugen, sondern nur
        /// weniger koennen.
        /// </summary>
        public void Apply()
        {
            if (TryGetComponent(out EnemyPerception perception))
            {
                perception.Configure(
                    SightRadius, SightAngle, HearingRadius, LoseTargetDelay);
            }

            if (TryGetComponent(out EnemyController controller))
            {
                controller.ConfigureSuspicion(SuspicionDuration);
            }

            if (TryGetComponent(out EnemyMovement movement))
            {
                movement.Configure(
                    MoveSpeed,
                    StopDistance,
                    StrafeSpeed,
                    StrafeInterval,
                    RetreatSpeed);
            }

            if (TryGetComponent(out EnemyAttack attack))
            {
                attack.Configure(
                    AttackRange,
                    AttackCooldown,
                    AttackDamage,
                    TelegraphDuration,
                    RecoveryDuration);
            }

            if (TryGetComponent(out EnemyHitReaction hitReaction))
            {
                hitReaction.Configure(
                    LightFlinchDuration, HeavyStaggerDuration);
            }

            if (TryGetComponent(out EnemyHealth health))
            {
                health.Configure(MaxHealth);
            }

            if (TryGetComponent(out EnemyRetreat retreat))
            {
                retreat.Configure(
                    RetreatHealthThreshold01, RetreatDuration);
            }

            if (TryGetComponent(out EnemyLeash leash))
            {
                leash.Configure(LeashRadius, LeashReturnTolerance);
            }
        }
    }
}
