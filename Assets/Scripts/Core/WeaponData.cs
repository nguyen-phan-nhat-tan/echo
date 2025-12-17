using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "ScriptableObjects/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Stats")]
    public string weaponName = "Pistol";
    public float fireRate = 5f;       // Shots per second
    public int bulletCount = 1;       // 1 = Pistol, 5 = Shotgun
    public float spreadAngle = 0f;    // 0 = Accurate, 15 = Spread
    
    [Header("Visuals")]
    public string bulletTag = "PlayerBullet"; // Matches your ObjectPooler tag
    public float shakeIntensity = 0.2f;       // Juice setting
}