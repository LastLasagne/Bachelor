using System;
using System.Collections.Generic;
using UnityEngine;

public static class HubMenuState
{
    private static readonly HashSet<int> openSources = new HashSet<int>();

    public static event Action<bool> OpenStateChanged;

    public static bool IsAnyHubMenuOpen => openSources.Count > 0;

    public static void RegisterOpen(UnityEngine.Object source)
    {
        if (source == null)
        {
            return;
        }

        bool wasOpen = IsAnyHubMenuOpen;
        openSources.Add(source.GetInstanceID());

        if (wasOpen != IsAnyHubMenuOpen)
        {
            OpenStateChanged?.Invoke(IsAnyHubMenuOpen);
        }
    }

    public static void RegisterClosed(UnityEngine.Object source)
    {
        if (source == null)
        {
            return;
        }

        bool wasOpen = IsAnyHubMenuOpen;
        openSources.Remove(source.GetInstanceID());

        if (wasOpen != IsAnyHubMenuOpen)
        {
            OpenStateChanged?.Invoke(IsAnyHubMenuOpen);
        }
    }

    public static void ClearAll()
    {
        bool wasOpen = IsAnyHubMenuOpen;
        openSources.Clear();

        if (wasOpen)
        {
            OpenStateChanged?.Invoke(false);
        }
    }
}