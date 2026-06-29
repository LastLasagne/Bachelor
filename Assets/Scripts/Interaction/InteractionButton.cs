using Obvious.Soap;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class InteractionButton : MonoBehaviour
{
    [SerializeField] private ScriptableEventNoParam interactPressed;

    private Button button;

    public ScriptableEventNoParam InteractPressed
    {
        get => interactPressed;
        set => interactPressed = value;
    }

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        button.onClick.AddListener(RaiseInteraction);
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(RaiseInteraction);
        }
    }

    public void RaiseInteraction()
    {
        if (interactPressed != null)
        {
            interactPressed.Raise();
        }
    }
}
