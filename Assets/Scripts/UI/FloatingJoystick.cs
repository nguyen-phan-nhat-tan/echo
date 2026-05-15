using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private PlayerController player;

    [Header("Behavior")]
    [SerializeField] private float radius = 110f;
    [SerializeField] private float deadZone = 0.12f;
    [SerializeField] private bool hideWhenReleased = true;

    public Vector2 Value { get; private set; }

    private Camera uiCamera;
    private int activePointerId = int.MinValue;
    private CanvasGroup backgroundCanvasGroup;

    private void Awake()
    {
        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }

        if (background == null)
        {
            background = transform as RectTransform;
        }

        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = parentCanvas.worldCamera;
        }

        if (background != null)
        {
            backgroundCanvasGroup = background.GetComponent<CanvasGroup>();
            if (backgroundCanvasGroup == null)
            {
                backgroundCanvasGroup = background.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (hideWhenReleased)
        {
            SetBackgroundVisible(false);
        }
    }

    private void OnDisable()
    {
        ResetJoystick();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId != int.MinValue)
        {
            return;
        }

        activePointerId = eventData.pointerId;

        if (hideWhenReleased)
        {
            SetBackgroundVisible(true);
        }

        if (background != null)
        {
            background.position = eventData.position;
        }

        UpdateJoystick(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
        {
            return;
        }

        UpdateJoystick(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
        {
            return;
        }

        ResetJoystick();
    }

    private void UpdateJoystick(PointerEventData eventData)
    {
        if (background == null || handle == null)
        {
            return;
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, uiCamera, out localPoint);

        Vector2 normalized = localPoint / Mathf.Max(1f, radius);
        if (normalized.magnitude > 1f)
        {
            normalized = normalized.normalized;
        }

        if (normalized.magnitude < deadZone)
        {
            normalized = Vector2.zero;
        }

        Value = normalized;
        handle.anchoredPosition = normalized * radius;

        if (player != null)
        {
            player.virtualMove = Value;
        }
    }

    private void ResetJoystick()
    {
        activePointerId = int.MinValue;
        Value = Vector2.zero;

        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }

        if (player != null)
        {
            player.virtualMove = Vector2.zero;
        }

        if (hideWhenReleased)
        {
            SetBackgroundVisible(false);
        }
    }

    private void SetBackgroundVisible(bool visible)
    {
        if (backgroundCanvasGroup == null)
        {
            return;
        }

        backgroundCanvasGroup.alpha = visible ? 1f : 0f;
        backgroundCanvasGroup.blocksRaycasts = true;
        backgroundCanvasGroup.interactable = false;
    }
}