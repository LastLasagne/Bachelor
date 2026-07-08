using UnityEngine;

[CreateAssetMenu(fileName = "ShopItem", menuName = "Game/Shop/Shop Item")]
public class ShopItemDefinition : ScriptableObject
{
    [SerializeField] private Sprite icon;
    [SerializeField] private string itemName;
    [SerializeField, TextArea(2, 6)] private string description;
    [SerializeField, Min(0)] private int cost = 10;

    public Sprite Icon => icon;
    public string ItemName => itemName;
    public string Description => description;
    public int Cost => cost;
}