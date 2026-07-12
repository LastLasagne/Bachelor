using UnityEngine;

public class GameFlowStoryController : MonoBehaviour
{
    [Header("Progress Requirements")]
    [SerializeField, Min(0)] private int requiredFoodProgression = 10;
    [SerializeField, Min(0)] private int requiredTrashProgression = 10;
    [SerializeField, Min(0)] private int requiredGalleryProgression = 20;
    [SerializeField] private ShopItemDefinition requiredBoatItem;

    [Header("Text")]
    [SerializeField, TextArea(4, 10)] private string openingStoryText = "Opening story placeholder. Explain why the player has arrived on the island.";
    [SerializeField, TextArea(4, 10)] private string endingStoryText = "Ending story placeholder. Explain what happens when the player leaves the island.";
    [SerializeField, TextArea(2, 5)] private string blockedLeaveText = "There is still something to do here";

    [Header("Runtime UI")]
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private RectTransform uiRoot;
    [SerializeField] private GameObject blackScreenPanel;
    [SerializeField] private UnityEngine.UI.Text blackScreenText;
    [SerializeField] private UnityEngine.UI.Button blackScreenCloseButton;
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private UnityEngine.UI.Text messageText;
    [SerializeField] private UnityEngine.UI.Button messageCloseButton;

    private Font runtimeFont;
    private bool endingShown;

    private void Awake()
    {
        runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        EnsureUi();
        HideBlackScreen();
        HideBlockedMessage();
    }

    private void Start()
    {
        GameProgressManager progress = GameProgressManager.Instance;
        if (progress == null || !progress.HasSeenOpeningStory)
        {
            ShowBlackScreen(openingStoryText, allowClose: true);
            progress?.MarkOpeningStorySeen();
        }
    }

    public bool AreEndingConditionsMet()
    {
        GameProgressManager progress = GameProgressManager.Instance;
        if (progress == null)
        {
            return false;
        }

        string boatItemId = requiredBoatItem != null ? requiredBoatItem.name : "ShopItem_Boat";
        return progress.FoodProgression >= requiredFoodProgression
            && progress.TrashProgression >= requiredTrashProgression
            && progress.GalleryProgression >= requiredGalleryProgression
            && progress.AreAllInventorStoriesRead
            && progress.HasPurchasedItem(boatItemId);
    }

    public void TryLeaveIsland()
    {
        if (AreEndingConditionsMet())
        {
            GameProgressManager progress = GameProgressManager.Instance;
            if (progress != null)
            {
                FirebaseStudyMetricsTracker.Instance?.RecordGameFinished(
                    progress.TrashProgression,
                    progress.FoodProgression,
                    progress.GalleryProgression);
            }

            endingShown = true;
            ShowBlackScreen(endingStoryText, allowClose: false);
        }
        else
        {
            ShowBlockedMessage(blockedLeaveText);
        }
    }

    private void ShowBlackScreen(string text, bool allowClose)
    {
        EnsureUi();
        blackScreenText.text = text;
        blackScreenPanel.SetActive(true);
        blackScreenCloseButton.gameObject.SetActive(allowClose);
        HubMenuState.RegisterOpen(this);
    }

    public void HideBlackScreen()
    {
        if (blackScreenPanel != null)
        {
            blackScreenPanel.SetActive(false);
        }

        if (!endingShown)
        {
            HubMenuState.RegisterClosed(this);
        }
    }

    private void ShowBlockedMessage(string text)
    {
        EnsureUi();
        messageText.text = text;
        messagePanel.SetActive(true);
        HubMenuState.RegisterOpen(this);
    }

    public void HideBlockedMessage()
    {
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }

        HubMenuState.RegisterClosed(this);
    }

    private void EnsureUi()
    {
        ResolveUiRoot();
        Transform root = uiRoot != null ? uiRoot : transform;

        if (blackScreenPanel == null)
        {
            blackScreenPanel = CreatePanel("Game Flow Black Screen", root, Color.black);
            var panelRect = blackScreenPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            blackScreenText = CreateText("Story Text", panelRect, openingStoryText, 36, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            blackScreenText.rectTransform.anchorMin = new Vector2(0.08f, 0.18f);
            blackScreenText.rectTransform.anchorMax = new Vector2(0.92f, 0.82f);
            blackScreenText.rectTransform.offsetMin = Vector2.zero;
            blackScreenText.rectTransform.offsetMax = Vector2.zero;
            blackScreenText.horizontalOverflow = HorizontalWrapMode.Wrap;
            blackScreenText.verticalOverflow = VerticalWrapMode.Overflow;

            blackScreenCloseButton = CreateButton("Continue", panelRect, "Continue", new Color(0.9f, 0.45f, 0.36f, 1f), 34, Color.white);
        }

        EnsureBlackScreenButton();

        if (messagePanel == null)
        {
            messagePanel = CreatePanel("Leave Blocked Message", root, new Color(0.96f, 0.91f, 0.78f, 0.99f));
            var panelRect = messagePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.14f, 0.32f);
            panelRect.anchorMax = new Vector2(0.86f, 0.68f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            messageText = CreateText("Message", panelRect, blockedLeaveText, 34, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.18f, 0.27f, 0.16f, 1f));
            messageText.rectTransform.anchorMin = new Vector2(0.08f, 0.28f);
            messageText.rectTransform.anchorMax = new Vector2(0.92f, 0.86f);
            messageText.rectTransform.offsetMin = Vector2.zero;
            messageText.rectTransform.offsetMax = Vector2.zero;
            messageText.horizontalOverflow = HorizontalWrapMode.Wrap;

            messageCloseButton = CreateButton("Close", panelRect, "Close", new Color(0.9f, 0.45f, 0.36f, 1f), 32, Color.white);
        }

        EnsureMessageButton();
    }

    private void EnsureBlackScreenButton()
    {
        if (blackScreenPanel == null)
        {
            return;
        }

        RectTransform panelRect = blackScreenPanel.GetComponent<RectTransform>();
        if (blackScreenCloseButton == null)
        {
            Transform existing = blackScreenPanel.transform.Find("Continue");
            blackScreenCloseButton = existing != null ? existing.GetComponent<UnityEngine.UI.Button>() : null;
        }

        if (blackScreenCloseButton == null)
        {
            blackScreenCloseButton = CreateButton("Continue", panelRect, "Continue", new Color(0.9f, 0.45f, 0.36f, 1f), 34, Color.white);
        }

        var closeRect = blackScreenCloseButton.GetComponent<RectTransform>();
        closeRect.SetParent(panelRect, false);
        closeRect.anchorMin = new Vector2(0.30f, 0.06f);
        closeRect.anchorMax = new Vector2(0.70f, 0.15f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;
        blackScreenCloseButton.interactable = true;
        blackScreenCloseButton.onClick.RemoveListener(HideBlackScreen);
        blackScreenCloseButton.onClick.AddListener(HideBlackScreen);
        blackScreenCloseButton.transform.SetAsLastSibling();
    }

    private void EnsureMessageButton()
    {
        if (messagePanel == null)
        {
            return;
        }

        RectTransform panelRect = messagePanel.GetComponent<RectTransform>();
        if (messageCloseButton == null)
        {
            Transform existing = messagePanel.transform.Find("Close");
            messageCloseButton = existing != null ? existing.GetComponent<UnityEngine.UI.Button>() : null;
        }

        if (messageCloseButton == null)
        {
            messageCloseButton = CreateButton("Close", panelRect, "Close", new Color(0.9f, 0.45f, 0.36f, 1f), 32, Color.white);
        }

        var closeRect = messageCloseButton.GetComponent<RectTransform>();
        closeRect.SetParent(panelRect, false);
        closeRect.anchorMin = new Vector2(0.30f, 0.08f);
        closeRect.anchorMax = new Vector2(0.70f, 0.24f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;
        messageCloseButton.interactable = true;
        messageCloseButton.onClick.RemoveListener(HideBlockedMessage);
        messageCloseButton.onClick.AddListener(HideBlockedMessage);
        messageCloseButton.transform.SetAsLastSibling();
    }

    private void ResolveUiRoot()
    {
        if (parentCanvas == null || parentCanvas.transform.parent != null)
        {
            parentCanvas = FindRootCanvas();
        }

        Transform canvasTransform = parentCanvas != null ? parentCanvas.transform : transform;
        if (uiRoot == null || uiRoot.parent != canvasTransform)
        {
            Transform existing = canvasTransform.Find("Game Flow UI");
            if (existing == null)
            {
                var rootObject = new GameObject("Game Flow UI", typeof(RectTransform));
                rootObject.transform.SetParent(canvasTransform, false);
                uiRoot = rootObject.GetComponent<RectTransform>();
            }
            else
            {
                uiRoot = existing as RectTransform;
            }
        }

        uiRoot.anchorMin = Vector2.zero;
        uiRoot.anchorMax = Vector2.one;
        uiRoot.offsetMin = Vector2.zero;
        uiRoot.offsetMax = Vector2.zero;
        uiRoot.SetAsLastSibling();

        MovePanelToRoot(blackScreenPanel);
        MovePanelToRoot(messagePanel);
    }

    private static Canvas FindRootCanvas()
    {
        Canvas fallback = null;
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (fallback == null)
            {
                fallback = canvas;
            }

            if (canvas.transform.parent == null || canvas.name == "UI Canvas")
            {
                return canvas;
            }
        }

        return fallback;
    }

    private void MovePanelToRoot(GameObject panel)
    {
        if (panel == null || uiRoot == null || panel.transform.parent == uiRoot)
        {
            return;
        }

        panel.transform.SetParent(uiRoot, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    private GameObject CreatePanel(string objectName, Transform parent, Color color)
    {
        var panel = new GameObject(objectName, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        var image = panel.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        return panel;
    }

    private UnityEngine.UI.Text CreateText(string objectName, RectTransform parent, string text, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        var textComponent = textObject.AddComponent<UnityEngine.UI.Text>();
        textComponent.font = runtimeFont;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = alignment;
        textComponent.color = color;
        textComponent.text = text;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    private UnityEngine.UI.Button CreateButton(string objectName, RectTransform parent, string label, Color color, int fontSize, Color labelColor)
    {
        var buttonObject = new GameObject(objectName, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);
        var image = buttonObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        var button = buttonObject.AddComponent<UnityEngine.UI.Button>();

        UnityEngine.UI.Text labelText = CreateText("Label", buttonObject.GetComponent<RectTransform>(), label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, labelColor);
        labelText.rectTransform.anchorMin = Vector2.zero;
        labelText.rectTransform.anchorMax = Vector2.one;
        labelText.rectTransform.offsetMin = Vector2.zero;
        labelText.rectTransform.offsetMax = Vector2.zero;
        return button;
    }
}
