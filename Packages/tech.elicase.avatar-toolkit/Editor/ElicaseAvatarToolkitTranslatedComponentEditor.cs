using System.Collections.Generic;
using Tech.Elicase.UITheme.Editor;

namespace BlendShapeSearch
{
    internal sealed class ElicaseAvatarToolkitTranslatedComponentEditor : IElicaseEditorWindowExtension
    {
        private static readonly IReadOnlyList<ElicaseEditorWindowKind> InspectorWindow =
            new[] { ElicaseEditorWindowKind.Inspector };

        public string Id => "tech.elicase.avatar-toolkit.component-translation";
        public string DisplayName => "Elicase Avatar Toolkit Component Translation";
        public IReadOnlyList<ElicaseEditorWindowKind> TargetWindows => InspectorWindow;

        public void OnAttach(ElicaseEditorWindowContext context)
        {
            Refresh(context);
        }

        public void OnDetach(ElicaseEditorWindowContext context)
        {
            ElicaseAvatarToolkitInspectorTitleTranslator.RestoreAll();
        }

        public void OnThemeChanged(ElicaseEditorWindowContext context)
        {
            Refresh(context);
        }

        private static void Refresh(ElicaseEditorWindowContext context)
        {
            if (ElicaseAvatarToolkitComponentSettings.IsComponentTranslationEnabled)
            {
                ElicaseAvatarToolkitInspectorTitleTranslator.ApplyToInspector(context.RootVisualElement);
            }
        }
    }
}
