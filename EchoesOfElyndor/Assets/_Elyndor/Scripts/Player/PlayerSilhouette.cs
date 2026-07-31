using System.Collections.Generic;
using UnityEngine;

namespace Elyndor.Player
{
    /// <summary>
    /// Hält Aren in der Top-Down-Ansicht sichtbar, wenn Baumkronen oder
    /// Gelände ihn verdecken: Jedem Renderer wird ein Silhouetten-Material
    /// (Shader "Elyndor/OccludedSilhouette", ZTest Greater) angehängt, das
    /// ausschließlich auf verdeckten Pixeln zeichnet. Kein Update-Loop,
    /// keine Raycasts — die Sichtbarkeitsprüfung übernimmt die GPU.
    /// </summary>
    public class PlayerSilhouette : MonoBehaviour
    {
        [SerializeField] private Material silhouetteMaterial;

        private void Start()
        {
            if (silhouetteMaterial == null)
            {
                Debug.LogWarning($"{name}: Kein Silhouetten-Material zugewiesen.", this);
                return;
            }

            foreach (Renderer childRenderer in GetComponentsInChildren<Renderer>())
            {
                AppendSilhouetteMaterial(childRenderer);
            }
        }

        private void AppendSilhouetteMaterial(Renderer target)
        {
            List<Material> materials = new List<Material>(target.sharedMaterials);

            if (materials.Contains(silhouetteMaterial))
            {
                return;
            }

            materials.Add(silhouetteMaterial);
            target.sharedMaterials = materials.ToArray();
        }
    }
}
