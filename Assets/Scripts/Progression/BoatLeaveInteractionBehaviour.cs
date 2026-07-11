using Obvious.Soap;
using UnityEngine;

public class BoatLeaveInteractionBehaviour : InteractionBehaviour
{
    [SerializeField] private ScriptableEventGameObject interactionDispatched;
    [SerializeField] private GameFlowStoryController gameFlowStoryController;

    private bool subscribed;

    private void OnEnable() => Subscribe();
    private void Start() => Subscribe();

    private void OnDisable()
    {
        if (subscribed && interactionDispatched != null)
        {
            interactionDispatched.OnRaised -= HandleInteractionDispatched;
        }

        subscribed = false;
    }

    private void Subscribe()
    {
        if (subscribed || interactionDispatched == null)
        {
            return;
        }

        interactionDispatched.OnRaised += HandleInteractionDispatched;
        subscribed = true;
    }

    protected override void Interact()
    {
        if (gameFlowStoryController == null)
        {
            gameFlowStoryController = FindFirstObjectByType<GameFlowStoryController>(FindObjectsInactive.Include);
        }

        gameFlowStoryController?.TryLeaveIsland();
    }
}