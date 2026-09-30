using System;
using System.Reflection;

internal static class TestUtils
{
    /// <summary>Sets a private field, searching base classes, so fixtures can build state without Awake or tweens.</summary>
    public static void SetField(object target, string fieldName, object value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(fieldName, flags | BindingFlags.DeclaredOnly);
            if (field == null) continue;

            field.SetValue(target, value);
            return;
        }

        throw new MissingFieldException(target.GetType().Name, fieldName);
    }

    public static T GetField<T>(object target, string fieldName)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(fieldName, flags | BindingFlags.DeclaredOnly);
            if (field != null) return (T)field.GetValue(target);
        }

        throw new MissingFieldException(target.GetType().Name, fieldName);
    }
}
