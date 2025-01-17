using BoneLib.BoneMenu;

using MelonLoader;
using System;
using UnityEngine;

namespace Portals;

public static class PortalPreferences
{
    public static bool RenderView { get; private set; }

    private static float _renderScale = 1f;
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

    public static int MaxRecursion { get; private set; }

    public static event Action<float> OnRenderScaleChanged;

    private static MelonPreferences_Category _preferencesCategory = null;
    private static MelonPreferences_Entry<bool> _renderViewPreference = null;
    private static MelonPreferences_Entry<float> _renderScalePreference = null;
    private static MelonPreferences_Entry<int> _maxRecursionPreference = null;

    private static Page _portalsPage = null;
    private static BoolElement _renderViewElement = null;
    private static FloatElement _renderScaleElement = null;
    private static IntElement _maxRecursionElement = null;

    private static bool _preferencesSetup = false;

    public static void SetupPreferences()
    {
        _preferencesCategory = MelonPreferences.CreateCategory("Portals");

        _renderViewPreference = _preferencesCategory.CreateEntry("Render View", true);
        _renderScalePreference = _preferencesCategory.CreateEntry("Render Scale", 1f);
        _maxRecursionPreference = _preferencesCategory.CreateEntry("Max Recursion", 1);

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
        MaxRecursion = _maxRecursionPreference.Value;

        _renderViewElement.Value = RenderView;
        _renderScaleElement.Value = RenderScale;
        _maxRecursionElement.Value = MaxRecursion;
    }

    private static void SetupBoneMenu()
    {
        _portalsPage = Page.Root.CreatePage("Portals", Color.cyan);

        _renderViewElement = _portalsPage.CreateBool("Render View", Color.yellow, _renderViewPreference.Value, OnSetRenderView);
        _renderScaleElement = _portalsPage.CreateFloat("Render Scale", Color.yellow, _renderScalePreference.Value, 0.1f, 0.1f, 1f, OnSetRenderScale);
        _maxRecursionElement = _portalsPage.CreateInt("Max Recursion", Color.yellow, _maxRecursionPreference.Value, 1, 1, 8, OnSetMaxRecursion);
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

    private static void OnSetMaxRecursion(int value)
    {
        MaxRecursion = value;
        _maxRecursionPreference.Value = value;

        _preferencesCategory.SaveToFile(false);
    }
}
