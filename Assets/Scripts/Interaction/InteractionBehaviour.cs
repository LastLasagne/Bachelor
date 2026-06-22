using UnityEngine;

public abstract class InteractionBehaviour : MonoBehaviour
{
    // Invoked by SOAP's native EventListenerGameObject.
    public void HandleInteractionDispatched(GameObject interactedObject)
    {
        if (interactedObject != gameObject)
        {
            return;
        }

        Interact();
    }

    protected abstract void Interact();
}
