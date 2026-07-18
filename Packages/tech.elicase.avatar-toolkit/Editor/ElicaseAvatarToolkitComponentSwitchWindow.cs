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
        private const string PreferencePrefix = "ElicaseAvatarToolkit.Components.";

        private static readonly ElicaseAvatarToolkitComponentDefinition[] componentDefinitions =
        {
            new ElicaseAvatarToolkitComponentDefinition(
                BlendShapeSearchComponentId,
                "component.blendShapeSearch",
                "component.blendShapeSearchTooltip")
        };

        internal static event Action Changed;

        internal static IReadOnlyList<ElicaseAvatarToolkitComponentDefinition> Components => componentDefinitions;

        internal static bool IsBlendShapeSearchEnabled => IsEnabled(BlendShapeSearchComponentId);

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
        [MenuItem("Tools/ElicaseAvatarToolkit/组件开关")]
        public static void Open()
        {
            var window = GetWindow<ElicaseAvatarToolkitComponentSwitchWindow>();
            window.titleContent = new UnityEngine.GUIContent(Text("window.componentSwitch"));
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
            titleContent = new UnityEngine.GUIContent(Text("window.componentSwitch"));
            ElicaseThemeManager.Apply(rootVisualElement);
            rootVisualElement.Clear();
            rootVisualElement.style.paddingLeft = 10f;
            rootVisualElement.style.paddingRight = 10f;
            rootVisualElement.style.paddingTop = 8f;
            rootVisualElement.style.paddingBottom = 8f;

            var panel = new ElicasePanel();
            var title = new Label(Text("window.componentSwitch"));
            title.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            title.style.marginBottom = 6f;
            panel.Add(title);

            foreach (var component in ElicaseAvatarToolkitComponentSettings.Components)
            {
                panel.Add(CreateComponentToggle(component));
            }

            rootVisualElement.Add(panel);
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
