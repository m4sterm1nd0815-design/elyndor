using UnityEngine;

namespace Elyndor.Companion
{
    /// <summary>
    /// Ein Sitzpunkt für Link.
    ///
    /// Sitzpunkte sind Orte in der Welt, keine Hinweise. Sie kennen weder
    /// Rätsel noch Lösung — genau deshalb kann Link durch seine Wahl nichts
    /// verraten. Was ein Sitzpunkt beeinflusst, ist allein, wohin der Blick
    /// des Spielers beiläufig wandert.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LinkPerch : MonoBehaviour
    {
        [Tooltip("Blickrichtung, wenn Aren zu weit weg ist, um ihn anzusehen. " +
                 "Leer bedeutet die Vorwaertsrichtung dieses Objekts.")]
        [SerializeField] private Transform lookTarget;

        [Tooltip("Ruhiger Sitzplatz: hier ruft Link nicht. Fuer Orte, an " +
                 "denen ein Ruf stoeren wuerde.")]
        [SerializeField] private bool quiet;

        [Tooltip("Naeher als dieser Abstand zu Aren wird der Sitzpunkt nicht " +
                 "gewaehlt — eine Eule setzt sich einem nicht auf die Schulter.")]
        [Min(0f)] [SerializeField] private float minPlayerDistance = 3f;

        public bool Quiet => quiet;
        public float MinPlayerDistance => minPlayerDistance;

        /// <summary>Wohin Link von hier aus schaut, wenn Aren fern ist.</summary>
        public Vector3 RestingLookDirection =>
            lookTarget != null
                ? (lookTarget.position - transform.position).normalized
                : transform.forward;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = quiet
                ? new Color(0.4f, 0.6f, 0.6f)
                : new Color(0.2f, 0.8f, 0.75f);

            Gizmos.DrawWireSphere(transform.position, 0.25f);
            Gizmos.DrawRay(transform.position, RestingLookDirection * 1.2f);
        }
    }
}
