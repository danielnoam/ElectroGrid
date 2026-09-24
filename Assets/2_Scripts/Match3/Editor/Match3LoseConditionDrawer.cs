#if UNITY_EDITOR
using UnityEditor;

[CustomPropertyDrawer(typeof(Match3LoseCondition), true)]
public class Match3LoseConditionDrawer : ManagedReferenceTypeDrawer<Match3LoseCondition>
{
    protected override string TypeLabel => "Lose Condition";

    protected override string TrimTypeName(string typeName)
    {
        if (typeName.EndsWith("LoseCondition")) return typeName.Substring(0, typeName.Length - "LoseCondition".Length);
        if (typeName.EndsWith("Condition")) return typeName.Substring(0, typeName.Length - "Condition".Length);
        return typeName;
    }
}
#endif
