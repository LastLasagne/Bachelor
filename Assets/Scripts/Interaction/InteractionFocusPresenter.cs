using UnityEngine;

public class InteractionFocusPresenter : MonoBehaviour
{
    [SerializeField] private GameObject worldPrompt;

    public GameObject WorldPrompt
    {
        get => worldPrompt;
        set => worldPrompt = value;
    }

    private void Awake()
    {
        SetPromptVisible(false);
    }

    private void OnDisable()
    {
        SetPromptVisible(false);
    }

    // Invoked by SOAP's native EventListenerGameObject.
    public void HandleFocusChanged(GameObject focusedObject)
    {
        SetPromptVisible(focusedObject == gameObject);
    }

    private void SetPromptVisible(bool visible)
    {
        if (worldPrompt != null)
        {
            worldPrompt.SetActive(visible);
        }
    }
}
