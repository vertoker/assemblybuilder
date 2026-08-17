using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssemblyBuilder.Model;
using NUnit.Framework;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssemblyBuilder.Tests
{
    /// <summary>
    /// Base of every AssemblyBuilder test.
    /// Builder resolves references through AssetDatabase GUID's, so every test
    /// works with real .asmdef files inside own temp folder.
    /// Builders themselves don't need to be assets, they live only in memory
    /// </summary>
    public abstract class AssemblyBuilderTestFixture
    {
        private const string TempFolderName = "AssemblyBuilderTests";
        protected const string TempFolderPath = "Assets/" + TempFolderName;

        private readonly List<Object> _createdObjects = new();
        private int _nextObjectId;

        [SetUp]
        public void SetUpTempFolder()
        {
            DeleteTempFolder();
            AssetDatabase.CreateFolder("Assets", TempFolderName);
        }

        [TearDown]
        public void TearDownTempFolder()
        {
            foreach (var createdObject in _createdObjects)
            {
                if (createdObject) Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
            DeleteTempFolder();
        }

        private static void DeleteTempFolder()
        {
            if (AssetDatabase.IsValidFolder(TempFolderPath))
                AssetDatabase.DeleteAsset(TempFolderPath);
        }

        /// <summary>
        /// Creates real .asmdef file inside own folder of temp folder.
        /// Every .asmdef needs separate folder, editor doesn't allow two of them in one place.
        /// Folders contain no scripts, so no assembly is compiled from them
        /// </summary>
        protected AssemblyDefinitionAsset CreateDefinition(string assetName, AssemblyDefinitionModel model = null)
        {
            model ??= new AssemblyDefinitionModel();
            model.name ??= TempFolderName + "." + assetName;

            AssetDatabase.CreateFolder(TempFolderPath, assetName);

            var path = DefinitionPath(assetName);
            File.WriteAllText(path, JsonUtility.ToJson(model, true));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var definition = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
            Assert.That(definition, Is.Not.Null, $"Can't import test AssemblyDefinition [{path}]");
            return definition;
        }

        protected static string DefinitionFolderPath(string assetName)
        {
            return $"{TempFolderPath}/{assetName}";
        }

        protected static string DefinitionPath(string assetName)
        {
            return $"{DefinitionFolderPath(assetName)}/{assetName}.asmdef";
        }

        protected AssemblyBuilder CreateBuilder(params AssemblyDefinitionAsset[] definitions)
        {
            var builder = CreateInMemory<AssemblyBuilder>("Builder");
            builder._definitions.AddRange(definitions);
            return builder;
        }

        protected AssemblyBuilderCollection CreateCollection(params BaseAssemblyBuilder[] builders)
        {
            var collection = CreateInMemory<AssemblyBuilderCollection>("Collection");
            collection._builders.AddRange(builders);
            return collection;
        }

        private T CreateInMemory<T>(string namePrefix) where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            instance.name = $"{namePrefix}_{_nextObjectId++}";
            _createdObjects.Add(instance);
            return instance;
        }

        /// <summary>
        /// Builds without AssetDatabase.Refresh, it's not needed for tests
        /// and only slows down every single build call
        /// </summary>
        protected static void BuildWithoutRefresh(BaseAssemblyBuilder builder)
        {
            builder.BuildInternal(new HashSet<BaseAssemblyBuilder>());
        }

        /// <summary>
        /// Reads .asmdef from disk, not through AssemblyDefinitionAsset.text,
        /// because builder writes file directly and cached text stays outdated
        /// </summary>
        protected static AssemblyDefinitionModel ReadModel(AssemblyDefinitionAsset definition)
        {
            var path = AssetDatabase.GetAssetPath(definition);
            return JsonUtility.FromJson<AssemblyDefinitionModel>(File.ReadAllText(path));
        }

        protected static string ReadText(AssemblyDefinitionAsset definition)
        {
            return File.ReadAllText(AssetDatabase.GetAssetPath(definition));
        }

        /// <summary>
        /// Reimports definition, so AssemblyDefinitionAsset.text becomes actual again.
        /// Editor does the same through AssetDatabase.Refresh in the end of Build
        /// </summary>
        protected static void Reimport(AssemblyDefinitionAsset definition)
        {
            var path = AssetDatabase.GetAssetPath(definition);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        protected static string Reference(AssemblyDefinitionAsset definition)
        {
            var path = AssetDatabase.GetAssetPath(definition);
            return "GUID:" + AssetDatabase.AssetPathToGUID(path);
        }

        protected static void AssertReferences(AssemblyDefinitionAsset definition,
            params AssemblyDefinitionAsset[] expected)
        {
            var references = ReadModel(definition).references;
            Assert.That(references, Is.Not.Null, "references field is missing in built .asmdef");

            var expectedReferences = expected.Select(Reference).ToList();
            CollectionAssert.AreEquivalent(expectedReferences, references,
                $"expected [{Describe(expectedReferences)}], but was [{Describe(references)}]");
        }

        // references are GUID's, they say nothing in assert message, so they are resolved back to names
        private static string Describe(IEnumerable<string> references)
        {
            var names = references.Select(reference =>
            {
                var guid = reference.StartsWith("GUID:") ? reference.Substring("GUID:".Length) : reference;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                return string.IsNullOrEmpty(path) ? reference : Path.GetFileNameWithoutExtension(path);
            });
            return string.Join(", ", names);
        }
    }
}
