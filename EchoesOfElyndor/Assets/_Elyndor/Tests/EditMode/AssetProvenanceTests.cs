using System.Collections.Generic;
using System.IO;
using Elyndor.EditorTools;
using NUnit.Framework;

namespace Elyndor.Tests.EditMode
{
    /// <summary>
    /// Prueft die Herkunfts-/Lizenzangabe neben einem Asset.
    ///
    /// Die Negativfaelle werden dem Pruefer als Text uebergeben, nicht als
    /// Datei auf der Platte. Der naheliegende Weg — die echte Herkunftsdatei
    /// des Testfelsens kaputtmachen, pruefen, wiederherstellen — haette den
    /// Zustand veraendert, ueber den anschliessend das Gate urteilt: Bricht
    /// ein Test in der Mitte ab, bleibt der Fels ohne gueltige Herkunft
    /// zurueck, und der naechste Validatorlauf meldet einen Fehler, den
    /// niemand gebaut hat.
    ///
    /// Der Positivfall liest ausdruecklich die echte Datei. Er ist der
    /// einzige Test hier, der das tut, und er schreibt nicht.
    /// </summary>
    public sealed class AssetProvenanceTests
    {
        private const string TestRockPath =
            "Assets/_Elyndor/Art/_PipelineTest/ELY_Test_Rock_A.fbx";

        /// <summary>Ein Pfad im selben Ordner, zu dem es nichts gibt.</summary>
        private const string PathWithoutSidecar =
            "Assets/_Elyndor/Art/_PipelineTest/ELY_Test_Rock_Ohne_Herkunft.fbx";

        [Test]
        public void Testfels_HatGueltigeHerkunftsangabe()
        {
            IReadOnlyList<string> problems =
                AssetProvenance.ValidateFile(TestRockPath, out string summary);

            Assert.That(
                problems,
                Is.Empty,
                $"Herkunft des Testfelsens beanstandet ({summary}): " +
                Describe(problems));
        }

        [Test]
        public void Testfels_HerkunftsdateiLiegtNebenDemAsset()
        {
            string sidecarPath = AssetProvenance.SidecarPathFor(TestRockPath);

            Assert.That(
                sidecarPath,
                Is.EqualTo(TestRockPath + ".provenance.json"),
                "Der Ablageort der Herkunftsdatei ist Teil des Formats.");

            Assert.That(
                File.Exists(sidecarPath),
                Is.True,
                $"'{sidecarPath}' fehlt auf der Platte.");
        }

        [Test]
        public void FehlendeHerkunftsdatei_WirdBeanstandet()
        {
            string sidecarPath = AssetProvenance.SidecarPathFor(PathWithoutSidecar);

            Assert.That(
                File.Exists(sidecarPath),
                Is.False,
                "Vorbedingung: zu diesem Pfad darf keine Herkunftsdatei liegen.");

            IReadOnlyList<string> problems =
                AssetProvenance.ValidateFile(PathWithoutSidecar, out _);

            Assert.That(
                problems,
                Is.Not.Empty,
                "Ein Asset ohne Herkunftsdatei muss rot sein.");

            // Der Pruefer darf sich den Zustand nicht selbst herstellen, den
            // er prueft: eine angelegte Leerdatei waere beim naechsten Lauf
            // gruen und wuerde nichts belegen.
            Assert.That(
                File.Exists(sidecarPath),
                Is.False,
                "Die Pruefung hat eine Herkunftsdatei angelegt.");
        }

        [Test]
        public void KaputtesJson_WirdBeanstandet()
        {
            const string truncated =
                "{\n  \"schema\": \"elyndor.asset-provenance\",\n  \"schemaVersion\": 1,";

            IReadOnlyList<string> problems =
                AssetProvenance.Validate(truncated, TestRockPath, out _);

            Assert.That(
                problems,
                Is.Not.Empty,
                "Abgeschnittenes JSON muss als Fehler ankommen und nicht als " +
                "leerer Datensatz.");
        }

        [Test]
        public void FremderAssetpfad_WirdBeanstandet()
        {
            string json = BuildRecord(
                assetPath: "Assets/_Elyndor/Art/_PipelineTest/ELY_Test_Rock_B.fbx");

            IReadOnlyList<string> problems =
                AssetProvenance.Validate(json, TestRockPath, out _);

            Assert.That(
                problems,
                Is.Not.Empty,
                "Eine Herkunftsangabe, die ein anderes Asset nennt, gehoert " +
                "nicht zu diesem.");
        }

        [Test]
        public void FehlendePflichtangaben_WerdenBeanstandet()
        {
            string json = BuildRecord(
                origin: string.Empty,
                creator: string.Empty,
                source: string.Empty,
                license: string.Empty);

            IReadOnlyList<string> problems =
                AssetProvenance.Validate(json, TestRockPath, out _);

            Assert.That(
                problems.Count,
                Is.EqualTo(4),
                "Erwartet je eine Beanstandung fuer origin, creator, source " +
                "und license: " + Describe(problems));
        }

        [Test]
        public void UnbekannteSchemaversion_WirdBeanstandet()
        {
            string json = BuildRecord(schemaVersion: AssetProvenance.SchemaVersion + 1);

            IReadOnlyList<string> problems =
                AssetProvenance.Validate(json, TestRockPath, out _);

            Assert.That(
                problems,
                Is.Not.Empty,
                "Eine Version, die dieser Leser nicht kennt, wird gemeldet " +
                "statt geraten.");
        }

        [Test]
        public void VollstaendigeAngabe_WirdNichtBeanstandet()
        {
            IReadOnlyList<string> problems =
                AssetProvenance.Validate(BuildRecord(), TestRockPath, out _);

            Assert.That(problems, Is.Empty, Describe(problems));
        }

        /// <summary>
        /// Eine gueltige Angabe, aus der jeder Test genau das Feld heraus-
        /// nimmt, um das es ihm geht. So steht in jedem Test nur der
        /// Unterschied zum gueltigen Fall.
        /// </summary>
        private static string BuildRecord(
            string schema = AssetProvenance.SchemaId,
            int schemaVersion = AssetProvenance.SchemaVersion,
            string assetPath = TestRockPath,
            string origin = "authored-in-house",
            string creator = "Elyndor-Projekt",
            string source = "Art_Source/PipelineTest0/ELYNDOR_MCP_PIPELINE_TEST.blend",
            string license = "Projekteigen.",
            string recordedOn = "2026-08-16")
        {
            return
                "{\n" +
                $"  \"schema\": \"{schema}\",\n" +
                $"  \"schemaVersion\": {schemaVersion},\n" +
                $"  \"assetPath\": \"{assetPath}\",\n" +
                $"  \"origin\": \"{origin}\",\n" +
                $"  \"creator\": \"{creator}\",\n" +
                $"  \"source\": \"{source}\",\n" +
                $"  \"license\": \"{license}\",\n" +
                $"  \"recordedOn\": \"{recordedOn}\"\n" +
                "}";
        }

        private static string Describe(IReadOnlyList<string> problems)
        {
            return problems.Count == 0
                ? "keine Beanstandung"
                : string.Join(" | ", problems);
        }
    }
}
