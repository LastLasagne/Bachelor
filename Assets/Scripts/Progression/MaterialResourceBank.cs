using UnityEngine;

[CreateAssetMenu(fileName = "MaterialResourceBank", menuName = "Game/Progression/Material Resource Bank")]
public class MaterialResourceBank : ScriptableObject
{
    [SerializeField, Min(0)] private int materialAmount;

    public int MaterialAmount => materialAmount;

    public void Add(int amount)
    {
        materialAmount = Mathf.Max(0, materialAmount + amount);
    }

    public bool CanAfford(int amount)
    {
        return amount >= 0 && materialAmount >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount))
        {
            return false;
        }

        materialAmount -= amount;
        return true;
    }

    public void ResetAmount()
    {
        materialAmount = 0;
    }
}