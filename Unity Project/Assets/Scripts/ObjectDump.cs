#if DEBUG
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System;

[DisallowMultipleComponent]
public class ObjectDump : MonoBehaviour
{
    [SerializeField]
    private bool dumpOnAwake = false;
    [SerializeField]
    public bool dumpOnceOnly = true;
    private bool hasDumped = false;

    private void Awake()
    {
        if (dumpOnAwake)
            DumpGameObject();
    }

    public void DumpGameObject()
    {
        if (dumpOnceOnly && hasDumped)
        {
            Debug.Log($"[ObjectDump] {gameObject.name} has already been logged.");
            return;
        }
        Debug.Log($"[ObjectDump] Dumping {gameObject.name}:");
        string result = $"{{\"name\": \"{gameObject.name}\", \"id\": {gameObject.GetInstanceID()}, \"components\": [";
        result += GetComponents<Component>().Where(o =>
            typeof(Transform).IsAssignableFrom(o.GetType()) ||
            typeof(CanvasRenderer).IsAssignableFrom(o.GetType()) ||
            //typeof(UnityEngine.UI.ScrollRect).IsAssignableFrom(o.GetType()) ||
            typeof(Canvas).IsAssignableFrom(o.GetType()) ||
            typeof(UnityEngine.UI.GraphicRaycaster).IsAssignableFrom(o.GetType()) ||
            typeof(CanvasGroup).IsAssignableFrom(o.GetType())
            ).Select(Dump).Aggregate((a, b) => string.Join(", ", a, b));
        result += "]}";
        Debug.Log(result);
        Debug.Log("[ObjectDump] Finished ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^");
        //return result;
        hasDumped = true;
    }
    public static string Dump(object obj)
    {
        string output = $"{{\"type\": \"{obj.GetType().Name}\"";

        BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        IEnumerable<MemberInfo> members = obj.GetType().GetFields(bindingFlags).Union<MemberInfo>(obj.GetType().GetProperties(bindingFlags));
        output = string.Join(", ", output, members.Where(IsValidType)
            .Select(f => $"\"{f.Name}\": {FormatOutput(f, obj)}")
            .Aggregate((a, b) => string.Join(", ", a, b)));

        output += "}";
        return output;
    }
    private static bool IsValidType(MemberInfo member)
    {
        Type type;
        if (member is FieldInfo field)
            type = field.FieldType;
        else if (member is PropertyInfo property)
            type = property.PropertyType;
        else
            return false;
        if (type.IsPrimitive)
            return true;
        if (type.Namespace.Contains("UnityEngine"))
            return true;
        return false;
    }
    private static string FormatOutput(MemberInfo member, object obj)
    {
        if (obj == null)
            return string.Empty;
        object value;
        if (member is FieldInfo field)
            value = field.GetValue(obj);
        else if (member is PropertyInfo property)
            value = property.GetValue(obj);
        else
            return string.Empty;

        string output = value?.ToString() ?? "null";
        if (value == null || value.GetType() == typeof(string) || !value.GetType().IsPrimitive)
            output = $"\"{output}\"";
        return output;
    }
}
#endif