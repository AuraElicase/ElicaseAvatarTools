using Tech.Elicase.UITheme.Editor;
using UnityEngine;

namespace BlendShapeSearch
{
    internal sealed class ElicaseAvatarToolkitComponentTitleProvider :
        IElicaseInspectorComponentTitleProvider,
        IElicaseInspectorTitleTextProvider
    {
        public string Id => "tech.elicase.avatar-toolkit.component-title-translation";

        public bool TryGetTitle(Component component, out string title)
        {
            title = null;
            if (!ElicaseAvatarToolkitComponentSettings.IsComponentTranslationEnabled || component == null)
            {
                return false;
            }

            var sourceTitle = ElicaseAvatarToolkitComponentTitles.GetTitle(component.GetType());
            return BlendShapeSearchLocalization.TryGetComponentDisplayName(sourceTitle, out title);
        }

        public bool TryGetTitle(string sourceTitle, out string title)
        {
            title = null;
            return ElicaseAvatarToolkitComponentSettings.IsComponentTranslationEnabled
                   && BlendShapeSearchLocalization.TryGetComponentDisplayName(sourceTitle, out title);
        }
    }
}
