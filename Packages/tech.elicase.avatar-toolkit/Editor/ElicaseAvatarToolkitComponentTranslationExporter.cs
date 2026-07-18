using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BlendShapeSearch
{
    internal static class ElicaseAvatarToolkitComponentTranslationExporter
    {
        [MenuItem("Tools/ElicaseAvatarToolkit/导出组件翻译原文")]
        private static void Export()
        {
            var names = ElicaseAvatarToolkitComponentTitles.GetTitles(TypeCache.GetTypesDerivedFrom<Component>());
            var entries = names.Select(name => new KeyValuePair<string, string>(name, name));
            var path = BlendShapeSearchPaths.CreateOutputPath("component-titles", ".components.lang");

            try
            {
                File.WriteAllText(path, FlatYaml.SerializeStrings(entries), new UTF8Encoding(false));
                BlendShapeSearchPaths.RevealAsset(path);
                EditorUtility.DisplayDialog(Text("dialog.exportComponents"), Text("dialog.exportComponentsSucceeded"), Text("ui.ok"));
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog(Text("dialog.exportComponentsFailed"), exception.Message, Text("ui.ok"));
            }
        }

        private static string Text(string key)
        {
            return BlendShapeSearchLocalization.Text(key);
        }
    }

    internal static class ElicaseAvatarToolkitComponentTitles
    {
        internal static IReadOnlyList<string> GetTitles(IEnumerable<Type> componentTypes)
        {
            return componentTypes
                .Where(IsSupportedComponentType)
                .Select(GetTitle)
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(title => title, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool IsSupportedComponentType(Type type)
        {
            return type != null
                   && typeof(Component).IsAssignableFrom(type)
                   && !type.IsAbstract
                   && !type.ContainsGenericParameters
                   && (type.IsPublic || type.IsNestedPublic);
        }

        private static string GetTitle(Type type)
        {
            var menu = Attribute.GetCustomAttribute(type, typeof(AddComponentMenu), true) as AddComponentMenu;
            var menuPath = menu != null ? menu.componentMenu : string.Empty;
            if (!string.IsNullOrWhiteSpace(menuPath))
            {
                var separator = menuPath.LastIndexOf('/');
                return separator >= 0 ? menuPath.Substring(separator + 1) : menuPath;
            }

            return ObjectNames.NicifyVariableName(type.Name);
        }
    }
}
