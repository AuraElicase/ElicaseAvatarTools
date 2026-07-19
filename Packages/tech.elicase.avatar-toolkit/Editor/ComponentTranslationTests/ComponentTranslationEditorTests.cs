using NUnit.Framework;
using UnityEngine;

namespace BlendShapeSearch.Tests
{
    public sealed class ComponentTranslationEditorTests
    {
        [Test]
        public void ComponentTitleProviderReturnsTheTranslatedTitle()
        {
            var previousLanguage = BlendShapeSearchLocalization.LanguageOverride;
            var previousEnabled = ElicaseAvatarToolkitComponentSettings.IsComponentTranslationEnabled;
            var gameObject = new GameObject("Component Title Provider Test");

            try
            {
                Assert.That(BlendShapeSearchLocalization.SetLanguageOverride("zh-cn"), Is.True);
                ElicaseAvatarToolkitComponentSettings.SetEnabled(
                    ElicaseAvatarToolkitComponentSettings.ComponentTranslationComponentId,
                    true);

                var provider = new ElicaseAvatarToolkitComponentTitleProvider();
                string title;
                Assert.That(provider.TryGetTitle(gameObject.AddComponent<SkinnedMeshRenderer>(), out title), Is.True);
                Assert.That(title, Is.EqualTo("蒙皮网格渲染"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                ElicaseAvatarToolkitComponentSettings.SetEnabled(
                    ElicaseAvatarToolkitComponentSettings.ComponentTranslationComponentId,
                    previousEnabled);
                RestoreLanguage(previousLanguage);
            }
        }

        [Test]
        public void SkinnedMeshRendererHeaderUsesTheComponentTranslationKey()
        {
            var previousLanguage = BlendShapeSearchLocalization.LanguageOverride;
            try
            {
                Assert.That(BlendShapeSearchLocalization.SetLanguageOverride("zh-cn"), Is.True);

                var title = ElicaseAvatarToolkitComponentTitles.GetTitle(typeof(SkinnedMeshRenderer));
                Assert.That(title, Is.EqualTo("Skinned Mesh Renderer"));
                Assert.That(BlendShapeSearchLocalization.GetComponentDisplayName(title), Is.EqualTo("蒙皮网格渲染"));
            }
            finally
            {
                RestoreLanguage(previousLanguage);
            }
        }

        [Test]
        public void ComponentTitleProviderUsesTheNativeTitlePathWhenTranslationIsDisabled()
        {
            var previousEnabled = ElicaseAvatarToolkitComponentSettings.IsComponentTranslationEnabled;
            var gameObject = new GameObject("Disabled Component Title Provider Test");

            try
            {
                ElicaseAvatarToolkitComponentSettings.SetEnabled(
                    ElicaseAvatarToolkitComponentSettings.ComponentTranslationComponentId,
                    false);

                string title;
                Assert.That(
                    new ElicaseAvatarToolkitComponentTitleProvider().TryGetTitle(
                        gameObject.AddComponent<SkinnedMeshRenderer>(),
                        out title),
                    Is.False);
                Assert.That(title, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                ElicaseAvatarToolkitComponentSettings.SetEnabled(
                    ElicaseAvatarToolkitComponentSettings.ComponentTranslationComponentId,
                    previousEnabled);
            }
        }

        [Test]
        public void ComponentTitleProviderTranslatesUiToolkitHeaderText()
        {
            var previousLanguage = BlendShapeSearchLocalization.LanguageOverride;
            var previousEnabled = ElicaseAvatarToolkitComponentSettings.IsComponentTranslationEnabled;
            try
            {
                Assert.That(BlendShapeSearchLocalization.SetLanguageOverride("zh-cn"), Is.True);
                ElicaseAvatarToolkitComponentSettings.SetEnabled(
                    ElicaseAvatarToolkitComponentSettings.ComponentTranslationComponentId,
                    true);

                string title;
                Assert.That(
                    new ElicaseAvatarToolkitComponentTitleProvider().TryGetTitle("Skinned Mesh Renderer", out title),
                    Is.True);
                Assert.That(title, Is.EqualTo("蒙皮网格渲染"));
            }
            finally
            {
                ElicaseAvatarToolkitComponentSettings.SetEnabled(
                    ElicaseAvatarToolkitComponentSettings.ComponentTranslationComponentId,
                    previousEnabled);
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
