using System.Collections.Generic;
using System.Text.RegularExpressions;
using AssemblyBuilder.Model;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AssemblyBuilder.Tests
{
    /// <summary>
    /// Parents are a part of build hierarchy: a child can't be up to date while it's parent is not,
    /// so build goes up through parents and rewrites every one of them
    /// </summary>
    [TestFixture]
    public class RecursiveBuildTests : AssemblyBuilderTestFixture
    {
        private static readonly Regex CyclicError = new("Cyclic parent reference");

        [Test]
        public void Build_BuildsPublicParents()
        {
            var grandDefinition = CreateDefinition("Grand");
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var parent = CreateBuilder(parentDefinition);
            parent._publicParents.Add(CreateBuilder(grandDefinition));

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(parent);

            BuildWithoutRefresh(child);

            AssertReferences(parentDefinition, grandDefinition);
        }

        [Test]
        public void Build_BuildsPrivateParents()
        {
            var grandDefinition = CreateDefinition("Grand");
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var parent = CreateBuilder(parentDefinition);
            parent._publicParents.Add(CreateBuilder(grandDefinition));

            var child = CreateBuilder(childDefinition);
            child._privateParents.Add(parent);

            BuildWithoutRefresh(child);

            AssertReferences(parentDefinition, grandDefinition);
        }

        [Test]
        public void Build_BuildsParentsThroughWholeDepth()
        {
            var greatDefinition = CreateDefinition("Great");
            var grandDefinition = CreateDefinition("Grand");
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var grand = CreateBuilder(grandDefinition);
            grand._publicParents.Add(CreateBuilder(greatDefinition));

            var parent = CreateBuilder(parentDefinition);
            parent._publicParents.Add(grand);

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(parent);

            BuildWithoutRefresh(child);

            AssertReferences(grandDefinition, greatDefinition);
            AssertReferences(parentDefinition, grandDefinition, greatDefinition);
            AssertReferences(childDefinition, parentDefinition, grandDefinition, greatDefinition);
        }

        [Test]
        public void Build_BuildsBuildersOfCollectionInParents()
        {
            var grandDefinition = CreateDefinition("Grand");
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var parent = CreateBuilder(parentDefinition);
            parent._publicParents.Add(CreateBuilder(grandDefinition));

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(CreateCollection(parent));

            BuildWithoutRefresh(child);

            AssertReferences(parentDefinition, grandDefinition);
        }

        [Test]
        public void Build_OfCollection_BuildsParentsOfItsBuilders()
        {
            var grandDefinition = CreateDefinition("Grand");
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var parent = CreateBuilder(parentDefinition);
            parent._publicParents.Add(CreateBuilder(grandDefinition));

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(parent);

            BuildWithoutRefresh(CreateCollection(child));

            AssertReferences(parentDefinition, grandDefinition);
        }

        [Test]
        public void Build_BuildsEveryBuilderOfDiamondOnce()
        {
            var top = CreateBuilder(CreateDefinition("Top"));

            var left = CreateBuilder(CreateDefinition("Left"));
            left._publicParents.Add(top);

            var right = CreateBuilder(CreateDefinition("Right"));
            right._publicParents.Add(top);

            var child = CreateBuilder(CreateDefinition("Child"));
            child._publicParents.Add(left);
            child._publicParents.Add(right);

            var visited = new HashSet<BaseAssemblyBuilder>();
            child.BuildInternal(visited);

            CollectionAssert.AreEquivalent(new BaseAssemblyBuilder[] { child, left, right, top }, visited);
        }

        [Test]
        public void Build_OfReadonlyBuilder_StillBuildsItsParents()
        {
            var grandDefinition = CreateDefinition("Grand");
            var parentDefinition = CreateDefinition("Parent");
            var readonlyDefinition = CreateDefinition("Readonly");

            var parent = CreateBuilder(parentDefinition);
            parent._publicParents.Add(CreateBuilder(grandDefinition));

            var readonlyBuilder = CreateBuilder(readonlyDefinition);
            readonlyBuilder._publicParents.Add(parent);
            readonlyBuilder._readonly = true;

            var textBeforeBuild = ReadText(readonlyDefinition);
            BuildWithoutRefresh(readonlyBuilder);

            Assert.That(ReadText(readonlyDefinition), Is.EqualTo(textBeforeBuild), "readonly file is rewritten");
            AssertReferences(parentDefinition, grandDefinition);
        }

        [Test]
        public void Build_WithCyclicParents_DoesNotHangAndBuildsBothSides()
        {
            var firstDefinition = CreateDefinition("First");
            var secondDefinition = CreateDefinition("Second");

            var first = CreateBuilder(firstDefinition);
            var second = CreateBuilder(secondDefinition);
            first._publicParents.Add(second);
            second._publicParents.Add(first);

            // one error per builder of the cycle, every one of them collects references for itself
            LogAssert.Expect(LogType.Error, CyclicError);
            LogAssert.Expect(LogType.Error, CyclicError);
            BuildWithoutRefresh(first);

            AssertReferences(firstDefinition, secondDefinition);
            AssertReferences(secondDefinition, firstDefinition);
        }

        [Test]
        public void Build_DoesNotBuildChildrenOfBuilder()
        {
            var foreignDefinition = CreateDefinition("Foreign");
            var parentDefinition = CreateDefinition("Parent");
            // child keeps a reference, which build would wipe, so untouched file is visible
            var childDefinition = CreateDefinition("Child", new AssemblyDefinitionModel
            {
                references = new List<string> { Reference(foreignDefinition) },
            });

            var parent = CreateBuilder(parentDefinition);

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(parent);

            BuildWithoutRefresh(parent);

            AssertReferences(childDefinition, foreignDefinition);
        }

        [Test]
        public void NullParent_DoesNotBreakRecursiveBuild()
        {
            var grandDefinition = CreateDefinition("Grand");
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var parent = CreateBuilder(parentDefinition);
            parent._publicParents.Add(CreateBuilder(grandDefinition));

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(null);
            child._publicParents.Add(parent);
            child._privateParents.Add(null);

            BuildWithoutRefresh(child);

            AssertReferences(parentDefinition, grandDefinition);
        }
    }
}
