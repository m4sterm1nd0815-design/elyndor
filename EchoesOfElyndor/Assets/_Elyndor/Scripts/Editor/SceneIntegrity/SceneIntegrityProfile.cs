using System;
using System.Collections.Generic;

namespace Elyndor.EditorTools.SceneIntegrity
{
    /// <summary>
    /// Beschreibt, was eine Szene strukturell leisten muss.
    ///
    /// Bewusst datengetrieben statt fest verdrahtet: Der Analysator kennt keine
    /// einzige Elyndor-Komponente. Dadurch bleibt die Pruefung testbar, ohne
    /// dass Tests echte Spielszenen laden muessen, und eine neue Szene braucht
    /// nur ein neues Profil statt neuen Pruefcode.
    /// </summary>
    public sealed class SceneIntegrityProfile
    {
        public SceneIntegrityProfile(string name)
        {
            Name = name;
        }

        public string Name { get; }

        /// <summary>
        /// Komponenten, die vorhanden UND wirksam sein muessen. "Wirksam"
        /// heisst: nicht unter einem deaktivierten Vorfahren. Genau diese
        /// Bedingung war in Finsterwald verletzt.
        /// </summary>
        public List<Type> RequiredActiveTypes { get; } = new List<Type>();

        /// <summary>
        /// Komponenten, von denen es hoechstens eine geben darf. Schuetzt vor
        /// doppelten HUDs, doppelten Event-Abos und mehrfach laufenden
        /// Tutorial-Sequenzen.
        /// </summary>
        public List<Type> SingleInstanceTypes { get; } = new List<Type>();

        /// <summary>
        /// Wurzelobjekte, deren Transform-Scale nicht null sein darf. Ein
        /// UI-Root mit Scale 0 ist unsichtbar, ohne deaktiviert zu wirken.
        /// </summary>
        public List<string> ScaleCheckedRootNames { get; } = new List<string>();

        /// <summary>
        /// Pflichtfelder je Komponententyp. Nur ausdruecklich benannte Felder
        /// werden geprueft — eine generische Null-Suche ueber alle
        /// serialisierten Referenzen wuerde vor allem legitime optionale
        /// Felder melden und die echten Treffer im Rauschen begraben.
        /// </summary>
        public Dictionary<Type, string[]> RequiredReferences { get; } =
            new Dictionary<Type, string[]>();

        public SceneIntegrityProfile RequireActive(params Type[] types)
        {
            RequiredActiveTypes.AddRange(types);
            return this;
        }

        public SceneIntegrityProfile RequireSingleInstance(params Type[] types)
        {
            SingleInstanceTypes.AddRange(types);
            return this;
        }

        public SceneIntegrityProfile RequireNonZeroScale(params string[] rootNames)
        {
            ScaleCheckedRootNames.AddRange(rootNames);
            return this;
        }

        public SceneIntegrityProfile RequireReferences(
            Type type,
            params string[] fieldNames)
        {
            RequiredReferences[type] = fieldNames;
            return this;
        }
    }
}
