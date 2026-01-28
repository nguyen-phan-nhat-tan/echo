using System;
using UnityEngine;

public enum GameState
{
    Intro,
    Playing,
    LoopTransition,
    Rewinding,
    GameOver,
    Paused
}

public static class GameEvents
{
    // Lifecycle
    public static Action<int> OnLoopStart;   
    public static Action OnLoopCompleted;    
    public static Action OnLoopEnded;        
    
    // State & UI
    public static Action<GameState> OnStateChanged;
    public static Action<int> OnScoreChanged;
    public static Action<float> OnTimerUpdate;
    public static Action<WeaponData> OnWeaponChange; 

    // Player Actions
    public static Action<AudioClip> OnPlayerShoot; 
    public static Action OnPlayerDash;

    // VFX / Physics Events
    public static Action<Vector2, Quaternion> OnBulletImpact; 
    public static Action<Vector2> OnEnemyExplosion;
    public static Action OnPlayerDeath;
    public static Action OnEnemyDeath;
}