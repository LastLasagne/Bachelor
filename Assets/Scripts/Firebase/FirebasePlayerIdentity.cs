using System;
using UnityEngine;

public static class FirebasePlayerIdentity
{
    private const string LocalPlayerIdKey = "AnimalXing.Firebase.LocalPlayerId";

    public static string LocalPlayerId
    {
        get
        {
            string localPlayerId = PlayerPrefs.GetString(LocalPlayerIdKey, string.Empty);
            if (string.IsNullOrWhiteSpace(localPlayerId))
            {
                localPlayerId = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(LocalPlayerIdKey, localPlayerId);
                PlayerPrefs.Save();
            }

            return localPlayerId;
        }
    }
}
