using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlendShapeSearch
{
    internal static class BlendShapeSearchPaths
    {
        internal const string RootAssetPath = "Assets/ElicaseAvatarToolkit";
        internal const string OutputsAssetPath = RootAssetPath + "/Outputs";
        internal const string LanguagesAssetPath = RootAssetPath + "/Langs";
        internal const string PackageI18nAssetPath = "Packages/tech.elicase.avatar-toolkit/i18n";

        internal static string OutputsAbsolutePath => ToAbsolutePath(OutputsAssetPath);
        internal static string LanguagesAbsolutePath => ToAbsolutePath(LanguagesAssetPath);
        internal static string PackageI18nAbsolutePath => ToAbsolutePath(PackageI18nAssetPath);

        internal static void EnsureAssetFolders()
        {
            var createdFolder = EnsureDirectoryExists(RootAssetPath);
            createdFolder |= EnsureDirectoryExists(OutputsAssetPath);
            createdFolder |= EnsureDirectoryExists(LanguagesAssetPath);

            if (createdFolder)
            {
                AssetDatabase.Refresh();
            }
        }

        internal static void EnsureLanguageFolder(string language)
        {
            EnsureAssetFolders();
            if (!string.IsNullOrWhiteSpace(language) && EnsureDirectoryExists(LanguagesAssetPath + "/" + language))
            {
                AssetDatabase.Refresh();
            }
        }

        internal static string CreateOutputPath(string filenamePrefix, string extension = ".yml")
        {
            EnsureAssetFolders();
            return Path.Combine(OutputsAbsolutePath, filenamePrefix + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + extension);
        }

        internal static void RevealAsset(string absolutePath)
        {
            AssetDatabase.Refresh();
            var assetPath = RootAssetPath + absolutePath.Substring(ToAbsolutePath(RootAssetPath).Length).Replace('\\', '/');
            var asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(assetPath);
            if (asset != null)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        private static string ToAbsolutePath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
        }

        private static bool EnsureDirectoryExists(string assetPath)
        {
            var absolutePath = ToAbsolutePath(assetPath);
            if (Directory.Exists(absolutePath))
            {
                return false;
            }

            Directory.CreateDirectory(absolutePath);
            return true;
        }
    }

    internal static class BlendShapeSearchLocalization
    {
        private const string LanguageOverridePreferenceKey = "ElicaseAvatarToolkit.Language.Override";
        private const string LegacyLanguagePreferenceKey = "ElicaseAvatarToolkit.BlendShapeSearch.Language";
        private static Dictionary<string, string> uiTexts = new Dictionary<string, string>(StringComparer.Ordinal);
        internal const string BlendShapeTranslationSearchPattern = "*.blendshapes.lang";
        internal const string ComponentTranslationSearchPattern = "*.components.lang";

        private static Dictionary<string, string> blendShapeTranslations = new Dictionary<string, string>(StringComparer.Ordinal);
        private static Dictionary<string, string> componentTranslations = new Dictionary<string, string>(StringComparer.Ordinal);
        private static string currentLanguage;

        internal static event Action LanguageChanged;

        internal static string CurrentLanguage
        {
            get
            {
                EnsureInitialized();
                return currentLanguage;
            }
        }

        internal static string LanguageOverride
        {
            get
            {
                EnsureInitialized();
                return EditorPrefs.GetString(LanguageOverridePreferenceKey, string.Empty);
            }
        }

        internal static IReadOnlyList<string> GetLanguages()
        {
            BlendShapeSearchPaths.EnsureAssetFolders();
            var languages = new List<string>();
            if (Directory.Exists(BlendShapeSearchPaths.PackageI18nAbsolutePath))
            {
                languages.AddRange(Directory.GetFiles(BlendShapeSearchPaths.PackageI18nAbsolutePath, "*.yml")
                    .Select(Path.GetFileNameWithoutExtension)
                    .OrderBy(language => language, StringComparer.Ordinal));
            }

            if (languages.Count == 0)
            {
                languages.Add("en-us");
            }

            return languages;
        }

        internal static bool SetLanguageOverride(string language)
        {
            if (!GetLanguages().Contains(language, StringComparer.Ordinal))
            {
                return false;
            }

            if (LanguageOverride == language)
            {
                return true;
            }

            EditorPrefs.SetString(LanguageOverridePreferenceKey, language);
            currentLanguage = language;
            LoadLanguageResources();
            LanguageChanged?.Invoke();
            return true;
        }

        internal static void FollowUnityLanguage()
        {
            EnsureInitialized();
            var defaultLanguage = GetDefaultLanguage(GetLanguages());
            if (!EditorPrefs.HasKey(LanguageOverridePreferenceKey) && currentLanguage == defaultLanguage)
            {
                return;
            }

            EditorPrefs.DeleteKey(LanguageOverridePreferenceKey);
            currentLanguage = defaultLanguage;
            LoadLanguageResources();
            LanguageChanged?.Invoke();
        }

        internal static string Text(string key)
        {
            EnsureInitialized();
            return uiTexts.TryGetValue(key, out var text) ? text : key;
        }

        internal static string GetBlendShapeDisplayName(string sourceName)
        {
            EnsureInitialized();
            return blendShapeTranslations.TryGetValue(sourceName, out var translatedName)
                   && !string.IsNullOrEmpty(translatedName)
                ? translatedName
                : sourceName;
        }

        internal static string GetComponentDisplayName(string sourceName)
        {
            return TryGetComponentDisplayName(sourceName, out var displayName) ? displayName : sourceName;
        }

        internal static bool TryGetComponentDisplayName(string sourceName, out string displayName)
        {
            EnsureInitialized();
            if (componentTranslations.TryGetValue(sourceName, out var translatedName)
                && !string.IsNullOrEmpty(translatedName)
                && translatedName != sourceName)
            {
                displayName = translatedName;
                return true;
            }

            displayName = sourceName;
            return false;
        }

        internal static void ReloadCurrentLanguage()
        {
            EnsureInitialized();
            LoadLanguageResources();
            LanguageChanged?.Invoke();
        }

        internal static Dictionary<string, string> MergeFirstWins(IEnumerable<IDictionary<string, string>> mappings)
        {
            var merged = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var mapping in mappings)
            {
                foreach (var entry in mapping)
                {
                    if (!merged.ContainsKey(entry.Key))
                    {
                        merged.Add(entry.Key, entry.Value);
                    }
                }
            }

            return merged;
        }

        private static void EnsureInitialized()
        {
            EditorPrefs.DeleteKey(LegacyLanguagePreferenceKey);
            if (!string.IsNullOrEmpty(currentLanguage))
            {
                return;
            }

            var languages = GetLanguages();
            var languageOverride = EditorPrefs.GetString(LanguageOverridePreferenceKey, string.Empty);
            currentLanguage = languages.Contains(languageOverride, StringComparer.Ordinal)
                ? languageOverride
                : GetDefaultLanguage(languages);
            LoadLanguageResources();
        }

        private static string GetDefaultLanguage(IReadOnlyList<string> languages)
        {
            var editorLanguage = Application.systemLanguage == SystemLanguage.ChineseSimplified ? "zh-cn"
                : Application.systemLanguage == SystemLanguage.Japanese ? "ja-jp"
                : "en-us";
            if (languages.Contains(editorLanguage, StringComparer.Ordinal))
            {
                return editorLanguage;
            }

            return languages.Contains("en-us", StringComparer.Ordinal) ? "en-us" : languages[0];
        }

        private static void LoadLanguageResources()
        {
            BlendShapeSearchPaths.EnsureAssetFolders();
            var englishPath = Path.Combine(BlendShapeSearchPaths.PackageI18nAbsolutePath, "en-us.yml");
            var languagePath = Path.Combine(BlendShapeSearchPaths.PackageI18nAbsolutePath, currentLanguage + ".yml");
            uiTexts = ReadYamlFile(englishPath);
            foreach (var entry in ReadYamlFile(languagePath))
            {
                uiTexts[entry.Key] = entry.Value;
            }

            BlendShapeSearchPaths.EnsureLanguageFolder(currentLanguage);
            var translationDirectory = Path.Combine(BlendShapeSearchPaths.LanguagesAbsolutePath, currentLanguage);
            blendShapeTranslations = LoadTranslationMappings(translationDirectory, BlendShapeTranslationSearchPattern);
            componentTranslations = LoadTranslationMappings(translationDirectory, ComponentTranslationSearchPattern);
        }

        internal static Dictionary<string, string> LoadTranslationMappings(string directory, string searchPattern)
        {
            if (!Directory.Exists(directory))
            {
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }

            var mappings = Directory.GetFiles(directory, searchPattern)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(ReadYamlFile);
            return MergeFirstWins(mappings);
        }

        private static Dictionary<string, string> ReadYamlFile(string path)
        {
            try
            {
                return File.Exists(path)
                    ? FlatYaml.Parse(File.ReadAllText(path))
                    : new Dictionary<string, string>(StringComparer.Ordinal);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Blend Shape Search could not read YAML file '" + path + "': " + exception.Message);
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }
        }
    }
}
