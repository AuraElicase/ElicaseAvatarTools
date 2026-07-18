using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace BlendShapeSearch
{
    internal static class ElicaseAvatarToolkitInspectorTitleTranslator
    {
        private static readonly Dictionary<TextElement, TrackedTitle> trackedTitles =
            new Dictionary<TextElement, TrackedTitle>();

        internal static void ApplyToInspector(VisualElement root)
        {
            Apply(root, ElicaseAvatarToolkitGameObjectInspectorEditor.IsComponentHeaderTitle);
        }

        internal static void ApplyToAddComponentWindow(VisualElement root)
        {
            Apply(root, _ => true);
        }

        internal static void RestoreAll()
        {
            foreach (var pair in trackedTitles)
            {
                if (pair.Key.panel != null && pair.Key.text == pair.Value.AppliedText)
                {
                    pair.Key.text = pair.Value.RawText;
                }
            }

            trackedTitles.Clear();
        }

        private static void Apply(VisualElement root, Func<TextElement, bool> isCandidate)
        {
            if (root == null)
            {
                return;
            }

            root.Query<TextElement>().ForEach(element => ApplyElement(element, isCandidate));
        }

        private static void ApplyElement(TextElement element, Func<TextElement, bool> isCandidate)
        {
            if (!isCandidate(element))
            {
                return;
            }

            TrackedTitle tracked;
            if (!trackedTitles.TryGetValue(element, out tracked))
            {
                tracked = new TrackedTitle(element.text ?? string.Empty);
                trackedTitles.Add(element, tracked);
            }
            else if (element.text != tracked.RawText && element.text != tracked.AppliedText)
            {
                tracked.RawText = element.text ?? string.Empty;
            }

            var translated = BlendShapeSearchLocalization.GetComponentDisplayName(tracked.RawText);
            if (translated == tracked.RawText && element.text == tracked.RawText)
            {
                trackedTitles.Remove(element);
                return;
            }

            element.text = translated;
            tracked.AppliedText = translated;
        }

        private sealed class TrackedTitle
        {
            internal TrackedTitle(string rawText)
            {
                RawText = rawText;
                AppliedText = rawText;
            }

            internal string RawText { get; set; }
            internal string AppliedText { get; set; }
        }
    }
}
