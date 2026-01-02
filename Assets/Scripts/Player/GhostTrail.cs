using UnityEngine;
using System.Collections.Generic;
using DG.Tweening; // Assuming DOTween is available as per PlayerController usage

public class GhostTrail : MonoBehaviour
{
    [Header("Settings")]
    public float delay = 0.1f;
    public float lifetime = 0.5f;
    public Color trailColor = new Color(1, 1, 1, 0.5f);
    public bool isEnabled = true;

    [Header("References")]
    public SpriteRenderer targetSR;
    public Transform targetTransform;

    private float timer;
    
    // Simple local pool to avoid Garbage Collection
    private Queue<GameObject> trailPool = new Queue<GameObject>();
    private List<GameObject> activeTrails = new List<GameObject>();
    private Transform trailContainer;

    void Awake()
    {
        if (targetSR == null) targetSR = GetComponent<SpriteRenderer>();
        if (targetTransform == null) targetTransform = transform;

        // Create a dedicated container to keep hierarchy clean
        GameObject container = new GameObject($"Trails_{gameObject.name}");
        trailContainer = container.transform;
    }

    void Update()
    {
        if (!isEnabled) return;

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            SpawnTrail();
            timer = delay;
        }
    }

    void SpawnTrail()
    {
        if (targetSR == null || targetSR.sprite == null) return;

        GameObject trailPart = GetTrailFromPool();
        
        // Align
        trailPart.transform.position = targetTransform.position;
        trailPart.transform.rotation = targetTransform.rotation;
        trailPart.transform.localScale = targetTransform.localScale;

        // Setup Visuals
        SpriteRenderer sr = trailPart.GetComponent<SpriteRenderer>();
        sr.sprite = targetSR.sprite;
        sr.color = trailColor;
        sr.sortingLayerID = targetSR.sortingLayerID;
        sr.sortingOrder = targetSR.sortingOrder - 1; // Behind player

        // Animate
        // 1. Fade out. Link to gameObject so tween dies if object dies.
        sr.DOFade(0f, lifetime)
          .SetEase(Ease.Linear)
          .SetLink(trailPart)
          .OnComplete(() => ReturnToPool(trailPart));
    }

    GameObject GetTrailFromPool()
    {
        GameObject obj;
        if (trailPool.Count > 0)
        {
            obj = trailPool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        // Create new if pool empty
        obj = new GameObject("Trail");
        obj.transform.SetParent(trailContainer);
        obj.AddComponent<SpriteRenderer>();
        return obj;
    }

    void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        // Reset color alpha for next use
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        Color c = sr.color;
        c.a = 1f; 
        sr.color = c;
        
        trailPool.Enqueue(obj);
    }

    void OnDestroy()
    {
        if (trailContainer != null) Destroy(trailContainer.gameObject);
    }
}
