using System.Collections.Generic;
using Obvious.Soap;
using UnityEngine;

public enum PhotoGalleryUnlockRewardType
{
    StoryBit,
    ShopRecipe
}

[System.Serializable]
public class PhotoGalleryProgressionUnlock
{
    [SerializeField, Min(1)] private int threshold = 1;
    [SerializeField] private PhotoGalleryUnlockRewardType rewardType;
    [SerializeField] private StoryBitDefinition storyBit;
    [SerializeField, TextArea(2, 4)] private string rewardMessage;
    [SerializeField] private List<ShopOffer> shopOffers = new List<ShopOffer>();

    public int Threshold => threshold;
    public PhotoGalleryUnlockRewardType RewardType => rewardType;
    public StoryBitDefinition StoryBit => storyBit;
    public string RewardMessage => rewardMessage;
    public IReadOnlyList<ShopOffer> ShopOffers => shopOffers;

    public PhotoGalleryProgressionUnlock()
    {
    }

    public PhotoGalleryProgressionUnlock(int threshold, PhotoGalleryUnlockRewardType rewardType)
    {
        this.threshold = threshold;
        this.rewardType = rewardType;
    }

    public void SetStoryBit(StoryBitDefinition story)
    {
        storyBit = story;
    }

    public void SetRewardMessage(string message)
    {
        rewardMessage = message;
    }

    public void AddShopOffer(ShopItemDefinition item, GameObject collectionToActivate)
    {
        AddShopOffer(item, collectionToActivate, ShopPurchaseEffectType.ActivateCollection);
    }

    public void AddShopOffer(ShopItemDefinition item, GameObject collectionToActivate, ShopPurchaseEffectType purchaseEffect)
    {
        AddShopOffer(item, collectionToActivate, purchaseEffect, null);
    }

    public void AddShopOffer(ShopItemDefinition item, GameObject collectionToActivate, ShopPurchaseEffectType purchaseEffect, GameObject exclusiveChoiceGroup)
    {
        if (item == null)
        {
            return;
        }

        foreach (ShopOffer offer in shopOffers)
        {
            if (offer != null && offer.Item == item)
            {
                return;
            }
        }

        shopOffers.Add(new ShopOffer(item, collectionToActivate, purchaseEffect, exclusiveChoiceGroup));
    }
}

public class PhotoGalleryProgressionController : MonoBehaviour, IQuestRewardMessageProvider
{
    [SerializeField] private IntVariable photoGalleryProgression;
    [SerializeField] private StoryWindowController storyWindowController;
    [SerializeField] private ShopController shopController;
    [SerializeField] private List<PhotoGalleryProgressionUnlock> unlocks = new List<PhotoGalleryProgressionUnlock>();

    private int lastHandledProgression;

    public IReadOnlyList<PhotoGalleryProgressionUnlock> Unlocks => unlocks;

    private void OnEnable()
    {
        EnsureDefaultUnlocks();
        ApplyRecipeUnlocksUpTo(CurrentProgression);
        lastHandledProgression = CurrentProgression;
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        EnsureDefaultUnlocks();
    }

    private int CurrentProgression => photoGalleryProgression != null ? photoGalleryProgression.Value : 0;

    private void Subscribe()
    {
        if (photoGalleryProgression != null)
        {
            photoGalleryProgression.OnValueChanged += HandleProgressionChanged;
        }
    }

    private void Unsubscribe()
    {
        if (photoGalleryProgression != null)
        {
            photoGalleryProgression.OnValueChanged -= HandleProgressionChanged;
        }
    }

    private void HandleProgressionChanged(int newProgression)
    {
        if (newProgression > lastHandledProgression)
        {
            ApplyNewUnlocks(lastHandledProgression, newProgression, showStories: true);
        }

        lastHandledProgression = newProgression;
    }

    private void ApplyRecipeUnlocksUpTo(int progression)
    {
        ApplyNewUnlocks(0, progression, showStories: false);
    }

    private void ApplyNewUnlocks(int previousProgression, int newProgression, bool showStories)
    {
        foreach (PhotoGalleryProgressionUnlock unlock in unlocks)
        {
            if (unlock == null || unlock.Threshold <= previousProgression || unlock.Threshold > newProgression)
            {
                continue;
            }

            switch (unlock.RewardType)
            {
                case PhotoGalleryUnlockRewardType.StoryBit:
                    if (showStories)
                    {
                        StoryWindowController targetStoryWindow = ResolveStoryWindowController();
                        targetStoryWindow?.ShowStory(unlock.StoryBit);
                    }
                    break;
                case PhotoGalleryUnlockRewardType.ShopRecipe:
                    ShopController targetShop = ResolveShopController();
                    if (targetShop == null)
                    {
                        break;
                    }

                    foreach (ShopOffer offer in unlock.ShopOffers)
                    {
                        targetShop.AddOffer(offer);
                    }
                    break;
            }
        }
    }

    public IEnumerable<string> BuildRewardMessages(QuestDefinition quest, int previousProgression, int newProgression)
    {
        bool isGalleryQuest = quest != null && (quest.Category == QuestCategory.Gallery || quest.CategoryTotalProgression == photoGalleryProgression);
        if (!isGalleryQuest || newProgression <= previousProgression)
        {
            yield break;
        }

        foreach (PhotoGalleryProgressionUnlock unlock in unlocks)
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

    private static string ResolveRewardMessage(PhotoGalleryProgressionUnlock unlock)
    {
        if (!string.IsNullOrWhiteSpace(unlock.RewardMessage))
        {
            return unlock.RewardMessage
                .Replace("{count}", unlock.ShopOffers.Count.ToString())
                .Replace("{threshold}", unlock.Threshold.ToString());
        }

        switch (unlock.RewardType)
        {
            case PhotoGalleryUnlockRewardType.StoryBit:
                return "Unlocked a new story.";
            case PhotoGalleryUnlockRewardType.ShopRecipe:
                int count = unlock.ShopOffers.Count;
                return count == 1 ? "Unlocked a new shop recipe." : $"Unlocked {count} new shop recipes.";
            default:
                return string.Empty;
        }
    }

    private StoryWindowController ResolveStoryWindowController()
    {
        if (storyWindowController == null)
        {
            storyWindowController = FindFirstObjectByType<StoryWindowController>(FindObjectsInactive.Include);
        }

        return storyWindowController;
    }

    private ShopController ResolveShopController()
    {
        if (shopController == null)
        {
            shopController = FindFirstObjectByType<ShopController>(FindObjectsInactive.Include);
        }

        return shopController;
    }

    private void EnsureDefaultUnlocks()
    {
        if (unlocks.Count > 0)
        {
            return;
        }

        unlocks.Add(new PhotoGalleryProgressionUnlock(1, PhotoGalleryUnlockRewardType.StoryBit));
        unlocks.Add(new PhotoGalleryProgressionUnlock(2, PhotoGalleryUnlockRewardType.StoryBit));
        unlocks.Add(new PhotoGalleryProgressionUnlock(3, PhotoGalleryUnlockRewardType.StoryBit));
        unlocks.Add(new PhotoGalleryProgressionUnlock(4, PhotoGalleryUnlockRewardType.ShopRecipe));
        unlocks.Add(new PhotoGalleryProgressionUnlock(5, PhotoGalleryUnlockRewardType.StoryBit));
        unlocks.Add(new PhotoGalleryProgressionUnlock(6, PhotoGalleryUnlockRewardType.ShopRecipe));
        unlocks.Add(new PhotoGalleryProgressionUnlock(7, PhotoGalleryUnlockRewardType.ShopRecipe));
        unlocks.Add(new PhotoGalleryProgressionUnlock(8, PhotoGalleryUnlockRewardType.ShopRecipe));
        unlocks.Add(new PhotoGalleryProgressionUnlock(9, PhotoGalleryUnlockRewardType.ShopRecipe));
        unlocks.Add(new PhotoGalleryProgressionUnlock(10, PhotoGalleryUnlockRewardType.ShopRecipe));
    }
}