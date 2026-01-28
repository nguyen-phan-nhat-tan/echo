using UnityEngine;
using UnityEditor;

public class GlowMaterialGenerator : EditorWindow
{
    [MenuItem("Tools/Create Glow Material")]
    public static void CreateGlowMat()
    {
        // 1. Ensure Folder Exists
        string folderPath = "Assets/Material";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Material");
        }

        // 2. Create Material
        // URP 2D Shader
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (shader == null) 
        {
            shader = Shader.Find("Sprites/Default"); // Fallback if URP not found
            Debug.LogWarning("URP Sprite Shader not found, using Default Sprite shader.");
        }

        Material material = new Material(shader);
        
        // 3. Configure Emission
        material.EnableKeyword("_EMISSION");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        
        // HDR Color (White with Intensity 3)
        // usage: Color * Intensity
        Color glowColor = Color.white * 3f; 
        
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", glowColor);
        }
        
        // 4. Save Asset
        string path = folderPath + "/GlowSprite.mat";
        
        // Ensure unique name
        path = AssetDatabase.GenerateUniqueAssetPath(path);
        
        AssetDatabase.CreateAsset(material, path);
        
        // 5. Focus
        Selection.activeObject = material;
        EditorGUIUtility.PingObject(material);
        
        Debug.Log("Created Glow Material at: " + path);
    }
}
