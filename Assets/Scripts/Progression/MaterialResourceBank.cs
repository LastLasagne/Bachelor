using UnityEngine;

[CreateAssetMenu(fileName = "MaterialResourceBank", menuName = "Game/Progression/Material Resource Bank")]
public class MaterialResourceBank : ScriptableObject
{
    [SerializeField, Min(0)] private int materialAmount;
    public int MaterialAmount => materialAmount;
    public System.Action<int> AmountChanged;

    public void Add(int amount) => SetAmount(materialAmount + amount);
    public bool CanAfford(int amount) => amount >= 0 && materialAmount >= amount;

    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount)) return false;
        SetAmount(materialAmount - amount);
        return true;
    }

    public void SetAmount(int amount)
    {
        materialAmount = Mathf.Max(0, amount);
        AmountChanged?.Invoke(materialAmount);
    }

    public void ResetAmount() => SetAmount(0);
}
