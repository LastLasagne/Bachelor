using UnityEngine;

public class WorldInteractionPrompt : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = Vector3.up * 1.4f;

    private Transform followTarget;
    private Camera targetCamera;

    public Transform FollowTarget
    {
        get => followTarget;
        set => followTarget = value;
    }

    public Vector3 WorldOffset
    {
        get => worldOffset;
        set => worldOffset = value;
    }

    private void Awake()
    {
        if (followTarget == null && transform.parent != null)
        {
            followTarget = transform.parent;
        }

        targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.position + worldOffset;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(
                transform.position - targetCamera.transform.position,
                Vector3.up);
        }
    }
}
