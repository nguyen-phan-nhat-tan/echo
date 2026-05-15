using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MiniMapController : MonoBehaviour
{
    [Header("Mini Map References")]
    public Image miniMapBackground;
    public RectTransform miniMapContainer;

    [Header("Settings")]
    public float miniMapRadius = 48f; // 200x200 means 100 pixel radius
    public float dotSize = 4f;
    public Color ringColor = new Color(0.063f, 0.122f, 0.294f, 1f); // #101F4B
    public Color playerDotColor = Color.blue;
    public Color enemyDotColor = Color.red;

    private List<Image> activeDots = new List<Image>();
    private GameObject dotPrefab;
    private CanvasGroup miniMapCanvasGroup;
    private static Sprite circleSprite;

    void Start()
    {
        // Setup canvas group for show/hide
        if (miniMapContainer == null)
        {
            Debug.LogError("[MiniMapController] miniMapContainer not assigned!");
            return;
        }

        miniMapCanvasGroup = miniMapContainer.GetComponent<CanvasGroup>();
        if (miniMapCanvasGroup == null)
        {
            miniMapCanvasGroup = miniMapContainer.gameObject.AddComponent<CanvasGroup>();
        }

        // Initially hidden
        SetMiniMapVisible(false);

        // Subscribe to game state changes
        GameEvents.OnStateChanged += OnGameStateChanged;
    }

    void OnDestroy()
    {
        GameEvents.OnStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState newState)
    {
        // Only show during Playing, unless the current debuff disables it
        bool canShow = newState == GameState.Playing && !IsMiniMapDisabledByDebuff();
        SetMiniMapVisible(canShow);
    }

    private bool IsMiniMapDisabledByDebuff()
    {
        return GameManager.Instance != null &&
               GameManager.Instance.currentActiveDebuff != null &&
               GameManager.Instance.currentActiveDebuff.disableMiniMap;
    }

    void Update()
    {
        if (miniMapCanvasGroup == null || miniMapCanvasGroup.alpha == 0) return;

        // Clear previous dots
        foreach (var dot in activeDots)
        {
            if (dot != null) Destroy(dot.gameObject);
        }
        activeDots.Clear();

        // Draw player
        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            DrawDot(GameManager.Instance.player.transform.position, playerDotColor, dotSize);
        }

        // Draw enemies
        if (GameManager.Instance != null)
        {
            // Access active echoes through reflection or public method
            // For now, we'll use GetComponentsInChildren to find all EchoControllers
            EchoController[] echoes = FindObjectsOfType<EchoController>();
            foreach (var echo in echoes)
            {
                if (echo != null && echo.gameObject.CompareTag("Enemy"))
                {
                    DrawDot(echo.transform.position, enemyDotColor, dotSize);
                }
            }
        }

        // Draw projectiles
        Bullet[] bullets = FindObjectsOfType<Bullet>();
        foreach (var bullet in bullets)
        {
            if (bullet != null)
            {
                // Determine color based on bullet owner
                Color dotColor = playerDotColor; // Default: player bullet
                if (bullet.CompareTag("EnemyBullet")) dotColor = enemyDotColor;
                
                DrawDot(bullet.transform.position, dotColor, dotSize * 0.5f);
            }
        }
    }

    private void DrawDot(Vector3 worldPos, Color color, float size)
    {
        if (miniMapContainer == null) return;

        // Map world position to mini map coordinates
        // Assume map center is at (0, 0)
        Vector2 mapSize = GameManager.Instance != null ? GameManager.Instance.mapSize : Vector2.one * 70f;
        float mapHalfWidth = mapSize.x / 2f;
        float mapHalfHeight = mapSize.y / 2f;

        // Clamp to map bounds and normalize to [0, 1] range
        float normalizedX = (worldPos.x + mapHalfWidth) / mapSize.x;
        float normalizedY = (worldPos.y + mapHalfHeight) / mapSize.y;

        // Convert to mini map circle coordinates
        // Map normalized [0,1] to circle radius
        float circleX = (normalizedX - 0.5f) * 2f * miniMapRadius;
        float circleY = (normalizedY - 0.5f) * 2f * miniMapRadius;

        // Check if within circle
        float distFromCenter = Mathf.Sqrt(circleX * circleX + circleY * circleY);
        if (distFromCenter > miniMapRadius) return; // Outside circle

        // Create dot
        GameObject dotObj = new GameObject("MiniMapDot");
        RectTransform dotRect = dotObj.AddComponent<RectTransform>();
        Image dotImage = dotObj.AddComponent<Image>();

        // Parent to mini map container
        dotRect.SetParent(miniMapContainer, false);

        // Size
        dotRect.sizeDelta = new Vector2(size, size);

        // Position
        dotRect.anchoredPosition = new Vector2(circleX, circleY);

        // Color
        dotImage.color = color;
        dotImage.sprite = GetCircleSprite();
        dotImage.type = Image.Type.Simple;
        dotImage.preserveAspect = true;

        activeDots.Add(dotImage);
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        const int textureSize = 64;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        float radius = textureSize / 2f - 1f;
        float edgeSoftness = 1.5f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 pixel = new Vector2(x + 0.5f, y + 0.5f);
                float distance = Vector2.Distance(pixel, center);
                float alpha = Mathf.Clamp01((radius - distance) / edgeSoftness);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
        return circleSprite;
    }

    private void SetMiniMapVisible(bool visible)
    {
        if (miniMapCanvasGroup != null)
        {
            miniMapCanvasGroup.alpha = visible ? 1f : 0f;
            miniMapCanvasGroup.blocksRaycasts = visible;
        }
    }
}
