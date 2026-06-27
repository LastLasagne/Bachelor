using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Firebase.Firestore;
using Firebase.Storage;
using UnityEngine;

public class QuestPhotoFirebaseUploader : MonoBehaviour
{
    [Header("Firebase Paths")]
    [SerializeField] private string photoCollection = "questPhotos";
    [SerializeField] private string storageRootFolder = "questPhotos";

    [Header("Photo Metadata")]
    [SerializeField] private string defaultQuestId = "unknown_quest";
    [SerializeField] private string defaultHubId = "quest_hub";

#if UNITY_EDITOR
    [Header("Editor Testing")]
    [SerializeField] private string editorTestPhotoPath;
#endif

    public async void UploadPhoto(string localPhotoPath)
    {
        await UploadPhotoAsync(localPhotoPath, defaultQuestId, defaultHubId);
    }

#if UNITY_EDITOR
    [ContextMenu("Upload Editor Test Photo")]
    private async void UploadEditorTestPhoto()
    {
        await UploadPhotoAsync(editorTestPhotoPath, defaultQuestId, defaultHubId);
    }
#endif

    public async Task UploadPhotoAsync(string localPhotoPath, string questId, string hubId)
    {
        if (string.IsNullOrWhiteSpace(localPhotoPath))
        {
            Debug.LogWarning("Cannot upload quest photo because the local photo path is empty.", this);
            return;
        }

        if (!File.Exists(localPhotoPath))
        {
            Debug.LogWarning($"Cannot upload quest photo because the file does not exist: {localPhotoPath}", this);
            return;
        }

        try
        {
            var user = await FirebaseGameServices.EnsureSignedInAnonymouslyAsync();

            string photoId = Guid.NewGuid().ToString("N");
            string safeQuestId = ToFirebasePathSegment(questId, defaultQuestId);
            string storagePath = $"{storageRootFolder}/{safeQuestId}/{photoId}.jpg";

            StorageReference photoReference = FirebaseGameServices.Storage.RootReference.Child(storagePath);
            var metadata = new MetadataChange
            {
                ContentType = "image/jpeg"
            };

            Debug.Log($"Uploading quest photo to Firebase Storage: {storagePath}", this);
            StorageMetadata storageMetadata = await photoReference.PutFileAsync(localPhotoPath, metadata);

            Dictionary<string, object> photoData = new Dictionary<string, object>
            {
                { "photoId", photoId },
                { "questId", safeQuestId },
                { "hubId", string.IsNullOrWhiteSpace(hubId) ? defaultHubId : hubId },
                { "uploaderId", user.UserId },
                { "storagePath", storagePath },
                { "contentType", storageMetadata.ContentType },
                { "sizeBytes", storageMetadata.SizeBytes },
                { "thumbsUp", 0 },
                { "thumbsDown", 0 },
                { "reactionCount", 0 },
                { "status", "visible" },
                { "createdAt", FieldValue.ServerTimestamp }
            };

            await FirebaseGameServices.Firestore
                .Collection(photoCollection)
                .Document(photoId)
                .SetAsync(photoData);

            Debug.Log($"Quest photo upload complete. Firestore document: {photoCollection}/{photoId}", this);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Quest photo upload failed: {exception}", this);
        }
    }

    private static string ToFirebasePathSegment(string value, string fallback)
    {
        string segment = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return segment
            .Replace("\\", "_")
            .Replace("/", "_")
            .Replace("#", "_")
            .Replace("?", "_");
    }
}

