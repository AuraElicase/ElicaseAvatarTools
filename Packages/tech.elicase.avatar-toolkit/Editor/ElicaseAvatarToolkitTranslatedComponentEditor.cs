using System.Collections.Generic;
using Tech.Elicase.UITheme.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlendShapeSearch
{
    internal sealed class ElicaseAvatarToolkitTranslatedComponentEditor : IElicaseEditorWindowObserver
    {
        private const string AddComponentButtonName = "elicase-avatar-toolkit-add-component";

        private static readonly IReadOnlyList<ElicaseEditorWindowKind> InspectorWindow =
            new[] { ElicaseEditorWindowKind.Inspector };

        private readonly Dictionary<VisualElement, ReplacedAddComponentButton> replacedButtons =
            new Dictionary<VisualElement, ReplacedAddComponentButton>();
        private bool addComponentLocatorWarningLogged;

        public string Id => "tech.elicase.avatar-toolkit.component-translation";
        public string DisplayName => "Elicase Avatar Toolkit Component Translation";
        public IReadOnlyList<ElicaseEditorWindowKind> TargetWindows => InspectorWindow;

        public void OnAttach(ElicaseEditorWindowContext context)
        {
            Refresh(context);
        }

        public void OnDetach(ElicaseEditorWindowContext context)
        {
            ElicaseAvatarToolkitInspectorTitleTranslator.Restore(context.RootVisualElement);
            RestoreAddComponentButton(context.RootVisualElement);
        }

        public void OnRefresh(ElicaseEditorWindowContext context)
        {
            Refresh(context);
        }

        private void Refresh(ElicaseEditorWindowContext context)
        {
            if (ElicaseAvatarToolkitComponentSettings.IsComponentTranslationEnabled)
            {
                ElicaseAvatarToolkitInspectorTitleTranslator.ApplyToInspector(context.RootVisualElement);
                ReplaceAddComponentButton(context.RootVisualElement);
                return;
            }

            ElicaseAvatarToolkitInspectorTitleTranslator.Restore(context.RootVisualElement);
            RestoreAddComponentButton(context.RootVisualElement);
        }

        private void ReplaceAddComponentButton(VisualElement root)
        {
            ReplacedAddComponentButton replaced;
            if (replacedButtons.TryGetValue(root, out replaced) && replaced.IsCurrent)
            {
                return;
            }

            RestoreAddComponentButton(root);
            if (UnityEditor.Selection.gameObjects.Length == 0)
            {
                return;
            }

            Button nativeButton;
            if (!ElicaseInspectorElements.TryGetAddComponentButton(root, out nativeButton))
            {
                if (!addComponentLocatorWarningLogged)
                {
                    Debug.LogWarning("Elicase Avatar Toolkit could not locate Unity's UI Toolkit Add Component button. "
                        + "Unity's original button remains available.");
                    addComponentLocatorWarningLogged = true;
                }

                return;
            }

            var parent = nativeButton.parent;
            if (parent == null)
            {
                return;
            }

            var button = new ElicaseButton(
                () => ElicaseAvatarToolkitComponentPickerWindow.Open(nativeButton),
                BlendShapeSearchLocalization.Text("ui.addComponent"))
            {
                name = AddComponentButtonName
            };
            button.style.marginLeft = nativeButton.resolvedStyle.marginLeft;
            button.style.marginRight = nativeButton.resolvedStyle.marginRight;
            button.style.marginTop = nativeButton.resolvedStyle.marginTop;
            button.style.marginBottom = nativeButton.resolvedStyle.marginBottom;

            var index = parent.IndexOf(nativeButton);
            parent.Insert(index, button);
            var originalDisplay = nativeButton.style.display;
            nativeButton.style.display = DisplayStyle.None;
            replacedButtons.Add(root, new ReplacedAddComponentButton(nativeButton, button, originalDisplay));
        }

        private void RestoreAddComponentButton(VisualElement root)
        {
            ReplacedAddComponentButton replaced;
            if (!replacedButtons.TryGetValue(root, out replaced))
            {
                return;
            }

            replaced.Restore();
            replacedButtons.Remove(root);
        }

        private sealed class ReplacedAddComponentButton
        {
            private readonly Button nativeButton;
            private readonly Button replacement;
            private readonly StyleEnum<DisplayStyle> originalDisplay;

            internal ReplacedAddComponentButton(
                Button nativeButton,
                Button replacement,
                StyleEnum<DisplayStyle> originalDisplay)
            {
                this.nativeButton = nativeButton;
                this.replacement = replacement;
                this.originalDisplay = originalDisplay;
            }

            internal bool IsCurrent => nativeButton != null
                && nativeButton.parent != null
                && replacement != null
                && replacement.parent != null;

            internal void Restore()
            {
                if (nativeButton != null)
                {
                    nativeButton.style.display = originalDisplay;
                }

                replacement?.RemoveFromHierarchy();
            }
        }
    }
}
