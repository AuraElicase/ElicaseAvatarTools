using System;
using System.IO;
using NUnit.Framework;

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
    }
}
