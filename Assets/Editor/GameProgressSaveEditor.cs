#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class GameProgressSaveEditor
{
    [MenuItem("Tools/Game Progress/Advance To Next Day")]
    private static void AdvanceToNextDay()
    {
        if (Application.isPlaying && GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.AdvanceToNextDayForDebug();
            return;
        }

        GameProgressSave save = Resources.Load<GameProgressSave>("GameProgressSave");
        if (save == null) { Debug.LogError("GameProgressSave asset could not be found."); return; }
        save.Load();
        save.Data.debugDayOffset++;
        save.Data.questDate = string.Empty;
        save.Commit();
        Debug.Log($"Quest clock will advance by one day on the next Play: offset is now {save.Data.debugDayOffset} day(s).");
    }
    [MenuItem("Tools/Game Progress/Reset Save File")]
    private static void ResetSave()
    {
        if (Application.isPlaying && GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.ResetAllProgressForDebug();
            return;
        }

        GameProgressSave save = Resources.Load<GameProgressSave>("GameProgressSave");
        if (save == null) { Debug.LogError("GameProgressSave asset could not be found."); return; }
        if (!EditorUtility.DisplayDialog("Reset game save?", "This permanently deletes the current local progression save.", "Reset", "Cancel")) return;
        save.Delete();
        Debug.Log($"Game progression save reset: {save.FilePath}");
    }
}
#endif
