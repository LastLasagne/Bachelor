using Obvious.Soap;
using UnityEngine;

public class QuestPointInteractionBehaviour : InteractionBehaviour
{
    [SerializeField] private ScriptableEventGameObject questMenuRequested;
    [SerializeField] private QuestDefinition quest;

    public ScriptableEventGameObject QuestMenuRequested
    {
        get => questMenuRequested;
        set => questMenuRequested = value;
    }

    public QuestDefinition Quest
    {
        get => quest;
        set => quest = value;
    }

    protected override void Interact()
    {
        if (quest != null)
        {
            questMenuRequested?.Raise(gameObject);
        }
    }
}
