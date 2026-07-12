using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Obvious.Soap;
using UnityEngine;

public sealed class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    private GameProgressSave save;
    private QuestDefinition activeTrashRepeatable;
    private QuestDefinition activeTrashRare;
    private QuestDefinition activeFoodRepeatable;
    private QuestDefinition activeFoodRare;
    private MaterialResourceBank bank;
    private ShopController shop;
    private IntVariable trash;
    private TrashProgressionIslandController trashController;
    private IntVariable food;
    private IntVariable gallery;
    private PhotoGalleryProgressionController galleryController;
    private bool applying;

    public QuestDefinition ActiveRepeatableQuest => GetActiveRepeatableQuest(QuestCategory.Trash);
    public QuestDefinition ActiveRareQuest => GetActiveRareQuest(QuestCategory.Trash);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null) new GameObject("Game Progress Manager").AddComponent<GameProgressManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        save = Resources.Load<GameProgressSave>("GameProgressSave");
        if (save == null) { Debug.LogError("GameProgressSave asset is missing from Resources."); return; }
        save.Load();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (shop == null)
        {
            ResolveSceneState();
            ApplySave();
            RotateQuestsIfNeeded();
            AssignDailyQuests();
            Subscribe();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnApplicationPause(bool paused) { if (paused) CaptureAndSave(); }
    private void OnApplicationQuit() => CaptureAndSave();
    private void OnApplicationFocus(bool focused) { if (focused) { RotateQuestsIfNeeded(); AssignDailyQuests(); } }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene _, UnityEngine.SceneManagement.LoadSceneMode __)
    {
        ResolveSceneState();
        ApplySave();
        RotateQuestsIfNeeded();
        AssignDailyQuests();
        Subscribe();
    }

    private void ResolveSceneState()
    {
        shop = FindFirstObjectByType<ShopController>(FindObjectsInactive.Include);
        bank = shop != null ? shop.MaterialResourceBank : null;
        var quests = Resources.LoadAll<QuestDefinition>("Quests");
        trash = quests.FirstOrDefault(q => q.Category == QuestCategory.Trash)?.CategoryTotalProgression;
        trashController = FindFirstObjectByType<TrashProgressionIslandController>(FindObjectsInactive.Include);
        food = quests.FirstOrDefault(q => q.Category == QuestCategory.Food)?.CategoryTotalProgression;
        galleryController = FindFirstObjectByType<PhotoGalleryProgressionController>(FindObjectsInactive.Include);
        gallery = galleryController != null ? galleryController.Progression : null;
    }

    private void ApplySave()
    {
        applying = true;
        var data = save.Data;
        if (trashController != null) trashController.RestoreSavedProgression(data.trashProgression);
        else if (trash != null) trash.Value = data.trashProgression;
        if (food != null) food.Value = data.foodProgression;
        if (galleryController != null) galleryController.RestoreSavedProgression(data.galleryProgression);
        else if (gallery != null) gallery.Value = data.galleryProgression;
        bank?.SetAmount(data.materialAmount);
        shop?.RestorePurchasedItems(data.purchasedShopItemIds);
        applying = false;
    }

    private void Subscribe()
    {
        if (trash != null) trash.OnValueChanged += OnStateChanged;
        if (food != null) food.OnValueChanged += OnStateChanged;
        if (gallery != null) gallery.OnValueChanged += OnStateChanged;
        if (bank != null) bank.AmountChanged += OnStateChanged;
        if (shop != null) shop.ItemPurchased += OnItemPurchased;
    }

    private void OnStateChanged(int _)
    {
        CaptureAndSave();
        FirebaseStudyMetricsTracker.Instance?.RecordProgression(TrashProgression, FoodProgression, GalleryProgression);
    }
    private void OnItemPurchased(ShopItemDefinition _) => CaptureAndSave();

    public QuestDefinition GetActiveRepeatableQuest(QuestCategory category)
    {
        return category == QuestCategory.Food ? activeFoodRepeatable : activeTrashRepeatable;
    }

    public QuestDefinition GetActiveRareQuest(QuestCategory category)
    {
        return category == QuestCategory.Food ? activeFoodRare : activeTrashRare;
    }

    public int TrashProgression => trash != null ? trash.Value : 0;
    public int FoodProgression => food != null ? food.Value : 0;
    public int GalleryProgression => gallery != null ? gallery.Value : 0;
    public bool HasSeenOpeningStory => save != null && save.Data.openingStorySeen;
    public bool AreAllInventorStoriesRead => save != null && save.Data.inventorStoryOneRead && save.Data.inventorStoryTwoRead;

    public int GalleryViewsToday => save != null ? save.Data.galleryViewsToday : 0;
    public bool CanViewGalleryPhoto(int dailyLimit) => save != null && save.Data.galleryViewsToday < Mathf.Max(1, dailyLimit);

    public void RecordGalleryView()
    {
        if (save == null) return;
        save.Data.galleryViewsToday++;
        save.Commit();
    }
    public void MarkOpeningStorySeen()
    {
        if (save == null || save.Data.openingStorySeen) return;
        save.Data.openingStorySeen = true;
        save.Commit();
    }

    public bool HasPurchasedItem(string itemId)
    {
        return save != null && !string.IsNullOrEmpty(itemId) && save.Data.purchasedShopItemIds.Contains(itemId);
    }
    public bool IsInventorStoryRead(int storyIndex)
    {
        if (save == null) return false;
        return storyIndex == 0 ? save.Data.inventorStoryOneRead : save.Data.inventorStoryTwoRead;
    }

    public void MarkInventorStoryRead(int storyIndex)
    {
        if (save == null) return;
        if (storyIndex == 0) save.Data.inventorStoryOneRead = true;
        else save.Data.inventorStoryTwoRead = true;
        save.Commit();
    }
    public int GetAmountDue(QuestDefinition quest)
    {
        if (quest == null) return 1;
        if (quest.Frequency != QuestFrequency.Repeatable) return quest.AmountDue;

        int completionCount = quest.Category == QuestCategory.Food
            ? save.Data.foodRepeatableCompletionCount
            : save.Data.trashRepeatableCompletionCount;
        return Mathf.Max(1, quest.AmountDue + completionCount / 2);
    }

    public int GetProgress(QuestDefinition quest)
    {
        if (quest == null || save == null) return 0;
        if (quest == activeTrashRepeatable) return save.Data.trashRepeatableProgress;
        if (quest == activeTrashRare) return save.Data.trashRareProgress;
        if (quest == activeFoodRepeatable) return save.Data.foodRepeatableProgress;
        if (quest == activeFoodRare) return save.Data.foodRareProgress;
        return 0;
    }

    public bool IsCompletedToday(QuestDefinition quest)
    {
        if (quest == null || save == null) return false;
        if (quest == activeTrashRepeatable) return save.Data.trashRepeatableCompletedToday;
        if (quest == activeTrashRare) return save.Data.trashRareCompletedToday;
        if (quest == activeFoodRepeatable) return save.Data.foodRepeatableCompletedToday;
        return quest == activeFoodRare && save.Data.foodRareCompletedToday;
    }

    public void SetQuestProgress(QuestDefinition quest, int progress)
    {
        if (quest == activeTrashRepeatable) save.Data.trashRepeatableProgress = progress;
        else if (quest == activeTrashRare) save.Data.trashRareProgress = progress;
        else if (quest == activeFoodRepeatable) save.Data.foodRepeatableProgress = progress;
        else if (quest == activeFoodRare) save.Data.foodRareProgress = progress;
        save.Commit();
    }

    public void CompleteQuest(QuestDefinition quest)
    {
        if (quest == activeTrashRepeatable)
        {
            save.Data.trashRepeatableCompletedToday = true;
            save.Data.trashRepeatableCompletionCount++;
        }
        else if (quest == activeTrashRare)
        {
            save.Data.trashRareCompletedToday = true;
        }
        else if (quest == activeFoodRepeatable)
        {
            save.Data.foodRepeatableCompletedToday = true;
            save.Data.foodRepeatableCompletionCount++;
        }
        else if (quest == activeFoodRare)
        {
            save.Data.foodRareCompletedToday = true;
        }

        CaptureAndSave();
        FirebaseStudyMetricsTracker.Instance?.RecordProgression(TrashProgression, FoodProgression, GalleryProgression);
    }

    private void RotateQuestsIfNeeded()
    {
        if (save == null) return;
        string today = DateTime.Now.AddDays(save.Data.debugDayOffset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        QuestDefinition[] all = Resources.LoadAll<QuestDefinition>("Quests");
        if (save.Data.questDate != today)
        {
            string previousTrashRepeatableId = save.Data.trashRepeatableQuestId;
            string previousTrashRareId = save.Data.trashRareQuestId;
            string previousFoodRepeatableId = save.Data.foodRepeatableQuestId;
            string previousFoodRareId = save.Data.foodRareQuestId;

            save.Data.questDate = today;
            save.Data.galleryViewsToday = 0;
            save.Data.trashRepeatableCompletedToday = false;
            save.Data.trashRareCompletedToday = false;
            save.Data.foodRepeatableCompletedToday = false;
            save.Data.foodRareCompletedToday = false;
            save.Data.trashRepeatableProgress = 0;
            save.Data.trashRareProgress = 0;
            save.Data.foodRepeatableProgress = 0;
            save.Data.foodRareProgress = 0;

            save.Data.trashRepeatableQuestId = Pick(all, QuestCategory.Trash, QuestFrequency.Repeatable, previousTrashRepeatableId)?.name;
            save.Data.trashRareQuestId = Pick(all, QuestCategory.Trash, QuestFrequency.Rare, previousTrashRareId)?.name;
            save.Data.foodRepeatableQuestId = Pick(all, QuestCategory.Food, QuestFrequency.Repeatable, previousFoodRepeatableId)?.name;
            save.Data.foodRareQuestId = Pick(all, QuestCategory.Food, QuestFrequency.Rare, previousFoodRareId)?.name;
            save.Commit();
        }

        activeTrashRepeatable = ResolveActive(all, save.Data.trashRepeatableQuestId, QuestCategory.Trash, QuestFrequency.Repeatable);
        activeTrashRare = ResolveActive(all, save.Data.trashRareQuestId, QuestCategory.Trash, QuestFrequency.Rare);
        activeFoodRepeatable = ResolveActive(all, save.Data.foodRepeatableQuestId, QuestCategory.Food, QuestFrequency.Repeatable);
        activeFoodRare = ResolveActive(all, save.Data.foodRareQuestId, QuestCategory.Food, QuestFrequency.Rare);

        bool resolvedIdsChanged = save.Data.trashRepeatableQuestId != activeTrashRepeatable?.name
            || save.Data.trashRareQuestId != activeTrashRare?.name
            || save.Data.foodRepeatableQuestId != activeFoodRepeatable?.name
            || save.Data.foodRareQuestId != activeFoodRare?.name;
        if (resolvedIdsChanged)
        {
            save.Data.trashRepeatableQuestId = activeTrashRepeatable?.name;
            save.Data.trashRareQuestId = activeTrashRare?.name;
            save.Data.foodRepeatableQuestId = activeFoodRepeatable?.name;
            save.Data.foodRareQuestId = activeFoodRare?.name;
            save.Commit();
        }
    }

    public void ResetAllProgressForDebug()
    {
        if (save == null) return;
        save.Delete();
        ApplySave();
        RotateQuestsIfNeeded();
        AssignDailyQuests();
        save.Commit();
        Debug.Log("Game progression and gallery view totals were reset.");
    }
    public void AdvanceToNextDayForDebug()
    {
        if (save == null) return;
        save.Data.debugDayOffset++;
        save.Data.questDate = string.Empty;
        RotateQuestsIfNeeded();
        AssignDailyQuests();
        save.Commit();
        Debug.Log($"Advanced quest clock to {DateTime.Now.AddDays(save.Data.debugDayOffset):yyyy-MM-dd}.");
    }

    private static QuestDefinition ResolveActive(QuestDefinition[] quests, string questId, QuestCategory category, QuestFrequency frequency)
    {
        return quests.FirstOrDefault(q => q.name == questId && q.Category == category && q.Frequency == frequency)
            ?? Pick(quests, category, frequency);
    }

    private static QuestDefinition Pick(IEnumerable<QuestDefinition> quests, QuestCategory category, QuestFrequency frequency, string excludedId = null)
    {
        QuestDefinition[] pool = quests.Where(q => q.Category == category && q.Frequency == frequency).ToArray();
        if (pool.Length > 1 && !string.IsNullOrEmpty(excludedId))
            pool = pool.Where(q => q.name != excludedId).ToArray();
        return pool.Length == 0 ? null : pool[UnityEngine.Random.Range(0, pool.Length)];
    }

    private void AssignDailyQuests()
    {
        foreach (QuestPointInteractionBehaviour point in FindObjectsByType<QuestPointInteractionBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            QuestCategory category = point.HubCategory;
            QuestDefinition quest = GetActiveRepeatableQuest(category) ?? GetActiveRareQuest(category);
            point.Quest = quest;
            point.gameObject.SetActive(quest != null);
        }
    }

    private void CaptureAndSave()
    {
        if (applying || save == null) return;
        save.Data.trashProgression = trash != null ? trash.Value : 0;
        save.Data.foodProgression = food != null ? food.Value : 0;
        save.Data.galleryProgression = gallery != null ? gallery.Value : 0;
        save.Data.materialAmount = bank != null ? bank.MaterialAmount : 0;
        save.Data.purchasedShopItemIds = shop != null ? shop.GetPurchasedItemIds() : new List<string>();
        save.Commit();
    }
}
