using UnityEngine;

public class RockInteractionBehaviour : InteractionBehaviour
{
    protected override void Interact()
    {
        Debug.Log($"The player interacted with a rock. Object: {name}", this);
    }
}
