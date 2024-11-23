#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine.Rendering;

public class PrerequisiteVariantStripper : IPreprocessShaders
{
    public int callbackOrder => 0;

    private readonly List<(ShaderKeyword subject, ShaderKeyword prereq)> prereqs = new List<(ShaderKeyword, ShaderKeyword)>();

    public PrerequisiteVariantStripper()
    {
        prereqs.Add((new ShaderKeyword("_MAPSOURCE_EVEN"), new ShaderKeyword("_MODE_CUSTOM")));
        prereqs.Add((new ShaderKeyword("_MAPSOURCE_ALPHA"), new ShaderKeyword("_MODE_CUSTOM")));
        //prereqs.Add((new ShaderKeyword("_CONTOURMAPSOURCE_EVEN"), new ShaderKeyword("_DRAWCONTOURS")));
        //prereqs.Add((new ShaderKeyword("_CONTOURMAPSOURCE_ALPHA"), new ShaderKeyword("_DRAWCONTOURS")));
        //prereqs.Add((new ShaderKeyword("_OUTLINE_SOURCE"), new ShaderKeyword("_DRAWOUTLINE")));
    }

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        for (int i = data.Count - 1; i >= 0; --i)
        {
            foreach (var entry in prereqs)
            {
                // If the entry doesn't exist, check the next entry
                if (!data[i].shaderKeywordSet.IsEnabled(entry.subject))
                    continue;
                // If the entry does exist, and the prerequisite isn't satisfied, remove the variant and check the next.
                if (!data[i].shaderKeywordSet.IsEnabled(entry.prereq))
                {
                    Debug.Log("Doesn't have: " + entry.prereq.ToString());
                    data.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
#endif