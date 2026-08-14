namespace Elyndor.EditorTools.SceneIntegrity
{
    public enum SceneIntegrityIssueCode
    {
        RequiredComponentMissing,
        RequiredComponentInactive,
        DuplicateSingleInstanceComponent,
        ZeroScaleRoot,
        MissingScriptReference,
        MissingRequiredReference
    }

    public sealed class SceneIntegrityIssue
    {
        public SceneIntegrityIssue(
            SceneIntegrityIssueCode code,
            string objectPath,
            string message)
        {
            Code = code;
            ObjectPath = objectPath;
            Message = message;
        }

        public SceneIntegrityIssueCode Code { get; }

        /// <summary>Hierarchiepfad, leer wenn das Objekt gar nicht existiert.</summary>
        public string ObjectPath { get; }

        public string Message { get; }

        public override string ToString() =>
            string.IsNullOrEmpty(ObjectPath)
                ? $"[{Code}] {Message}"
                : $"[{Code}] {ObjectPath}: {Message}";
    }
}
