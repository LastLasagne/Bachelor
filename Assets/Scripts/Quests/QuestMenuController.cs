using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class QuestMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Text menuTitle;
    [SerializeField] private Button questButton;
    [SerializeField] private Text questTextLabel;
    [SerializeField] private Text questProgressLabel;
    [SerializeField] private GameObject successHintPanel;
    [SerializeField] private Text successHintText;

    private readonly Dictionary<QuestDefinition, int> questProgress = new Dictionary<QuestDefinition, int>();
    private QuestDefinition currentQuest;

    public GameObject MenuPanel { get => menuPanel; set => menuPanel = value; }
    public Text MenuTitle { get => menuTitle; set => menuTitle = value; }
    public Button QuestButton { get => questButton; set => questButton = value; }
    public Text QuestTextLabel { get => questTextLabel; set => questTextLabel = value; }
    public Text QuestProgressLabel { get => questProgressLabel; set => questProgressLabel = value; }
    public GameObject SuccessHintPanel { get => successHintPanel; set => successHintPanel = value; }
    public Text SuccessHintText { get => successHintText; set => successHintText = value; }
    public QuestDefinition CurrentQuest => currentQuest;

    private void Awake()
    {
        menuPanel?.SetActive(false);
        successHintPanel?.SetActive(false);
    }

    // Invoked by SOAP's native EventListenerGameObject.
    public void HandleMenuRequested(GameObject pointObject)
    {
        if (pointObject == null || menuPanel == null)
        {
            return;
        }

        QuestPointInteractionBehaviour point = pointObject.GetComponent<QuestPointInteractionBehaviour>();
        if (point == null || point.Quest == null)
        {
            return;
        }

        currentQuest = point.Quest;

        if (!questProgress.ContainsKey(currentQuest))
        {
            questProgress.Add(currentQuest, 0);
        }

        if (menuTitle != null)
        {
            menuTitle.text = $"{currentQuest.Category} Quest";
        }

        successHintPanel?.SetActive(false);
        menuPanel.SetActive(true);
        RefreshQuestDisplay();
    }

    // Invoked by SOAP's native EventListenerNoParam.
    public void HandleQuestProgressRequested()
    {
        if (currentQuest == null || successHintPanel == null || successHintPanel.activeSelf)
        {
            return;
        }

        int progress = questProgress[currentQuest];
        progress = Mathf.Min(progress + 1, currentQuest.AmountDue);
        questProgress[currentQuest] = progress;
        RefreshQuestDisplay();

        if (progress >= currentQuest.AmountDue)
        {
            CompleteCurrentQuest();
        }
    }

    private void CompleteCurrentQuest()
    {
        if (currentQuest.CategoryTotalProgression != null)
        {
            currentQuest.CategoryTotalProgression.Add(currentQuest.ProgressionValue);
        }

        int categoryTotal = currentQuest.CategoryTotalProgression != null
            ? currentQuest.CategoryTotalProgression.Value
            : 0;

        Debug.Log(
            $"Quest completed: {currentQuest.QuestText} " +
            $"Progression +{currentQuest.ProgressionValue}. " +
            $"{currentQuest.Category} total progression: {categoryTotal}",
            this);

        if (successHintText != null)
        {
            successHintText.text = BuildSuccessHintText(currentQuest);
        }

        if (questButton != null)
        {
            questButton.interactable = false;
        }

        successHintPanel?.SetActive(true);
    }

    // Invoked by SOAP's native EventListenerNoParam.
    public void HandleSuccessHintDismissRequested()
    {
        if (successHintPanel == null || !successHintPanel.activeSelf)
        {
            return;
        }

        successHintPanel.SetActive(false);

        if (currentQuest != null && currentQuest.Frequency == QuestFrequency.Repeatable)
        {
            questProgress[currentQuest] = 0;
        }

        RefreshQuestDisplay();
    }

    // Invoked by SOAP's native EventListenerNoParam.
    public void HandleCloseRequested()
    {
        successHintPanel?.SetActive(false);
        menuPanel?.SetActive(false);
        currentQuest = null;
    }

    private void RefreshQuestDisplay()
    {
        if (currentQuest == null)
        {
            return;
        }

        int progress = questProgress[currentQuest];

        if (questTextLabel != null)
        {
            questTextLabel.text = currentQuest.QuestText;
        }

        if (questProgressLabel != null)
        {
            questProgressLabel.text = $"{progress}/{currentQuest.AmountDue}";
        }

        if (questButton != null)
        {
            questButton.interactable = progress < currentQuest.AmountDue;
        }
    }

    private static string BuildSuccessHintText(QuestDefinition quest)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Quest Complete!");
        builder.AppendLine();

        if (quest.SuccessKnowledgeHints.Count == 0)
        {
            builder.AppendLine("No knowledge hint has been added yet.");
        }
        else
        {
            foreach (string hint in quest.SuccessKnowledgeHints)
            {
                if (!string.IsNullOrWhiteSpace(hint))
                {
                    builder.AppendLine(hint);
                    builder.AppendLine();
                }
            }
        }

        builder.Append("Tap to continue");
        return builder.ToString();
    }
}
