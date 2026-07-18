using Tech.Elicase.UITheme.Editor;
using UnityEditor;
using UnityEngine;

namespace BlendShapeSearch
{
    [InitializeOnLoad]
    internal static class ElicaseAvatarToolkitInspectorTitleSourcePatch
    {
        private const double RefreshIntervalSeconds = 0.25d;

        private static readonly ElicaseAvatarToolkitTranslatedComponentEditor inspectorExtension =
            new ElicaseAvatarToolkitTranslatedComponentEditor();

        private static double nextRefreshTime;

        static ElicaseAvatarToolkitInspectorTitleSourcePatch()
        {
            ElicaseEditorWindowExtensions.Register(inspectorExtension);
            EditorApplication.update += RefreshVisibleWindows;
            EditorApplication.projectChanged += BlendShapeSearchLocalization.ReloadCurrentLanguage;
            BlendShapeSearchLocalization.LanguageChanged += RefreshNow;
            ElicaseAvatarToolkitComponentSettings.Changed += RefreshNow;
        }

        private static void RefreshVisibleWindows()
        {
            if (EditorApplication.timeSinceStartup < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = EditorApplication.timeSinceStartup + RefreshIntervalSeconds;
            RefreshNow();
        }

        private static void RefreshNow()
        {
            if (!ElicaseAvatarToolkitComponentSettings.IsComponentTranslationEnabled)
            {
                ElicaseAvatarToolkitInspectorTitleTranslator.RestoreAll();
                return;
            }

            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (ElicaseAvatarToolkitGameObjectInspectorEditor.IsInspectorWindow(window))
                {
                    ElicaseAvatarToolkitInspectorTitleTranslator.ApplyToInspector(window.rootVisualElement);
                }
                else
                {
                    ElicaseAvatarToolkitInspectorTitleTranslator.ApplyToAddComponentDropdown(window);
                    ElicaseAvatarToolkitInspectorTitleTranslator.ApplyToAddComponentWindow(window.rootVisualElement);
                }
            }
        }
    }
}
