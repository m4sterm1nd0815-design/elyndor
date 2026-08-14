using System.Collections.Generic;
using Elyndor.EditorTools.SceneIntegrity;
using NUnit.Framework;

namespace Elyndor.Tests.EditMode
{
    /// <summary>
    /// Regressionsschutz fuer die echte Finsterwald-Szene.
    ///
    /// Der Defekt, der diesen Test veranlasst hat, war unsichtbar: Das Projekt
    /// kompilierte fehlerfrei, die Konsole blieb still und alle vorhandenen
    /// Tests waren gruen — waehrend Interaktion, Narration, Erinnerungsuhr,
    /// Tutorial und der komplette Ton in der Szene tot waren. Genau dagegen
    /// laeuft dieser Test.
    /// </summary>
    public sealed class FinsterwaldSceneIntegrityTests
    {
        [Test]
        public void FinsterwaldSzene_ErfuelltDasIntegritaetsprofil()
        {
            List<SceneIntegrityIssue> issues =
                FinsterwaldSceneIntegrityValidator.Validate();

            Assert.That(
                issues,
                Is.Empty,
                FinsterwaldSceneIntegrityValidator.Format(issues));
        }
    }
}
