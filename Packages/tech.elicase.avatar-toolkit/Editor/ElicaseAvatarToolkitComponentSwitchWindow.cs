using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using Tech.Elicase.UITheme.Editor;

namespace BlendShapeSearch
{
    internal readonly struct ElicaseAvatarToolkitComponentDefinition
    {
        internal ElicaseAvatarToolkitComponentDefinition(string id, string labelKey, string tooltipKey)
        {
            Id = id;
            LabelKey = labelKey;
            TooltipKey = tooltipKey;
        }

        internal string Id { get; }
        internal string LabelKey { get; }
        internal string TooltipKey { get; }
    }

    internal static class ElicaseAvatarToolkitComponentSettings
    {
        internal const string BlendShapeSearchComponentId = "blend-shape-search";
        internal const string ComponentTranslationComponentId = "component-translation";
        private const string PreferencePrefix = "ElicaseAvatarToolkit.Components.";

        private static readonly ElicaseAvatarToolkitComponentDefinition[] componentDefinitions =
        {
            new ElicaseAvatarToolkitComponentDefinition(
                BlendShapeSearchComponentId,
                "component.blendShapeSearch",
                "component.blendShapeSearchTooltip"),
            new ElicaseAvatarToolkitComponentDefinition(
                ComponentTranslationComponentId,
                "component.translation",
                "component.translationTooltip")
        };

        internal static event Action Changed;

        internal static IReadOnlyList<ElicaseAvatarToolkitComponentDefinition> Components => componentDefinitions;

        internal static bool IsBlendShapeSearchEnabled => IsEnabled(BlendShapeSearchComponentId);
        internal static bool IsComponentTranslationEnabled => IsEnabled(ComponentTranslationComponentId);

        internal static bool IsEnabled(string componentId)
        {
            return EditorPrefs.GetBool(GetPreferenceKey(componentId), true);
        }

        internal static void SetEnabled(string componentId, bool enabled)
        {
            if (IsEnabled(componentId) == enabled)
            {
                return;
            }

            EditorPrefs.SetBool(GetPreferenceKey(componentId), enabled);
            Changed?.Invoke();
        }

        private static string GetPreferenceKey(string componentId)
        {
            return PreferencePrefix + componentId;
        }
    }

    public sealed class ElicaseAvatarToolkitComponentSwitchWindow : EditorWindow
    {
        private const string FollowUnityLanguageValue = "__follow-unity__";

        [MenuItem("Tools/ElicaseAvatarToolkit/设置")]
        public static void Open()
        {
            var window = GetWindow<ElicaseAvatarToolkitComponentSwitchWindow>();
            window.titleContent = new UnityEngine.GUIContent(Text("window.settings"));
            window.minSize = new UnityEngine.Vector2(300f, 120f);
            window.Show();
        }

        private void OnEnable()
        {
            BlendShapeSearchLocalization.LanguageChanged += Rebuild;
            ElicaseAvatarToolkitComponentSettings.Changed += Rebuild;
            Rebuild();
        }

        private void OnDisable()
        {
            BlendShapeSearchLocalization.LanguageChanged -= Rebuild;
            ElicaseAvatarToolkitComponentSettings.Changed -= Rebuild;
        }

        private void Rebuild()
        {
            titleContent = new UnityEngine.GUIContent(Text("window.settings"));
            ElicaseThemeManager.Apply(rootVisualElement);
            rootVisualElement.style.backgroundColor = GetUnityEditorBackgroundColor();
            rootVisualElement.Clear();
            rootVisualElement.style.paddingLeft = 10f;
            rootVisualElement.style.paddingRight = 10f;
            rootVisualElement.style.paddingTop = 8f;
            rootVisualElement.style.paddingBottom = 8f;

            var panel = new ElicasePanel();
            var title = new Label(Text("window.settings"));
            title.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            title.style.marginBottom = 6f;
            panel.Add(title);
            panel.Add(CreateLanguageSelect());

            foreach (var component in ElicaseAvatarToolkitComponentSettings.Components)
            {
                panel.Add(CreateComponentToggle(component));
            }

            rootVisualElement.Add(panel);
        }

        private static UnityEngine.Color GetUnityEditorBackgroundColor()
        {
            return EditorGUIUtility.isProSkin
                ? new UnityEngine.Color32(56, 56, 56, 255)
                : new UnityEngine.Color32(194, 194, 194, 255);
        }

        private static ElicaseSelectField CreateLanguageSelect()
        {
            var languages = new List<string> { FollowUnityLanguageValue };
            languages.AddRange(BlendShapeSearchLocalization.GetLanguages());
            var languageOverride = BlendShapeSearchLocalization.LanguageOverride;
            var selectedIndex = string.IsNullOrEmpty(languageOverride)
                ? 0
                : languages.IndexOf(languageOverride);
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            var select = new ElicaseSelectField(languages, selectedIndex, Text("ui.language"));
            select.formatSelectedValueCallback = FormatLanguageOption;
            select.formatListItemCallback = FormatLanguageOption;
            select.RegisterValueChangedCallback(change =>
            {
                if (change.newValue == FollowUnityLanguageValue)
                {
                    BlendShapeSearchLocalization.FollowUnityLanguage();
                    return;
                }

                BlendShapeSearchLocalization.SetLanguageOverride(change.newValue);
            });
            return select;
        }

        private static string FormatLanguageOption(string language)
        {
            return language == FollowUnityLanguageValue ? Text("ui.followUnityLanguage") : language;
        }

        private static ElicaseToggle CreateComponentToggle(ElicaseAvatarToolkitComponentDefinition component)
        {
            var toggle = new ElicaseToggle(Text(component.LabelKey))
            {
                tooltip = Text(component.TooltipKey),
                value = ElicaseAvatarToolkitComponentSettings.IsEnabled(component.Id)
            };
            toggle.style.marginTop = 4f;
            toggle.RegisterValueChangedCallback(change =>
                ElicaseAvatarToolkitComponentSettings.SetEnabled(component.Id, change.newValue));
            return toggle;
        }

        private static string Text(string key)
        {
            return BlendShapeSearchLocalization.Text(key);
        }
    }
}
