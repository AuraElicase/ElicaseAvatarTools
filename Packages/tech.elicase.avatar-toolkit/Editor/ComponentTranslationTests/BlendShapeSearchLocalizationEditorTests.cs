using System.IO;
using NUnit.Framework;

namespace BlendShapeSearch.Tests
{
    public sealed class BlendShapeSearchLocalizationEditorTests
    {
        [Test]
        public void ZhCnComponentTranslationsContainSkinnedMeshRenderer()
        {
            var directory = Path.Combine(
                Directory.GetCurrentDirectory(),
                BlendShapeSearchPaths.LanguagesAssetPath,
                "zh-cn");

            var translations = BlendShapeSearchLocalization.LoadTranslationMappings(
                directory,
                BlendShapeSearchLocalization.ComponentTranslationSearchPattern);

            Assert.That(translations["Skinned Mesh Renderer"], Is.EqualTo("蒙皮网格渲染"));
        }

        [Test]
        public void TranslationLookupKeepsUnmappedComponentTitle()
        {
            var previousLanguage = BlendShapeSearchLocalization.LanguageOverride;
            try
            {
                Assert.That(BlendShapeSearchLocalization.SetLanguageOverride("zh-cn"), Is.True);
                Assert.That(
                    BlendShapeSearchLocalization.GetComponentDisplayName("A Component Without A Translation"),
                    Is.EqualTo("A Component Without A Translation"));
            }
            finally
            {
                RestoreLanguage(previousLanguage);
            }
        }

        private static void RestoreLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                BlendShapeSearchLocalization.FollowUnityLanguage();
                return;
            }

            BlendShapeSearchLocalization.SetLanguageOverride(language);
        }
    }
}
