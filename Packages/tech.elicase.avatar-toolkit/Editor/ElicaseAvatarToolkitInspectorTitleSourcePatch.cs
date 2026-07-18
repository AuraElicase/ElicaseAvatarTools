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
            Editor.finishedDefaultHeaderGUI += TranslateDefaultHeader;
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

        private static void TranslateDefaultHeader(Editor editor)
        {
            if (!ElicaseAvatarToolkitComponentSettings.IsComponentTranslationEnabled
                || !(editor.target is Component component))
            {
                return;
            }

            var sourceTitle = ObjectNames.NicifyVariableName(component.GetType().Name);
            if (!BlendShapeSearchLocalization.TryGetComponentDisplayName(sourceTitle, out var translatedTitle))
            {
                return;
            }

            var headerRect = GUILayoutUtility.GetLastRect();
            if (headerRect.width <= 80f || headerRect.height <= 0f)
            {
                return;
            }

            var titleRect = new Rect(headerRect.x + 44f, headerRect.y + 2f, headerRect.width - 96f,
                EditorGUIUtility.singleLineHeight);
            var background = EditorGUIUtility.isProSkin
                ? new Color(0.235f, 0.235f, 0.235f)
                : new Color(0.76f, 0.76f, 0.76f);
            EditorGUI.DrawRect(titleRect, background);
            GUI.Label(titleRect, translatedTitle, EditorStyles.boldLabel);
        }
    }
}
