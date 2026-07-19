using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BlendShapeSearch
{
    internal static class ElicaseAvatarToolkitInspectorTitleTranslator
    {
        private static readonly Dictionary<object, TrackedDropdownTitle> trackedDropdownTitles =
            new Dictionary<object, TrackedDropdownTitle>();

        private static readonly Type AdvancedDropdownWindowType =
            Type.GetType("UnityEditor.IMGUI.Controls.AdvancedDropdownWindow, UnityEditor");

        private static readonly FieldInfo AdvancedDropdownTreeField = FindField(
            AdvancedDropdownWindowType,
            "m_CurrentlyRenderedTree");

        private static bool dropdownCompatibilityWarningLogged;

        internal static void ApplyToAddComponentDropdown(EditorWindow window)
        {
            if (!IsNativeAddComponentDropdown(window) || AdvancedDropdownTreeField == null)
            {
                return;
            }

            try
            {
                var tree = AdvancedDropdownTreeField.GetValue(window);
                if (tree != null && ApplyToDropdownTree(tree))
                {
                    window.Repaint();
                }
            }
            catch (Exception exception)
            {
                LogDropdownCompatibilityWarning(exception);
            }
        }

        internal static bool IsNativeAddComponentDropdown(EditorWindow window)
        {
            return window != null
                   && AdvancedDropdownWindowType != null
                   && AdvancedDropdownWindowType.IsInstanceOfType(window);
        }

        internal static void RestoreDropdownTitles()
        {
            foreach (var pair in trackedDropdownTitles)
            {
                pair.Value.Restore(pair.Key);
            }

            trackedDropdownTitles.Clear();
        }

        private static bool ApplyToDropdownTree(object item)
        {
            if (item == null)
            {
                return false;
            }

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

            var searchableNameField = FindField(item.GetType(), "m_SearchableName");
            var localizedNameField = FindField(item.GetType(), "m_LocalizedName");
            var content = FindField(item.GetType(), "m_Content")?.GetValue(item) as GUIContent;
            if (searchableNameField == null || localizedNameField == null || content == null)
            {
                LogDropdownCompatibilityWarning(null);
                return false;
            }

            var wasTracked = trackedDropdownTitles.TryGetValue(item, out var tracked);
            if (!wasTracked)
            {
                var rawName = searchableNameField.GetValue(item) as string;
                if (string.IsNullOrEmpty(rawName))
                {
                    return false;
                }

                tracked = new TrackedDropdownTitle(
                    searchableNameField,
                    rawName,
                    localizedNameField,
                    localizedNameField.GetValue(item) as string,
                    content,
                    content.text);
                trackedDropdownTitles.Add(item, tracked);
            }

            if (!BlendShapeSearchLocalization.TryGetComponentDisplayName(tracked.RawSearchableName, out var translated))
            {
                if (wasTracked)
                {
                    tracked.Restore(item);
                    trackedDropdownTitles.Remove(item);
                    return true;
                }

                return false;
            }

            return tracked.Apply(item, translated);
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

        private static void LogDropdownCompatibilityWarning(Exception exception)
        {
            if (dropdownCompatibilityWarningLogged)
            {
                return;
            }

            dropdownCompatibilityWarningLogged = true;
            var message = "Elicase Avatar Toolkit could not translate Unity's native Add Component menu. "
                + "Unity's original menu remains available.";
            Debug.LogWarning(exception == null ? message : message + " " + exception.Message);
        }

        private sealed class TrackedDropdownTitle
        {
            private readonly FieldInfo searchableNameField;
            private readonly FieldInfo localizedNameField;
            private readonly GUIContent content;
            private readonly string originalLocalizedName;
            private readonly string originalContentText;

            internal TrackedDropdownTitle(
                FieldInfo searchableNameField,
                string rawSearchableName,
                FieldInfo localizedNameField,
                string originalLocalizedName,
                GUIContent content,
                string originalContentText)
            {
                this.searchableNameField = searchableNameField;
                RawSearchableName = rawSearchableName;
                this.localizedNameField = localizedNameField;
                this.originalLocalizedName = originalLocalizedName;
                this.content = content;
                this.originalContentText = originalContentText;
            }

            internal string RawSearchableName { get; }

            internal bool Apply(object item, string translated)
            {
                var searchableName = RawSearchableName + " " + translated;
                var changed = searchableNameField.GetValue(item) as string != searchableName
                    || localizedNameField.GetValue(item) as string != translated
                    || content.text != translated;
                if (!changed)
                {
                    return false;
                }

                searchableNameField.SetValue(item, searchableName);
                localizedNameField.SetValue(item, translated);
                content.text = translated;
                return true;
            }

            internal void Restore(object item)
            {
                searchableNameField.SetValue(item, RawSearchableName);
                localizedNameField.SetValue(item, originalLocalizedName);
                content.text = originalContentText;
            }
        }

    }
}
