using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.UI;

public class FirebaseNegativePhotoNotificationController : MonoBehaviour
{
    [Header("Firebase")]
    [SerializeField] private string photoCollection = "questPhotos";
    [SerializeField] private string negativeCompletedStatus = "negative_completed";
    [SerializeField] private bool filterByQuestHub = false;
    [SerializeField] private string questId = "unknown_quest";
    [SerializeField] private string hubId = "quest_hub";

    [Header("Scene UI")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private Text messageText;
    [SerializeField] private Button dismissButton;

    [Header("Copy")]
    [SerializeField, TextArea(2, 5)]
    private string singleMessage = "One of your shared quest pictures received negative reactions. More feedback will be added here later.";
    [SerializeField, TextArea(2, 5)]
    private string multipleMessage = "Some of your shared quest pictures received negative reactions. More feedback will be added here later.";

    private readonly List<DocumentReference> pendingNoticeDocuments = new List<DocumentReference>();
    private bool isDismissing;

    private void Awake()
    {
        SetMessageVisible(false);
    }

    private void OnEnable()
    {
        if (dismissButton != null)
        {
            dismissButton.onClick.AddListener(HandleDismissClicked);
        }
    }

    private void OnDisable()
    {
        if (dismissButton != null)
        {
            dismissButton.onClick.RemoveListener(HandleDismissClicked);
        }
    }

    public async void CheckForNegativePhotos(QuestPointInteractionBehaviour questPoint)
    {
        await CheckForNegativePhotosAsync(questPoint);
    }

    private async Task CheckForNegativePhotosAsync(QuestPointInteractionBehaviour questPoint)
    {
        if (!ValidateReferences())
        {
            return;
        }

        try
        {
            var user = await FirebaseGameServices.EnsureSignedInAnonymouslyAsync();
            string localPlayerId = FirebasePlayerIdentity.LocalPlayerId;
            Debug.Log($"Negative photo notification identity. Auth user: {user.UserId}. Local player: {localPlayerId}", this);

            string resolvedQuestId = ResolveQuestId(questPoint);
            string resolvedHubId = ResolveHubId(questPoint);
            pendingNoticeDocuments.Clear();

            await AddMatchingNegativeNoticesAsync(
                FirebaseGameServices.Firestore.Collection(photoCollection).WhereEqualTo("uploaderId", user.UserId),
                resolvedQuestId,
                resolvedHubId);

            await AddMatchingNegativeNoticesAsync(
                FirebaseGameServices.Firestore.Collection(photoCollection).WhereEqualTo("uploaderPlayerId", localPlayerId),
                resolvedQuestId,
                resolvedHubId);

            if (pendingNoticeDocuments.Count == 0)
            {
                SetMessageVisible(false);
                return;
            }

            if (messageText != null)
            {
                messageText.text = pendingNoticeDocuments.Count == 1 ? singleMessage : multipleMessage;
            }

            SetMessageVisible(true);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not check negative photo notifications: {exception}", this);
        }
    }

    private async Task AddMatchingNegativeNoticesAsync(Query query, string resolvedQuestId, string resolvedHubId)
    {
        QuerySnapshot snapshot = await query.GetSnapshotAsync();
        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            if (!document.TryGetValue("status", out string status) || status != negativeCompletedStatus)
            {
                continue;
            }

            if (document.TryGetValue("negativeNoticeDismissed", out bool dismissed) && dismissed)
            {
                continue;
            }

            if (filterByQuestHub &&
                (!document.TryGetValue("questId", out string documentQuestId) || documentQuestId != resolvedQuestId ||
                 !document.TryGetValue("hubId", out string documentHubId) || documentHubId != resolvedHubId))
            {
                continue;
            }

            if (pendingNoticeDocuments.Exists(existing => existing.Path == document.Reference.Path))
            {
                continue;
            }

            pendingNoticeDocuments.Add(document.Reference);
        }
    }
    private async void HandleDismissClicked()
    {
        if (isDismissing)
        {
            return;
        }

        isDismissing = true;
        if (dismissButton != null)
        {
            dismissButton.interactable = false;
        }

        try
        {
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "negativeNoticeDismissed", true },
                { "negativeNoticeDismissedAt", FieldValue.ServerTimestamp }
            };

            foreach (DocumentReference document in pendingNoticeDocuments)
            {
                await document.UpdateAsync(updates);
            }

            pendingNoticeDocuments.Clear();
            SetMessageVisible(false);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not dismiss negative photo notification: {exception}", this);
        }
        finally
        {
            isDismissing = false;
            if (dismissButton != null)
            {
                dismissButton.interactable = true;
            }
        }
    }

    private string ResolveQuestId(QuestPointInteractionBehaviour questPoint)
    {
        if (!string.IsNullOrWhiteSpace(questId))
        {
            return questId.Trim();
        }

        return questPoint != null && questPoint.Quest != null ? questPoint.Quest.name : "unknown_quest";
    }

    private string ResolveHubId(QuestPointInteractionBehaviour questPoint)
    {
        if (!string.IsNullOrWhiteSpace(hubId))
        {
            return hubId.Trim();
        }

        return questPoint != null ? questPoint.gameObject.name : "quest_hub";
    }

    private bool ValidateReferences()
    {
        bool valid = messagePanel != null && messageText != null && dismissButton != null;
        if (!valid)
        {
            Debug.LogError("Negative photo notification UI references are missing.", this);
        }

        return valid;
    }

    private void SetMessageVisible(bool visible)
    {
        messagePanel?.SetActive(visible);
    }
}


