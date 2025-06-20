using BoneLib;
using BoneLib.BoneMenu;

using MelonLoader;

using System;

using UnityEngine;

namespace Portals;

public static class PortalPreferences
{
    public static bool RenderView { get; private set; }

    private static float _renderScale;
    public static float RenderScale
    {
        get
        {
            return _renderScale;
        }
        set
        {
            _renderScale = value;

            OnRenderScaleChanged?.Invoke(value);
        }
    }

    public static bool LimitDistance { get; private set; }

    private static int _renderDistance;
    public static int RenderDistance
    {
        get
        {
            return _renderDistance;
        }
        set
        {
            _renderDistance = value;
        }
    }

    public static int MaxRecursion { get; private set; }

    public static bool DisableInFusion { get; private set; } = false;

    public static event Action<float> OnRenderScaleChanged;

    private static MelonPreferences_Category _preferencesCategory = null;

    private static MelonPreferences_Entry<bool> _renderViewPreference = null;
    private static MelonPreferences_Entry<float> _renderScalePreference = null;

    private static MelonPreferences_Entry<bool> _limitDistancePreference = null;
    private static MelonPreferences_Entry<int> _renderDistancePreference = null;

    private static MelonPreferences_Entry<int> _maxRecursionPreference = null;

    private static MelonPreferences_Entry<bool> _disableInFusionPreference = null;

    private static Page _portalsPage = null;

    private static BoolElement _renderViewElement = null;
    private static FloatElement _renderScaleElement = null;

    private static BoolElement _limitDistanceElement = null;
    private static IntElement _renderDistanceElement = null;

    private static IntElement _maxRecursionElement = null;

    private static BoolElement _disableInFusionElement = null;

    private static bool _preferencesSetup = false;

    public static void SetupPreferences()
    {
        var defaultRenderView = true;
        var defaultRenderScale = 1f;

        var defaultLimitDistance = true;
        var defaultRenderDistance = 25;

        var defaultMaxRecursion = 1;

        bool defaultDisableInFusion = false;

        if (HelperMethods.IsAndroid())
        {
            defaultRenderDistance = 10;
        }

        _preferencesCategory = MelonPreferences.CreateCategory("Portals");

        _renderViewPreference = _preferencesCategory.CreateEntry("Render View", defaultRenderView);
        _renderScalePreference = _preferencesCategory.CreateEntry("Render Scale", defaultRenderScale);

        _limitDistancePreference = _preferencesCategory.CreateEntry("Limit Distance", defaultLimitDistance);
        _renderDistancePreference = _preferencesCategory.CreateEntry("Render Distance", defaultRenderDistance);

        _maxRecursionPreference = _preferencesCategory.CreateEntry("Max Recursion", defaultMaxRecursion);

        if (PortalsMod.HasFusion)
        {
            _disableInFusionPreference = _preferencesCategory.CreateEntry("Disable In Fusion", defaultDisableInFusion);
        }

        _preferencesSetup = true;

        SetupBoneMenu();

        LoadPreferences();
    }

    public static void LoadPreferences()
    {
        if (!_preferencesSetup)
        {
            return;
        }

        RenderView = _renderViewPreference.Value;
        RenderScale = _renderScalePreference.Value;
        LimitDistance = _limitDistancePreference.Value;
        RenderDistance = _renderDistancePreference.Value;
        MaxRecursion = _maxRecursionPreference.Value;

        _renderViewElement.Value = RenderView;
        _renderScaleElement.Value = RenderScale;
        _limitDistanceElement.Value = LimitDistance;
        _renderDistanceElement.Value = RenderDistance;
        _maxRecursionElement.Value = MaxRecursion;

        if (PortalsMod.HasFusion)
        {
            DisableInFusion = _disableInFusionPreference.Value;
            _disableInFusionElement.Value = DisableInFusion;
        }
    }

    private static void SetupBoneMenu()
    {
        _portalsPage = Page.Root.CreatePage("Portals", Color.cyan);

        _renderViewElement = _portalsPage.CreateBool("Render View", Color.yellow, _renderViewPreference.Value, OnSetRenderView);
        _renderScaleElement = _portalsPage.CreateFloat("Render Scale", Color.yellow, _renderScalePreference.Value, 0.1f, 0.1f, 1f, OnSetRenderScale);

        _limitDistanceElement = _portalsPage.CreateBool("Limit Distance", Color.yellow, _limitDistancePreference.Value, OnSetLimitDistance);
        _renderDistanceElement = _portalsPage.CreateInt("Render Distance", Color.yellow, _renderDistancePreference.Value, 1, 1, 100, OnSetRenderDistance);

        _maxRecursionElement = _portalsPage.CreateInt("Max Recursion", Color.yellow, _maxRecursionPreference.Value, 1, 1, 8, OnSetMaxRecursion);

        if (PortalsMod.HasFusion)
        {
            _disableInFusionElement = _portalsPage.CreateBool("Disable In Fusion", Color.red, _disableInFusionPreference.Value, OnSetDisableInFusion);
        }
    }

    private static void OnSetRenderView(bool value)
    {
        RenderView = value;
        _renderViewPreference.Value = value;

        _preferencesCategory.SaveToFile(false);
    }

    private static void OnSetRenderScale(float value)
    {
        RenderScale = value;
        _renderScalePreference.Value = value;

        _preferencesCategory.SaveToFile(false);
    }

    private static void OnSetLimitDistance(bool value)
    {
        LimitDistance = value;
        _limitDistancePreference.Value = value;

        _preferencesCategory.SaveToFile(false);
    }

    private static void OnSetRenderDistance(int value)
    {
        RenderDistance = value;
        _renderDistancePreference.Value = value;

        _preferencesCategory.SaveToFile(false);
    }

    private static void OnSetMaxRecursion(int value)
    {
        MaxRecursion = value;
        _maxRecursionPreference.Value = value;

        _preferencesCategory.SaveToFile(false);
    }

    private static void OnSetDisableInFusion(bool value)
    {
        DisableInFusion = value;
        _disableInFusionPreference.Value = value;

        _preferencesCategory.SaveToFile(false);
    }
}
