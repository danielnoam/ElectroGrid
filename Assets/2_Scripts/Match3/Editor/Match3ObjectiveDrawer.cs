#if UNITY_EDITOR
using UnityEditor;

[CustomPropertyDrawer(typeof(Match3Objective), true)]
public class Match3ObjectiveDrawer : ManagedReferenceTypeDrawer<Match3Objective>
{
    protected override string TypeLabel => "Objective";

    protected override string TrimTypeName(string typeName)
    {
        return typeName.EndsWith("Objective") ? typeName.Substring(0, typeName.Length - "Objective".Length) : typeName;
    }
}
#endif
