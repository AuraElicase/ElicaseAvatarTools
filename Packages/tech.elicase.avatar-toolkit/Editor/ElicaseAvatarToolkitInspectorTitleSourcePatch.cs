using Tech.Elicase.UITheme.Editor;
using UnityEditor;
using UnityEngine;

namespace BlendShapeSearch
{
    [InitializeOnLoad]
    internal static class ElicaseAvatarToolkitInspectorTitleSourcePatch
    {
        private const double DropdownRefreshIntervalSeconds = 0.25d;

        private static readonly ElicaseAvatarToolkitTranslatedComponentEditor inspectorExtension =
            new ElicaseAvatarToolkitTranslatedComponentEditor();
        private static readonly ElicaseAvatarToolkitComponentTitleProvider componentTitleProvider =
            new ElicaseAvatarToolkitComponentTitleProvider();
        private static double nextDropdownRefreshTime;

        static ElicaseAvatarToolkitInspectorTitleSourcePatch()
        {
            ElicaseEditorWindowObservers.Register(inspectorExtension);
            ElicaseInspectorComponentTitleProviders.Register(componentTitleProvider);
            EditorApplication.projectChanged += BlendShapeSearchLocalization.ReloadCurrentLanguage;
            EditorApplication.update += RefreshNativeAddComponentDropdown;
            BlendShapeSearchLocalization.LanguageChanged += RefreshNow;
            ElicaseAvatarToolkitComponentSettings.Changed += RefreshNow;
        }

        private static void RefreshNow()
        {
            ElicaseAvatarToolkitInspectorTitleTranslator.RestoreDropdownTitles();
            ElicaseInspectorComponentTitleProviders.RequestRefresh();
            ElicaseEditorWindowObservers.RequestRefresh();
        }

        private static void RefreshNativeAddComponentDropdown()
        {
            if (!ElicaseAvatarToolkitComponentSettings.IsComponentTranslationEnabled)
            {
                ElicaseAvatarToolkitInspectorTitleTranslator.RestoreDropdownTitles();
                return;
            }

            if (EditorApplication.timeSinceStartup < nextDropdownRefreshTime)
            {
                return;
            }

            nextDropdownRefreshTime = EditorApplication.timeSinceStartup + DropdownRefreshIntervalSeconds;
            var hasNativeDropdown = false;
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                hasNativeDropdown |= ElicaseAvatarToolkitInspectorTitleTranslator.IsNativeAddComponentDropdown(window);
                ElicaseAvatarToolkitInspectorTitleTranslator.ApplyToAddComponentDropdown(window);
            }

            if (!hasNativeDropdown)
            {
                ElicaseAvatarToolkitInspectorTitleTranslator.RestoreDropdownTitles();
            }
        }

    }
}
