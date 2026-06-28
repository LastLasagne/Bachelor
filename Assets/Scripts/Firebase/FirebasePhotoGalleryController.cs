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
    [SerializeField, Min(1)] private int maxDocumentsToSample = 50;

    [Header("Progression")]
    [SerializeField] private IntVariable viewedPhotoProgression;
    [SerializeField, Min(1)] private int progressionValuePerPhoto = 1;

    [Header("Photo Display")]
    [SerializeField] private float landscapePhotoRotationDegrees = 90f;

    [Header("Scene UI")]
    [SerializeField] private GameObject galleryPanel;
    [SerializeField] private RawImage photoImage;
    [SerializeField] private Button photoButton;
    [SerializeField] private Text statusText;
    [SerializeField] private Text counterText;
    [SerializeField] private Button closeButton;

    private readonly List<string> storagePaths = new List<string>();
    private readonly Queue<string> shuffledStorageQueue = new Queue<string>();
    private readonly System.Random random = new System.Random();
    private bool isOpen;
    private bool isLoading;
    private string currentStoragePath;

    private void Awake()
    {
        ValidateGalleryReferences();
        SetPanelVisible(false);
    }

    private void OnEnable()
    {
        if (photoButton != null)
        {
            photoButton.onClick.AddListener(HandlePhotoClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseGallery);
        }
    }

    private void OnDisable()
    {
        if (photoButton != null)
        {
            photoButton.onClick.RemoveListener(HandlePhotoClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseGallery);
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
            return;
        }

        SetPanelVisible(true);
        UpdateCounterText();
        await LoadNextPhotoAsync(refreshList: true);
    }

    public void CloseGallery()
    {
        isOpen = false;
        currentStoragePath = string.Empty;
        SetPhotoTexture(null);
        SetPanelVisible(false);
    }

    private async void HandlePhotoClicked()
    {
        if (!isOpen || isLoading || string.IsNullOrEmpty(currentStoragePath))
        {
            return;
        }

        if (viewedPhotoProgression != null)
        {
            viewedPhotoProgression.Add(progressionValuePerPhoto);
        }

        UpdateCounterText();
        SetPhotoTexture(null);
        await LoadNextPhotoAsync(refreshList: shuffledStorageQueue.Count == 0);
    }

    private async Task LoadNextPhotoAsync(bool refreshList)
    {
        if (isLoading)
        {
            return;
        }

        isLoading = true;
        currentStoragePath = string.Empty;
        SetStatus("Loading picture...");
        SetPhotoButtonInteractable(false);

        try
        {
            await FirebaseGameServices.EnsureSignedInAnonymouslyAsync();

            if (refreshList || shuffledStorageQueue.Count == 0)
            {
                await RefreshPhotoListAsync();
            }

            while (shuffledStorageQueue.Count > 0)
            {
                currentStoragePath = shuffledStorageQueue.Dequeue();
                Texture2D texture = await DownloadTextureAsync(currentStoragePath);

                if (texture == null)
                {
                    storagePaths.Remove(currentStoragePath);
                    currentStoragePath = string.Empty;
                    continue;
                }

                SetPhotoTexture(texture);
                SetStatus("Tap the picture to continue.");
                SetPhotoButtonInteractable(true);
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
        storagePaths.Clear();
        shuffledStorageQueue.Clear();

        Query query = FirebaseGameServices.Firestore
            .Collection(photoCollection)
            .WhereEqualTo("status", visibleStatus)
            .Limit(maxDocumentsToSample);

        QuerySnapshot snapshot = await query.GetSnapshotAsync();
        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            if (document.TryGetValue("storagePath", out string storagePath) && !string.IsNullOrWhiteSpace(storagePath))
            {
                storagePaths.Add(storagePath);
            }
        }

        ShuffleStorageQueue();
    }

    private void ShuffleStorageQueue()
    {
        shuffledStorageQueue.Clear();

        List<string> shuffled = new List<string>(storagePaths);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        foreach (string storagePath in shuffled)
        {
            shuffledStorageQueue.Enqueue(storagePath);
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

    private static int ReadJpegExifOrientation(byte[] bytes)
    {
        if (bytes.Length < 4 || bytes[0] != 0xff || bytes[1] != 0xd8)
        {
            return 1;
        }

        int offset = 2;
        while (offset + 4 <= bytes.Length)
        {
            if (bytes[offset] != 0xff)
            {
                break;
            }

            byte marker = bytes[offset + 1];
            int segmentLength = ReadUInt16BigEndian(bytes, offset + 2);
            if (segmentLength < 2 || offset + 2 + segmentLength > bytes.Length)
            {
                break;
            }

            if (marker == 0xe1 && segmentLength >= 8 && IsExifHeader(bytes, offset + 4))
            {
                return ReadTiffOrientation(bytes, offset + 10, offset + 2 + segmentLength);
            }

            offset += 2 + segmentLength;
        }

        return 1;
    }

    private static bool IsExifHeader(byte[] bytes, int offset)
    {
        return offset + 6 <= bytes.Length &&
               bytes[offset] == (byte)'E' &&
               bytes[offset + 1] == (byte)'x' &&
               bytes[offset + 2] == (byte)'i' &&
               bytes[offset + 3] == (byte)'f' &&
               bytes[offset + 4] == 0 &&
               bytes[offset + 5] == 0;
    }

    private static int ReadTiffOrientation(byte[] bytes, int tiffStart, int segmentEnd)
    {
        if (tiffStart + 8 > segmentEnd)
        {
            return 1;
        }

        bool littleEndian = bytes[tiffStart] == 0x49 && bytes[tiffStart + 1] == 0x49;
        bool bigEndian = bytes[tiffStart] == 0x4d && bytes[tiffStart + 1] == 0x4d;
        if (!littleEndian && !bigEndian)
        {
            return 1;
        }

        int firstIfdOffset = ReadUInt32(bytes, tiffStart + 4, littleEndian);
        int ifdStart = tiffStart + firstIfdOffset;
        if (ifdStart + 2 > segmentEnd)
        {
            return 1;
        }

        int entryCount = ReadUInt16(bytes, ifdStart, littleEndian);
        for (int i = 0; i < entryCount; i++)
        {
            int entryOffset = ifdStart + 2 + i * 12;
            if (entryOffset + 12 > segmentEnd)
            {
                break;
            }

            int tag = ReadUInt16(bytes, entryOffset, littleEndian);
            if (tag == 0x0112)
            {
                return ReadUInt16(bytes, entryOffset + 8, littleEndian);
            }
        }

        return 1;
    }

    private static int ReadUInt16BigEndian(byte[] bytes, int offset)
    {
        return (bytes[offset] << 8) | bytes[offset + 1];
    }

    private static int ReadUInt16(byte[] bytes, int offset, bool littleEndian)
    {
        return littleEndian
            ? bytes[offset] | (bytes[offset + 1] << 8)
            : (bytes[offset] << 8) | bytes[offset + 1];
    }

    private static int ReadUInt32(byte[] bytes, int offset, bool littleEndian)
    {
        return littleEndian
            ? bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24)
            : (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
    }

    private static Texture2D ApplyExifOrientation(Texture2D source, int orientation)
    {
        if (orientation == 1)
        {
            return source;
        }

        Color32[] sourcePixels = source.GetPixels32();
        int sourceWidth = source.width;
        int sourceHeight = source.height;

        bool swapsDimensions = orientation == 5 || orientation == 6 || orientation == 7 || orientation == 8;
        int targetWidth = swapsDimensions ? sourceHeight : sourceWidth;
        int targetHeight = swapsDimensions ? sourceWidth : sourceHeight;
        Color32[] targetPixels = new Color32[targetWidth * targetHeight];

        for (int y = 0; y < sourceHeight; y++)
        {
            for (int x = 0; x < sourceWidth; x++)
            {
                int targetX;
                int targetY;
                switch (orientation)
                {
                    case 2:
                        targetX = sourceWidth - 1 - x;
                        targetY = y;
                        break;
                    case 3:
                        targetX = sourceWidth - 1 - x;
                        targetY = sourceHeight - 1 - y;
                        break;
                    case 4:
                        targetX = x;
                        targetY = sourceHeight - 1 - y;
                        break;
                    case 5:
                        targetX = y;
                        targetY = x;
                        break;
                    case 6:
                        targetX = sourceHeight - 1 - y;
                        targetY = x;
                        break;
                    case 7:
                        targetX = sourceHeight - 1 - y;
                        targetY = sourceWidth - 1 - x;
                        break;
                    case 8:
                        targetX = y;
                        targetY = sourceWidth - 1 - x;
                        break;
                    default:
                        return source;
                }

                targetPixels[targetY * targetWidth + targetX] = sourcePixels[y * sourceWidth + x];
            }
        }

        Texture2D oriented = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        oriented.SetPixels32(targetPixels);
        oriented.Apply();
        Destroy(source);
        return oriented;
    }

    private bool ValidateGalleryReferences()
    {
        bool hasRequiredReferences = galleryPanel != null &&
                                     photoImage != null &&
                                     photoButton != null &&
                                     statusText != null &&
                                     counterText != null &&
                                     closeButton != null;

        if (!hasRequiredReferences)
        {
            Debug.LogError("Photo gallery UI references are missing. Assign the scene UI fields on FirebasePhotoGalleryController.", this);
        }

        return hasRequiredReferences;
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

    private void SetPhotoButtonInteractable(bool interactable)
    {
        if (photoButton != null)
        {
            photoButton.interactable = interactable;
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
}






