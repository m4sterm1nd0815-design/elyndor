using UnityEngine;

namespace Elyndor.Enemies
{
    /// <summary>
    /// Haelt einen Gegner an seine Begegnung gebunden.
    ///
    /// Ohne diese Grenze verfolgt ein Gegner sein Ziel so weit, wie seine
    /// Wahrnehmung reicht — und weil ein verlorenes Ziel noch einige Sekunden
    /// als bekannt gilt, wandert er dabei weit aus seinem Bereich heraus. Der
    /// Wurzelstreifer der Lichtung stand nach einer Flucht des Spielers
    /// fuenfzehn Meter neben der Lichtung; die naechste Begegnung faende dann
    /// nicht mehr dort statt, wo sie entworfen wurde.
    ///
    /// Ist die Grenze ueberschritten, kehrt der Gegner zurueck und nimmt bis
    /// dahin kein Ziel wahr. Ohne dieses Vergessen wuerde er an der Grenze
    /// zwischen Rueckkehr und Verfolgung hin und her kippen.
    ///
    /// Mit Radius 0 ist die Bindung abgeschaltet; ein Gegner ohne eigene Werte
    /// verhaelt sich damit unveraendert.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyLeash : MonoBehaviour
    {
        [Tooltip("Zulaessiger Abstand vom Heimatpunkt. 0 schaltet die " +
                 "Bindung ab.")]
        [Min(0f)] [SerializeField] private float leashRadius;

        [Tooltip("Abstand, ab dem die Rueckkehr als abgeschlossen gilt.")]
        [Min(0.1f)] [SerializeField] private float returnTolerance = 1.5f;

        /// <summary>Der Punkt, an dem die Begegnung entworfen wurde.</summary>
        public Vector3 Home { get; private set; }

        /// <summary>Kehrt der Gegner gerade zurueck?</summary>
        public bool IsReturning { get; private set; }

        public float LeashRadius => leashRadius;

        /// <summary>Abstand zum Heimatpunkt in der Ebene.</summary>
        public float DistanceFromHome
        {
            get
            {
                Vector3 offset = transform.position - Home;
                offset.y = 0f;

                return offset.magnitude;
            }
        }

        private void Awake()
        {
            SetHome(transform.position);
        }

        /// <summary>Setzt den Heimatpunkt, etwa aus einem Spawner.</summary>
        public void SetHome(Vector3 position)
        {
            Home = position;
            IsReturning = false;
        }

        public void Configure(float newLeashRadius, float newReturnTolerance)
        {
            leashRadius = Mathf.Max(0f, newLeashRadius);
            returnTolerance = Mathf.Max(0.1f, newReturnTolerance);
        }

        /// <summary>Wird vom <see cref="EnemyController"/> getaktet.</summary>
        public void Tick(float deltaTime)
        {
            if (leashRadius <= 0f)
            {
                IsReturning = false;
                return;
            }

            float distance = DistanceFromHome;

            if (IsReturning)
            {
                if (distance <= returnTolerance)
                {
                    IsReturning = false;
                }

                return;
            }

            if (distance > leashRadius)
            {
                IsReturning = true;
            }
        }
    }
}
