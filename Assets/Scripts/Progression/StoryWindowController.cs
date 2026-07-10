using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoryWindowController : MonoBehaviour
{
    [Header("Scene UI")]
    [SerializeField] private GameObject storyPanel;    [SerializeField] private Text storyTextLabel;
    [SerializeField] private Button closeButton;

    private readonly Queue<StoryBitDefinition> queuedStories = new Queue<StoryBitDefinition>();
    private Font runtimeFont;
    private bool registeredAsOpenHubMenu;

    private void Awake()
    {
        runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        EnsureUi();
        storyPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseCurrentStory);
        }
    }

    private void OnDisable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseCurrentStory);
        }

        SetHubMenuOpen(false);
    }

    public void ShowStory(StoryBitDefinition story)
    {
        if (story == null)
        {
            return;
        }

        EnsureUi();

        if (storyPanel.activeSelf)
        {
            queuedStories.Enqueue(story);
            return;
        }

        PresentStory(story);
    }

    public void ShowMessage(string message)
    {
        EnsureUi();
        if (storyTextLabel != null) storyTextLabel.text = message;
        storyPanel.SetActive(true);
        SetHubMenuOpen(true);
    }
    public void CloseCurrentStory()
    {
        if (queuedStories.Count > 0)
        {
            PresentStory(queuedStories.Dequeue());
            return;
        }

        if (storyPanel != null)
        {
            storyPanel.SetActive(false);
        }

        SetHubMenuOpen(false);
    }

    private void PresentStory(StoryBitDefinition story)
    {
        if (storyTextLabel != null)
        {
            storyTextLabel.text = story.StoryText;
        }

        storyPanel.SetActive(true);
        SetHubMenuOpen(true);
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

    private void EnsureUi()
    {
        if (storyPanel != null && storyTextLabel != null && closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseCurrentStory);
            closeButton.onClick.AddListener(CloseCurrentStory);
            ApplyStoryPanelLayout();
            return;
        }

        RectTransform root = transform as RectTransform;
        if (root == null)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            Transform uiParent = parentCanvas != null ? parentCanvas.transform : transform;
            root = CreateUiObject("Story Window Root", uiParent);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }

        storyPanel = CreateUiObject("Story Panel", root).gameObject;
        var panelRect = storyPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.12f);
        panelRect.anchorMax = new Vector2(0.92f, 0.88f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImage = storyPanel.AddComponent<Image>();
        panelImage.color = new Color(0.96f, 0.91f, 0.78f, 0.99f);

        Text title = CreateText("Title", panelRect, "STORY", 48, FontStyle.Bold, TextAnchor.MiddleLeft);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(40f, -24f);
        title.rectTransform.sizeDelta = new Vector2(-80f, 72f);
        title.color = new Color(0.18f, 0.27f, 0.16f, 1f);

        GameObject contentObject = CreateUiObject("Story Content", panelRect).gameObject;
        var contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.06f, 0.20f);
        contentRect.anchorMax = new Vector2(0.94f, 0.78f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        var contentImage = contentObject.AddComponent<Image>();
        contentImage.color = new Color(0.46f, 0.72f, 0.47f, 1f);

        storyTextLabel = CreateText("Story Text", contentRect, string.Empty, 34, FontStyle.Normal, TextAnchor.MiddleLeft);
        storyTextLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        storyTextLabel.verticalOverflow = VerticalWrapMode.Overflow;
        var textRect = storyTextLabel.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(34f, 34f);
        textRect.offsetMax = new Vector2(-34f, -34f);
        storyTextLabel.color = new Color(0.95f, 0.98f, 0.91f, 1f);

        closeButton = CreateButton("Close", panelRect, "Close", new Color(0.9f, 0.45f, 0.36f, 1f), 36, Color.white);
        var closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.30f, 0.05f);
        closeRect.anchorMax = new Vector2(0.70f, 0.15f);
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.anchoredPosition = Vector2.zero;
        closeRect.sizeDelta = Vector2.zero;
        closeButton.onClick.AddListener(CloseCurrentStory);
            ApplyStoryPanelLayout();
    }

    private void ApplyStoryPanelLayout()
    {
        if (storyPanel == null)
        {
            return;
        }

        Transform icon = storyPanel.transform.Find("Story Content/Story Icon");
        if (icon != null)
        {
            icon.gameObject.SetActive(false);
        }

        if (storyTextLabel != null)
        {
            RectTransform textRect = storyTextLabel.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(34f, 34f);
            textRect.offsetMax = new Vector2(-34f, -34f);
        }
    }

    private Text CreateText(string objectName, RectTransform parent, string text, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        GameObject textObject = CreateUiObject(objectName, parent).gameObject;
        var textComponent = textObject.AddComponent<Text>();
        textComponent.font = runtimeFont;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = alignment;
        textComponent.color = Color.white;
        textComponent.text = text;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    private Button CreateButton(string objectName, RectTransform parent, string label, Color color, int fontSize, Color labelColor)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent).gameObject;
        var image = buttonObject.AddComponent<Image>();
        image.color = color;
        var button = buttonObject.AddComponent<Button>();

        Text labelText = CreateText("Label", buttonObject.GetComponent<RectTransform>(), label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
        labelText.color = labelColor;
        labelText.rectTransform.anchorMin = Vector2.zero;
        labelText.rectTransform.anchorMax = Vector2.one;
        labelText.rectTransform.offsetMin = Vector2.zero;
        labelText.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private static RectTransform CreateUiObject(string objectName, Transform parent)
    {
        var uiObject = new GameObject(objectName, typeof(RectTransform));
        var rectTransform = uiObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }
}