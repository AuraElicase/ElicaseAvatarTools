using UnityEditor;
using UnityEngine.UIElements;

namespace BlendShapeSearch
{
    internal static class ElicaseAvatarToolkitGameObjectInspectorEditor
    {
        internal static bool IsInspectorWindow(EditorWindow window)
        {
            return window != null && window.GetType().FullName == "UnityEditor.InspectorWindow";
        }

        internal static bool IsAddComponentWindow(EditorWindow window)
        {
            var typeName = window == null ? string.Empty : window.GetType().FullName;
            return !string.IsNullOrEmpty(typeName)
                   && typeName.IndexOf("AddComponent", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsComponentHeaderTitle(TextElement element)
        {
            for (var current = element.parent; current != null; current = current.parent)
            {
                if (current.ClassListContains("unity-inspector-element__header")
                    || current.ClassListContains("unity-inspector-element__header-title"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
