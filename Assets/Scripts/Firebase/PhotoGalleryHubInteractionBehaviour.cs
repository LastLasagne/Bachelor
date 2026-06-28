using UnityEngine;

public class PhotoGalleryHubInteractionBehaviour : InteractionBehaviour
{
    [SerializeField] private FirebasePhotoGalleryController galleryController;

    public FirebasePhotoGalleryController GalleryController
    {
        get => galleryController;
        set => galleryController = value;
    }

    protected override void Interact()
    {
        if (galleryController == null)
        {
            galleryController = FindFirstObjectByType<FirebasePhotoGalleryController>(FindObjectsInactive.Include);
        }

        if (galleryController != null)
        {
            galleryController.OpenGallery();
        }
        else
        {
            Debug.LogWarning("Photo gallery hub was used, but no FirebasePhotoGalleryController exists in the scene.", this);
        }
    }
}
