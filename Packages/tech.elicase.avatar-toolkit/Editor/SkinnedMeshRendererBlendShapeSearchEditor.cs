using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlendShapeSearch
{
    [CustomEditor(typeof(SkinnedMeshRenderer))]
    [CanEditMultipleObjects]
    public sealed class SkinnedMeshRendererBlendShapeSearchEditor : Editor
    {
        private const string BlendShapeWeightsPropertyPath = "m_BlendShapeWeights";
        private const string MeshPropertyPath = "m_Mesh";
        private const string EmptyStateName = "blend-shape-search-empty-state";
        private const string InspectorFoldoutToggleClassName = "unity-foldout__toggle--inspector";
        private const string InspectorPropertyClassName = "unity-property-field__inspector-property";
        private const string InspectorFoldoutClassName = "unity-foldout--depth-1";
        private const float BlendShapeListViewportHeight = 300f;

        private readonly HashSet<int> selectedBlendShapeIndices = new HashSet<int>();
        private readonly List<BlendShapeEntry> blendShapeEntries = new List<BlendShapeEntry>();
        private readonly List<BlendShapeEntry> filteredBlendShapeEntries = new List<BlendShapeEntry>();
        private Editor builtInEditor;
        private VisualElement root;
        private VisualElement blendShapeList;
        private Button exportButton;
        private IMGUIContainer enhancedInspector;
        private Mesh selectedMesh;
        private Mesh cachedBlendShapeMesh;
        private string searchText = string.Empty;
        private Vector2 blendShapeScrollPosition;

        private sealed class BlendShapeEntry
        {
            internal int Index;
            internal string SourceName;
            internal string DisplayName;
            internal float SliderMinimum;
            internal float SliderMaximum;
        }

        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();
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
                cachedBlendShapeMesh = null;
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

            AddEnhancedInspector(root);
        }

        private void AddBuiltInInspector(VisualElement container)
        {
            var builtInEditorType = ResolveBuiltInEditorType();
            if (builtInEditorType != null)
            {
                CreateCachedEditor(targets, builtInEditorType, ref builtInEditor);
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
            return typeof(Editor).Assembly.GetType("UnityEditor.SkinnedMeshRendererEditor")
                   ?? Type.GetType("UnityEditor.SkinnedMeshRendererEditor, UnityEditor")
                   ?? Type.GetType("UnityEditor.SkinnedMeshRendererEditor, UnityEditor.CoreModule")
                   ?? AppDomain.CurrentDomain.GetAssemblies()
                       .Select(assembly => assembly.GetType("UnityEditor.SkinnedMeshRendererEditor"))
                       .FirstOrDefault(type => type != null);
        }

        private void AddBlendShapeSection(VisualElement container)
        {
            var section = new Foldout
            {
                text = Text("ui.blendShapes"),
                value = true
            };
            section.AddToClassList(InspectorPropertyClassName);
            section.AddToClassList(InspectorFoldoutClassName);
            var titleToggle = section.Q<Toggle>();
            titleToggle.AddToClassList(InspectorFoldoutToggleClassName);
            section.contentContainer.style.marginLeft = 0;

            if (targets.Length != 1)
            {
                section.Add(new HelpBox(Text("ui.singleSelectionOnly"), HelpBoxMessageType.Info));
                container.Add(section);
                return;
            }

            var searchField = new TextField { tooltip = Text("ui.filterTooltip") };
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
            container.Add(section);
            RefreshBlendShapeList();
        }

        private void AddEnhancedInspector(VisualElement container)
        {
            var builtInEditorType = ResolveBuiltInEditorType();
            if (builtInEditorType == null)
            {
                AddBlendShapeSection(container);
                AddDefaultProperties(container, false);
                container.Bind(serializedObject);
                TrackBlendShapeDependencies(container);
                return;
            }

            CreateCachedEditor(targets, builtInEditorType, ref builtInEditor);
            enhancedInspector = new IMGUIContainer(DrawEnhancedInspector);
            container.Add(enhancedInspector);
        }

        private void DrawEnhancedInspector()
        {
            var renderer = target as SkinnedMeshRenderer;
            if (builtInEditor == null || renderer == null || targets.Length != 1 || renderer.sharedMesh == null
                || renderer.sharedMesh.blendShapeCount == 0)
            {
                builtInEditor?.OnInspectorGUI();
                return;
            }

            var builtInSerializedObject = builtInEditor.serializedObject;
            builtInSerializedObject.Update();

            DrawEditBoundsButton(renderer);
            var bounds = builtInSerializedObject.FindProperty("m_AABB");
            var dirtyBounds = builtInSerializedObject.FindProperty("m_DirtyAABB");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(bounds, EditorGUIUtility.TrTextContent("Bounds", "The bounding box that encapsulates the mesh."));
            if (EditorGUI.EndChangeCheck() && dirtyBounds != null)
            {
                dirtyBounds.boolValue = false;
            }

            DrawBlendShapeSearch(renderer, builtInSerializedObject.FindProperty(BlendShapeWeightsPropertyPath));
            EditorGUILayout.PropertyField(builtInSerializedObject.FindProperty("m_Quality"), EditorGUIUtility.TrTextContent("Quality", "Number of bones to use per vertex during skinning."));
            EditorGUILayout.PropertyField(builtInSerializedObject.FindProperty("m_UpdateWhenOffscreen"), EditorGUIUtility.TrTextContent("Update When Offscreen", "If an accurate bounding volume representation should be calculated every frame. "));

            InvokeBuiltInMethod("OnMeshUI");
            EditorGUILayout.PropertyField(builtInSerializedObject.FindProperty("m_RootBone"), EditorGUIUtility.TrTextContent("Root Bone", "Transform with which the bounds move, and the space in which skinning is computed."));
            InvokeBuiltInMethod("DrawMaterials");
            InvokeBuiltInMethod("LightingSettingsGUI", false);
            InvokeBuiltInMethod("RayTracingSettingsGUI");
            InvokeBuiltInMethod("OtherSettingsGUI", false, true, false);

            builtInSerializedObject.ApplyModifiedProperties();
        }

        private void DrawEditBoundsButton(SkinnedMeshRenderer renderer)
        {
            var editModeType = FindLoadedEditorType("UnityEditorInternal.EditMode")
                               ?? FindLoadedEditorType("UnityEditor.EditMode");
            var boundsHandleType = FindLoadedEditorType("UnityEditor.IMGUI.Controls.PrimitiveBoundsHandle");
            var sceneViewEditModeType = editModeType?.GetNestedType("SceneViewEditMode", BindingFlags.Public | BindingFlags.NonPublic);
            var buttonProperty = boundsHandleType?.GetProperty("editModeButton", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var buttonField = boundsHandleType?.GetField("editModeButton", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var editModeButton = buttonProperty?.GetValue(null) ?? buttonField?.GetValue(null);
            var methods = editModeType?
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(candidate => candidate.Name == "DoEditModeInspectorModeButton")
                .ToArray();
            if (sceneViewEditModeType == null || editModeButton == null || methods == null)
            {
                return;
            }

            var editMode = Enum.Parse(sceneViewEditModeType, "Collider");
            var fourArgumentMethod = methods.FirstOrDefault(method => method.GetParameters().Length == 4
                                                              && method.GetParameters()[0].ParameterType == sceneViewEditModeType);
            if (fourArgumentMethod != null)
            {
                fourArgumentMethod.Invoke(null, new[] { editMode, "Edit Bounds", editModeButton, builtInEditor });
                return;
            }

            var fiveArgumentMethod = methods.FirstOrDefault(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 5
                       && parameters[0].ParameterType == sceneViewEditModeType
                       && parameters[4].ParameterType.IsInstanceOfType(builtInEditor);
            });
            if (fiveArgumentMethod == null)
            {
                return;
            }

            var boundsParameterType = fiveArgumentMethod.GetParameters()[3].ParameterType;
            object boundsArgument = boundsParameterType == typeof(Bounds)
                ? renderer.bounds
                : new Func<Bounds>(() => renderer.bounds);
            fiveArgumentMethod.Invoke(null, new[] { editMode, "Edit Bounds", editModeButton, boundsArgument, builtInEditor });
        }

        private static Type FindLoadedEditorType(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName))
                .FirstOrDefault(type => type != null);
        }

        private void InvokeBuiltInMethod(string methodName, params object[] arguments)
        {
            for (var type = builtInEditor.GetType(); type != null; type = type.BaseType)
            {
                var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (method != null && method.GetParameters().Length == arguments.Length)
                {
                    method.Invoke(builtInEditor, arguments);
                    return;
                }
            }
        }

        private void DrawBlendShapeSearch(SkinnedMeshRenderer renderer, SerializedProperty blendShapeWeights)
        {
            EditorGUILayout.PropertyField(blendShapeWeights, new GUIContent(Text("ui.blendShapes")), false);
            if (!blendShapeWeights.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            var searchRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            var newSearchText = EditorGUI.TextField(searchRect, searchText);
            if (EditorGUI.EndChangeCheck())
            {
                searchText = newSearchText ?? string.Empty;
                blendShapeScrollPosition = Vector2.zero;
                UpdateFilteredBlendShapeEntries();
            }

            EnsureBlendShapeCache(renderer.sharedMesh);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Space(EditorGUI.indentLevel * 15f);
            if (GUILayout.Button(Text("ui.selectAll"), EditorStyles.toolbarButton))
            {
                SelectAllBlendShapes();
            }

            if (GUILayout.Button(Text("ui.clear"), EditorStyles.toolbarButton))
            {
                ClearBlendShapeSelection();
            }

            using (new EditorGUI.DisabledScope(selectedBlendShapeIndices.Count == 0))
            {
                if (GUILayout.Button(Text("ui.exportSelected"), EditorStyles.toolbarButton))
                {
                    ExportSelectedBlendShapes();
                }
            }

            if (GUILayout.Button(Text("ui.import"), EditorStyles.toolbarButton))
            {
                ImportBlendShapeWeights();
            }

            if (GUILayout.Button(Text("ui.exportBlendShapeText"), EditorStyles.toolbarButton))
            {
                ExportBlendShapeText();
            }

            EditorGUILayout.EndHorizontal();

            DrawVirtualizedBlendShapeRows(renderer, blendShapeWeights);

            EditorGUI.indentLevel--;
        }

        private void DrawVirtualizedBlendShapeRows(SkinnedMeshRenderer renderer, SerializedProperty blendShapeWeights)
        {
            if (filteredBlendShapeEntries.Count == 0)
            {
                EditorGUILayout.HelpBox(Text("ui.noMatches"), MessageType.Info);
                return;
            }

            var rowHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var totalHeight = filteredBlendShapeEntries.Count * rowHeight;
            var viewportHeight = Mathf.Min(BlendShapeListViewportHeight, totalHeight);
            blendShapeScrollPosition = EditorGUILayout.BeginScrollView(blendShapeScrollPosition, GUILayout.Height(viewportHeight));
            var rowsRect = GUILayoutUtility.GetRect(0f, totalHeight, GUILayout.ExpandWidth(true));
            var firstVisibleRow = Mathf.Max(0, Mathf.FloorToInt(blendShapeScrollPosition.y / rowHeight));
            var lastVisibleRow = Mathf.Min(
                filteredBlendShapeEntries.Count,
                Mathf.CeilToInt((blendShapeScrollPosition.y + viewportHeight) / rowHeight) + 1);
            for (var rowIndex = firstVisibleRow; rowIndex < lastVisibleRow; rowIndex++)
            {
                var rowRect = new Rect(
                    rowsRect.x,
                    rowsRect.y + rowIndex * rowHeight,
                    rowsRect.width,
                    EditorGUIUtility.singleLineHeight);
                DrawBlendShapeRow(renderer, blendShapeWeights, filteredBlendShapeEntries[rowIndex], rowRect);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawBlendShapeRow(
            SkinnedMeshRenderer renderer,
            SerializedProperty blendShapeWeights,
            BlendShapeEntry blendShape,
            Rect rowRect)
        {
            rowRect = EditorGUI.IndentedRect(rowRect);
            var toggleRect = new Rect(rowRect.x, rowRect.y, 16f, rowRect.height);
            var isSelected = EditorGUI.Toggle(toggleRect, selectedBlendShapeIndices.Contains(blendShape.Index));
            if (isSelected)
            {
                selectedBlendShapeIndices.Add(blendShape.Index);
            }
            else
            {
                selectedBlendShapeIndices.Remove(blendShape.Index);
            }

            var property = blendShape.Index < blendShapeWeights.arraySize
                ? blendShapeWeights.GetArrayElementAtIndex(blendShape.Index)
                : null;
            var sliderLabel = new GUIContent(blendShape.DisplayName, blendShape.SourceName);
            var sliderRect = new Rect(toggleRect.xMax + EditorGUIUtility.standardVerticalSpacing, rowRect.y,
                rowRect.xMax - toggleRect.xMax - EditorGUIUtility.standardVerticalSpacing, rowRect.height);
            if (property != null)
            {
                EditorGUI.Slider(sliderRect, property, blendShape.SliderMinimum, blendShape.SliderMaximum, sliderLabel);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                var value = EditorGUI.Slider(sliderRect, sliderLabel, 0f, blendShape.SliderMinimum, blendShape.SliderMaximum);
                if (EditorGUI.EndChangeCheck())
                {
                    blendShapeWeights.arraySize = renderer.sharedMesh.blendShapeCount;
                    blendShapeWeights.GetArrayElementAtIndex(blendShape.Index).floatValue = value;
                }
            }
        }

        private VisualElement CreateToolbar()
        {
            var toolbar = new Toolbar();

            toolbar.Add(new ToolbarButton(SelectAllBlendShapes) { text = Text("ui.selectAll") });
            toolbar.Add(new ToolbarButton(ClearBlendShapeSelection) { text = Text("ui.clear") });
            exportButton = new ToolbarButton(ExportSelectedBlendShapes) { text = Text("ui.exportSelected") };
            toolbar.Add(exportButton);
            toolbar.Add(new ToolbarButton(ImportBlendShapeWeights) { text = Text("ui.import") });
            toolbar.Add(new ToolbarButton(ExportBlendShapeText) { text = Text("ui.exportBlendShapeText") });
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

            EnsureBlendShapeCache(mesh);
            UpdateFilteredBlendShapeEntries();
            foreach (var blendShape in filteredBlendShapeEntries)
            {
                blendShapeList.Add(CreateBlendShapeRow(renderer, blendShape));
            }

            if (filteredBlendShapeEntries.Count == 0)
            {
                blendShapeList.Add(new HelpBox(Text("ui.noMatches"), HelpBoxMessageType.Info) { name = EmptyStateName });
            }

            UpdateExportButton();
        }

        private static bool MatchesSearch(string name, string query)
        {
            return string.IsNullOrEmpty(query) || name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void EnsureBlendShapeCache(Mesh mesh)
        {
            if (cachedBlendShapeMesh == mesh)
            {
                return;
            }

            cachedBlendShapeMesh = mesh;
            blendShapeEntries.Clear();
            filteredBlendShapeEntries.Clear();
            blendShapeScrollPosition = Vector2.zero;
            if (mesh == null)
            {
                return;
            }

            for (var index = 0; index < mesh.blendShapeCount; index++)
            {
                var sliderMinimum = 0f;
                var sliderMaximum = 0f;
                for (var frameIndex = 0; frameIndex < mesh.GetBlendShapeFrameCount(index); frameIndex++)
                {
                    var frameWeight = mesh.GetBlendShapeFrameWeight(index, frameIndex);
                    sliderMinimum = Mathf.Min(sliderMinimum, frameWeight);
                    sliderMaximum = Mathf.Max(sliderMaximum, frameWeight);
                }

                var sourceName = mesh.GetBlendShapeName(index);
                blendShapeEntries.Add(new BlendShapeEntry
                {
                    Index = index,
                    SourceName = sourceName,
                    DisplayName = BlendShapeSearchLocalization.GetBlendShapeDisplayName(sourceName),
                    SliderMinimum = sliderMinimum,
                    SliderMaximum = sliderMaximum
                });
            }

            UpdateFilteredBlendShapeEntries();
        }

        private void UpdateFilteredBlendShapeEntries()
        {
            filteredBlendShapeEntries.Clear();
            foreach (var blendShape in blendShapeEntries)
            {
                if (MatchesSearch(blendShape.SourceName, searchText) || MatchesSearch(blendShape.DisplayName, searchText))
                {
                    filteredBlendShapeEntries.Add(blendShape);
                }
            }
        }

        private VisualElement CreateBlendShapeRow(SkinnedMeshRenderer renderer, BlendShapeEntry blendShape)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            var selectionToggle = new Toggle { tooltip = Text("ui.selectionTooltip") };
            selectionToggle.SetValueWithoutNotify(selectedBlendShapeIndices.Contains(blendShape.Index));
            selectionToggle.RegisterValueChangedCallback(change =>
            {
                if (change.newValue)
                {
                    selectedBlendShapeIndices.Add(blendShape.Index);
                }
                else
                {
                    selectedBlendShapeIndices.Remove(blendShape.Index);
                }

                UpdateExportButton();
            });

            var slider = new Slider(blendShape.DisplayName, 0f, 100f)
            {
                showInputField = true,
                tooltip = blendShape.DisplayName == blendShape.SourceName ? string.Empty : blendShape.SourceName
            };
            slider.style.flexGrow = 1f;
            if (blendShape.DisplayName != blendShape.SourceName)
            {
                slider.labelElement.tooltip = blendShape.SourceName;
            }
            slider.SetValueWithoutNotify(renderer.GetBlendShapeWeight(blendShape.Index));
            slider.RegisterValueChangedCallback(change => ApplyBlendShapeWeight(renderer, blendShape.Index, change.newValue));
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
