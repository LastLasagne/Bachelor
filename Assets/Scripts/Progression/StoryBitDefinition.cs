using UnityEngine;

[CreateAssetMenu(fileName = "StoryBit", menuName = "Game/Story/Story Bit")]
public class StoryBitDefinition : ScriptableObject
{
    [SerializeField] private Sprite icon;
    [SerializeField, TextArea(4, 12)] private string storyText;

    public Sprite Icon => icon;
    public string StoryText => storyText;
}