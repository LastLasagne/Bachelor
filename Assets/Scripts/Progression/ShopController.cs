using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ShopPurchaseEffectType
{
    ActivateCollection,
    DeactivateCollection
}

[System.Serializable]
public class ShopOffer
{
    [SerializeField] private ShopItemDefinition item;
    [SerializeField] private GameObject collectionToActivate;
    [SerializeField] private ShopPurchaseEffectType purchaseEffect = ShopPurchaseEffectType.ActivateCollection;
    [SerializeField] private GameObject exclusiveChoiceGroup;
    [SerializeField] private bool purchased;

    public ShopItemDefinition Item => item;
    public GameObject CollectionToActivate => collectionToActivate;
    public ShopPurchaseEffectType PurchaseEffect => purchaseEffect;
    public GameObject ExclusiveChoiceGroup => exclusiveChoiceGroup;
    public bool Purchased => purchased;

    public ShopOffer()
    {
    }

    public ShopOffer(ShopItemDefinition item, GameObject collectionToActivate)
        : this(item, collectionToActivate, ShopPurchaseEffectType.ActivateCollection, null)
    {
    }

    public ShopOffer(ShopItemDefinition item, GameObject collectionToActivate, ShopPurchaseEffectType purchaseEffect)
        : this(item, collectionToActivate, purchaseEffect, null)
    {
    }

    public ShopOffer(ShopItemDefinition item, GameObject collectionToActivate, ShopPurchaseEffectType purchaseEffect, GameObject exclusiveChoiceGroup)
    {
        this.item = item;
        this.collectionToActivate = collectionToActivate;
        this.purchaseEffect = purchaseEffect;
        this.exclusiveChoiceGroup = exclusiveChoiceGroup;
        purchased = false;
    }

    public bool HasSameItem(ShopItemDefinition otherItem)
    {
        return item != null && item == otherItem;
    }

    public void MarkPurchased()
    {
        purchased = true;
    }
}
public class ShopController : MonoBehaviour
{
    [Header("Resources")]
    [SerializeField] private MaterialResourceBank materialResourceBank;

    [Header("Shop Items")]
    [SerializeField] private List<ShopOffer> offers = new List<ShopOffer>();

    [Header("UI")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Text resourceAmountLabel;
    [SerializeField] private RectTransform itemContentRoot;
    [SerializeField] private Button closeButton;

    private readonly List<GameObject> spawnedRows = new List<GameObject>();
    private Font runtimeFont;
    private bool registeredAsOpenHubMenu;

    public MaterialResourceBank MaterialResourceBank { get => materialResourceBank; set => materialResourceBank = value; }
    public List<ShopOffer> Offers => offers;

    public bool AddOffer(ShopOffer offer)
    {
        if (offer == null || offer.Item == null || ContainsOfferForItem(offer.Item))
        {
            return false;
        }

        offers.Add(new ShopOffer(offer.Item, offer.CollectionToActivate, offer.PurchaseEffect, offer.ExclusiveChoiceGroup));

        if (shopPanel != null && shopPanel.activeSelf)
        {
            RebuildShopList();
            RefreshResourceAmount();
        }

        return true;
    }

    private bool ContainsOfferForItem(ShopItemDefinition item)
    {
        foreach (ShopOffer offer in offers)
        {
            if (offer != null && offer.HasSameItem(item))
            {
                return true;
            }
        }

        return false;
    }

    private void Awake()
    {
        runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        EnsureUi();
        shopPanel.SetActive(false);
    }

    public void OpenShop()
    {
        EnsureUi();
        shopPanel.SetActive(true);
        SetHubMenuOpen(true);
        RebuildShopList();
        RefreshResourceAmount();
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        SetHubMenuOpen(false);
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

    private void RebuildShopList()
    {
        ClearSpawnedRows();
        RemovePurchasedOffers();

        for (int i = 0; i < offers.Count; i++)
        {
            ShopOffer offer = offers[i];
            if (offer?.Item == null || offer.Purchased)
            {
                continue;
            }

            GameObject row = CreateOfferRow(offer);
            spawnedRows.Add(row);
        }
    }

    private GameObject CreateOfferRow(ShopOffer offer)
    {
        GameObject row = CreateUiObject("Shop Item", itemContentRoot).gameObject;
        var rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(0f, 250f);

        var image = row.AddComponent<Image>();
        image.color = new Color(0.46f, 0.72f, 0.47f, 1f);

        var group = row.AddComponent<CanvasGroup>();
        bool canAfford = CanAfford(offer);
        group.alpha = canAfford ? 1f : 0.42f;


        Text nameText = CreateText("Name", rowRect, offer.Item.ItemName, 42, FontStyle.Bold, TextAnchor.UpperLeft);
        var nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.anchoredPosition = new Vector2(34f, -28f);
        nameRect.sizeDelta = new Vector2(-300f, 58f);
        nameText.color = Color.white;
        ConfigureTextBestFit(nameText, 24, 42);

        Text descriptionText = CreateText("Description", rowRect, offer.Item.Description, 28, FontStyle.Normal, TextAnchor.UpperLeft);
        var descriptionRect = descriptionText.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 0f);
        descriptionRect.anchorMax = new Vector2(1f, 1f);
        descriptionRect.pivot = new Vector2(0f, 1f);
        descriptionRect.anchoredPosition = new Vector2(34f, -96f);
        descriptionRect.sizeDelta = new Vector2(-300f, -122f);
        descriptionText.color = new Color(0.95f, 0.98f, 0.91f, 1f);
        ConfigureTextBestFit(descriptionText, 18, 28);

        Button priceButton = CreateButton("Price Tag", rowRect, $"{offer.Item.Cost}", new Color(0.96f, 0.66f, 0.24f, 0.92f), 44, Color.black);
        var priceRect = priceButton.GetComponent<RectTransform>();
        priceRect.anchorMin = new Vector2(1f, 0.5f);
        priceRect.anchorMax = new Vector2(1f, 0.5f);
        priceRect.pivot = new Vector2(1f, 0.5f);
        priceRect.anchoredPosition = new Vector2(-32f, 0f);
        priceRect.sizeDelta = new Vector2(220f, 96f);
        priceButton.interactable = canAfford;
        priceButton.onClick.AddListener(() => TryPurchase(offer));

        return row;
    }

    private void TryPurchase(ShopOffer offer)
    {
        if (offer?.Item == null || materialResourceBank == null)
        {
            return;
        }

        if (!materialResourceBank.TrySpend(offer.Item.Cost))
        {
            RebuildShopList();
            RefreshResourceAmount();
            return;
        }

        ApplyPurchaseEffect(offer);

        offer.MarkPurchased();
        RemoveExclusiveChoiceOffers(offer);
        RemovePurchasedOffers();
        RebuildShopList();
        RefreshResourceAmount();
    }

    private static void ApplyPurchaseEffect(ShopOffer offer)
    {
        if (offer.CollectionToActivate == null)
        {
            return;
        }

        switch (offer.PurchaseEffect)
        {
            case ShopPurchaseEffectType.ActivateCollection:
                ActivateCollection(offer.CollectionToActivate);
                break;
            case ShopPurchaseEffectType.DeactivateCollection:
                offer.CollectionToActivate.SetActive(false);
                break;
        }
    }
    private static void ActivateCollection(GameObject target)
    {
        Transform current = target.transform.parent;
        while (current != null)
        {
            current.gameObject.SetActive(true);
            current = current.parent;
        }

        SetActiveRecursively(target, true);
    }
    private static void SetActiveRecursively(GameObject target, bool active)
    {
        target.SetActive(active);

        foreach (Transform child in target.transform)
        {
            SetActiveRecursively(child.gameObject, active);
        }
    }
    private bool CanAfford(ShopOffer offer)
    {
        return offer?.Item != null && materialResourceBank != null && materialResourceBank.CanAfford(offer.Item.Cost);
    }

    private void RefreshResourceAmount()
    {
        if (resourceAmountLabel != null)
        {
            int amount = materialResourceBank != null ? materialResourceBank.MaterialAmount : 0;
            resourceAmountLabel.text = $"Materials: {amount}";
        }
    }

    private void RemoveExclusiveChoiceOffers(ShopOffer purchasedOffer)
    {
        if (purchasedOffer == null || purchasedOffer.ExclusiveChoiceGroup == null)
        {
            return;
        }

        for (int i = offers.Count - 1; i >= 0; i--)
        {
            ShopOffer offer = offers[i];
            if (offer == null || offer == purchasedOffer || offer.Purchased)
            {
                continue;
            }

            if (offer.ExclusiveChoiceGroup == purchasedOffer.ExclusiveChoiceGroup)
            {
                offers.RemoveAt(i);
            }
        }
    }
    private void RemovePurchasedOffers()
    {
        for (int i = offers.Count - 1; i >= 0; i--)
        {
            if (offers[i] == null || offers[i].Purchased)
            {
                offers.RemoveAt(i);
            }
        }
    }

    private void ClearSpawnedRows()
    {
        foreach (GameObject row in spawnedRows)
        {
            if (row != null)
            {
                Destroy(row);
            }
        }

        spawnedRows.Clear();
    }

    private void EnsureUi()
    {
        if (shopPanel != null && itemContentRoot != null && resourceAmountLabel != null && closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseShop);
            closeButton.onClick.AddListener(CloseShop);
        ApplyShopPanelLayout();
            RefreshResourceAmount();
            return;
        }

        RectTransform root = transform as RectTransform;
        if (root == null)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            Transform uiParent = parentCanvas != null ? parentCanvas.transform : transform;
            root = CreateUiObject("Shop UI Root", uiParent);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }

        shopPanel = CreateUiObject("Shop Panel", root).gameObject;
        var panelRect = shopPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.08f);
        panelRect.anchorMax = new Vector2(0.92f, 0.92f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImage = shopPanel.AddComponent<Image>();
        panelImage.color = new Color(0.96f, 0.91f, 0.78f, 0.99f);

        Text title = CreateText("Title", panelRect, "SHOP", 48, FontStyle.Bold, TextAnchor.MiddleLeft);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(40f, -24f);
        title.rectTransform.sizeDelta = new Vector2(-300f, 72f);
        title.color = new Color(0.18f, 0.27f, 0.16f, 1f);

        resourceAmountLabel = CreateText("Resource Amount", panelRect, "Materials: 0", 36, FontStyle.Bold, TextAnchor.MiddleRight);
        resourceAmountLabel.rectTransform.anchorMin = new Vector2(1f, 1f);
        resourceAmountLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
        resourceAmountLabel.rectTransform.pivot = new Vector2(1f, 1f);
        resourceAmountLabel.rectTransform.anchoredPosition = new Vector2(-42f, -30f);
        resourceAmountLabel.rectTransform.sizeDelta = new Vector2(340f, 60f);
        resourceAmountLabel.color = new Color(0.18f, 0.27f, 0.16f, 1f);
        resourceAmountLabel.raycastTarget = false;

        closeButton = CreateButton("Close", panelRect, "Close", new Color(0.9f, 0.45f, 0.36f, 1f), 36, Color.white);
        var closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.30f, 0.04f);
        closeRect.anchorMax = new Vector2(0.70f, 0.13f);
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.anchoredPosition = Vector2.zero;
        closeRect.sizeDelta = Vector2.zero;
        closeButton.onClick.AddListener(CloseShop);

        GameObject viewportObject = CreateUiObject("Item Viewport", panelRect).gameObject;
        var viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.offsetMin = new Vector2(40f, 340f);
        viewportRect.offsetMax = new Vector2(-40f, -112f);
        var viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(0.46f, 0.72f, 0.47f, 0.22f);
        var mask = viewportObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentObject = CreateUiObject("Items", viewportRect).gameObject;
        itemContentRoot = contentObject.GetComponent<RectTransform>();
        itemContentRoot.anchorMin = new Vector2(0f, 1f);
        itemContentRoot.anchorMax = new Vector2(1f, 1f);
        itemContentRoot.pivot = new Vector2(0.5f, 1f);
        itemContentRoot.anchoredPosition = Vector2.zero;
        itemContentRoot.sizeDelta = new Vector2(0f, 0f);
        var layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = shopPanel.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = itemContentRoot;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 42f;
        ApplyShopPanelLayout();
    }

    private void ApplyShopPanelLayout()
    {
        if (shopPanel == null)
        {
            return;
        }

        RectTransform viewport = FindChildRect(shopPanel.transform, "Item Viewport");
        if (viewport != null)
        {
            viewport.anchorMin = new Vector2(0f, 0f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.offsetMin = new Vector2(40f, 340f);
            viewport.offsetMax = new Vector2(-40f, -112f);
        }

        EnsurePanelTitle();
        EnsureResourceCounter();

        if (closeButton != null)
        {
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.30f, 0.04f);
            closeRect.anchorMax = new Vector2(0.70f, 0.13f);
            closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.anchoredPosition = Vector2.zero;
            closeRect.sizeDelta = Vector2.zero;
            EnsureButtonLabel(closeButton, "Close", 36, Color.white);
            closeRect.SetAsLastSibling();
        }
    }

    private void EnsureResourceCounter()
    {
        if (resourceAmountLabel == null)
        {
            Transform existing = shopPanel.transform.Find("Resource Amount");
            if (existing != null)
            {
                resourceAmountLabel = existing.GetComponent<Text>();
            }
        }

        if (resourceAmountLabel == null)
        {
            resourceAmountLabel = CreateText("Resource Amount", shopPanel.GetComponent<RectTransform>(), "Materials: 0", 36, FontStyle.Bold, TextAnchor.MiddleRight);
        }

        RectTransform resourceRect = resourceAmountLabel.rectTransform;
        resourceRect.anchorMin = new Vector2(1f, 1f);
        resourceRect.anchorMax = new Vector2(1f, 1f);
        resourceRect.pivot = new Vector2(1f, 1f);
        resourceRect.anchoredPosition = new Vector2(-42f, -30f);
        resourceRect.sizeDelta = new Vector2(340f, 60f);
        resourceAmountLabel.font = runtimeFont;
        resourceAmountLabel.fontSize = 36;
        resourceAmountLabel.fontStyle = FontStyle.Bold;
        resourceAmountLabel.alignment = TextAnchor.MiddleRight;
        resourceAmountLabel.color = new Color(0.18f, 0.27f, 0.16f, 1f);
        resourceAmountLabel.raycastTarget = false;

        resourceAmountLabel.gameObject.SetActive(true);
    }

    private void EnsurePanelTitle()
    {
        Text titleText = null;
        Transform existing = shopPanel.transform.Find("Title");
        if (existing != null)
        {
            titleText = existing.GetComponent<Text>();
        }

        if (titleText == null)
        {
            titleText = CreateText("Title", shopPanel.GetComponent<RectTransform>(), "SHOP", 48, FontStyle.Bold, TextAnchor.MiddleLeft);
        }

        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(40f, -24f);
        titleRect.sizeDelta = new Vector2(-300f, 72f);
        titleText.text = "SHOP";
        titleText.font = runtimeFont;
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.color = new Color(0.18f, 0.27f, 0.16f, 1f);
        titleText.raycastTarget = false;
        titleText.gameObject.SetActive(true);
    }

    private static RectTransform FindChildRect(Transform root, string childName)
    {
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == childName)
            {
                return rect;
            }
        }

        return null;
    }

    private static void ConfigureTextBestFit(Text text, int minSize, int maxSize)
    {
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = minSize;
        text.resizeTextMaxSize = maxSize;
    }

    private void EnsureButtonLabel(Button button, string label, int fontSize, Color labelColor)
    {
        Text labelText = button.GetComponentInChildren<Text>(true);
        if (labelText == null)
        {
            labelText = CreateText("Label", button.GetComponent<RectTransform>(), label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
        }

        labelText.text = label;
        labelText.font = runtimeFont;
        labelText.fontSize = fontSize;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = labelColor;
        labelText.raycastTarget = false;
        labelText.gameObject.SetActive(true);
        labelText.rectTransform.anchorMin = Vector2.zero;
        labelText.rectTransform.anchorMax = Vector2.one;
        labelText.rectTransform.offsetMin = Vector2.zero;
        labelText.rectTransform.offsetMax = Vector2.zero;
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

    private Button CreateButton(string objectName, RectTransform parent, string label, Color color, int fontSize = 16, Color? labelColor = null)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent).gameObject;
        var image = buttonObject.AddComponent<Image>();
        image.color = color;
        var button = buttonObject.AddComponent<Button>();

        Text labelText = CreateText("Label", buttonObject.GetComponent<RectTransform>(), label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
        labelText.color = labelColor ?? Color.black;
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