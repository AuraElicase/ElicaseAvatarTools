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

        internal static void Restore(VisualElement root)
        {
            var titlesToRemove = new List<TextElement>();
            foreach (var pair in trackedTitles)
            {
                if (!IsDescendantOf(pair.Key, root))
                {
                    continue;
                }

                if (pair.Key.panel != null && pair.Key.text == pair.Value.AppliedText)
                {
                    pair.Key.text = pair.Value.RawText;
                }

                titlesToRemove.Add(pair.Key);
            }

            foreach (var title in titlesToRemove)
            {
                trackedTitles.Remove(title);
            }
        }

        private static void Apply(VisualElement root, Func<TextElement, bool> isCandidate)
        {
            if (root == null)
            {
                return;
            }

            RemoveDetachedTitles();
            root.Query<TextElement>().ForEach(element => ApplyElement(element, isCandidate));
        }

        private static void RemoveDetachedTitles()
        {
            var titlesToRemove = new List<TextElement>();
            foreach (var pair in trackedTitles)
            {
                if (pair.Key.panel == null)
                {
                    titlesToRemove.Add(pair.Key);
                }
            }

            foreach (var title in titlesToRemove)
            {
                trackedTitles.Remove(title);
            }
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

            if (!BlendShapeSearchLocalization.TryGetComponentDisplayName(tracked.RawText, out var translated))
            {
                if (element.text == tracked.AppliedText)
                {
                    element.text = tracked.RawText;
                }

                trackedTitles.Remove(element);
                return;
            }

            element.text = translated;
            tracked.AppliedText = translated;
        }

        private static bool IsDescendantOf(VisualElement element, VisualElement root)
        {
            if (element == null || root == null)
            {
                return false;
            }

            for (var current = element; current != null; current = current.parent)
            {
                if (current == root)
                {
                    return true;
                }
            }

            return false;
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
