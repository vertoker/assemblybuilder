using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace AssemblyBuilder.Tests
{
    /// <summary>
    /// Creation of AssemblyBuilder from selected .asmdef file (menu item with shortcut)
    /// </summary>
    [TestFixture]
    public class AssemblyBuilderCreationTests : AssemblyBuilderTestFixture
    {
        // builder is created near source .asmdef file, so it lands into folder of definition
        private static string BuilderPath(string definitionName, string assetName = null)
        {
            return $"{DefinitionFolderPath(definitionName)}/{assetName ?? definitionName}.asset";
        }

        [Test]
        public void Creation_CreatesBuilderNearDefinition()
        {
            var definition = CreateDefinition("Sample");

            AssemblyBuilderCreation.CreateAssemblyBuilderFromAssemblyDefinition(definition);

            var builder = AssetDatabase.LoadAssetAtPath<AssemblyBuilder>(BuilderPath("Sample"));
            Assert.That(builder, Is.Not.Null, "AssemblyBuilder is not created near .asmdef file");
        }

        [Test]
        public void Creation_AddsSourceDefinitionIntoBuilder()
        {
            var definition = CreateDefinition("Sample");

            AssemblyBuilderCreation.CreateAssemblyBuilderFromAssemblyDefinition(definition);

            var builder = AssetDatabase.LoadAssetAtPath<AssemblyBuilder>(BuilderPath("Sample"));
            Assert.That(builder.Definitions, Is.EqualTo(new[] { definition }));
        }

        [Test]
        public void Creation_UsesAssetDatabasePathSeparators()
        {
            var definition = CreateDefinition("Sample");

            AssemblyBuilderCreation.CreateAssemblyBuilderFromAssemblyDefinition(definition);

            var builder = AssetDatabase.LoadAssetAtPath<AssemblyBuilder>(BuilderPath("Sample"));
            var path = AssetDatabase.GetAssetPath(builder);

            Assert.That(path, Is.EqualTo(BuilderPath("Sample")));
            Assert.That(path, Does.Not.Contain("\\"), "AssetDatabase path contains windows separators");
        }

        [Test]
        public void Creation_GeneratesUniqueName_OnRepeatedCall()
        {
            var definition = CreateDefinition("Sample");

            AssemblyBuilderCreation.CreateAssemblyBuilderFromAssemblyDefinition(definition);
            AssemblyBuilderCreation.CreateAssemblyBuilderFromAssemblyDefinition(definition);

            Assert.That(File.Exists(BuilderPath("Sample")), Is.True);
            Assert.That(File.Exists(BuilderPath("Sample", "Sample 1")), Is.True,
                "second builder overwrote the first one");
        }

        [Test]
        public void Creation_OfSeveralDefinitions_CreatesSeveralBuilders()
        {
            var firstDefinition = CreateDefinition("First");
            var secondDefinition = CreateDefinition("Second");

            AssemblyBuilderCreation.CreateAssemblyBuilderFromAssemblyDefinition(firstDefinition);
            AssemblyBuilderCreation.CreateAssemblyBuilderFromAssemblyDefinition(secondDefinition);

            var first = AssetDatabase.LoadAssetAtPath<AssemblyBuilder>(BuilderPath("First"));
            var second = AssetDatabase.LoadAssetAtPath<AssemblyBuilder>(BuilderPath("Second"));

            Assert.That(first.Definitions, Is.EqualTo(new[] { firstDefinition }));
            Assert.That(second.Definitions, Is.EqualTo(new[] { secondDefinition }));
        }

        [Test]
        public void CreatedBuilder_IsReadyToBuild()
        {
            var parentDefinition = CreateDefinition("Parent");
            var definition = CreateDefinition("Sample");

            AssemblyBuilderCreation.CreateAssemblyBuilderFromAssemblyDefinition(definition);

            var builder = AssetDatabase.LoadAssetAtPath<AssemblyBuilder>(BuilderPath("Sample"));
            builder._publicParents.Add(CreateBuilder(parentDefinition));

            BuildWithoutRefresh(builder);

            AssertReferences(definition, parentDefinition);
        }

        [Test]
        public void Creation_WithoutAssetPath_DoesNotCreateAnything()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Can't resolve asset path"));
            AssemblyBuilderCreation.CreateAssemblyBuilderFromAssemblyDefinition(null);

            Assert.That(Directory.GetFiles(TempFolderPath, "*.asset", SearchOption.AllDirectories), Is.Empty);
        }
    }
}
