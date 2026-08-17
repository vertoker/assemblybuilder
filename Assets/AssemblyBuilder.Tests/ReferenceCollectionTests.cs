using System.Collections.Generic;
using AssemblyBuilder.Model;
using NUnit.Framework;
using UnityEditor;

namespace AssemblyBuilder.Tests
{
    /// <summary>
    /// Main functional of package - collecting references from hierarchy of builders
    /// </summary>
    [TestFixture]
    public class ReferenceCollectionTests : AssemblyBuilderTestFixture
    {
        [Test]
        public void BuilderWithoutParents_HasNoReferences()
        {
            var childDefinition = CreateDefinition("Child");
            var child = CreateBuilder(childDefinition);

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition);
        }

        [Test]
        public void NoInherit_ClearsReferencesField()
        {
            var parentDefinition = CreateDefinition("Parent");
            // reference of definition, which is not a parent, must be removed by build
            var foreignDefinition = CreateDefinition("Foreign");
            var childDefinition = CreateDefinition("Child", new AssemblyDefinitionModel
            {
                references = new List<string> { Reference(foreignDefinition) },
            });

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(CreateBuilder(parentDefinition));
            child._inheritMode = AssemblyInheritMode.NoInherit;

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition);
        }

        [Test]
        public void Inherit_CollectsOnlyNearestPublicParents()
        {
            var grandDefinition = CreateDefinition("Grand");
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var parent = CreateBuilder(parentDefinition);
            parent._publicParents.Add(CreateBuilder(grandDefinition));

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(parent);
            child._inheritMode = AssemblyInheritMode.Inherit;

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition);
        }

        [Test]
        public void Inherit_CollectsPublicAndPrivateParents()
        {
            var publicDefinition = CreateDefinition("Public");
            var privateDefinition = CreateDefinition("Private");
            var childDefinition = CreateDefinition("Child");

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(CreateBuilder(publicDefinition));
            child._privateParents.Add(CreateBuilder(privateDefinition));
            child._inheritMode = AssemblyInheritMode.Inherit;

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, publicDefinition, privateDefinition);
        }

        [Test]
        public void DeepInherit_CollectsWholeHierarchy()
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
            child._inheritMode = AssemblyInheritMode.DeepInherit;

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition, grandDefinition, greatDefinition);
        }

        [Test]
        public void DeepInherit_IsDefaultMode()
        {
            var grandDefinition = CreateDefinition("Grand");
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var parent = CreateBuilder(parentDefinition);
            parent._publicParents.Add(CreateBuilder(grandDefinition));

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(parent);

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition, grandDefinition);
        }

        [Test]
        public void DeepInherit_SkipsPrivateParentsOfParents()
        {
            var grandPublicDefinition = CreateDefinition("GrandPublic");
            var grandPrivateDefinition = CreateDefinition("GrandPrivate");
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var parent = CreateBuilder(parentDefinition);
            parent._publicParents.Add(CreateBuilder(grandPublicDefinition));
            parent._privateParents.Add(CreateBuilder(grandPrivateDefinition));

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(parent);

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition, grandPublicDefinition);
        }

        [Test]
        public void DeepInherit_CollectsPublicParentsOfOwnPrivateParent()
        {
            var grandDefinition = CreateDefinition("Grand");
            var privateDefinition = CreateDefinition("Private");
            var childDefinition = CreateDefinition("Child");

            var privateParent = CreateBuilder(privateDefinition);
            privateParent._publicParents.Add(CreateBuilder(grandDefinition));

            var child = CreateBuilder(childDefinition);
            child._privateParents.Add(privateParent);

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, privateDefinition, grandDefinition);
        }

        [Test]
        public void DeepInherit_IgnoresInheritModeOfParents()
        {
            // hierarchy must be at least 4 layers deep,
            // mode of parent used to cut exactly one layer of grandparents
            var greatDefinition = CreateDefinition("Great");
            var grandDefinition = CreateDefinition("Grand");
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var grand = CreateBuilder(grandDefinition);
            grand._publicParents.Add(CreateBuilder(greatDefinition));

            var parent = CreateBuilder(parentDefinition);
            parent._publicParents.Add(grand);
            parent._inheritMode = AssemblyInheritMode.NoInherit;

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(parent);
            child._inheritMode = AssemblyInheritMode.DeepInherit;

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition, grandDefinition, greatDefinition);
        }

        [Test]
        public void NoInherit_OfParent_DoesNotClearReferencesOfChild()
        {
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var parent = CreateBuilder(parentDefinition);
            parent._inheritMode = AssemblyInheritMode.NoInherit;

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(parent);

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, parentDefinition);
        }

        [Test]
        public void DeepInherit_CollectsDiamondWithoutDuplicates()
        {
            var topDefinition = CreateDefinition("Top");
            var leftDefinition = CreateDefinition("Left");
            var rightDefinition = CreateDefinition("Right");
            var childDefinition = CreateDefinition("Child");

            var top = CreateBuilder(topDefinition);

            var left = CreateBuilder(leftDefinition);
            left._publicParents.Add(top);

            var right = CreateBuilder(rightDefinition);
            right._publicParents.Add(top);

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(left);
            child._publicParents.Add(right);

            BuildWithoutRefresh(child);

            // AreEquivalent compares amount of every element, so duplicated Top fails test
            AssertReferences(childDefinition, leftDefinition, rightDefinition, topDefinition);
        }

        [Test]
        public void BuilderWithoutDefinitions_WorksAsGroupOfParents()
        {
            var firstDefinition = CreateDefinition("First");
            var secondDefinition = CreateDefinition("Second");
            var childDefinition = CreateDefinition("Child");

            var group = CreateBuilder();
            group._publicParents.Add(CreateBuilder(firstDefinition));
            group._publicParents.Add(CreateBuilder(secondDefinition));

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(group);

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, firstDefinition, secondDefinition);
        }

        [Test]
        public void SeveralDefinitions_GetSameReferences()
        {
            var parentDefinition = CreateDefinition("Parent");
            var firstDefinition = CreateDefinition("First");
            var secondDefinition = CreateDefinition("Second");

            var child = CreateBuilder(firstDefinition, secondDefinition);
            child._publicParents.Add(CreateBuilder(parentDefinition));

            BuildWithoutRefresh(child);

            AssertReferences(firstDefinition, parentDefinition);
            AssertReferences(secondDefinition, parentDefinition);
        }

        [Test]
        public void ParentWithSeveralDefinitions_GivesEveryDefinition()
        {
            var firstDefinition = CreateDefinition("First");
            var secondDefinition = CreateDefinition("Second");
            var childDefinition = CreateDefinition("Child");

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(CreateBuilder(firstDefinition, secondDefinition));

            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, firstDefinition, secondDefinition);
        }

        [Test]
        public void Reference_IsWrittenInGuidFormat()
        {
            var parentDefinition = CreateDefinition("Parent");
            var childDefinition = CreateDefinition("Child");

            var child = CreateBuilder(childDefinition);
            child._publicParents.Add(CreateBuilder(parentDefinition));

            BuildWithoutRefresh(child);

            var expectedGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(parentDefinition));
            var references = ReadModel(childDefinition).references;

            Assert.That(references, Has.Count.EqualTo(1));
            Assert.That(references[0], Is.EqualTo("GUID:" + expectedGuid));
            Assert.That(expectedGuid, Is.Not.Empty);
        }
    }
}
