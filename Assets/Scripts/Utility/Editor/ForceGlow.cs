using UnityEngine;
using UnityEditor;

public class ForceGlow : EditorWindow
{
    [MenuItem("Tools/Force Glow Intensity")]
    public static void ForceGlowIntensity()
    {
        Material mat = Selection.activeObject as Material;
        if (mat == null)
        {
            Debug.LogError("Select a Material first!");
            return;
        }

        // 1. Force Shader to Universal Render Pipeline/2D/Sprite-Unlit-Default
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader != null) 
        {
            mat.shader = shader;
            Debug.Log($"Switched shader to: {shader.name}");
        }
        else
        {
            Debug.LogError("Could not find URP 2D Unlit shader! Are you sure URP is installed?");
            return;
        }

        // 2. Enable Emission Keyword (Unlit often just uses Color, but we'll try standard emission too)
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.EnableKeyword("_EMISSION");

        // 3. Force Color with Intensity
        float intensity = 5f;
        Color glowingColor = Color.white * intensity; // High intensity white
        
        // For 'Sprite-Unlit-Default', the main color property is usually just "_Color"
        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", glowingColor);
            Debug.Log($"Forced '_Color' to Intensity {intensity} (Unlit Glow)");
        }
        
        // Sometimes strictly need _EmissionColor
        if (mat.HasProperty("_EmissionColor"))
        {
             mat.SetColor("_EmissionColor", glowingColor);
        }
        
        // Force update
        EditorUtility.SetDirty(mat);
    }
}
