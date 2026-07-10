using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using Firebase.Storage;
using Obvious.Soap;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FirebasePhotoGalleryController : MonoBehaviour
{
    [Header("Firebase")]
    [SerializeField] private string photoCollection = "questPhotos";
    [SerializeField] private string visibleStatus = "visible";
    [SerializeField] private string positiveCompletedStatus = "positive_completed";
    [SerializeField] private string negativeCompletedStatus = "negative_completed";
    [SerializeField, Min(1)] private int maxDocumentsToSample = 50;
    [SerializeField, Min(1)] private int positiveReactionThreshold = 3;
    [SerializeField, Min(1)] private int negativeReactionThreshold = 3;

    [Header("Progression")]
    [SerializeField] private IntVariable viewedPhotoProgression;
    [SerializeField, Min(1)] private int progressionValuePerPhoto = 1;

    [Header("Photo Display")]
    [SerializeField] private float landscapePhotoRotationDegrees = 90f;

    [Header("Scene UI")]
    [SerializeField] private GameObject galleryPanel;
    [SerializeField] private RawImage photoImage;
    [SerializeField] private Button photoButton;
    [SerializeField] private Button thumbsUpButton;
    [SerializeField] private Button thumbsDownButton;
    [SerializeField] private Text statusText;
    [SerializeField] private Text counterText;
    [SerializeField] private Button closeButton;

    private readonly List<GalleryPhotoEntry> photoEntries = new List<GalleryPhotoEntry>();
    private readonly Queue<GalleryPhotoEntry> shuffledPhotoQueue = new Queue<GalleryPhotoEntry>();
    private readonly System.Random random = new System.Random();
    private bool isOpen;
    private bool isLoading;
    private GalleryPhotoEntry currentPhoto;
    private bool registeredAsOpenHubMenu;
    private Font runtimeFont;

    private void Awake()
    {
        runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        ValidateGalleryReferences();
        SetPanelVisible(false);
    }

    private void OnEnable()
    {
        if (photoButton != null)
        {
            photoButton.interactable = false;
        }

        if (thumbsUpButton != null)
        {
            thumbsUpButton.onClick.AddListener(HandleThumbsUpClicked);
        }

        if (thumbsDownButton != null)
        {
            thumbsDownButton.onClick.AddListener(HandleThumbsDownClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseGallery);
        }
    }

    private void OnDisable()
    {
        if (thumbsUpButton != null)
        {
            thumbsUpButton.onClick.RemoveListener(HandleThumbsUpClicked);
        }

        if (thumbsDownButton != null)
        {
            thumbsDownButton.onClick.RemoveListener(HandleThumbsDownClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseGallery);
        }
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

    public async void OpenGallery()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        if (!ValidateGalleryReferences())
        {
            isOpen = false;
        SetHubMenuOpen(false);
            return;
        }

        SetPanelVisible(true);
        SetHubMenuOpen(true);
        UpdateCounterText();
        await LoadNextPhotoAsync(refreshList: true);
    }

    public void CloseGallery()
    {
        isOpen = false;
        currentPhoto = default;
        SetPhotoTexture(null);
        SetPanelVisible(false);
        SetHubMenuOpen(false);
    }

    private async void HandleThumbsUpClicked()
    {
        await ReactToCurrentPhotoAsync(true);
    }

    private async void HandleThumbsDownClicked()
    {
        await ReactToCurrentPhotoAsync(false);
    }

    private async Task ReactToCurrentPhotoAsync(bool isPositive)
    {
        if (!isOpen || isLoading || !currentPhoto.IsValid)
        {
            return;
        }

        isLoading = true;
        SetReactionButtonsInteractable(false);
        SetStatus("Saving reaction...");

        try
        {
            bool reactionSaved = await SaveReactionAsync(currentPhoto, isPositive);
            if (reactionSaved && viewedPhotoProgression != null)
            {
                viewedPhotoProgression.Add(progressionValuePerPhoto);
            }

            UpdateCounterText();
            SetPhotoTexture(null);
            currentPhoto = default;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Photo gallery failed to save reaction: {exception}", this);
            SetStatus("Could not save reaction.");
            SetReactionButtonsInteractable(true);
            return;
        }
        finally
        {
            isLoading = false;
        }

        await LoadNextPhotoAsync(refreshList: shuffledPhotoQueue.Count == 0);
    }

    private async Task LoadNextPhotoAsync(bool refreshList)
    {
        if (isLoading)
        {
            return;
        }

        isLoading = true;
        currentPhoto = default;
        SetStatus("Loading picture...");
        SetReactionButtonsInteractable(false);

        try
        {
            await FirebaseGameServices.EnsureSignedInAnonymouslyAsync();

            if (refreshList || shuffledPhotoQueue.Count == 0)
            {
                await RefreshPhotoListAsync();
            }

            while (shuffledPhotoQueue.Count > 0)
            {
                currentPhoto = shuffledPhotoQueue.Dequeue();
                Texture2D texture = await DownloadTextureAsync(currentPhoto.StoragePath);

                if (texture == null)
                {
                    photoEntries.RemoveAll(entry => entry.DocumentId == currentPhoto.DocumentId);
                    currentPhoto = default;
                    continue;
                }

                SetPhotoTexture(texture);
                SetStatus("React to this picture.");
                SetReactionButtonsInteractable(true);
                return;
            }

            SetPhotoTexture(null);
            SetStatus("No pictures available yet.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Photo gallery failed to load a picture: {exception}", this);
            SetPhotoTexture(null);
            SetStatus("Could not load a picture.");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task RefreshPhotoListAsync()
    {
        photoEntries.Clear();
        shuffledPhotoQueue.Clear();

        var user = await FirebaseGameServices.EnsureSignedInAnonymouslyAsync();
        Query query = FirebaseGameServices.Firestore
            .Collection(photoCollection)
            .WhereEqualTo("status", visibleStatus)
            .Limit(maxDocumentsToSample);

        QuerySnapshot snapshot = await query.GetSnapshotAsync();
        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            if (!document.TryGetValue("storagePath", out string storagePath) || string.IsNullOrWhiteSpace(storagePath))
            {
                continue;
            }

            if (document.TryGetValue("uploaderId", out string uploaderId) && uploaderId == user.UserId)
            {
                continue;
            }

            if (HasUserReacted(document, user.UserId))
            {
                continue;
            }

            photoEntries.Add(new GalleryPhotoEntry(document.Id, storagePath));
        }

        ShuffleStorageQueue();
    }

    private void ShuffleStorageQueue()
    {
        shuffledPhotoQueue.Clear();

        List<GalleryPhotoEntry> shuffled = new List<GalleryPhotoEntry>(photoEntries);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        foreach (GalleryPhotoEntry entry in shuffled)
        {
            shuffledPhotoQueue.Enqueue(entry);
        }
    }

    private async Task<bool> SaveReactionAsync(GalleryPhotoEntry photo, bool isPositive)
    {
        var user = await FirebaseGameServices.EnsureSignedInAnonymouslyAsync();
        DocumentReference photoDocument = FirebaseGameServices.Firestore.Collection(photoCollection).Document(photo.DocumentId);
        DocumentSnapshot snapshot = await photoDocument.GetSnapshotAsync();
        if (!snapshot.Exists)
        {
            return false;
        }

        if (snapshot.TryGetValue("uploaderId", out string uploaderId) && uploaderId == user.UserId)
        {
            Debug.Log("Ignoring gallery reaction because the current player uploaded this photo.", this);
            return false;
        }

        Dictionary<string, object> reactions = ReadReactionMap(snapshot);
        if (reactions.ContainsKey(user.UserId))
        {
            Debug.Log("Ignoring duplicate gallery reaction from this player.", this);
            return false;
        }

        int thumbsUp = ReadInt(snapshot, "thumbsUp");
        int thumbsDown = ReadInt(snapshot, "thumbsDown");
        if (isPositive)
        {
            thumbsUp++;
            reactions[user.UserId] = "up";
        }
        else
        {
            thumbsDown++;
            reactions[user.UserId] = "down";
        }

        int reactionCount = thumbsUp + thumbsDown;

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "thumbsUp", thumbsUp },
            { "thumbsDown", thumbsDown },
            { "reactionCount", reactionCount },
            { "reactions", reactions },
            { "lastReactionAt", FieldValue.ServerTimestamp }
        };

        if (thumbsUp >= positiveReactionThreshold)
        {
            updates["status"] = positiveCompletedStatus;
            updates["completedReason"] = "positive";
            updates["completedAt"] = FieldValue.ServerTimestamp;
            await photoDocument.UpdateAsync(updates);
            await DeletePositiveCompletedPhotoAsync(photoDocument, photo.StoragePath);
            return true;
        }

        if (thumbsDown >= negativeReactionThreshold)
        {
            updates["status"] = negativeCompletedStatus;
            updates["completedReason"] = "negative";
            updates["negativeNoticeDismissed"] = false;
            updates["completedAt"] = FieldValue.ServerTimestamp;
        }

        await photoDocument.UpdateAsync(updates);
        return true;
    }

    private async Task DeletePositiveCompletedPhotoAsync(DocumentReference photoDocument, string storagePath)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(storagePath))
            {
                await FirebaseGameServices.Storage.RootReference.Child(storagePath).DeleteAsync();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Positive completed photo is hidden, but Storage deletion failed for {storagePath}: {exception.Message}", this);
        }

        try
        {
            await photoDocument.DeleteAsync();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Positive completed photo is hidden, but Firestore deletion failed: {exception.Message}", this);
        }
    }

    private async Task<Texture2D> DownloadTextureAsync(string storagePath)
    {
        StorageReference photoReference = FirebaseGameServices.Storage.RootReference.Child(storagePath);
        Uri downloadUrl = await photoReference.GetDownloadUrlAsync();

        using (UnityWebRequest request = UnityWebRequest.Get(downloadUrl.ToString()))
        {
            await SendWebRequestAsync(request);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Could not download gallery photo {storagePath}: {request.error}", this);
                return null;
            }

            byte[] imageBytes = request.downloadHandler.data;
            if (imageBytes == null || imageBytes.Length == 0)
            {
                Debug.LogWarning($"Downloaded gallery photo {storagePath} had no bytes.", this);
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(imageBytes))
            {
                Destroy(texture);
                Debug.LogWarning($"Could not decode gallery photo {storagePath}.", this);
                return null;
            }

            return texture;
        }
    }

    private static Task SendWebRequestAsync(UnityWebRequest request)
    {
        TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        operation.completed += _ => completionSource.TrySetResult(true);
        return completionSource.Task;
    }

    private static bool HasUserReacted(DocumentSnapshot document, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        return ReadReactionMap(document).ContainsKey(userId);
    }

    private static bool IsOwnedByCurrentPlayer(DocumentSnapshot document, string authUserId, string localPlayerId)
    {
        if (!string.IsNullOrWhiteSpace(authUserId) &&
            document.TryGetValue("uploaderId", out string uploaderId) &&
            uploaderId == authUserId)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(localPlayerId) &&
               document.TryGetValue("uploaderPlayerId", out string uploaderPlayerId) &&
               uploaderPlayerId == localPlayerId;
    }

    private static Dictionary<string, object> ReadReactionMap(DocumentSnapshot document)
    {
        if (document.TryGetValue("reactions", out Dictionary<string, object> reactions) && reactions != null)
        {
            return new Dictionary<string, object>(reactions);
        }

        return new Dictionary<string, object>();
    }

    private static int ReadInt(DocumentSnapshot document, string field)
    {
        if (document.TryGetValue(field, out int intValue))
        {
            return intValue;
        }

        if (document.TryGetValue(field, out long longValue))
        {
            return (int)longValue;
        }

        return 0;
    }

    private bool ValidateGalleryReferences()
    {
        bool hasRequiredReferences = galleryPanel != null &&
                                     photoImage != null &&
                                     photoButton != null &&
                                     thumbsUpButton != null &&
                                     thumbsDownButton != null &&
                                     statusText != null &&
                                     counterText != null &&
                                     closeButton != null;

        if (!hasRequiredReferences)
        {
            Debug.LogError("Photo gallery UI references are missing. Assign the scene UI fields on FirebasePhotoGalleryController.", this);
        }

        else
        {
            ApplyGalleryStyle();
        }

        return hasRequiredReferences;
    }

    private void ApplyGalleryStyle()
    {
        RectTransform panelRect = galleryPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.08f);
        panelRect.anchorMax = new Vector2(0.92f, 0.92f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = galleryPanel.GetComponent<Image>() ?? galleryPanel.AddComponent<Image>();
        panelImage.color = new Color(0.96f, 0.91f, 0.78f, 0.99f);

        Text titleText = FindOrCreatePanelText("Title", "GALLERY", 48, FontStyle.Bold, TextAnchor.MiddleLeft);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(40f, -24f);
        titleRect.sizeDelta = new Vector2(-300f, 72f);
        titleText.color = new Color(0.18f, 0.27f, 0.16f, 1f);

        RectTransform counterRect = counterText.rectTransform;
        counterRect.anchorMin = new Vector2(1f, 1f);
        counterRect.anchorMax = new Vector2(1f, 1f);
        counterRect.pivot = new Vector2(1f, 1f);
        counterRect.anchoredPosition = new Vector2(-42f, -30f);
        counterRect.sizeDelta = new Vector2(300f, 60f);
        ConfigureText(counterText, 32, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.18f, 0.27f, 0.16f, 1f));

        RectTransform photoButtonRect = photoButton.GetComponent<RectTransform>();
        photoButtonRect.anchorMin = new Vector2(0.06f, 0.30f);
        photoButtonRect.anchorMax = new Vector2(0.94f, 0.84f);
        photoButtonRect.offsetMin = Vector2.zero;
        photoButtonRect.offsetMax = Vector2.zero;
        Image photoButtonImage = photoButton.GetComponent<Image>() ?? photoButton.gameObject.AddComponent<Image>();
        photoButtonImage.color = new Color(0.46f, 0.72f, 0.47f, 1f);

        RectTransform photoRect = photoImage.rectTransform;
        photoRect.anchorMin = Vector2.zero;
        photoRect.anchorMax = Vector2.one;
        photoRect.offsetMin = new Vector2(24f, 18f);
        photoRect.offsetMax = new Vector2(-24f, -18f);
        photoImage.color = Color.white;

        RectTransform statusRect = statusText.rectTransform;
        statusRect.anchorMin = new Vector2(0.10f, 0.20f);
        statusRect.anchorMax = new Vector2(0.90f, 0.28f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;
        ConfigureText(statusText, 30, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.18f, 0.27f, 0.16f, 1f));

        StyleButton(thumbsUpButton, null, new Color(0.46f, 0.72f, 0.47f, 1f), 30, Color.white, new Vector2(0.14f, 0.14f), new Vector2(0.42f, 0.22f));
        StyleButton(thumbsDownButton, null, new Color(0.9f, 0.45f, 0.36f, 1f), 30, Color.white, new Vector2(0.58f, 0.14f), new Vector2(0.86f, 0.22f));
        StyleButton(closeButton, "Close", new Color(0.9f, 0.45f, 0.36f, 1f), 36, Color.white, new Vector2(0.30f, 0.04f), new Vector2(0.70f, 0.12f));
        closeButton.transform.SetAsLastSibling();
    }

    private Text FindOrCreatePanelText(string objectName, string text, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        Transform existing = galleryPanel.transform.Find(objectName);
        Text textComponent = existing != null ? existing.GetComponent<Text>() : null;
        if (textComponent == null)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(galleryPanel.transform, false);
            textComponent = textObject.AddComponent<Text>();
        }

        textComponent.text = text;
        ConfigureText(textComponent, fontSize, fontStyle, alignment, Color.white);
        textComponent.raycastTarget = false;
        textComponent.gameObject.SetActive(true);
        return textComponent;
    }

    private void StyleButton(Button button, string labelOverride, Color backgroundColor, int fontSize, Color labelColor, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        Image buttonImage = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>();
        buttonImage.color = backgroundColor;

        Text labelText = button.GetComponentInChildren<Text>(true);
        if (labelText == null && !string.IsNullOrEmpty(labelOverride))
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(button.transform, false);
            labelText = labelObject.AddComponent<Text>();
        }

        if (labelText == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(labelOverride))
        {
            labelText.text = labelOverride;
        }

        ConfigureText(labelText, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, labelColor);
        labelText.raycastTarget = false;
        labelText.gameObject.SetActive(true);
        labelText.rectTransform.anchorMin = Vector2.zero;
        labelText.rectTransform.anchorMax = Vector2.one;
        labelText.rectTransform.offsetMin = Vector2.zero;
        labelText.rectTransform.offsetMax = Vector2.zero;
    }

    private void ConfigureText(Text text, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
    {
        text.font = runtimeFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
    }

    private void SetPanelVisible(bool visible)
    {
        galleryPanel?.SetActive(visible);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void SetPhotoTexture(Texture texture)
    {
        if (photoImage == null)
        {
            return;
        }

        photoImage.texture = texture;
        photoImage.enabled = texture != null;

        AspectRatioFitter aspectRatioFitter = photoImage.GetComponent<AspectRatioFitter>();
        RectTransform photoRect = photoImage.rectTransform;
        photoRect.localRotation = Quaternion.identity;

        if (texture == null)
        {
            if (aspectRatioFitter != null)
            {
                aspectRatioFitter.aspectRatio = 1f;
            }

            return;
        }

        if (aspectRatioFitter == null)
        {
            return;
        }

        if (texture.width > texture.height && !Mathf.Approximately(landscapePhotoRotationDegrees, 0f))
        {
            photoRect.localRotation = Quaternion.Euler(0f, 0f, landscapePhotoRotationDegrees);
        }

        aspectRatioFitter.aspectRatio = texture.width / (float)texture.height;
    }

    private void SetReactionButtonsInteractable(bool interactable)
    {
        if (photoButton != null)
        {
            photoButton.interactable = false;
        }

        if (thumbsUpButton != null)
        {
            thumbsUpButton.interactable = interactable;
        }

        if (thumbsDownButton != null)
        {
            thumbsDownButton.interactable = interactable;
        }
    }

    private void UpdateCounterText()
    {
        if (counterText == null)
        {
            return;
        }

        int count = viewedPhotoProgression != null ? viewedPhotoProgression.Value : 0;
        counterText.text = $"Pictures viewed: {count}";
    }

    private readonly struct GalleryPhotoEntry
    {
        public GalleryPhotoEntry(string documentId, string storagePath)
        {
            DocumentId = documentId;
            StoragePath = storagePath;
        }

        public string DocumentId { get; }
        public string StoragePath { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(DocumentId) && !string.IsNullOrWhiteSpace(StoragePath);
    }
}



