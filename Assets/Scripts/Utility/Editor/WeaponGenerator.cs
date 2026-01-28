using UnityEngine;
using UnityEditor;
using System.IO;

public class WeaponGenerator
{
    [MenuItem("Tools/Generate Weapons")]
    public static void Generate()
    {
        string path = "Assets/Resources/Weapons";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        // Name, FireRate, Count, Spread, Burst, BurstDelay, Dmg, Speed, Ricochet, Wave, Spiral, FixedPattern
        CreateWeapon(path, "Shotgun", 1.2f, 5, 25f, 1, 0f, 0.8f, 15f, false, false, 0, 5); // Fixed Pattern 5-way
        CreateWeapon(path, "SMG", 12f, 1, 8f, 1, 0f, 0.4f, 18f, false, false, 0, 0);
        CreateWeapon(path, "BurstRifle", 2.0f, 1, 2f, 3, 0.08f, 0.7f, 22f, false, false, 0, 0);
        CreateWeapon(path, "Sniper", 0.8f, 1, 0f, 1, 0f, 3f, 35f, true, false, 0, 0); // Ricochet
        CreateWeapon(path, "Pistol_Auto", 6f, 1, 3f, 1, 0f, 0.8f, 16f, false, false, 0, 0);
        
        // NEW WEAPONS
        CreateWeapon(path, "WaveBlaster", 4f, 1, 0f, 1, 0f, 1f, 12f, false, true, 0, 0); // Wave
        CreateWeapon(path, "SpiralGun", 15f, 1, 0f, 1, 0f, 0.5f, 15f, false, false, 20f, 0); // Spiral 20deg/shot
        CreateWeapon(path, "RicochetRifle", 3f, 1, 0f, 1, 0f, 1.2f, 25f, true, false, 0, 0); // Ricochet

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Weapons Generated in " + path);
    }

    private static void CreateWeapon(string path, string name, float fireRate, int count, float spread, int burst, float burstDelay, float damage, float speed, bool ricochet, bool wave, float spiral, int pattern)
    {
        string assetPath = path + "/" + name + ".asset";
        WeaponData asset = AssetDatabase.LoadAssetAtPath<WeaponData>(assetPath);
        
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<WeaponData>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        asset.weaponName = name;
        asset.fireRate = fireRate;
        asset.bulletCount = count;
        asset.spreadAngle = spread;
        asset.burstCount = burst;
        asset.burstDelay = burstDelay;
        asset.damage = damage;
        asset.bulletSpeed = speed;
        
        // New Mechanics
        asset.ricochet = ricochet;
        asset.waveMovement = wave;
        asset.spiralRate = spiral;
        asset.fixedPatternCount = pattern;

        // Default visual settings
        asset.bulletTag = "PlayerBullet";
        asset.shakeIntensity = 0.2f; 
        
        EditorUtility.SetDirty(asset);
    }
}
