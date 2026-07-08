using Obvious.Soap;
using UnityEngine;

public class ShopHubInteractionBehaviour : InteractionBehaviour
{
    [SerializeField] private ShopController shopController;
    [SerializeField] private ScriptableEventGameObject interactionDispatched;

    private bool subscribedToInteraction;

    public ShopController ShopController
    {
        get => shopController;
        set => shopController = value;
    }

    public ScriptableEventGameObject InteractionDispatched
    {
        get => interactionDispatched;
        set => interactionDispatched = value;
    }

    private void OnEnable()
    {
        SubscribeToInteractionEvent();
    }

    private void Start()
    {
        SubscribeToInteractionEvent();
    }

    private void OnDisable()
    {
        UnsubscribeFromInteractionEvent();
    }

    private void SubscribeToInteractionEvent()
    {
        if (subscribedToInteraction || interactionDispatched == null)
        {
            return;
        }

        interactionDispatched.OnRaised += HandleInteractionDispatched;
        subscribedToInteraction = true;
    }

    private void UnsubscribeFromInteractionEvent()
    {
        if (!subscribedToInteraction || interactionDispatched == null)
        {
            return;
        }

        interactionDispatched.OnRaised -= HandleInteractionDispatched;
        subscribedToInteraction = false;
    }

    protected override void Interact()
    {
        if (shopController == null)
        {
            shopController = FindFirstObjectByType<ShopController>(FindObjectsInactive.Include);
        }

        if (shopController != null)
        {
            shopController.OpenShop();
        }
        else
        {
            Debug.LogWarning("Shop hub was used, but no ShopController exists in the scene.", this);
        }
    }
}