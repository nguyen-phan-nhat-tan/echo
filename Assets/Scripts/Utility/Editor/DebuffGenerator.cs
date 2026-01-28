using UnityEngine;
using UnityEditor;
using System.IO;

public class DebuffGenerator
{
    [MenuItem("Tools/Generate Debuffs")]
    public static void Generate()
    {
        string path = "Assets/Resources/Debuffs";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        // Name, Move, Fire, Dash, Timer, Drift, Fog
        CreateDebuff(path, "Sluggish", 0.6f, 1f, 1f, 1f, false, false);
        CreateDebuff(path, "Jammed", 1f, 0.5f, 1f, 1f, false, false);
        CreateDebuff(path, "Heavy", 1f, 1f, 0.5f, 1f, false, false);
        CreateDebuff(path, "Weak", 0.8f, 0.8f, 0.8f, 1f, false, false);
        CreateDebuff(path, "Rush", 1f, 1f, 1f, 1.5f, false, false);
        CreateDebuff(path, "Drift", 1.2f, 1f, 1f, 1f, true, false); // Slippery, slightly faster
        CreateDebuff(path, "Fog", 1f, 1f, 1f, 1f, false, true);     // Limited vision

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Debuffs Generated in " + path);
    }

    private static void CreateDebuff(string path, string name, float move, float fire, float dash, float timer, bool drift, bool fog)
    {
        string assetPath = path + "/" + name + ".asset";
        DebuffData asset = AssetDatabase.LoadAssetAtPath<DebuffData>(assetPath);
        
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<DebuffData>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        asset.debuffName = name;
        asset.moveSpeedMultiplier = move;
        asset.fireRateMultiplier = fire;
        asset.dashCooldownMultiplier = dash;
        asset.timerSpeedMultiplier = timer;
        asset.drift = drift;
        asset.fog = fog;
        
        EditorUtility.SetDirty(asset);
    }
}
