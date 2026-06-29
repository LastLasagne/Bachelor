using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Animal Crossing Style Framing")]
    [SerializeField] private float fixedYaw = 0f;
    [SerializeField] private float distance = 10f;
    [SerializeField] private float height = 9f;
    [SerializeField] private float targetFocusHeight = 1f;
    [SerializeField] private float lookAheadDistance = 0f;

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.18f;

    private Vector3 velocity;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 planarOffset = Quaternion.Euler(0f, fixedYaw, 0f) * new Vector3(0f, 0f, -distance);
        Vector3 desiredPosition = target.position + planarOffset + (Vector3.up * height);
        Vector3 focusPoint = target.position + (Vector3.up * targetFocusHeight) + (target.forward * lookAheadDistance);

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        transform.rotation = Quaternion.LookRotation(focusPoint - transform.position, Vector3.up);
    }

    private void OnValidate()
    {
        distance = Mathf.Max(0.1f, distance);
        height = Mathf.Max(0.1f, height);
        smoothTime = Mathf.Max(0.01f, smoothTime);
    }
}
