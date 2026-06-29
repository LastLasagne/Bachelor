using System.Threading.Tasks;
using DeadMosquito.AndroidGoodies;
using Obvious.Soap;
using UnityEngine;
using UnityEngine.UI;

public class QuestCameraCaptureCoordinator : MonoBehaviour
{
    [SerializeField] private GameObject editorSimulationPanel;
    [SerializeField] private ScriptableEventNoParam photoCaptureRequested;
    [SerializeField] private ScriptableEventNoParam photoCaptured;
    [SerializeField] private ScriptableEventNoParam photoCaptureCancelled;
    [SerializeField] private FirebaseQuestPhotoUploader photoUploader;
    [SerializeField] private string firebaseQuestId = "unknown_quest";
    [SerializeField] private string firebaseHubId = "quest_hub";

    [Header("Upload Feedback")]
    [SerializeField] private GameObject uploadProgressPanel;
    [SerializeField] private Text uploadProgressMessage;
    [SerializeField, TextArea(2, 4)]
    private string uploadProgressText = "Uploading picture...\nPlease stay on this screen.";

    private bool captureInProgress;

    public GameObject EditorSimulationPanel
    {
        get => editorSimulationPanel;
        set => editorSimulationPanel = value;
    }

    public ScriptableEventNoParam PhotoCaptureRequested
    {
        get => photoCaptureRequested;
        set => photoCaptureRequested = value;
    }

    public ScriptableEventNoParam PhotoCaptured
    {
        get => photoCaptured;
        set => photoCaptured = value;
    }

    public ScriptableEventNoParam PhotoCaptureCancelled
    {
        get => photoCaptureCancelled;
        set => photoCaptureCancelled = value;
    }

    public bool HasCapturedPhoto { get; private set; }
    public string LastCapturedPhotoPath { get; private set; }

    private void Awake()
    {
        EnsureUploadProgressPanel();
        SetUploadProgressVisible(false);
    }

    private void OnEnable()
    {
        if (photoCaptureRequested != null)
        {
            photoCaptureRequested.OnRaised += HandleCaptureRequested;
        }
    }

    private void OnDisable()
    {
        if (photoCaptureRequested != null)
        {
            photoCaptureRequested.OnRaised -= HandleCaptureRequested;
        }
    }

    // Invoked by SOAP's native EventListenerNoParam and direct event subscription.
    public void HandleCaptureRequested()
    {
        if (captureInProgress)
        {
            Debug.Log("Quest photo capture request ignored because a capture is already in progress.", this);
            return;
        }

        Debug.Log("Quest photo capture requested.", this);

        captureInProgress = true;
        HasCapturedPhoto = false;
        LastCapturedPhotoPath = string.Empty;

#if UNITY_ANDROID && !UNITY_EDITOR
        AGPermissions.ExecuteIfHasPermission(
            AGPermissions.CAMERA,
            TakePhoto,
            () =>
            {
                Debug.LogWarning("Camera permission was denied.", this);
                photoCaptureCancelled?.Raise();
            });
#else
        if (editorSimulationPanel != null)
        {
            editorSimulationPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Editor camera simulation panel is not assigned.", this);
            photoCaptureCancelled?.Raise();
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // Mirrors the working AndroidGoodies example without loading the image into a texture.
    private void TakePhoto()
    {
        const bool shouldGenerateThumbnails = false;
        const ImageResultSize imageResultSize = ImageResultSize.Max1024;

        AGCamera.TakePhoto(
            async selectedImage =>
            {
                LastCapturedPhotoPath = selectedImage.OriginalPath;

                Debug.Log(
                    $"Quest photo captured: {selectedImage.DisplayName}. " +
                    $"Local path: {LastCapturedPhotoPath}. " +
                    $"Size from picker: {selectedImage.Size} bytes.",
                    this);

                SetUploadProgressVisible(true);

                try
                {
                    await UploadLastCapturedPhotoAsync();
                    photoCaptured?.Raise();
                }
                finally
                {
                    SetUploadProgressVisible(false);
                }
            },
            error =>
            {
                Debug.Log($"Photo capture cancelled: {error}", this);
                photoCaptureCancelled?.Raise();
            },
            imageResultSize,
            shouldGenerateThumbnails);
    }
#endif

    // Invoked by SOAP after either a real or simulated successful capture.
    public void HandleCaptureSucceeded()
    {
        captureInProgress = false;
        HasCapturedPhoto = true;
        editorSimulationPanel?.SetActive(false);
    }

    // Invoked by SOAP after cancellation or permission failure.
    public void HandleCaptureCancelled()
    {
        captureInProgress = false;
        HasCapturedPhoto = false;
        LastCapturedPhotoPath = string.Empty;
        SetUploadProgressVisible(false);
        editorSimulationPanel?.SetActive(false);
    }

    private async Task UploadLastCapturedPhotoAsync()
    {
        if (photoUploader == null)
        {
            photoUploader = GetComponent<FirebaseQuestPhotoUploader>();
        }

        if (photoUploader == null)
        {
            photoUploader = gameObject.AddComponent<FirebaseQuestPhotoUploader>();
        }

        await photoUploader.UploadPhotoAsync(LastCapturedPhotoPath, firebaseQuestId, firebaseHubId);
    }

    private void EnsureUploadProgressPanel()
    {
        if (uploadProgressPanel != null)
        {
            if (uploadProgressMessage != null)
            {
                uploadProgressMessage.text = uploadProgressText;
            }

            return;
        }

        Transform panelParent = editorSimulationPanel != null && editorSimulationPanel.transform.parent != null
            ? editorSimulationPanel.transform.parent
            : transform;

        uploadProgressPanel = new GameObject("Photo Upload Progress", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        uploadProgressPanel.transform.SetParent(panelParent, false);
        uploadProgressPanel.transform.SetAsLastSibling();

        RectTransform panelRect = uploadProgressPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = uploadProgressPanel.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.09f, 0.1f, 0.86f);
        panelImage.raycastTarget = true;

        GameObject messageObject = new GameObject("Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        messageObject.transform.SetParent(uploadProgressPanel.transform, false);

        RectTransform messageRect = messageObject.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.1f, 0.38f);
        messageRect.anchorMax = new Vector2(0.9f, 0.62f);
        messageRect.offsetMin = Vector2.zero;
        messageRect.offsetMax = Vector2.zero;

        uploadProgressMessage = messageObject.GetComponent<Text>();
        uploadProgressMessage.alignment = TextAnchor.MiddleCenter;
        uploadProgressMessage.color = Color.white;
        uploadProgressMessage.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        uploadProgressMessage.fontSize = 26;
        uploadProgressMessage.resizeTextForBestFit = true;
        uploadProgressMessage.resizeTextMinSize = 16;
        uploadProgressMessage.resizeTextMaxSize = 30;
        uploadProgressMessage.raycastTarget = false;
        uploadProgressMessage.text = uploadProgressText;
    }

    private void SetUploadProgressVisible(bool visible)
    {
        EnsureUploadProgressPanel();

        if (uploadProgressMessage != null)
        {
            uploadProgressMessage.text = uploadProgressText;
        }

        uploadProgressPanel?.SetActive(visible);
    }
}

