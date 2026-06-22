using UnityEngine;

public class TreeInteractionBehaviour : InteractionBehaviour
{
    protected override void Interact()
    {
        Debug.Log($"The player interacted with a tree. Object: {name}", this);
    }
}
