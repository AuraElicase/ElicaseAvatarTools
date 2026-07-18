using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Tech.Elicase.UITheme.Editor;

namespace BlendShapeSearch
{
    [CustomEditor(typeof(SkinnedMeshRenderer))]
    [CanEditMultipleObjects]
    public sealed class SkinnedMeshRendererBlendShapeSearchEditor : Editor
    {
        private static readonly Type BuiltInEditorType = ResolveBuiltInEditorType();
        private const string BlendShapeWeightsPropertyPath = "m_BlendShapeWeights";
        private const string MeshPropertyPath = "m_Mesh";
        private const string SliderClassName = "blend-shape-search-slider";
        private const string EmptyStateName = "blend-shape-search-empty-state";

        private readonly HashSet<int> selectedBlendShapeIndices = new HashSet<int>();
        private Editor builtInEditor;
        private VisualElement root;
        private VisualElement blendShapeList;
        private Button exportButton;
        private Mesh selectedMesh;
        private string searchText = string.Empty;

        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();
            ElicaseThemeManager.Apply(root);
            BuildInspector();
            root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                Undo.undoRedoPerformed += RefreshBlendShapeList;
                BlendShapeSearchLocalization.LanguageChanged += RebuildInspector;
                ElicaseAvatarToolkitComponentSettings.Changed += RebuildInspector;
            });
            root.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                Undo.undoRedoPerformed -= RefreshBlendShapeList;
                BlendShapeSearchLocalization.LanguageChanged -= RebuildInspector;
                ElicaseAvatarToolkitComponentSettings.Changed -= RebuildInspector;
            });
            return root;
        }

        private void OnDisable()
        {
            DestroyBuiltInEditor();
        }

        private void RebuildInspector()
        {
            if (root != null)
            {
                BuildInspector();
            }
        }

        private void BuildInspector()
        {
            root.Unbind();
            root.Clear();
            blendShapeList = null;
            exportButton = null;

            if (!ElicaseAvatarToolkitComponentSettings.IsBlendShapeSearchEnabled)
            {
                AddBuiltInInspector(root);
                return;
            }

            DestroyBuiltInEditor();
            AddBlendShapeSection(root);
            AddDefaultProperties(root, false);
            root.Bind(serializedObject);
            TrackBlendShapeDependencies(root);
        }

        private void AddBuiltInInspector(VisualElement container)
        {
            if (BuiltInEditorType != null)
            {
                CreateCachedEditor(targets, BuiltInEditorType, ref builtInEditor);
                container.Add(new IMGUIContainer(() =>
                {
                    if (builtInEditor != null)
                    {
                        builtInEditor.OnInspectorGUI();
                    }
                }));
                return;
            }

            AddDefaultProperties(container, true);
            container.Bind(serializedObject);
        }

        private void DestroyBuiltInEditor()
        {
            if (builtInEditor != null)
            {
                DestroyImmediate(builtInEditor);
                builtInEditor = null;
            }
        }

        private void AddDefaultProperties(VisualElement container, bool includeBlendShapeWeights)
        {
            var property = serializedObject.GetIterator();
            var enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (includeBlendShapeWeights || property.propertyPath != BlendShapeWeightsPropertyPath)
                {
                    container.Add(new PropertyField(property.Copy()));
                }
            }
        }

        private static Type ResolveBuiltInEditorType()
        {
            return Type.GetType("UnityEditor.SkinnedMeshRendererEditor, UnityEditor")
                   ?? Type.GetType("UnityEditor.SkinnedMeshRendererEditor, UnityEditor.CoreModule");
        }

        private void AddBlendShapeSection(VisualElement container)
        {
            var panel = new ElicasePanel();
            var section = new Foldout
            {
                text = Text("ui.blendShapes"),
                value = true
            };

            if (targets.Length != 1)
            {
                section.Add(new HelpBox(Text("ui.singleSelectionOnly"), HelpBoxMessageType.Info));
                panel.Add(section);
                container.Add(panel);
                return;
            }

            var searchField = new ElicaseTextField { tooltip = Text("ui.filterTooltip") };
            searchField.SetValueWithoutNotify(searchText);
            searchField.RegisterValueChangedCallback(change =>
            {
                searchText = change.newValue ?? string.Empty;
                RefreshBlendShapeList();
            });

            blendShapeList = new VisualElement();
            section.Add(searchField);
            section.Add(CreateToolbar());
            section.Add(blendShapeList);
            panel.Add(section);
            container.Add(panel);
            RefreshBlendShapeList();
        }

        private VisualElement CreateToolbar()
        {
            var toolbar = new ElicaseToolbar();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.flexWrap = Wrap.Wrap;
            toolbar.style.marginTop = 2f;
            toolbar.style.marginBottom = 2f;

            toolbar.Add(new ElicaseButton(SelectAllBlendShapes, Text("ui.selectAll")));
            toolbar.Add(new ElicaseButton(ClearBlendShapeSelection, Text("ui.clear")));
            exportButton = new ElicaseButton(ExportSelectedBlendShapes, Text("ui.exportSelected"));
            toolbar.Add(exportButton);
            toolbar.Add(new ElicaseButton(ImportBlendShapeWeights, Text("ui.import")));
            toolbar.Add(new ElicaseButton(ExportBlendShapeText, Text("ui.exportBlendShapeText")));
            UpdateExportButton();
            return toolbar;
        }

        private void TrackBlendShapeDependencies(VisualElement container)
        {
            var meshProperty = serializedObject.FindProperty(MeshPropertyPath);
            if (meshProperty != null)
            {
                container.TrackPropertyValue(meshProperty, _ => RefreshBlendShapeList());
            }
        }

        private void RefreshBlendShapeList()
        {
            if (blendShapeList == null || targets.Length != 1)
            {
                return;
            }

            blendShapeList.Clear();
            var renderer = target as SkinnedMeshRenderer;
            var mesh = renderer != null ? renderer.sharedMesh : null;
            if (mesh == null)
            {
                selectedMesh = null;
                selectedBlendShapeIndices.Clear();
                UpdateExportButton();
                blendShapeList.Add(new HelpBox(Text("ui.noMesh"), HelpBoxMessageType.Info));
                return;
            }

            if (selectedMesh != mesh)
            {
                selectedMesh = mesh;
                selectedBlendShapeIndices.Clear();
            }

            var matchCount = 0;
            for (var index = 0; index < mesh.blendShapeCount; index++)
            {
                var sourceName = mesh.GetBlendShapeName(index);
                var displayName = BlendShapeSearchLocalization.GetBlendShapeDisplayName(sourceName);
                if (!MatchesSearch(sourceName, searchText) && !MatchesSearch(displayName, searchText))
                {
                    continue;
                }

                blendShapeList.Add(CreateBlendShapeRow(renderer, index, sourceName, displayName));
                matchCount++;
            }

            if (matchCount == 0)
            {
                blendShapeList.Add(new HelpBox(Text("ui.noMatches"), HelpBoxMessageType.Info) { name = EmptyStateName });
            }

            UpdateExportButton();
        }

        private static bool MatchesSearch(string name, string query)
        {
            return string.IsNullOrEmpty(query) || name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private VisualElement CreateBlendShapeRow(SkinnedMeshRenderer renderer, int index, string sourceName, string displayName)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            var selectionToggle = new ElicaseToggle { tooltip = Text("ui.selectionTooltip") };
            selectionToggle.SetValueWithoutNotify(selectedBlendShapeIndices.Contains(index));
            selectionToggle.RegisterValueChangedCallback(change =>
            {
                if (change.newValue)
                {
                    selectedBlendShapeIndices.Add(index);
                }
                else
                {
                    selectedBlendShapeIndices.Remove(index);
                }

                UpdateExportButton();
            });

            var slider = new Slider(displayName, 0f, 100f)
            {
                showInputField = true,
                tooltip = displayName == sourceName ? string.Empty : sourceName
            };
            slider.AddToClassList(SliderClassName);
            slider.style.flexGrow = 1f;
            if (displayName != sourceName)
            {
                slider.labelElement.tooltip = sourceName;
            }
            slider.SetValueWithoutNotify(renderer.GetBlendShapeWeight(index));
            slider.RegisterValueChangedCallback(change => ApplyBlendShapeWeight(renderer, index, change.newValue));
            row.Add(selectionToggle);
            row.Add(slider);
            return row;
        }

        private void SelectAllBlendShapes()
        {
            var mesh = (target as SkinnedMeshRenderer)?.sharedMesh;
            if (mesh == null)
            {
                return;
            }

            selectedMesh = mesh;
            selectedBlendShapeIndices.Clear();
            for (var index = 0; index < mesh.blendShapeCount; index++)
            {
                selectedBlendShapeIndices.Add(index);
            }

            RefreshBlendShapeList();
        }

        private void ClearBlendShapeSelection()
        {
            selectedBlendShapeIndices.Clear();
            RefreshBlendShapeList();
        }

        private void UpdateExportButton()
        {
            if (exportButton != null)
            {
                exportButton.SetEnabled(selectedBlendShapeIndices.Count > 0);
            }
        }

        private void ExportSelectedBlendShapes()
        {
            var renderer = target as SkinnedMeshRenderer;
            var mesh = renderer != null ? renderer.sharedMesh : null;
            if (mesh == null || selectedBlendShapeIndices.Count == 0)
            {
                return;
            }

            var weights = new List<KeyValuePair<string, float>>();
            for (var index = 0; index < mesh.blendShapeCount; index++)
            {
                if (selectedBlendShapeIndices.Contains(index))
                {
                    weights.Add(new KeyValuePair<string, float>(mesh.GetBlendShapeName(index), renderer.GetBlendShapeWeight(index)));
                }
            }

            ExportYaml(BlendShapeSearchPaths.CreateOutputPath(renderer.name + "-blend-shapes"), FlatYaml.SerializeFloats(weights),
                Text("dialog.exportWeightsFailed"));
        }

        private void ExportBlendShapeText()
        {
            var mesh = (target as SkinnedMeshRenderer)?.sharedMesh;
            if (mesh == null)
            {
                return;
            }

            var names = Enumerable.Range(0, mesh.blendShapeCount)
                .Select(index => mesh.GetBlendShapeName(index))
                .Select(name => new KeyValuePair<string, string>(name, name));
            ExportYaml(BlendShapeSearchPaths.CreateOutputPath(mesh.name + "-blend-shape-text", ".blendshapes.lang"), FlatYaml.SerializeStrings(names),
                Text("dialog.exportTextFailed"));
        }

        private static void ExportYaml(string path, string yaml, string errorTitle)
        {
            try
            {
                File.WriteAllText(path, yaml, new System.Text.UTF8Encoding(false));
                BlendShapeSearchPaths.RevealAsset(path);
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog(errorTitle, exception.Message, Text("ui.ok"));
            }
        }

        private void ImportBlendShapeWeights()
        {
            var renderer = target as SkinnedMeshRenderer;
            var mesh = renderer != null ? renderer.sharedMesh : null;
            if (mesh == null)
            {
                return;
            }

            var path = EditorUtility.OpenFilePanel(Text("dialog.importWeights"), BlendShapeSearchPaths.OutputsAbsolutePath, "yml");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Dictionary<string, string> importedYaml;
            try
            {
                importedYaml = FlatYaml.Parse(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog(Text("dialog.importWeightsFailed"), exception.Message, Text("ui.ok"));
                return;
            }

            var importedWeights = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (var entry in importedYaml)
            {
                if (FlatYaml.TryParseFloat(entry.Value, out var weight))
                {
                    importedWeights[entry.Key] = Mathf.Clamp(weight, 0f, 100f);
                }
            }

            var changes = new List<KeyValuePair<int, float>>();
            for (var index = 0; index < mesh.blendShapeCount; index++)
            {
                if (importedWeights.TryGetValue(mesh.GetBlendShapeName(index), out var weight)
                    && !Mathf.Approximately(renderer.GetBlendShapeWeight(index), weight))
                {
                    changes.Add(new KeyValuePair<int, float>(index, weight));
                }
            }

            if (changes.Count == 0)
            {
                EditorUtility.DisplayDialog(Text("dialog.importWeights"), Text("dialog.noWeightChanges"), Text("ui.ok"));
                return;
            }

            Undo.RecordObject(renderer, Text("undo.importWeights"));
            foreach (var change in changes)
            {
                renderer.SetBlendShapeWeight(change.Key, change.Value);
            }

            MarkRendererDirty(renderer);
            RefreshBlendShapeList();
        }

        private static void ApplyBlendShapeWeight(SkinnedMeshRenderer renderer, int index, float weight)
        {
            if (renderer == null)
            {
                return;
            }

            Undo.RecordObject(renderer, Text("undo.changeWeight"));
            renderer.SetBlendShapeWeight(index, weight);
            MarkRendererDirty(renderer);
        }

        private static void MarkRendererDirty(SkinnedMeshRenderer renderer)
        {
            EditorUtility.SetDirty(renderer);
            if (!EditorApplication.isPlaying && renderer.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
            }
        }

        private static string Text(string key)
        {
            return BlendShapeSearchLocalization.Text(key);
        }
    }
}
