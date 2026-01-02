using UnityEngine;

[RequireComponent(typeof(VectorGrid))]
public class ReactiveGrid : MonoBehaviour
{
    public static ReactiveGrid Instance;

    private VectorGrid m_VectorGrid;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        m_VectorGrid = GetComponent<VectorGrid>();
        if (m_VectorGrid == null) m_VectorGrid = gameObject.AddComponent<VectorGrid>();
    }

    void Start()
    {
        // Auto-resize removed by user request. 
        if (m_VectorGrid != null && m_VectorGrid.m_GridScale == 0.1f)
        {
             Debug.LogWarning("[ReactiveGrid] Warning: Grid Scale is default (0.1). If effects are invisible, increase Scale to 0.5+");
        }
    }

    /// <summary>
    /// Applies a radial force to the grid.
    /// </summary>
    /// <param name="position">World position of the force origin.</param>
    /// <param name="force">Magnitude of the force.</param>
    /// <param name="radius">Radius of influence.</param>
    /// <param name="color">Optional color to apply (ignored if null/default, handled by bool).</param>
    /// <param name="applyColor">Whether to apply the color.</param>
    public void ApplyForce(Vector3 position, float force, float radius, Color? color = null, bool applyColor = false)
    {
        if (m_VectorGrid != null)
        {
            // Fix: Project position onto grid plane to ensure interaction works regardless of Z depth
            Vector3 localPos = m_VectorGrid.transform.InverseTransformPoint(position);
            localPos.z = 0f;
            Vector3 projectedWorldPos = m_VectorGrid.transform.TransformPoint(localPos);

            // VectorGrid.AddGridForce(Vector3 worldPosition, float force, float radius, Color color, bool hasColor)
            m_VectorGrid.AddGridForce(projectedWorldPos, force, radius, color.GetValueOrDefault(Color.white), applyColor);
        }
    }

    [Header("Ambient Effects")]
    public bool enableAmbient = true;
    public float ambientAmplitude = 0.5f; // Max distance points wander from origin (Visual Scale)
    public float ambientStiffness = 5.0f; // How tightly they hold to the wandering target (Snapiness)
    public float noiseScale = 0.5f;    // Turbulence Scale (Spatial)
    public float timeScale = 0.5f;     // Turbulence Speed (Temporal)
    public Vector2 noiseOffset = Vector2.zero; // Manual Offset

    void Update()
    {
        if (enableAmbient && m_VectorGrid != null && m_VectorGrid.GridPoints != null)
        {
            var points = m_VectorGrid.GridPoints;
            int width = points.GetLength(0);
            int height = points.GetLength(1);
            float time = Time.time;

            // Pre-calculate common values for performance
            float noiseXOffset = noiseOffset.x;
            float noiseYOffset = noiseOffset.y;
            float timeOffset = time * timeScale;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    VectorGridPoint p = points[x, y];
                    if (p == null || p.m_InverseMass == 0) continue;

                    // --- Wandering Target Physics Model ---
                    
                    // 1. Determine "Where the point WANTS to be" (Target Position)
                    // We calculate a per-point target that swirls around the Original position
                    float sampleX = (p.m_OriginalPosition.x * noiseScale) + noiseXOffset;
                    float sampleY = (p.m_OriginalPosition.y * noiseScale) + noiseYOffset;

                    float noiseX = Mathf.PerlinNoise(sampleX + timeOffset, sampleY);
                    float noiseY = Mathf.PerlinNoise(sampleX, sampleY + timeOffset);

                    // Map 0..1 perlin noise to -1..1 range
                    float offsetX = (noiseX - 0.5f) * 2f;
                    float offsetY = (noiseY - 0.5f) * 2f;
                    
                    // The Target is the Original Position + the Noise Offset (Amplitude)
                    Vector3 targetPos = p.m_OriginalPosition;
                    targetPos.x += offsetX * ambientAmplitude;
                    targetPos.y += offsetY * ambientAmplitude;

                    // 2. Apply Spring Force towards that Target
                    // Hooke's Law: Force = -k * displacement
                    Vector3 displacement = p.m_Position - targetPos;
                    Vector3 springForce = -displacement * ambientStiffness;

                    // Apply to acceleration
                    p.m_Acceleration += springForce * p.m_InverseMass;
                }
            }
        }
    }

    /// <summary>
    /// Legacy overload for existing calls without color.
    /// </summary>
    public void ApplyForce(Vector3 position, float force, float radius)
    {
        ApplyForce(position, force, radius, null, false);
    }

    /// <summary>
    /// Sets the base color of the grid. 
    /// Note: VectorGrid doesn't have a direct runtime "SetBaseColor" for all points efficiently exposed in the snippet,
    /// so this might need adjustment or only affect future spawns/reverts if supported.
    /// For now, we will leave this as a stub or try to access VectorGrid properties if available.
    /// Reviewing VectorGrid.cs, it uses m_ThickLineSpawnColor / m_ThinLineSpawnColor for initialization.
    /// </summary>
    public void SetGridColor(Color color)
    {
        if (m_VectorGrid != null)
        {
            m_VectorGrid.m_ThickLineSpawnColor = color;
            m_VectorGrid.m_ThinLineSpawnColor = new Color(color.r, color.g, color.b, 0.5f);
            
            // Force re-init to apply if runtime change is immediate requirement?
            // m_VectorGrid.InitGrid(); // Uncomment if immediate brute-force update is needed. 
            // Warning: InitGrid() is expensive (reallocates).
        }
    }

    public void Resize(Vector2 newSize)
    {
        if (m_VectorGrid != null)
        {
            // Approximate resize by adjusting Width/Height based on GridScale
            // Assuming 1 unit scale for simplicity or keeping existing scale
            m_VectorGrid.m_GridWidth = Mathf.CeilToInt(newSize.x / m_VectorGrid.m_GridScale);
            m_VectorGrid.m_GridHeight = Mathf.CeilToInt(newSize.y / m_VectorGrid.m_GridScale);
            m_VectorGrid.InitGrid();
        }
    }
}