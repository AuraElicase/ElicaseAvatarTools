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
        internal const string I18nAssetPath = RootAssetPath + "/i18n";

        internal static string OutputsAbsolutePath => ToAbsolutePath(OutputsAssetPath);
        internal static string LanguagesAbsolutePath => ToAbsolutePath(LanguagesAssetPath);
        internal static string I18nAbsolutePath => ToAbsolutePath(I18nAssetPath);

        internal static string CreateOutputPath(string filenamePrefix)
        {
            Directory.CreateDirectory(OutputsAbsolutePath);
            return Path.Combine(OutputsAbsolutePath, filenamePrefix + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".yml");
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
    }

    internal static class BlendShapeSearchLocalization
    {
        private const string LanguagePreferenceKey = "ElicaseAvatarToolkit.BlendShapeSearch.Language";
        private static Dictionary<string, string> uiTexts = new Dictionary<string, string>(StringComparer.Ordinal);
        private static Dictionary<string, string> blendShapeTranslations = new Dictionary<string, string>(StringComparer.Ordinal);
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

        internal static IReadOnlyList<string> GetLanguages()
        {
            var languages = new List<string>();
            if (Directory.Exists(BlendShapeSearchPaths.I18nAbsolutePath))
            {
                languages.AddRange(Directory.GetFiles(BlendShapeSearchPaths.I18nAbsolutePath, "*.yml")
                    .Select(Path.GetFileNameWithoutExtension)
                    .OrderBy(language => language, StringComparer.Ordinal));
            }

            if (languages.Count == 0)
            {
                languages.Add("en-us");
            }

            return languages;
        }

        internal static void SetLanguage(string language)
        {
            if (!GetLanguages().Contains(language, StringComparer.Ordinal))
            {
                return;
            }

            currentLanguage = language;
            EditorPrefs.SetString(LanguagePreferenceKey, language);
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
            if (!string.IsNullOrEmpty(currentLanguage))
            {
                return;
            }

            var languages = GetLanguages();
            var savedLanguage = EditorPrefs.GetString(LanguagePreferenceKey, string.Empty);
            currentLanguage = languages.Contains(savedLanguage, StringComparer.Ordinal)
                ? savedLanguage
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
            var englishPath = Path.Combine(BlendShapeSearchPaths.I18nAbsolutePath, "en-us.yml");
            var languagePath = Path.Combine(BlendShapeSearchPaths.I18nAbsolutePath, currentLanguage + ".yml");
            uiTexts = ReadYamlFile(englishPath);
            foreach (var entry in ReadYamlFile(languagePath))
            {
                uiTexts[entry.Key] = entry.Value;
            }

            var translationDirectory = Path.Combine(BlendShapeSearchPaths.LanguagesAbsolutePath, currentLanguage);
            if (!Directory.Exists(translationDirectory))
            {
                blendShapeTranslations = new Dictionary<string, string>(StringComparer.Ordinal);
                return;
            }

            var mappings = Directory.GetFiles(translationDirectory, "*.yml")
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(ReadYamlFile);
            blendShapeTranslations = MergeFirstWins(mappings);
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
