using System;
using System.Collections.Generic;
using Obvious.Soap;
using UnityEngine;

[Serializable]
public class GameProgressSaveData
{
    public int version = 2;
    public int trashProgression;
    public int foodProgression;
    public int galleryProgression;
    public int materialAmount;
    public List<string> purchasedShopItemIds = new List<string>();
    public string questDate;

    // Legacy shared quest state kept so old saves can deserialize safely.
    public string repeatableQuestId;
    public string rareQuestId;
    public bool repeatableCompletedToday;
    public bool rareCompletedToday;
    public int repeatableCompletionCount;
    public int repeatableProgress;
    public int rareProgress;

    public string trashRepeatableQuestId;
    public string trashRareQuestId;
    public string foodRepeatableQuestId;
    public string foodRareQuestId;

    public bool trashRepeatableCompletedToday;
    public bool trashRareCompletedToday;
    public bool foodRepeatableCompletedToday;
    public bool foodRareCompletedToday;

    public int trashRepeatableCompletionCount;
    public int foodRepeatableCompletionCount;

    public int trashRepeatableProgress;
    public int trashRareProgress;
    public int foodRepeatableProgress;
    public int foodRareProgress;

    public int debugDayOffset;
}

[CreateAssetMenu(fileName = "GameProgressSave", menuName = "Game/Save/Game Progress Save")]
public class GameProgressSave : ScriptableSave<GameProgressSaveData>
{
    public GameProgressSaveData Data => _data;
    public void Commit() => Save();
}
