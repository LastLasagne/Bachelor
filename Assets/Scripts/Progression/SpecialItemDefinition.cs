using UnityEngine;

[CreateAssetMenu(fileName = "SpecialItem", menuName = "Game/Progression/Special Item")]
public class SpecialItemDefinition : ScriptableObject
{
    [SerializeField] private Sprite icon;
    [SerializeField] private string itemName;
    [SerializeField, TextArea(3, 8)] private string description;

    public Sprite Icon => icon;
    public string ItemName => itemName;
    public string Description => description;
}