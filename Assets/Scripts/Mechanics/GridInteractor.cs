using UnityEngine;

public class GridInteractor : MonoBehaviour
{
    [Header("Settings")]
    public float force = 5f;
    public float radius = 2f;
    public Color color = Color.white;
    public bool applyColor = false;
    public bool triggerOnStart = false;
    public bool triggerOnEnable = false;

    void Start()
    {
        if (triggerOnStart) TriggerForce();
    }

    void OnEnable()
    {
        if (triggerOnEnable) TriggerForce();
    }

    public void TriggerForce()
    {
        TriggerForce(transform.position);
    }

    public void TriggerForce(Vector3 position)
    {
        if (ReactiveGrid.Instance != null)
        {
            ReactiveGrid.Instance.ApplyForce(position, force, radius, applyColor ? color : (Color?)null, applyColor);
        }
    }
}
