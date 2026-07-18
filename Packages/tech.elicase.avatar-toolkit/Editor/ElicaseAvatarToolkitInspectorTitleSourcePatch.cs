using Tech.Elicase.UITheme.Editor;
using UnityEditor;

namespace BlendShapeSearch
{
    [InitializeOnLoad]
    internal static class ElicaseAvatarToolkitInspectorTitleSourcePatch
    {
        private static readonly ElicaseAvatarToolkitTranslatedComponentEditor inspectorExtension =
            new ElicaseAvatarToolkitTranslatedComponentEditor();

        static ElicaseAvatarToolkitInspectorTitleSourcePatch()
        {
            ElicaseEditorWindowObservers.Register(inspectorExtension);
            EditorApplication.projectChanged += BlendShapeSearchLocalization.ReloadCurrentLanguage;
            BlendShapeSearchLocalization.LanguageChanged += RefreshNow;
            ElicaseAvatarToolkitComponentSettings.Changed += RefreshNow;
        }

        private static void RefreshNow()
        {
            ElicaseEditorWindowObservers.RequestRefresh();
        }
    }
}
