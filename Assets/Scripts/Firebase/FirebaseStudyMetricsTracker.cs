using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public sealed class FirebaseStudyMetricsTracker : MonoBehaviour
{
    public static FirebaseStudyMetricsTracker Instance { get; private set; }

    private const string CollectionName = "studyMetrics";
    private const float AutoFlushIntervalSeconds = 30f;

    private string participantId;
    private string sessionId;
    private float sessionStartRealtime;
    private float lastFlushRealtime;
    private bool initialized;
    private bool sessionEnded;
    private bool flushInProgress;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
        {
            new GameObject("Firebase Study Metrics Tracker").AddComponent<FirebaseStudyMetricsTracker>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        participantId = FirebasePlayerIdentity.LocalPlayerId;
        sessionId = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        sessionStartRealtime = Time.realtimeSinceStartup;
        lastFlushRealtime = sessionStartRealtime;
        _ = StartSessionAsync();
    }

    private void Update()
    {
        if (!initialized || sessionEnded || flushInProgress)
        {
            return;
        }

        if (Time.realtimeSinceStartup - lastFlushRealtime >= AutoFlushIntervalSeconds)
        {
            _ = FlushSessionAsync(ended: false);
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            _ = FlushSessionAsync(ended: false);
        }
    }

    private void OnApplicationQuit()
    {
        _ = FlushSessionAsync(ended: true);
    }

    public void RecordProgression(int trashProgression, int foodProgression, int galleryProgression)
    {
        _ = RecordProgressionAsync(trashProgression, foodProgression, galleryProgression);
    }

    public void RecordGameFinished(int trashProgression, int foodProgression, int galleryProgression)
    {
        _ = RecordGameFinishedAsync(trashProgression, foodProgression, galleryProgression);
    }

    private async Task StartSessionAsync()
    {
        try
        {
            var user = await FirebaseGameServices.EnsureSignedInAnonymouslyAsync();
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            DocumentReference participant = ParticipantDocument;
            DocumentReference session = SessionDocument;
            DocumentSnapshot existing = await participant.GetSnapshotAsync();

            Dictionary<string, object> activeDays = new Dictionary<string, object>();
            if (existing.TryGetValue("activeDays", out Dictionary<string, object> existingActiveDays) && existingActiveDays != null)
            {
                foreach (KeyValuePair<string, object> entry in existingActiveDays)
                {
                    activeDays[entry.Key] = entry.Value;
                }
            }

            activeDays[today] = true;

            Dictionary<string, object> participantUpdates = new Dictionary<string, object>
            {
                { "participantId", participantId },
                { "authUserId", user.UserId },
                { "lastSeenAt", FieldValue.ServerTimestamp },
                { "totalSessions", FieldValue.Increment(1) },
                { "activeDays", activeDays },
                { "activeDayCount", activeDays.Count },
                { "lastSessionId", sessionId }
            };

            if (existing.Exists)
            {
                await MarkPreviousOpenSessionsEndedAsync();
            }
            else
            {
                participantUpdates["createdAt"] = FieldValue.ServerTimestamp;
                participantUpdates["finishedGame"] = false;
                participantUpdates["trashProgressionReached"] = 0;
                participantUpdates["foodProgressionReached"] = 0;
                participantUpdates["galleryProgressionReached"] = 0;
                participantUpdates["totalProgressionReached"] = 0;
            }

            await participant.SetAsync(participantUpdates, SetOptions.MergeAll);
            await RemoveDeprecatedPlaytimeAggregatesAsync(participant);

            await session.SetAsync(new Dictionary<string, object>
            {
                { "participantId", participantId },
                { "authUserId", user.UserId },
                { "sessionId", sessionId },
                { "startedAt", FieldValue.ServerTimestamp },
                { "lastUpdatedAt", FieldValue.ServerTimestamp },
                { "playtimeSeconds", 0 },
                { "ended", false }
            }, SetOptions.MergeAll);

            initialized = true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not start Firebase study metrics session: {exception.Message}", this);
        }
    }

    private async Task RemoveDeprecatedPlaytimeAggregatesAsync(DocumentReference participant)
    {
        await participant.UpdateAsync(new Dictionary<string, object>
        {
            { "totalPlaytimeSeconds", FieldValue.Delete },
            { "lastSessionPlaytimeSeconds", FieldValue.Delete }
        });
    }
    private async Task MarkPreviousOpenSessionsEndedAsync()
    {
        QuerySnapshot openSessions = await ParticipantDocument.Collection("sessions")
            .WhereEqualTo("ended", false)
            .GetSnapshotAsync();

        foreach (DocumentSnapshot openSession in openSessions.Documents)
        {
            if (openSession.Id == sessionId)
            {
                continue;
            }

            await openSession.Reference.SetAsync(new Dictionary<string, object>
            {
                { "lastUpdatedAt", FieldValue.ServerTimestamp },
                { "ended", true },
                { "endedAt", FieldValue.ServerTimestamp },
                { "endedReason", "next_session_started" }
            }, SetOptions.MergeAll);
        }
    }
    private async Task FlushSessionAsync(bool ended)
    {
        if (flushInProgress || string.IsNullOrWhiteSpace(participantId) || string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        flushInProgress = true;
        try
        {
            float now = Time.realtimeSinceStartup;
            int playtimeSeconds = Mathf.Max(0, Mathf.RoundToInt(now - sessionStartRealtime));
            lastFlushRealtime = now;
            if (ended)
            {
                sessionEnded = true;
            }

            Dictionary<string, object> sessionUpdates = new Dictionary<string, object>
            {
                { "lastUpdatedAt", FieldValue.ServerTimestamp },
                { "playtimeSeconds", playtimeSeconds },
                { "ended", ended }
            };

            if (ended)
            {
                sessionUpdates["endedAt"] = FieldValue.ServerTimestamp;
            }

            await SessionDocument.SetAsync(sessionUpdates, SetOptions.MergeAll);
            await ParticipantDocument.SetAsync(new Dictionary<string, object>
            {
                { "lastSeenAt", FieldValue.ServerTimestamp },
                { "lastSessionId", sessionId }
            }, SetOptions.MergeAll);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not flush Firebase study metrics session: {exception.Message}", this);
        }
        finally
        {
            flushInProgress = false;
        }
    }

    private async Task RecordProgressionAsync(int trashProgression, int foodProgression, int galleryProgression)
    {
        try
        {
            await FirebaseGameServices.EnsureSignedInAnonymouslyAsync();
            int totalProgression = Mathf.Max(0, trashProgression) + Mathf.Max(0, foodProgression) + Mathf.Max(0, galleryProgression);
            await ParticipantDocument.SetAsync(new Dictionary<string, object>
            {
                { "lastSeenAt", FieldValue.ServerTimestamp },
                { "trashProgressionReached", trashProgression },
                { "foodProgressionReached", foodProgression },
                { "galleryProgressionReached", galleryProgression },
                { "totalProgressionReached", totalProgression }
            }, SetOptions.MergeAll);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not write Firebase study progression metrics: {exception.Message}", this);
        }
    }

    private async Task RecordGameFinishedAsync(int trashProgression, int foodProgression, int galleryProgression)
    {
        try
        {
            await FirebaseGameServices.EnsureSignedInAnonymouslyAsync();
            int totalProgression = Mathf.Max(0, trashProgression) + Mathf.Max(0, foodProgression) + Mathf.Max(0, galleryProgression);
            await ParticipantDocument.SetAsync(new Dictionary<string, object>
            {
                { "lastSeenAt", FieldValue.ServerTimestamp },
                { "finishedGame", true },
                { "finishedGameAt", FieldValue.ServerTimestamp },
                { "trashProgressionReached", trashProgression },
                { "foodProgressionReached", foodProgression },
                { "galleryProgressionReached", galleryProgression },
                { "totalProgressionReached", totalProgression }
            }, SetOptions.MergeAll);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not write Firebase study game-finished metrics: {exception.Message}", this);
        }
    }


    private DocumentReference ParticipantDocument => FirebaseGameServices.Firestore.Collection(CollectionName).Document(participantId);
    private DocumentReference SessionDocument => ParticipantDocument.Collection("sessions").Document(sessionId);
}
