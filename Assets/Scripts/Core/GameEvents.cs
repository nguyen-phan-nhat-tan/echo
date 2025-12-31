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
    public static Action OnPlayerDeath;
    public static Action OnEnemyDeath;
    
    // Lifecycle
    public static Action<int> OnLoopStart;
    public static Action OnLoopCompleted;
    public static Action OnLoopEnded;
    
    // State & UI
    public static Action<GameState> OnStateChanged;
    public static Action<int> OnScoreChanged;
    public static Action<float> OnTimerUpdate;
    public static Action<WeaponData> OnWeaponChange; 
    
    // VFX Events
    public static Action<Vector2, Quaternion> OnBulletImpact; 
    public static Action<Vector2> OnEnemyExplosion;
    public static Action OnPlayerHit;
}