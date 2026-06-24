using System.Collections.Generic;
using Obvious.Soap;
using UnityEngine;

public enum QuestCategory
{
    Trash,
    Food
}

public enum QuestFrequency
{
    Repeatable,
    Rare
}

[CreateAssetMenu(
    fileName = "Quest_New",
    menuName = "Game/Quests/Quest Definition")]
public class QuestDefinition : ScriptableBase
{
    [Header("Quest")]
    [SerializeField] private QuestCategory category;
    [SerializeField] private QuestFrequency frequency = QuestFrequency.Repeatable;

    [SerializeField, TextArea(3, 8)]
    private string questText;

    [SerializeField, Min(1)]
    private int amountDue = 1;

    [Header("Progression")]
    [Tooltip("Value added to this category's total progression when the quest succeeds.")]
    [SerializeField, Min(0)]
    private int progressionValue = 1;

    [Tooltip("SOAP progression total owned by this quest's category.")]
    [SerializeField]
    private IntVariable categoryTotalProgression;

    [Header("Knowledge Hints")]
    [Tooltip("Knowledge hints awarded when the quest succeeds.")]
    [SerializeField]
    private List<string> successKnowledgeHints = new List<string>();

    [Tooltip("Knowledge hints shown when the quest fails.")]
    [SerializeField]
    private List<string> failureKnowledgeHints = new List<string>();

    public QuestCategory Category => category;
    public QuestFrequency Frequency => frequency;
    public string QuestText => questText;
    public int AmountDue => amountDue;
    public int ProgressionValue => progressionValue;
    public IntVariable CategoryTotalProgression => categoryTotalProgression;
    public IReadOnlyList<string> SuccessKnowledgeHints => successKnowledgeHints;
    public IReadOnlyList<string> FailureKnowledgeHints => failureKnowledgeHints;

    private void OnValidate()
    {
        amountDue = Mathf.Max(1, amountDue);
        progressionValue = Mathf.Max(0, progressionValue);
    }
}
