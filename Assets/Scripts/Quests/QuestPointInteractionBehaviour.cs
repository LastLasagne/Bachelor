using Obvious.Soap;
using UnityEngine;

public class QuestPointInteractionBehaviour : InteractionBehaviour
{
    [SerializeField] private ScriptableEventGameObject questMenuRequested;
    [SerializeField] private QuestCategory hubCategory = QuestCategory.Trash;
    [SerializeField] private QuestDefinition quest;

    public ScriptableEventGameObject QuestMenuRequested
    {
        get => questMenuRequested;
        set => questMenuRequested = value;
    }

    public QuestCategory HubCategory
    {
        get => hubCategory;
        set => hubCategory = value;
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (quest != null)
        {
            hubCategory = quest.Category;
        }
    }
#endif
}
