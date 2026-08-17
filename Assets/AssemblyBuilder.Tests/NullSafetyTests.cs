using System.Linq;
using NUnit.Framework;

namespace AssemblyBuilder.Tests
{
    /// <summary>
    /// Empty slots in lists are a normal state of any Unity inspector,
    /// they must not break build of whole hierarchy
    /// </summary>
    [TestFixture]
    public class NullSafetyTests : AssemblyBuilderTestFixture
    {
        [Test]
        public void NullDefinition_IsSkipped()
        {
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var child = CreateBuilder(childDefinition);
            child._definitions.Insert(0, null);
            child._publicParents.Add(CreateBuilder(parentDefinition));

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition);
        }

        [Test]
        public void NullPublicParent_IsSkipped()
        {
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(null);
            child._publicParents.Add(CreateBuilder(parentDefinition));

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition);
        }

        [Test]
        public void NullPrivateParent_IsSkipped()
        {
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var child = CreateBuilder(childDefinition);
            child._privateParents.Add(null);
            child._privateParents.Add(CreateBuilder(parentDefinition));

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition);
        }

        [Test]
        public void NullParent_InsideHierarchy_IsSkipped()
        {
            var grandDefinition = CreateDefinition("Grand");
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var parent = CreateBuilder(parentDefinition);
            parent._publicParents.Add(null);
            parent._publicParents.Add(CreateBuilder(grandDefinition));

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(parent);

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition, grandDefinition);
        }

        [Test]
        public void NullDefinitionOfParent_DoesNotCreateBrokenReference()
        {
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var parent = CreateBuilder(parentDefinition);
            parent._definitions.Insert(0, null);

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(parent);

            BuildWithoutRefresh(child);

            var references = ReadModel(childDefinition).references;
            Assert.That(references.Any(reference => reference == "GUID:"), Is.False,
                "broken reference is written into .asmdef");
            AssertReferences(childDefinition, parentDefinition);
        }

        [Test]
        public void BuilderWithOnlyNullDefinitions_DoesNothing()
        {
            var childDefinition = CreateDefinition("Child");

            var emptyBuilder = CreateBuilder();
            emptyBuilder._definitions.Add(null);

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(emptyBuilder);

            BuildWithoutRefresh(emptyBuilder);
            BuildWithoutRefresh(child);

            AssertReferences(childDefinition);
        }

        [Test]
        public void NullBuilder_InsideCollection_IsSkipped()
        {
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(CreateBuilder(parentDefinition));

            var collection = CreateCollection(null, child);
            BuildWithoutRefresh(collection);

            AssertReferences(childDefinition, parentDefinition);
        }

        [Test]
        public void NullBuilder_InsideCollection_IsNotCounted()
        {
            var collection = CreateCollection(null, CreateBuilder(), null);

            Assert.That(collection.CountBuilders(), Is.EqualTo(1));
        }
    }
}
