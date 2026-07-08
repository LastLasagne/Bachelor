using System.Collections.Generic;
using UnityEngine;

public class HubMenuMobileControls : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0f;
    [SerializeField] private bool disableControlsWhileHidden = true;

    private readonly List<CanvasGroup> controlGroups = new List<CanvasGroup>();
    private readonly List<MobileJoystick> joysticks = new List<MobileJoystick>();

    private void Awake()
    {
        CacheControls();
        RefreshVisibility(HubMenuState.IsAnyHubMenuOpen);
    }

    private void OnEnable()
    {
        HubMenuState.OpenStateChanged += RefreshVisibility;
        RefreshVisibility(HubMenuState.IsAnyHubMenuOpen);
    }

    private void OnDisable()
    {
        HubMenuState.OpenStateChanged -= RefreshVisibility;
    }

    private void CacheControls()
    {
        controlGroups.Clear();
        joysticks.Clear();

        AddControl(GetComponentInChildren<MobileJoystick>(true)?.gameObject);
        AddControl(GetComponentInChildren<InteractionButton>(true)?.gameObject);

        joysticks.AddRange(GetComponentsInChildren<MobileJoystick>(true));
    }

    private void AddControl(GameObject control)
    {
        if (control == null)
        {
            return;
        }

        CanvasGroup group = control.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = control.AddComponent<CanvasGroup>();
        }

        if (!controlGroups.Contains(group))
        {
            controlGroups.Add(group);
        }
    }

    private void RefreshVisibility(bool hubMenuOpen)
    {
        if (controlGroups.Count == 0)
        {
            CacheControls();
        }

        bool controlsEnabled = !hubMenuOpen;

        foreach (CanvasGroup group in controlGroups)
        {
            if (group == null)
            {
                continue;
            }

            group.alpha = controlsEnabled ? 1f : hiddenAlpha;
            group.interactable = controlsEnabled || !disableControlsWhileHidden;
            group.blocksRaycasts = controlsEnabled || !disableControlsWhileHidden;
        }

        foreach (MobileJoystick joystick in joysticks)
        {
            if (joystick != null)
            {
                joystick.SetInputEnabled(controlsEnabled);
            }
        }
    }
}