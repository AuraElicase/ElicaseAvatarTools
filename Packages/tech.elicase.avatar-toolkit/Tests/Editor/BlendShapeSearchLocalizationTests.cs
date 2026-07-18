using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace BlendShapeSearch.Tests
{
    public sealed class BlendShapeSearchLocalizationTests
    {
        [Test]
        public void LoadTranslationMappings_UsesExtensionAndFirstFileWins()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ElicaseAvatarToolkitTests-" + Guid.NewGuid());
            Directory.CreateDirectory(directory);

            try
            {
                File.WriteAllText(Path.Combine(directory, "a.blendshapes.lang"), "\"Smile\": \"First\"\n");
                File.WriteAllText(Path.Combine(directory, "b.blendshapes.lang"), "\"Smile\": \"Second\"\n\"Blink\": \"Blink Localized\"\n");
                File.WriteAllText(Path.Combine(directory, "ignored.yml"), "\"Smile\": \"Ignored\"\n");

                var mappings = BlendShapeSearchLocalization.LoadTranslationMappings(
                    directory,
                    BlendShapeSearchLocalization.BlendShapeTranslationSearchPattern);

                Assert.That(mappings["Smile"], Is.EqualTo("First"));
                Assert.That(mappings["Blink"], Is.EqualTo("Blink Localized"));
                Assert.That(mappings.ContainsKey("Ignored"), Is.False);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void LoadTranslationMappings_SeparatesComponentFiles()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ElicaseAvatarToolkitTests-" + Guid.NewGuid());
            Directory.CreateDirectory(directory);

            try
            {
                File.WriteAllText(Path.Combine(directory, "titles.components.lang"), "\"Camera\": \"摄影机\"\n");
                File.WriteAllText(Path.Combine(directory, "shapes.blendshapes.lang"), "\"Camera\": \"Ignored\"\n");

                var mappings = BlendShapeSearchLocalization.LoadTranslationMappings(
                    directory,
                    BlendShapeSearchLocalization.ComponentTranslationSearchPattern);

                Assert.That(mappings.Count, Is.EqualTo(1));
                Assert.That(mappings["Camera"], Is.EqualTo("摄影机"));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void ComponentTitles_AreDistinctAndOrdinallySorted()
        {
            var titles = ElicaseAvatarToolkitComponentTitles.GetTitles(new[]
            {
                typeof(Light),
                typeof(Camera),
                typeof(Camera)
            });

            Assert.That(titles, Is.EqualTo(titles.OrderBy(title => title, StringComparer.Ordinal)));
            Assert.That(titles, Is.EqualTo(titles.Distinct(StringComparer.Ordinal)));
            Assert.That(titles, Does.Contain("Camera"));
            Assert.That(titles, Does.Contain("Light"));
        }
    }
}
