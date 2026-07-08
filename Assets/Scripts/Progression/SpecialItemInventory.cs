using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpecialItemInventory", menuName = "Game/Progression/Special Item Inventory")]
public class SpecialItemInventory : ScriptableObject
{
    [SerializeField] private List<SpecialItemDefinition> items = new List<SpecialItemDefinition>();

    public IReadOnlyList<SpecialItemDefinition> Items => items;

    public void Add(SpecialItemDefinition item)
    {
        if (item != null && !items.Contains(item))
        {
            items.Add(item);
        }
    }

    public void Clear()
    {
        items.Clear();
    }
}