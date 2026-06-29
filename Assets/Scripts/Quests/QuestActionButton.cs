using Obvious.Soap;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class QuestActionButton : MonoBehaviour
{
    [SerializeField] private ScriptableEventNoParam actionRequested;

    private Button button;

    public ScriptableEventNoParam ActionRequested
    {
        get => actionRequested;
        set => actionRequested = value;
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

        button.onClick.AddListener(RaiseAction);
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(RaiseAction);
        }
    }

    public void RaiseAction()
    {
        actionRequested?.Raise();
    }
}
