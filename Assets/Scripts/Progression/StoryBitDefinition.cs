using UnityEngine;

[CreateAssetMenu(fileName = "StoryBit", menuName = "Game/Story/Story Bit")]
public class StoryBitDefinition : ScriptableObject
{    [SerializeField, TextArea(4, 12)] private string storyText;    public string StoryText => storyText;
}