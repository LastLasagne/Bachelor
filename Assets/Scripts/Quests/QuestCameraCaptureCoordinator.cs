using DeadMosquito.AndroidGoodies;
using Obvious.Soap;
using UnityEngine;

public class QuestCameraCaptureCoordinator : MonoBehaviour
{
    [SerializeField] private GameObject editorSimulationPanel;
    [SerializeField] private ScriptableEventNoParam photoCaptured;
    [SerializeField] private ScriptableEventNoParam photoCaptureCancelled;

    private bool captureInProgress;

    public GameObject EditorSimulationPanel
    {
        get => editorSimulationPanel;
        set => editorSimulationPanel = value;
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

    // Invoked by SOAP's native EventListenerNoParam.
    public void HandleCaptureRequested()
    {
        if (captureInProgress)
        {
            return;
        }

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
            selectedImage =>
            {
                LastCapturedPhotoPath = selectedImage.OriginalPath;

                Debug.Log(
                    $"Quest photo captured: {selectedImage.DisplayName}. " +
                    $"Local path: {LastCapturedPhotoPath}",
                    this);

                photoCaptured?.Raise();
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
        editorSimulationPanel?.SetActive(false);
    }
}
