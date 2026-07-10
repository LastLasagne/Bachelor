using System.Collections.Generic;

public interface IQuestRewardMessageProvider
{
    IEnumerable<string> BuildRewardMessages(QuestDefinition quest, int previousProgression, int newProgression);
}