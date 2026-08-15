using Elyndor.Persistence;
using NUnit.Framework;

namespace Elyndor.Tests
{
    /// <summary>
    /// Sorgt dafuer, dass <b>kein</b> Test der Suite jemals einen echten
    /// Spielstand anfasst.
    ///
    /// Das ist keine Vorsichtsmassnahme, sondern notwendig: sobald das
    /// Speichersystem laeuft, schreibt jeder Fortschritt sofort weg. Ohne
    /// diese Klasse schriebe die Kampf-, Raetsel- und Regenerationssuite in
    /// den Spielstand des Entwicklers — und der Integrationstest laese beim
    /// naechsten Lauf den Fortschritt des vorherigen.
    ///
    /// <see cref="SetUpFixtureAttribute"/> laeuft einmal fuer den gesamten
    /// Namensraum, vor jedem Test darin. Der eingehaengte Speicher lebt nur
    /// im Arbeitsspeicher; <see cref="SaveBootstrap"/> haelt sich heraus,
    /// sobald ein Dienst eingehaengt ist.
    /// </summary>
    [SetUpFixture]
    public sealed class SaveIsolationSetup
    {
        [OneTimeSetUp]
        public void InstallTemporaryStore()
        {
            SaveService.Install(new SaveService(new MemorySaveStore()));
        }

        [OneTimeTearDown]
        public void RemoveTemporaryStore()
        {
            SaveService.Uninstall();
        }
    }
}
