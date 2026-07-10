using Obvious.Soap;
using UnityEngine;

public class InventorStoryHubInteractionBehaviour : InteractionBehaviour
{
    [SerializeField] private ScriptableEventGameObject interactionDispatched;
    [SerializeField] private StoryWindowController storyWindowController;
    [SerializeField] private StoryBitDefinition firstStory;
    [SerializeField] private StoryBitDefinition secondStory;
    [SerializeField, Min(0)] private int firstTrashThreshold = 4;
    [SerializeField, Min(0)] private int firstGalleryThreshold = 11;
    [SerializeField, Min(0)] private int secondTrashThreshold = 9;
    [SerializeField, Min(0)] private int secondGalleryThreshold = 18;
    [SerializeField, TextArea(2, 4)] private string emptyMessage = "Nothing here to find right now. You should check again soon.";

    private bool subscribed;

    private void OnEnable() => Subscribe();
    private void Start() => Subscribe();
    private void OnDisable()
    {
        if (subscribed && interactionDispatched != null) interactionDispatched.OnRaised -= HandleInteractionDispatched;
        subscribed = false;
    }

    private void Subscribe()
    {
        if (subscribed || interactionDispatched == null) return;
        interactionDispatched.OnRaised += HandleInteractionDispatched;
        subscribed = true;
    }

    protected override void Interact()
    {
        GameProgressManager progress = GameProgressManager.Instance;
        StoryWindowController window = ResolveStoryWindow();
        if (progress == null || window == null) return;

        if (!progress.IsInventorStoryRead(0)
            && progress.TrashProgression >= firstTrashThreshold
            && progress.GalleryProgression >= firstGalleryThreshold)
        {
            progress.MarkInventorStoryRead(0);
            window.ShowStory(firstStory);
            return;
        }

        if (!progress.IsInventorStoryRead(1)
            && progress.TrashProgression >= secondTrashThreshold
            && progress.GalleryProgression >= secondGalleryThreshold)
        {
            progress.MarkInventorStoryRead(1);
            window.ShowStory(secondStory);
            return;
        }

        window.ShowMessage(emptyMessage);
    }

    private StoryWindowController ResolveStoryWindow()
    {
        if (storyWindowController == null)
            storyWindowController = FindFirstObjectByType<StoryWindowController>(FindObjectsInactive.Include);
        return storyWindowController;
    }
}
