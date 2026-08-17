using System.Collections.Generic;
using NUnit.Framework;

namespace AssemblyBuilder.Tests
{
    /// <summary>
    /// Build All builds only roots of hierarchy, everything else is reached through collections.
    /// Roots must cover every builder of project, even the ones locked in a cycle of collections
    /// </summary>
    [TestFixture]
    public class RootCollectionTests : AssemblyBuilderTestFixture
    {
        [Test]
        public void StandaloneBuilders_AreAllRoots()
        {
            var first = CreateBuilder();
            var second = CreateBuilder();

            var roots = CollectRoots(first, second);

            CollectionAssert.AreEquivalent(new BaseAssemblyBuilder[] { first, second }, roots);
        }

        [Test]
        public void BuilderInsideCollection_IsNotRoot()
        {
            var builder = CreateBuilder();
            var collection = CreateCollection(builder);

            var roots = CollectRoots(builder, collection);

            CollectionAssert.AreEquivalent(new BaseAssemblyBuilder[] { collection }, roots);
        }

        [Test]
        public void NestedCollections_LeaveOnlyTopOneAsRoot()
        {
            var builder = CreateBuilder();
            var inner = CreateCollection(builder);
            var outer = CreateCollection(inner);

            var roots = CollectRoots(builder, inner, outer);

            CollectionAssert.AreEquivalent(new BaseAssemblyBuilder[] { outer }, roots);
        }

        [Test]
        public void SharedBuilder_LeavesBothCollectionsAsRoots()
        {
            var builder = CreateBuilder();
            var first = CreateCollection(builder);
            var second = CreateCollection(builder);

            var roots = CollectRoots(builder, first, second);

            CollectionAssert.AreEquivalent(new BaseAssemblyBuilder[] { first, second }, roots);
        }

        /// <summary>
        /// Parents give references, they are not a part of build hierarchy,
        /// so a parent is not built through it's child and needs it's own root
        /// </summary>
        [Test]
        public void Parent_StaysRoot()
        {
            var parent = CreateBuilder();
            var child = CreateBuilder();
            child._publicParents.Add(parent);

            var roots = CollectRoots(parent, child);

            CollectionAssert.AreEquivalent(new BaseAssemblyBuilder[] { parent, child }, roots);
        }

        [Test]
        public void SelfReferencingCollection_StaysRoot()
        {
            var collection = CreateCollection();
            collection._builders.Add(collection);

            var roots = CollectRoots(collection);

            CollectionAssert.AreEquivalent(new BaseAssemblyBuilder[] { collection }, roots);
        }

        /// <summary>
        /// Cycle of collections has no builder outside of it, so nothing is a root by reference rule.
        /// One of them is picked anyway, otherwise whole branch is never built
        /// </summary>
        [Test]
        public void CollectionCycle_WithoutEntryPoint_GetsRoot()
        {
            var builder = CreateBuilder();
            var first = CreateCollection(builder);
            var second = CreateCollection(first);
            first._builders.Add(second);

            var roots = CollectRoots(builder, first, second);

            Assert.That(roots, Has.Count.EqualTo(1), "cycle of collections must give exactly one root");
            CollectionAssert.Contains(new BaseAssemblyBuilder[] { first, second }, roots[0]);
        }

        [Test]
        public void CollectionCycle_WithoutEntryPoint_DoesNotHangBuild()
        {
            var definition = CreateDefinition("Child");

            var builder = CreateBuilder(definition);
            var first = CreateCollection(builder);
            var second = CreateCollection(first);
            first._builders.Add(second);

            var roots = CollectRoots(builder, first, second);
            var visited = new HashSet<BaseAssemblyBuilder>();

            foreach (var root in roots)
            {
                root.BuildInternal(visited);
            }

            Assert.That(visited, Contains.Item(builder), "builder locked in cycle of collections is not built");
        }

        private static List<BaseAssemblyBuilder> CollectRoots(params BaseAssemblyBuilder[] builders)
        {
            return AssemblyBuilderMenu.CollectRoots(builders);
        }
    }
}
