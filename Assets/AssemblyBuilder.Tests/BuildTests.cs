using System.Collections.Generic;
using System.IO;
using AssemblyBuilder.Model;
using NUnit.Framework;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AssemblyBuilder.Tests
{
    /// <summary>
    /// Builder rewrites .asmdef files, so everything except references field
    /// must survive every build, and readonly builders must not be touched at all
    /// </summary>
    [TestFixture]
    public class BuildTests : AssemblyBuilderTestFixture
    {
        [Test]
        public void Build_PreservesVersionDefines()
        {
            var definition = CreateDefinition("Child", new AssemblyDefinitionModel
            {
                versionDefines = new List<VersionDefine>
                {
                    new() { name = "com.unity.inputsystem", expression = "1.0.0", define = "INPUT_SYSTEM" },
                    new() { name = "com.unity.ugui", expression = "2.0.0", define = "UGUI" },
                },
            });

            var builder = CreateBuilder(definition);
            BuildWithoutRefresh(builder);

            var model = ReadModel(definition);
            Assert.That(model.versionDefines, Is.Not.Null, "versionDefines are lost after build");
            Assert.That(model.versionDefines, Has.Count.EqualTo(2));
            Assert.That(model.versionDefines[0].name, Is.EqualTo("com.unity.inputsystem"));
            Assert.That(model.versionDefines[0].expression, Is.EqualTo("1.0.0"));
            Assert.That(model.versionDefines[0].define, Is.EqualTo("INPUT_SYSTEM"));
            Assert.That(model.versionDefines[1].name, Is.EqualTo("com.unity.ugui"));
            Assert.That(model.versionDefines[1].define, Is.EqualTo("UGUI"));
        }

        [Test]
        public void Build_PreservesEveryFieldExceptReferences()
        {
            var foreignDefinition = CreateDefinition("Foreign");
            var definition = CreateDefinition("Child", new AssemblyDefinitionModel
            {
                rootNamespace = "Custom.Root.Namespace",
                references = new List<string> { Reference(foreignDefinition) },
                includePlatforms = new List<string> { "Editor" },
                allowUnsafeCode = true,
                overrideReferences = true,
                precompiledReferences = new List<string> { "nunit.framework.dll" },
                autoReferenced = false,
                defineConstraints = new List<string> { "UNITY_INCLUDE_TESTS" },
                noEngineReferences = true,
            });

            var expectedName = ReadModel(definition).name;

            var parentDefinition = CreateDefinition("Parent");
            var builder = CreateBuilder(definition);
            builder._publicParents.Add(CreateBuilder(parentDefinition));

            BuildWithoutRefresh(builder);

            var model = ReadModel(definition);
            Assert.That(model.name, Is.EqualTo(expectedName));
            Assert.That(model.rootNamespace, Is.EqualTo("Custom.Root.Namespace"));
            Assert.That(model.includePlatforms, Is.EqualTo(new[] { "Editor" }));
            Assert.That(model.allowUnsafeCode, Is.True);
            Assert.That(model.overrideReferences, Is.True);
            Assert.That(model.precompiledReferences, Is.EqualTo(new[] { "nunit.framework.dll" }));
            Assert.That(model.autoReferenced, Is.False);
            Assert.That(model.defineConstraints, Is.EqualTo(new[] { "UNITY_INCLUDE_TESTS" }));
            Assert.That(model.noEngineReferences, Is.True);

            // references is the only field, which builder is allowed to change
            AssertReferences(definition, parentDefinition);
        }

        [Test]
        public void Build_PreservesExcludePlatforms()
        {
            // exclude and include platforms can't be used together, so it's a separate test
            var definition = CreateDefinition("Child", new AssemblyDefinitionModel
            {
                excludePlatforms = new List<string> { "Android", "iOS" },
            });

            BuildWithoutRefresh(CreateBuilder(definition));

            Assert.That(ReadModel(definition).excludePlatforms, Is.EqualTo(new[] { "Android", "iOS" }));
        }

        [Test]
        public void Build_WritesFileWhichEditorCanImportBack()
        {
            var parentDefinition = CreateDefinition("Parent");
            var definition = CreateDefinition("Child");

            var builder = CreateBuilder(definition);
            builder._publicParents.Add(CreateBuilder(parentDefinition));

            BuildWithoutRefresh(builder);
            Reimport(definition);

            var reimported = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(DefinitionPath("Child"));
            Assert.That(reimported, Is.Not.Null, "built .asmdef can't be imported back");

            var model = JsonUtility.FromJson<AssemblyDefinitionModel>(reimported.text);
            Assert.That(model, Is.Not.Null);
            Assert.That(model.references, Is.EqualTo(new[] { Reference(parentDefinition) }));
        }

        [Test]
        public void Build_IsIdempotent()
        {
            var parentDefinition = CreateDefinition("Parent");
            var definition = CreateDefinition("Child");

            var builder = CreateBuilder(definition);
            builder._publicParents.Add(CreateBuilder(parentDefinition));

            BuildWithoutRefresh(builder);
            var firstBuildText = ReadText(definition);

            Reimport(definition);
            BuildWithoutRefresh(builder);
            var secondBuildText = ReadText(definition);

            Assert.That(secondBuildText, Is.EqualTo(firstBuildText));
        }

        [Test]
        public void Readonly_DoesNotRewriteFile()
        {
            var foreignDefinition = CreateDefinition("Foreign");
            var parentDefinition = CreateDefinition("Parent");
            var definition = CreateDefinition("Child", new AssemblyDefinitionModel
            {
                references = new List<string> { Reference(foreignDefinition) },
            });

            var builder = CreateBuilder(definition);
            builder._publicParents.Add(CreateBuilder(parentDefinition));
            builder._readonly = true;

            var textBeforeBuild = ReadText(definition);
            BuildWithoutRefresh(builder);

            Assert.That(ReadText(definition), Is.EqualTo(textBeforeBuild));
        }

        [Test]
        public void Readonly_StillGivesReferencesToChildren()
        {
            var readonlyDefinition = CreateDefinition("Readonly");
            var childDefinition = CreateDefinition("Child");

            var readonlyBuilder = CreateBuilder(readonlyDefinition);
            readonlyBuilder._readonly = true;

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(readonlyBuilder);

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, readonlyDefinition);
        }

        [Test]
        public void PublicBuild_WritesReferences()
        {
            var parentDefinition = CreateDefinition("Parent");
            var definition = CreateDefinition("Child");

            var builder = CreateBuilder(definition);
            builder._publicParents.Add(CreateBuilder(parentDefinition));

            // full build, with AssetDatabase.Refresh in the end
            builder.Build();

            AssertReferences(definition, parentDefinition);
        }

        [Test]
        public void Build_DoesNotCreateOrDeleteDefinitionFiles()
        {
            var parentDefinition = CreateDefinition("Parent");
            var definition = CreateDefinition("Child");

            var builder = CreateBuilder(definition);
            builder._publicParents.Add(CreateBuilder(parentDefinition));

            var filesBeforeBuild = Directory.GetFiles(TempFolderPath, "*.asmdef", SearchOption.AllDirectories);
            BuildWithoutRefresh(builder);
            var filesAfterBuild = Directory.GetFiles(TempFolderPath, "*.asmdef", SearchOption.AllDirectories);

            Assert.That(filesAfterBuild, Is.EquivalentTo(filesBeforeBuild));
        }

        [Test]
        public void PublicProperties_MirrorSerializedLists()
        {
            var definition = CreateDefinition("Child");
            var builder = CreateBuilder(definition);
            var parent = CreateBuilder();
            builder._publicParents.Add(parent);

            Assert.That(builder.Definitions, Is.SameAs(builder._definitions));
            Assert.That(builder.PublicParents, Is.SameAs(builder._publicParents));
            Assert.That(builder.PrivateParents, Is.SameAs(builder._privateParents));
            Assert.That(builder.Definitions, Is.EqualTo(new[] { definition }));
            Assert.That(builder.PublicParents, Is.EqualTo(new[] { parent }));
        }
    }
}
