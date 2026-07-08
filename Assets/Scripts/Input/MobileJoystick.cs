using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform handle;
    [SerializeField, Range(0.1f, 1f)] private float handleRange = 0.55f;
    [SerializeField] private bool hideWhenReleased = false;

    private RectTransform background;
    private Canvas canvas;
    private Camera uiCamera;
    private Vector2 input;
    private bool inputEnabled = true;

    public Vector2 Input => inputEnabled ? input : Vector2.zero;
    public float Horizontal => Input.x;
    public float Vertical => Input.y;

    private void Awake()
    {
        background = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        if (handle == null && transform.childCount > 0)
        {
            handle = transform.GetChild(0) as RectTransform;
        }

        SetVisible(!hideWhenReleased);
        ResetHandle();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!inputEnabled)
        {
            return;
        }

        SetVisible(true);
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!inputEnabled)
        {
            return;
        }

        if (background == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            uiCamera,
            out Vector2 localPoint);

        Vector2 radius = background.sizeDelta * 0.5f;
        input = new Vector2(
            radius.x > 0f ? localPoint.x / radius.x : 0f,
            radius.y > 0f ? localPoint.y / radius.y : 0f);

        input = input.magnitude > 1f ? input.normalized : input;

        if (handle != null)
        {
            handle.anchoredPosition = new Vector2(
                input.x * radius.x * handleRange,
                input.y * radius.y * handleRange);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        ResetHandle();
        SetVisible(inputEnabled && !hideWhenReleased);
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!inputEnabled)
        {
            input = Vector2.zero;
            ResetHandle();
        }

        SetVisible(inputEnabled && !hideWhenReleased);
    }

    private void ResetHandle()
    {
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
    }

    private void SetVisible(bool visible)
    {
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = visible ? 1f : 0f;
        }
    }
}
