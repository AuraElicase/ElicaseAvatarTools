using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlendShapeSearch
{
    internal static class ElicaseAvatarToolkitInspectorTitleTranslator
    {
        private static readonly Dictionary<TextElement, TrackedTitle> trackedTitles =
            new Dictionary<TextElement, TrackedTitle>();
        private static readonly Dictionary<object, TrackedDropdownTitle> trackedDropdownTitles =
            new Dictionary<object, TrackedDropdownTitle>();

        private static readonly Type AdvancedDropdownWindowType =
            Type.GetType("UnityEditor.IMGUI.Controls.AdvancedDropdownWindow, UnityEditor");

        private static readonly FieldInfo AdvancedDropdownTreeField = FindField(
            AdvancedDropdownWindowType,
            "m_CurrentlyRenderedTree");

        internal static void ApplyToInspector(VisualElement root)
        {
            Apply(root, _ => true);
        }

        internal static void ApplyToAddComponentWindow(VisualElement root)
        {
            Apply(root, _ => true);
        }

        internal static void ApplyToAddComponentDropdown(EditorWindow window)
        {
            if (window == null || AdvancedDropdownWindowType == null || AdvancedDropdownTreeField == null
                || !AdvancedDropdownWindowType.IsInstanceOfType(window))
            {
                return;
            }

            var tree = AdvancedDropdownTreeField.GetValue(window);
            if (tree != null && ApplyToDropdownTree(tree))
            {
                window.Repaint();
            }
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
            foreach (var pair in trackedDropdownTitles)
            {
                pair.Value.Restore(pair.Key);
            }

            trackedDropdownTitles.Clear();
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

        private static bool ApplyToDropdownTree(object item)
        {
            var changed = ApplyToDropdownItem(item);
            var childrenField = FindField(item.GetType(), "m_Children");
            if (!(childrenField?.GetValue(item) is IEnumerable children))
            {
                return changed;
            }

            foreach (var child in children)
            {
                changed |= ApplyToDropdownTree(child);
            }

            return changed;
        }

        private static bool ApplyToDropdownItem(object item)
        {
            if (item.GetType().FullName != "UnityEditor.AddComponent.ComponentDropdownItem")
            {
                return false;
            }

            var rawName = FindField(item.GetType(), "m_SearchableName")?.GetValue(item) as string;
            var localizedNameField = FindField(item.GetType(), "m_LocalizedName");
            var content = FindField(item.GetType(), "m_Content")?.GetValue(item) as GUIContent;
            if (string.IsNullOrEmpty(rawName) || localizedNameField == null || content == null)
            {
                return false;
            }

            if (!BlendShapeSearchLocalization.TryGetComponentDisplayName(rawName, out var translated))
            {
                if (trackedDropdownTitles.TryGetValue(item, out var tracked))
                {
                    tracked.Restore(item);
                    trackedDropdownTitles.Remove(item);
                    return true;
                }

                return false;
            }

            if (!trackedDropdownTitles.TryGetValue(item, out var title))
            {
                title = new TrackedDropdownTitle(
                    localizedNameField,
                    localizedNameField.GetValue(item) as string,
                    content,
                    content.text);
                trackedDropdownTitles.Add(item, title);
            }

            localizedNameField.SetValue(item, translated);
            content.text = translated;
            return true;
        }

        private static FieldInfo FindField(Type type, string name)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                var field = current.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
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

        private sealed class TrackedDropdownTitle
        {
            private readonly FieldInfo localizedNameField;
            private readonly string originalLocalizedName;
            private readonly GUIContent content;
            private readonly string originalContentText;

            internal TrackedDropdownTitle(
                FieldInfo localizedNameField,
                string originalLocalizedName,
                GUIContent content,
                string originalContentText)
            {
                this.localizedNameField = localizedNameField;
                this.originalLocalizedName = originalLocalizedName;
                this.content = content;
                this.originalContentText = originalContentText;
            }

            internal void Restore(object item)
            {
                localizedNameField.SetValue(item, originalLocalizedName);
                content.text = originalContentText;
            }
        }
    }
}
