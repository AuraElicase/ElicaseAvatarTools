# UI Toolkit 组件标题与组件选择器翻译

## 实现范围

- `tech.elicase.elicase-ui-theme-for-unity` 提供独立于视觉主题设置的内置窗口观察器。
- `tech.elicase.avatar-toolkit` 使用观察器翻译 Inspector 组件标题，并以 UI Toolkit 选择器替换 Inspector 的添加组件入口。
- 翻译继续读取 `Assets/ElicaseAvatarToolkit/Langs/{language}/*.components.lang`，首个按序文件拥有优先级。

## 主题包 API

`IElicaseEditorWindowObserver` 定义三个生命周期：

- `OnAttach`：主题桥首次发现目标窗口时执行。
- `OnRefresh`：主题桥每次刷新窗口时执行；注册、注销和 `RequestRefresh` 会触发即时刷新。
- `OnDetach`：窗口关闭或观察器注销时执行。

使用 `ElicaseEditorWindowObservers.Register` 注册观察器，使用 `Unregister` 注销，使用 `RequestRefresh` 响应外部状态变化。观察器在内置窗口主题和插件扩展设置关闭时保持工作；原有 `IElicaseEditorWindowExtension` 保持原有主题设置语义。

`ElicaseInspectorElements.TryGetAddComponentButton` 通过 Unity 2022.3 `InspectorElement.s_AddComponentClassName` 定位原生 UI Toolkit 按钮。定位失败时调用方保留原生入口并记录一次兼容性诊断。

## 工具包行为

- 标题翻译只处理父级链包含 `unity-inspector-element__header` 或 `unity-inspector-element__header-title` 的 `TextElement`。每个元素保存原文，语言切换、文件重载和开关切换都可恢复或重新应用文本。
- 组件翻译开启时，观察器将原生添加组件按钮隐藏，在相同父级插入主题按钮；关闭翻译或卸载观察器时恢复原生按钮。
- 自定义选择器使用 `TypeCache.GetTypesDerivedFrom<Component>()` 建立条目，排除 Transform、抽象、泛型和非公开组件。`AddComponentMenu` 的首段作为分组，缺少菜单路径的组件归入 Scripts。
- 搜索同时匹配组件原文、组件译文和完整菜单路径。列表显示译文，并为每项提供原文和菜单路径提示。
- 选择组件时，对当前所有非持久化 `Selection.gameObjects` 执行 `Undo.AddComponent`。`DisallowMultipleComponent` 已存在的对象会被跳过；部分失败会显示对象列表，全部成功时关闭选择器。

## Unity 2022.3 兼容与验收

本实现以 Unity 2022.3.22f1 为基线。升级 Unity 后，先验证原生添加组件按钮定位器；兼容性回退保留 Unity 按钮并输出一次诊断。

验收步骤：

1. 选择带 Skinned Mesh Renderer 的对象，确认组件标题显示 `蒙皮网格渲染`。
2. 切换语言、修改并重载 `.components.lang`、关闭和重新开启组件翻译，确认标题和入口同步更新。
3. 使用自定义添加组件入口搜索原文、译文和菜单路径，验证单对象与多对象添加、重复限制、Undo/Redo。
4. 关闭主题视觉设置和插件扩展设置，确认组件翻译仍然工作。
5. 在 Inspector 重建、窗口关闭和原生按钮定位失败时，确认原生 UI 恢复且没有重复入口。
