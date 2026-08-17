using NUnit.Framework;

namespace AssemblyBuilder.Tests
{
    /// <summary>
    /// Collection builds every builder inside it, including nested collections.
    /// Cyclic collections must not hang editor, every builder is built only once
    /// </summary>
    [TestFixture]
    public class AssemblyBuilderCollectionTests : AssemblyBuilderTestFixture
    {
        [Test]
        public void Collection_BuildsEveryBuilder()
        {
            var parentDefinition = CreateDefinition("Parent");
            var firstDefinition = CreateDefinition("First");
            var secondDefinition = CreateDefinition("Second");

            var parent = CreateBuilder(parentDefinition);

            var first = CreateBuilder(firstDefinition);
            first._publicParents.Add(parent);

            var second = CreateBuilder(secondDefinition);
            second._publicParents.Add(parent);

            BuildWithoutRefresh(CreateCollection(first, second));

            AssertReferences(firstDefinition, parentDefinition);
            AssertReferences(secondDefinition, parentDefinition);
        }

        [Test]
        public void Collection_BuildsNestedCollections()
        {
            var parentDefinition = CreateDefinition("Parent");
            var deepDefinition = CreateDefinition("Deep");

            var deep = CreateBuilder(deepDefinition);
            deep._publicParents.Add(CreateBuilder(parentDefinition));

            var nested = CreateCollection(deep);
            var root = CreateCollection(nested);

            BuildWithoutRefresh(root);

            AssertReferences(deepDefinition, parentDefinition);
        }

        [Test]
        public void Collection_SkipsReadonlyBuilders()
        {
            var parentDefinition = CreateDefinition("Parent");
            var readonlyDefinition = CreateDefinition("Readonly");

            var readonlyBuilder = CreateBuilder(readonlyDefinition);
            readonlyBuilder._publicParents.Add(CreateBuilder(parentDefinition));
            readonlyBuilder._readonly = true;

            var textBeforeBuild = ReadText(readonlyDefinition);
            BuildWithoutRefresh(CreateCollection(readonlyBuilder));

            Assert.That(ReadText(readonlyDefinition), Is.EqualTo(textBeforeBuild));
        }

        [Test]
        public void CyclicCollections_DoNotHang()
        {
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(CreateBuilder(parentDefinition));

            var first = CreateCollection(child);
            var second = CreateCollection(first);
            first._builders.Add(second);

            BuildWithoutRefresh(first);

            AssertReferences(childDefinition, parentDefinition);
        }

        [Test]
        public void SelfContainingCollection_DoesNotHang()
        {
            var childDefinition = CreateDefinition("Child");
            var child = CreateBuilder(childDefinition);

            var collection = CreateCollection(child);
            collection._builders.Add(collection);

            BuildWithoutRefresh(collection);

            AssertReferences(childDefinition);
        }

        [Test]
        public void PublicBuild_OfCollection_BuildsEveryBuilder()
        {
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(CreateBuilder(parentDefinition));

            // full build, with AssetDatabase.Refresh in the end
            CreateCollection(child).Build();

            AssertReferences(childDefinition, parentDefinition);
        }

        [Test]
        public void CountBuilders_CountsPlainBuilders()
        {
            var collection = CreateCollection(CreateBuilder(), CreateBuilder(), CreateBuilder());

            Assert.That(collection.CountBuilders(), Is.EqualTo(3));
        }

        [Test]
        public void CountBuilders_CountsBuildersOfNestedCollections()
        {
            var nested = CreateCollection(CreateBuilder(), CreateBuilder());
            var root = CreateCollection(CreateBuilder(), nested);

            // nested collection itself is not counted, only builders inside it
            Assert.That(root.CountBuilders(), Is.EqualTo(3));
        }

        [Test]
        public void CountBuilders_CountsSharedBuilderOnce()
        {
            var shared = CreateBuilder();
            var first = CreateCollection(CreateBuilder(), shared);
            var second = CreateCollection(shared, CreateBuilder());
            var root = CreateCollection(first, second);

            Assert.That(root.CountBuilders(), Is.EqualTo(3));
        }

        [Test]
        public void CountBuilders_CountsDuplicatedBuilderOnce()
        {
            var duplicated = CreateBuilder();
            var collection = CreateCollection(duplicated, duplicated);

            Assert.That(collection.CountBuilders(), Is.EqualTo(1));
        }

        [Test]
        public void CountBuilders_WithCyclicCollections_DoesNotHang()
        {
            var first = CreateCollection(CreateBuilder());
            var second = CreateCollection(CreateBuilder(), first);
            first._builders.Add(second);

            Assert.That(first.CountBuilders(), Is.EqualTo(2));
        }

        [Test]
        public void CountBuilders_OfEmptyCollection_IsZero()
        {
            Assert.That(CreateCollection().CountBuilders(), Is.EqualTo(0));
        }
    }
}
