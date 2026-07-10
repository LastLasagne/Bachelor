using System.Collections.Generic;
using Obvious.Soap;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public enum TrashUnlockRewardType
{
    RemoveTrashObjects,
    AddMaterialResource,
    ShowStoryBit
}

[System.Serializable]
public class TrashProgressionUnlock
{
    [SerializeField, Min(1)] private int threshold = 1;
    [SerializeField] private TrashUnlockRewardType rewardType;
    [SerializeField, Min(0)] private int materialAmount;
    [SerializeField] private StoryBitDefinition storyBit;
    [SerializeField, TextArea(2, 4)] private string rewardMessage;
    [SerializeField] private List<GameObject> trashObjects = new List<GameObject>();

    public int Threshold => threshold;
    public TrashUnlockRewardType RewardType => rewardType;
    public int MaterialAmount => materialAmount;
    public StoryBitDefinition StoryBit => storyBit;
    public string RewardMessage => rewardMessage;
    public IReadOnlyList<GameObject> TrashObjects => trashObjects;

    public TrashProgressionUnlock(int threshold, TrashUnlockRewardType rewardType)
    {
        this.threshold = threshold;
        this.rewardType = rewardType;
    }

    public void SetMaterialAmount(int amount)
    {
        materialAmount = Mathf.Max(0, amount);
    }

    public void SetStoryBit(StoryBitDefinition story)
    {
        storyBit = story;
    }

    public void SetRewardMessage(string message)
    {
        rewardMessage = message;
    }

    public void SetTrashObjects(IEnumerable<GameObject> objects)
    {
        trashObjects.Clear();
        if (objects == null)
        {
            return;
        }

        foreach (GameObject trashObject in objects)
        {
            if (trashObject != null && !trashObjects.Contains(trashObject))
            {
                trashObjects.Add(trashObject);
            }
        }
    }
}

[ExecuteAlways]
public class TrashProgressionIslandController : MonoBehaviour, IQuestRewardMessageProvider
{
    [SerializeField] private IntVariable trashProgression;
    [SerializeField] private MaterialResourceBank materialResourceBank;
    [SerializeField] private StoryWindowController storyWindowController;
    [SerializeField] private StoryBitDefinition thresholdThreeStory;
    [SerializeField] private StoryBitDefinition thresholdNineStory;
    [SerializeField] private List<TrashProgressionUnlock> unlocks = new List<TrashProgressionUnlock>();

    private int lastHandledProgression;
    private bool restoringSavedProgression;

    public IReadOnlyList<TrashProgressionUnlock> Unlocks => unlocks;

    private void OnEnable()
    {
        EnsureDefaultSetup();
        Subscribe();
        lastHandledProgression = CurrentProgression;
        RefreshTrashObjectState(lastHandledProgression);
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        EnsureDefaultSetup();
        RefreshTrashObjectState(CurrentProgression);
    }

    private void Subscribe()
    {
        if (trashProgression != null)
        {
            trashProgression.OnValueChanged += HandleTrashProgressionChanged;
        }
    }

    private void Unsubscribe()
    {
        if (trashProgression != null)
        {
            trashProgression.OnValueChanged -= HandleTrashProgressionChanged;
        }
    }

    private int CurrentProgression => trashProgression != null ? trashProgression.Value : 0;

    public void RestoreSavedProgression(int progression)
    {
        if (trashProgression == null) return;
        restoringSavedProgression = true;
        trashProgression.Value = Mathf.Max(0, progression);
        restoringSavedProgression = false;
        lastHandledProgression = trashProgression.Value;
        RefreshTrashObjectState(lastHandledProgression);
    }
    private void HandleTrashProgressionChanged(int newProgression)
    {
        if (restoringSavedProgression)
        {
            lastHandledProgression = newProgression;
            RefreshTrashObjectState(newProgression);
            return;
        }

        if (newProgression > lastHandledProgression)
        {
            ApplyNewUnlocks(lastHandledProgression, newProgression);
        }

        lastHandledProgression = newProgression;
        RefreshTrashObjectState(newProgression);
    }

    private void ApplyNewUnlocks(int previousProgression, int newProgression)
    {
        foreach (TrashProgressionUnlock unlock in unlocks)
        {
            if (unlock == null || unlock.Threshold <= previousProgression || unlock.Threshold > newProgression)
            {
                continue;
            }

            switch (unlock.RewardType)
            {
                case TrashUnlockRewardType.AddMaterialResource:
                    materialResourceBank?.Add(unlock.MaterialAmount);
                    break;
                case TrashUnlockRewardType.ShowStoryBit:
                    if (!restoringSavedProgression) ResolveStoryWindowController()?.ShowStory(unlock.StoryBit);
                    break;
            }
        }
    }

    public IEnumerable<string> BuildRewardMessages(QuestDefinition quest, int previousProgression, int newProgression)
    {
        if (quest == null || quest.Category != QuestCategory.Trash || newProgression <= previousProgression)
        {
            yield break;
        }

        foreach (TrashProgressionUnlock unlock in unlocks)
        {
            if (unlock == null || unlock.Threshold <= previousProgression || unlock.Threshold > newProgression)
            {
                continue;
            }

            string message = ResolveRewardMessage(unlock);
            if (!string.IsNullOrWhiteSpace(message))
            {
                yield return message;
            }
        }
    }

    private static string ResolveRewardMessage(TrashProgressionUnlock unlock)
    {

        if (!string.IsNullOrWhiteSpace(unlock.RewardMessage))
        {
            return unlock.RewardMessage
                .Replace("{amount}", unlock.MaterialAmount.ToString())
                .Replace("{story}", unlock.StoryBit != null ? unlock.StoryBit.name : "story")
                .Replace("{threshold}", unlock.Threshold.ToString());
        }

        switch (unlock.RewardType)
        {
            case TrashUnlockRewardType.AddMaterialResource:
                return $"Gained {unlock.MaterialAmount} materials.";
            case TrashUnlockRewardType.ShowStoryBit:
                return "Unlocked a new story piece.";
            case TrashUnlockRewardType.RemoveTrashObjects:
                return "Cleared trash from the island.";
            default:
                return string.Empty;
        }
    }

    private void RefreshTrashObjectState(int progression)
    {
        foreach (TrashProgressionUnlock unlock in unlocks)
        {
            if (unlock == null || unlock.RewardType != TrashUnlockRewardType.RemoveTrashObjects)
            {
                continue;
            }

            bool shouldShowTrash = progression < unlock.Threshold;
            foreach (GameObject trashObject in unlock.TrashObjects)
            {
                if (trashObject != null)
                {
                    trashObject.SetActive(shouldShowTrash);
                }
            }
        }
    }

    private void EnsureDefaultSetup()
    {
        EnsureTrashGroups();
        EnsureDefaultUnlocks();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private void EnsureDefaultUnlocks()
    {
        if (unlocks.Count == 0)
        {
            unlocks.Add(new TrashProgressionUnlock(1, TrashUnlockRewardType.RemoveTrashObjects));
            unlocks.Add(new TrashProgressionUnlock(2, TrashUnlockRewardType.AddMaterialResource));
            unlocks.Add(new TrashProgressionUnlock(3, TrashUnlockRewardType.ShowStoryBit));
            unlocks.Add(new TrashProgressionUnlock(4, TrashUnlockRewardType.AddMaterialResource));
            unlocks.Add(new TrashProgressionUnlock(5, TrashUnlockRewardType.RemoveTrashObjects));
            unlocks.Add(new TrashProgressionUnlock(6, TrashUnlockRewardType.AddMaterialResource));
            unlocks.Add(new TrashProgressionUnlock(7, TrashUnlockRewardType.AddMaterialResource));
            unlocks.Add(new TrashProgressionUnlock(8, TrashUnlockRewardType.RemoveTrashObjects));
            unlocks.Add(new TrashProgressionUnlock(9, TrashUnlockRewardType.ShowStoryBit));
            AssignDefaultResourceAmounts();
        }

        AssignDefaultStories();
        AssignDefaultTrashObjects();
    }

    private void AssignDefaultResourceAmounts()
    {
        SetMaterialAmountForThreshold(2, 25);
        SetMaterialAmountForThreshold(4, 50);
        SetMaterialAmountForThreshold(6, 75);
        SetMaterialAmountForThreshold(7, 100);
    }

    private void AssignDefaultStories()
    {
        SetStoryForThreshold(3, thresholdThreeStory);
        SetStoryForThreshold(9, thresholdNineStory);
    }

    private void AssignDefaultTrashObjects()
    {
        SetTrashObjectsForThreshold(1, "Trash Cleanup 01");
        SetTrashObjectsForThreshold(5, "Trash Cleanup 05");
        SetTrashObjectsForThreshold(8, "Trash Cleanup 08");
    }

    private void SetMaterialAmountForThreshold(int threshold, int amount)
    {
        TrashProgressionUnlock unlock = FindUnlock(threshold, TrashUnlockRewardType.AddMaterialResource);
        unlock?.SetMaterialAmount(amount);
    }

    private void SetStoryForThreshold(int threshold, StoryBitDefinition story)
    {
        TrashProgressionUnlock unlock = FindUnlock(threshold, TrashUnlockRewardType.ShowStoryBit);
        if (unlock != null && unlock.StoryBit == null)
        {
            unlock.SetStoryBit(story);
        }
    }

    private void SetTrashObjectsForThreshold(int threshold, string groupName)
    {
        TrashProgressionUnlock unlock = FindUnlock(threshold, TrashUnlockRewardType.RemoveTrashObjects);
        Transform group = transform.Find($"Trash Objects/{groupName}");
        if (unlock == null || group == null || unlock.TrashObjects.Count > 0)
        {
            return;
        }

        var objects = new List<GameObject>();
        for (int i = 0; i < group.childCount; i++)
        {
            objects.Add(group.GetChild(i).gameObject);
        }

        unlock.SetTrashObjects(objects);
    }

    private TrashProgressionUnlock FindUnlock(int threshold, TrashUnlockRewardType rewardType)
    {
        return unlocks.Find(unlock => unlock != null && unlock.Threshold == threshold && unlock.RewardType == rewardType);
    }

    private StoryWindowController ResolveStoryWindowController()
    {
        if (storyWindowController == null)
            storyWindowController = FindFirstObjectByType<StoryWindowController>(FindObjectsInactive.Include);
        return storyWindowController;
    }
    private void EnsureTrashGroups()
    {
        Transform trashRoot = FindOrCreateChild(transform, "Trash Objects");
        EnsureTrashGroup(trashRoot, "Trash Cleanup 01", new Vector3(-12f, 4.1f, -5f));
        EnsureTrashGroup(trashRoot, "Trash Cleanup 05", new Vector3(-16f, 4.2f, -11f));
        EnsureTrashGroup(trashRoot, "Trash Cleanup 08", new Vector3(-8f, 4.1f, -15f));
    }

    private void EnsureTrashGroup(Transform trashRoot, string groupName, Vector3 localPosition)
    {
        Transform group = FindOrCreateChild(trashRoot, groupName, out bool wasCreated);
        if (wasCreated)
        {
            group.localPosition = localPosition;
        }

        if (group.childCount > 0)
        {            return;
        }

        AddTrashPlaceholder(group, "Trash Bag", PrimitiveType.Capsule, new Vector3(-0.45f, 0.35f, 0f), new Vector3(0.35f, 0.35f, 0.35f));
        AddTrashPlaceholder(group, "Can Placeholder", PrimitiveType.Cylinder, new Vector3(0.05f, 0.25f, 0.15f), new Vector3(0.16f, 0.28f, 0.16f));
        AddTrashPlaceholder(group, "Bottle Placeholder", PrimitiveType.Cylinder, new Vector3(0.5f, 0.3f, -0.1f), new Vector3(0.12f, 0.35f, 0.12f));
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        return FindOrCreateChild(parent, childName, out _);
    }

    private static Transform FindOrCreateChild(Transform parent, string childName, out bool wasCreated)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            wasCreated = false;
            return child;
        }

        wasCreated = true;
        var childObject = new GameObject(childName);
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
        }
#endif
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    private static void RemovePlaceholderColliders(Transform group)
    {
        for (int i = 0; i < group.childCount; i++)
        {
            Collider collider = group.GetChild(i).GetComponent<Collider>();
            if (collider != null)
            {
                DestroySmart(collider);
            }
        }
    }
    private static void AddTrashPlaceholder(Transform parent, string objectName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale)
    {
        GameObject placeholder = GameObject.CreatePrimitive(primitiveType);
        placeholder.name = objectName;
        placeholder.transform.SetParent(parent, false);
        placeholder.transform.localPosition = localPosition;
        placeholder.transform.localScale = localScale;

        Collider collider = placeholder.GetComponent<Collider>();
        if (collider != null)
        {
            DestroySmart(collider);
        }
    }

    private static void DestroySmart(Object target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}