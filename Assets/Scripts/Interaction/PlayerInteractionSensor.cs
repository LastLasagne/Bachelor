using Obvious.Soap;
using UnityEngine;

public class PlayerInteractionSensor : MonoBehaviour
{
    [SerializeField] private float interactionRadius = 2.75f;
    [SerializeField] private LayerMask interactionLayers = ~0;
    [SerializeField] private ScriptableEventGameObject focusChanged;
    [SerializeField] private ScriptableEventGameObject interactionDispatched;

    private readonly Collider[] overlapResults = new Collider[24];
    private InteractionBehaviour focusedInteractable;

    public ScriptableEventGameObject FocusChanged { get => focusChanged; set => focusChanged = value; }
    public ScriptableEventGameObject InteractionDispatched { get => interactionDispatched; set => interactionDispatched = value; }

    private void Update()
    {
        SetFocus(FindNearestInteractable());
    }

    private InteractionBehaviour FindNearestInteractable()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            interactionRadius,
            overlapResults,
            interactionLayers,
            QueryTriggerInteraction.Collide);

        InteractionBehaviour nearest = null;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            Collider candidateCollider = overlapResults[i];
            if (candidateCollider == null)
            {
                continue;
            }

            InteractionBehaviour candidate = candidateCollider.GetComponentInParent<InteractionBehaviour>();
            if (candidate == null || !candidate.isActiveAndEnabled)
            {
                continue;
            }

            float distance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private void SetFocus(InteractionBehaviour next)
    {
        if (focusedInteractable == next)
        {
            return;
        }

        focusedInteractable = next;
        focusChanged?.Raise(focusedInteractable != null ? focusedInteractable.gameObject : null);
    }

    // Invoked by SOAP's native EventListenerNoParam.
    public void HandleInteractPressed()
    {
        if (focusedInteractable != null)
        {
            interactionDispatched?.Raise(focusedInteractable.gameObject);
        }
    }

    private void OnDisable()
    {
        focusedInteractable = null;
        focusChanged?.Raise(null);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.82f, 0.25f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
