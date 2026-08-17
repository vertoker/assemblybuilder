using System.Text.RegularExpressions;
using AssemblyBuilder.Model;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AssemblyBuilder.Tests
{
    /// <summary>
    /// Collection inside parents is transparent: it stands for builders inside it,
    /// not for a layer of inheritance. Putting a collection into parents must give
    /// exactly the same result as putting it's builders there one by one
    /// </summary>
    [TestFixture]
    public class CollectionAsParentTests : AssemblyBuilderTestFixture
    {
        private static readonly Regex CyclicError = new("Cyclic parent reference");

        [Test]
        public void Collection_InPublicParents_GivesDefinitionsOfEveryBuilder()
        {
            var firstDefinition = CreateDefinition("First");
            var secondDefinition = CreateDefinition("Second");
            var childDefinition = CreateDefinition("Child");

            var collection = CreateCollection(CreateBuilder(firstDefinition), CreateBuilder(secondDefinition));

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(collection);

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, firstDefinition, secondDefinition);
        }

        [Test]
        public void Collection_InPrivateParents_GivesDefinitionsOfEveryBuilder()
        {
            var firstDefinition = CreateDefinition("First");
            var secondDefinition = CreateDefinition("Second");
            var childDefinition = CreateDefinition("Child");

            var collection = CreateCollection(CreateBuilder(firstDefinition), CreateBuilder(secondDefinition));

            var child = CreateBuilder(childDefinition);
            child._privateParents.Add(collection);

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, firstDefinition, secondDefinition);
        }

        [Test]
        public void Collection_InPrivateParents_IsNotInheritedByChildren()
        {
            var privateDefinition = CreateDefinition("Private");
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var parent = CreateBuilder(parentDefinition);
            parent._privateParents.Add(CreateCollection(CreateBuilder(privateDefinition)));

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(parent);

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition);
        }

        [Test]
        public void Inherit_DoesNotCountCollectionAsLayer()
        {
            var grandDefinition = CreateDefinition("Grand");
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var parent = CreateBuilder(parentDefinition);
            parent._publicParents.Add(CreateBuilder(grandDefinition));

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(CreateCollection(parent));
            child._inheritMode = AssemblyInheritMode.Inherit;

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition);
        }

        [Test]
        public void DeepInherit_CollectsPublicParentsOfBuildersInCollection()
        {
            var grandDefinition = CreateDefinition("Grand");
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var parent = CreateBuilder(parentDefinition);
            parent._publicParents.Add(CreateBuilder(grandDefinition));

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(CreateCollection(parent));

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition, grandDefinition);
        }

        [Test]
        public void DeepInherit_SkipsPrivateParentsOfBuildersInCollection()
        {
            var grandPublicDefinition = CreateDefinition("GrandPublic");
            var grandPrivateDefinition = CreateDefinition("GrandPrivate");
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var parent = CreateBuilder(parentDefinition);
            parent._publicParents.Add(CreateBuilder(grandPublicDefinition));
            parent._privateParents.Add(CreateBuilder(grandPrivateDefinition));

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(CreateCollection(parent));

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition, grandPublicDefinition);
        }

        [Test]
        public void NestedCollections_InParents_AreExpandedRecursively()
        {
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var nested = CreateCollection(CreateBuilder(parentDefinition));
            var root = CreateCollection(nested);

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(root);

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition);
        }

        [Test]
        public void CyclicCollections_InParents_DoNotHang()
        {
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var first = CreateCollection(CreateBuilder(parentDefinition));
            var second = CreateCollection(first);
            first._builders.Add(second);

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(first);

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition);
        }

        [Test]
        public void CollectionContainingChildItself_ReportsCycleAndSkipsSelfReference()
        {
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(CreateCollection(child, CreateBuilder(parentDefinition)));

            LogAssert.Expect(LogType.Error, CyclicError);
            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition);
        }

        [Test]
        public void NullBuilder_InCollectionParent_IsSkipped()
        {
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var collection = CreateCollection(CreateBuilder(parentDefinition));
            collection._builders.Insert(0, null);

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(collection);

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition);
        }

        [Test]
        public void ReadonlyBuilder_InCollectionParent_StillGivesDefinitions()
        {
            var readonlyDefinition = CreateDefinition("Readonly");
            var childDefinition = CreateDefinition("Child");

            // readonly only forbids writing into own definitions,
            // as a parent such builder is a source of references like any other
            var readonlyBuilder = CreateBuilder(readonlyDefinition);
            readonlyBuilder._readonly = true;

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(CreateCollection(readonlyBuilder));

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, readonlyDefinition);
        }
    }
}
