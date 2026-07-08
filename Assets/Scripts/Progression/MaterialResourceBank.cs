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

    public void ResetAmount()
    {
        materialAmount = 0;
    }
}