using System;
using System.Collections.Generic;
using System.Linq;
using Tech.Elicase.UITheme.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlendShapeSearch
{
    internal sealed class ElicaseAvatarToolkitComponentPickerWindow : EditorWindow
    {
        private const float PickerWidth = 440f;
        private const float PickerHeight = 520f;

        private static ComponentPickerEntry[] entries;

        private readonly List<GameObject> targets = new List<GameObject>();
        private string query = string.Empty;
        private ScrollView results;
        private HelpBox status;

        internal static void Open(VisualElement anchor)
        {
            if (anchor == null)
            {
                return;
            }

            var window = CreateInstance<ElicaseAvatarToolkitComponentPickerWindow>();
            window.titleContent = new GUIContent(BlendShapeSearchLocalization.Text("ui.addComponent"));
            window.RefreshTargets();

            var screenPosition = GUIUtility.GUIToScreenPoint(anchor.worldBound.position);
            window.ShowAsDropDown(new Rect(screenPosition, anchor.worldBound.size), new Vector2(PickerWidth, PickerHeight));
        }

        private void OnEnable()
        {
            Selection.selectionChanged += HandleSelectionChanged;
            BlendShapeSearchLocalization.LanguageChanged += Rebuild;
            ElicaseAvatarToolkitComponentSettings.Changed += HandleComponentSettingsChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= HandleSelectionChanged;
            BlendShapeSearchLocalization.LanguageChanged -= Rebuild;
            ElicaseAvatarToolkitComponentSettings.Changed -= HandleComponentSettingsChanged;
        }

        private void CreateGUI()
        {
            Rebuild();
        }

        private void HandleSelectionChanged()
        {
            RefreshTargets();
            Rebuild();
        }

        private void HandleComponentSettingsChanged()
        {
            if (!ElicaseAvatarToolkitComponentSettings.IsComponentTranslationEnabled)
            {
                Close();
            }
        }

        private void Rebuild()
        {
            if (rootVisualElement == null)
            {
                return;
            }

            titleContent = new GUIContent(BlendShapeSearchLocalization.Text("ui.addComponent"));
            ElicaseThemeManager.Apply(rootVisualElement);
            rootVisualElement.Clear();
            rootVisualElement.style.paddingLeft = 8f;
            rootVisualElement.style.paddingRight = 8f;
            rootVisualElement.style.paddingTop = 8f;
            rootVisualElement.style.paddingBottom = 8f;

            var title = new Label(BlendShapeSearchLocalization.Text("ui.addComponent"));
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            rootVisualElement.Add(title);

            var search = new ElicaseTextField { value = query, tooltip = BlendShapeSearchLocalization.Text("ui.componentSearchTooltip") };
            search.style.marginTop = 6f;
            search.RegisterValueChangedCallback(change =>
            {
                query = change.newValue ?? string.Empty;
                PopulateResults();
            });
            rootVisualElement.Add(search);

            status = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            status.style.marginTop = 6f;
            rootVisualElement.Add(status);

            results = new ScrollView();
            results.style.flexGrow = 1f;
            results.style.marginTop = 6f;
            rootVisualElement.Add(results);
            PopulateResults();
        }

        private void PopulateResults()
        {
            if (results == null || status == null)
            {
                return;
            }

            results.Clear();
            if (targets.Count == 0)
            {
                status.text = BlendShapeSearchLocalization.Text("ui.noGameObjectsSelected");
                status.style.display = DisplayStyle.Flex;
                return;
            }

            status.style.display = DisplayStyle.None;
            var matchingEntries = GetEntries().Where(MatchesQuery).ToArray();
            if (matchingEntries.Length == 0)
            {
                status.text = BlendShapeSearchLocalization.Text("ui.noMatches");
                status.style.display = DisplayStyle.Flex;
                return;
            }

            var previousGroup = string.Empty;
            foreach (var entry in matchingEntries)
            {
                if (entry.Group != previousGroup)
                {
                    previousGroup = entry.Group;
                    var group = new Label(previousGroup);
                    group.style.unityFontStyleAndWeight = FontStyle.Bold;
                    group.style.marginTop = 8f;
                    results.Add(group);
                }

                results.Add(CreateEntryButton(entry));
            }
        }

        private VisualElement CreateEntryButton(ComponentPickerEntry entry)
        {
            var displayName = BlendShapeSearchLocalization.GetComponentDisplayName(entry.RawTitle);
            var button = new ElicaseButton(() => AddComponent(entry), displayName)
            {
                tooltip = entry.MenuPath == entry.RawTitle ? entry.RawTitle : entry.MenuPath + "\n" + entry.RawTitle
            };
            button.style.marginTop = 2f;
            return button;
        }

        private bool MatchesQuery(ComponentPickerEntry entry)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            var translated = BlendShapeSearchLocalization.GetComponentDisplayName(entry.RawTitle);
            return Contains(entry.RawTitle, query)
                || Contains(translated, query)
                || Contains(entry.MenuPath, query);
        }

        private void AddComponent(ComponentPickerEntry entry)
        {
            var failedTargets = new List<string>();
            var succeededCount = 0;
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Add " + entry.RawTitle);

            foreach (var target in targets)
            {
                if (!CanAddComponent(target, entry.Type))
                {
                    failedTargets.Add(target == null ? BlendShapeSearchLocalization.Text("ui.missingObject") : target.name);
                    continue;
                }

                try
                {
                    Undo.AddComponent(target, entry.Type);
                    succeededCount++;
                }
                catch (Exception exception)
                {
                    failedTargets.Add(target.name + " (" + exception.Message + ")");
                }
            }

            if (failedTargets.Count == 0 && succeededCount > 0)
            {
                Close();
                return;
            }

            status.text = BlendShapeSearchLocalization.Text("ui.componentAddPartial") + "\n"
                + string.Join("\n", failedTargets.ToArray());
            status.messageType = HelpBoxMessageType.Warning;
            status.style.display = DisplayStyle.Flex;
        }

        private void RefreshTargets()
        {
            targets.Clear();
            foreach (var gameObject in Selection.gameObjects)
            {
                if (gameObject != null && !EditorUtility.IsPersistent(gameObject))
                {
                    targets.Add(gameObject);
                }
            }
        }

        private static ComponentPickerEntry[] GetEntries()
        {
            if (entries != null)
            {
                return entries;
            }

            entries = TypeCache.GetTypesDerivedFrom<Component>()
                .Where(ElicaseAvatarToolkitComponentTitles.IsSupportedComponentType)
                .Select(type => new ComponentPickerEntry(type))
                .OrderBy(entry => entry.Group, StringComparer.Ordinal)
                .ThenBy(entry => entry.RawTitle, StringComparer.Ordinal)
                .ToArray();
            return entries;
        }

        private static bool CanAddComponent(GameObject target, Type type)
        {
            if (target == null || EditorUtility.IsPersistent(target))
            {
                return false;
            }

            return !Attribute.IsDefined(type, typeof(DisallowMultipleComponent), true)
                   || target.GetComponent(type) == null;
        }

        private static bool Contains(string value, string valueToFind)
        {
            return !string.IsNullOrEmpty(value)
                   && value.IndexOf(valueToFind, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private sealed class ComponentPickerEntry
        {
            internal Type Type { get; }
            internal string RawTitle { get; }
            internal string MenuPath { get; }
            internal string Group { get; }

            internal ComponentPickerEntry(Type type)
            {
                Type = type;
                RawTitle = ElicaseAvatarToolkitComponentTitles.GetTitle(type);
                MenuPath = ElicaseAvatarToolkitComponentTitles.GetMenuPath(type);
                if (string.IsNullOrWhiteSpace(MenuPath))
                {
                    MenuPath = RawTitle;
                    Group = "Scripts";
                    return;
                }

                var separator = MenuPath.IndexOf('/');
                Group = separator < 0 ? MenuPath : MenuPath.Substring(0, separator);
            }
        }
    }
}
