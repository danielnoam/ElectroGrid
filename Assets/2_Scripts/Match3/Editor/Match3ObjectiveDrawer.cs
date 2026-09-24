using UnityEditor;

[CustomPropertyDrawer(typeof(Match3Objective), true)]
internal class Match3ObjectiveDrawer : ManagedReferenceTypeDrawer<Match3Objective>
{
    protected override string TypeLabel => "Objective";

    protected override string TrimTypeName(string typeName)
    {
        return typeName.EndsWith("Objective") ? typeName.Substring(0, typeName.Length - "Objective".Length) : typeName;
    }
}
