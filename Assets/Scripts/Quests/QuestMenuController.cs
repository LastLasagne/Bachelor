using System.Collections.Generic;
using System.Text;
using Firebase.Analytics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class QuestMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Text menuTitle;
    [SerializeField] private Button questButton;
    [SerializeField] private Text questTextLabel;
    [SerializeField] private Text questProgressLabel;
    [SerializeField] private GameObject successHintPanel;
    [SerializeField] private Text successHintText;
    [SerializeField] private FirebaseNegativePhotoNotificationController negativePhotoNotificationController;
    [SerializeField, TextArea(2, 4)] private string completedQuestMessage = "Completed! Come back tomorrow for a new quest.";

    private readonly Dictionary<QuestDefinition, int> questProgress = new Dictionary<QuestDefinition, int>();
    private QuestDefinition currentQuest;
    private QuestCategory currentHubCategory = QuestCategory.Trash;
    private bool registeredAsOpenHubMenu;
    private readonly List<QuestEntryView> questEntries = new List<QuestEntryView>();
    private Color repeatableDefaultBackgroundColor;
    private Color repeatableDefaultTextColor;
    private Color rareDefaultBackgroundColor;
    private Color rareDefaultTextColor;

    public GameObject MenuPanel { get => menuPanel; set => menuPanel = value; }
    public Text MenuTitle { get => menuTitle; set => menuTitle = value; }
    public Button QuestButton { get => questButton; set => questButton = value; }
    public Text QuestTextLabel { get => questTextLabel; set => questTextLabel = value; }
    public Text QuestProgressLabel { get => questProgressLabel; set => questProgressLabel = value; }
    public GameObject SuccessHintPanel { get => successHintPanel; set => successHintPanel = value; }
    public Text SuccessHintText { get => successHintText; set => successHintText = value; }
    public FirebaseNegativePhotoNotificationController NegativePhotoNotificationController { get => negativePhotoNotificationController; set => negativePhotoNotificationController = value; }
    public QuestDefinition CurrentQuest => currentQuest;
    public string CompletedQuestMessage { get => completedQuestMessage; set => completedQuestMessage = value; }

    private void Awake()
    {
        menuPanel?.SetActive(false);
        successHintPanel?.SetActive(false);
        CaptureDefaultQuestColors();
    }

    private void CaptureDefaultQuestColors()
    {
        if (questButton == null) return;
        Image repeatableImage = questButton.GetComponent<Image>();
        Text repeatableText = questButton.transform.Find(questTextLabel.gameObject.name)?.GetComponent<Text>();
        repeatableDefaultBackgroundColor = repeatableImage != null ? repeatableImage.color : Color.white;
        repeatableDefaultTextColor = repeatableText != null ? repeatableText.color : Color.black;

        Transform rareTransform = questButton.transform.parent.Find("Rare Quest");
        Image rareImage = rareTransform != null ? rareTransform.GetComponent<Image>() : null;
        Text rareText = rareTransform != null ? rareTransform.Find(questTextLabel.gameObject.name)?.GetComponent<Text>() : null;
        rareDefaultBackgroundColor = rareImage != null ? rareImage.color : repeatableDefaultBackgroundColor;
        rareDefaultTextColor = rareText != null ? rareText.color : repeatableDefaultTextColor;
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

        currentHubCategory = point.HubCategory;
        currentQuest = point.Quest;

        int savedProgress = GameProgressManager.Instance != null ? GameProgressManager.Instance.GetProgress(currentQuest) : 0;
        questProgress[currentQuest] = savedProgress;

        if (menuTitle != null)
        {
            menuTitle.text = $"{currentHubCategory} Quests";
        }

        successHintPanel?.SetActive(false);
        RebuildQuestEntries();
        menuPanel.SetActive(true);
        SetHubMenuOpen(true);
        RefreshQuestDisplay();
        negativePhotoNotificationController?.CheckForNegativePhotos(point);
    }

    private sealed class QuestEntryView
    {
        public QuestDefinition Quest;
        public GameObject GameObject;
        public Button Button;
        public Text Description;
        public Text Progress;
        public Image Background;
        public UnityAction ClickAction;
        public Color ActiveBackgroundColor;
        public Color ActiveTextColor;
    }

    private void RebuildQuestEntries()
    {
        foreach (QuestEntryView oldEntry in questEntries)
            if (oldEntry.Button != null && oldEntry.ClickAction != null) oldEntry.Button.onClick.RemoveListener(oldEntry.ClickAction);
        questEntries.Clear();

        GameProgressManager manager = GameProgressManager.Instance;
        if (manager == null || questButton == null) return;
        BindQuestEntry(questButton.gameObject, manager.GetActiveRepeatableQuest(currentHubCategory), "Repeatable Quest");
        Transform rareTransform = questButton.transform.parent.Find("Rare Quest");
        if (rareTransform != null) BindQuestEntry(rareTransform.gameObject, manager.GetActiveRareQuest(currentHubCategory), "Rare Quest");
        else Debug.LogError("The serialized Rare Quest UI object is missing from the Quest Menu Panel.", this);
    }

    private void BindQuestEntry(GameObject row, QuestDefinition quest, string displayName)
    {
        if (row == null || quest == null) return;
        row.name = displayName;
        row.SetActive(true);
        Button button = row.GetComponent<Button>();
        Text description = row.transform.Find(questTextLabel.gameObject.name)?.GetComponent<Text>();
        Text progress = row.transform.Find(questProgressLabel.gameObject.name)?.GetComponent<Text>();
        Image background = row.GetComponent<Image>();
        UnityAction clickAction = () => SelectQuest(quest);
        var entry = new QuestEntryView
        {
            Quest = quest,
            GameObject = row,
            Button = button,
            Description = description,
            Progress = progress,
            Background = background,
            ClickAction = clickAction,
            ActiveBackgroundColor = row == questButton.gameObject ? repeatableDefaultBackgroundColor : rareDefaultBackgroundColor,
            ActiveTextColor = row == questButton.gameObject ? repeatableDefaultTextColor : rareDefaultTextColor
        };
        questEntries.Add(entry);
        button.onClick.AddListener(clickAction);
        RefreshQuestEntry(entry);
    }

    private void SelectQuest(QuestDefinition quest)
    {
        currentQuest = quest;
        questProgress[quest] = GameProgressManager.Instance != null ? GameProgressManager.Instance.GetProgress(quest) : 0;
    }

    private void RefreshQuestEntry(QuestEntryView entry)
    {
        GameProgressManager manager = GameProgressManager.Instance;
        if (manager == null || entry == null || entry.Quest == null) return;
        bool completed = manager.IsCompletedToday(entry.Quest);
        if (entry.Description != null)
            entry.Description.text = completed ? completedQuestMessage : entry.Quest.QuestText;
        if (entry.Progress != null)
            entry.Progress.text = completed ? string.Empty : $"{manager.GetProgress(entry.Quest)}/{manager.GetAmountDue(entry.Quest)}";
        if (entry.Button != null) entry.Button.interactable = !completed;
        if (entry.Background != null) entry.Background.color = completed ? new Color(0.45f, 0.45f, 0.45f, 0.85f) : entry.ActiveBackgroundColor;
        if (entry.Description != null) entry.Description.color = completed ? Color.black : entry.ActiveTextColor;
    }
    // Invoked by SOAP's native EventListenerNoParam.
    public void HandleQuestProgressRequested()
    {
        if (currentQuest == null || successHintPanel == null || successHintPanel.activeSelf)
        {
            return;
        }

        if (GameProgressManager.Instance != null && GameProgressManager.Instance.IsCompletedToday(currentQuest)) return;
        int amountDue = GameProgressManager.Instance != null ? GameProgressManager.Instance.GetAmountDue(currentQuest) : currentQuest.AmountDue;
        int progress = questProgress[currentQuest];
        progress = Mathf.Min(progress + 1, amountDue);
        questProgress[currentQuest] = progress;
        GameProgressManager.Instance?.SetQuestProgress(currentQuest, progress);
        RefreshQuestDisplay();

        if (progress >= amountDue)
        {
            CompleteCurrentQuest();
        }
    }

    private void CompleteCurrentQuest()
    {
        GameProgressManager.Instance?.CompleteQuest(currentQuest);
        int previousCategoryTotal = currentQuest.CategoryTotalProgression != null
            ? currentQuest.CategoryTotalProgression.Value
            : 0;

        if (currentQuest.CategoryTotalProgression != null)
        {
            currentQuest.CategoryTotalProgression.Add(currentQuest.ProgressionValue);
        }

        int categoryTotal = currentQuest.CategoryTotalProgression != null
            ? currentQuest.CategoryTotalProgression.Value
            : previousCategoryTotal + currentQuest.ProgressionValue;

        Debug.Log(
            $"Quest completed: {currentQuest.QuestText} " +
            $"Progression +{currentQuest.ProgressionValue}. " +
            $"{currentQuest.Category} total progression: {categoryTotal}",
            this);
        LogQuestCompletedEvent(currentQuest, previousCategoryTotal, categoryTotal);

        if (successHintText != null)
        {
            successHintText.text = BuildSuccessHintText(currentQuest, previousCategoryTotal, categoryTotal);
        }

        if (questButton != null)
        {
            questButton.interactable = false;
        }

        RefreshQuestDisplay();
        successHintPanel?.SetActive(true);
    }
    private void LogQuestCompletedEvent(QuestDefinition quest, int previousProgression, int newProgression)
    {
        if (quest == null)
        {
            return;
        }

        try
        {
            int amountDue = GameProgressManager.Instance != null ? GameProgressManager.Instance.GetAmountDue(quest) : quest.AmountDue;
            FirebaseAnalytics.LogEvent(
                "quest_completed",
                new Parameter("quest_id", quest.name),
                new Parameter("quest_category", quest.Category.ToString().ToLowerInvariant()),
                new Parameter("quest_frequency", quest.Frequency.ToString().ToLowerInvariant()),
                new Parameter("amount_due", amountDue),
                new Parameter("progression_value", quest.ProgressionValue),
                new Parameter("previous_progression", previousProgression),
                new Parameter("new_progression", newProgression));
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"Could not log quest_completed analytics event: {exception.Message}", this);
        }
    }


    // Invoked by SOAP's native EventListenerNoParam.
    public void HandleSuccessHintDismissRequested()
    {
        if (successHintPanel == null || !successHintPanel.activeSelf)
        {
            return;
        }

        successHintPanel.SetActive(false);



        RefreshQuestDisplay();
    }

    // Invoked by SOAP's native EventListenerNoParam.
    public void HandleCloseRequested()
    {
        successHintPanel?.SetActive(false);
        menuPanel?.SetActive(false);
        SetHubMenuOpen(false);
        currentQuest = null;
    }

    private void OnDisable()
    {
        SetHubMenuOpen(false);
    }

    private void SetHubMenuOpen(bool open)
    {
        if (registeredAsOpenHubMenu == open)
        {
            return;
        }

        registeredAsOpenHubMenu = open;

        if (registeredAsOpenHubMenu)
        {
            HubMenuState.RegisterOpen(this);
        }
        else
        {
            HubMenuState.RegisterClosed(this);
        }
    }

    private void RefreshQuestDisplay()
    {
        foreach (QuestEntryView entry in questEntries)
            RefreshQuestEntry(entry);
    }

    private IEnumerable<string> BuildRewardMessages(QuestDefinition quest, int previousProgression, int newProgression)
    {
        foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var provider = behaviour as IQuestRewardMessageProvider;
            if (provider == null)
            {
                continue;
            }

            IEnumerable<string> messages = provider.BuildRewardMessages(quest, previousProgression, newProgression);
            if (messages == null)
            {
                continue;
            }

            foreach (string message in messages)
            {
                yield return message;
            }
        }
    }

    private string BuildSuccessHintText(QuestDefinition quest, int previousProgression, int newProgression)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Quest Complete!");
        builder.AppendLine();

        bool addedContent = false;
        foreach (string rewardMessage in BuildRewardMessages(quest, previousProgression, newProgression))
        {
            if (!string.IsNullOrWhiteSpace(rewardMessage))
            {
                builder.AppendLine(rewardMessage);
                addedContent = true;
            }
        }

        if (addedContent)
        {
            builder.AppendLine();
        }

        foreach (string hint in quest.SuccessKnowledgeHints)
        {
            if (!string.IsNullOrWhiteSpace(hint))
            {
                builder.AppendLine(hint);
                builder.AppendLine();
                addedContent = true;
            }
        }

        if (!addedContent)
        {
            builder.AppendLine("No reward details have been added yet.");
            builder.AppendLine();
        }

        builder.Append("Tap to continue");
        return builder.ToString();
    }
}


