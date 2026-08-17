using System.Text.RegularExpressions;
using AssemblyBuilder.Model;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AssemblyBuilder.Tests
{
    /// <summary>
    /// Cyclic hierarchy is a mistake of user, but it must not hang or crash editor.
    /// Builder writes error into console and stops inheritance on this branch
    /// </summary>
    [TestFixture]
    public class CyclicHierarchyTests : AssemblyBuilderTestFixture
    {
        private static readonly Regex CyclicError = new("Cyclic parent reference");

        [Test]
        public void DirectCycle_StopsAndReportsError()
        {
            var firstDefinition = CreateDefinition("First");
            var secondDefinition = CreateDefinition("Second");

            var first = CreateBuilder(firstDefinition);
            var second = CreateBuilder(secondDefinition);
            first._publicParents.Add(second);
            second._publicParents.Add(first);

            LogAssert.Expect(LogType.Error, CyclicError);
            BuildWithoutRefresh(first);

            AssertReferences(firstDefinition, secondDefinition);
        }

        [Test]
        public void DirectCycle_BuildsBothSides()
        {
            var firstDefinition = CreateDefinition("First");
            var secondDefinition = CreateDefinition("Second");

            var first = CreateBuilder(firstDefinition);
            var second = CreateBuilder(secondDefinition);
            first._publicParents.Add(second);
            second._publicParents.Add(first);

            LogAssert.Expect(LogType.Error, CyclicError);
            BuildWithoutRefresh(first);
            LogAssert.Expect(LogType.Error, CyclicError);
            BuildWithoutRefresh(second);

            AssertReferences(firstDefinition, secondDefinition);
            AssertReferences(secondDefinition, firstDefinition);
        }

        [Test]
        public void SelfParent_StopsAndReportsError()
        {
            var definition = CreateDefinition("Self");

            var builder = CreateBuilder(definition);
            builder._publicParents.Add(builder);

            LogAssert.Expect(LogType.Error, CyclicError);
            BuildWithoutRefresh(builder);

            AssertReferences(definition);
        }

        [Test]
        public void LongCycle_StopsAndReportsError()
        {
            var firstDefinition = CreateDefinition("First");
            var secondDefinition = CreateDefinition("Second");
            var thirdDefinition = CreateDefinition("Third");

            var first = CreateBuilder(firstDefinition);
            var second = CreateBuilder(secondDefinition);
            var third = CreateBuilder(thirdDefinition);

            first._publicParents.Add(second);
            second._publicParents.Add(third);
            third._publicParents.Add(first);

            LogAssert.Expect(LogType.Error, CyclicError);
            BuildWithoutRefresh(first);

            AssertReferences(firstDefinition, secondDefinition, thirdDefinition);
        }

        [Test]
        public void PrivateCycle_StopsAndReportsError()
        {
            var firstDefinition = CreateDefinition("First");
            var secondDefinition = CreateDefinition("Second");

            var first = CreateBuilder(firstDefinition);
            var second = CreateBuilder(secondDefinition);
            first._privateParents.Add(second);
            second._publicParents.Add(first);

            LogAssert.Expect(LogType.Error, CyclicError);
            BuildWithoutRefresh(first);

            AssertReferences(firstDefinition, secondDefinition);
        }

        [Test]
        public void Cycle_DoesNotAddDefinitionOfBuilderIntoItsOwnReferences()
        {
            var firstDefinition = CreateDefinition("First");
            var secondDefinition = CreateDefinition("Second");
            var extraDefinition = CreateDefinition("Extra");

            // builder with two definitions checks that every one of them is excluded
            var first = CreateBuilder(firstDefinition, extraDefinition);
            var second = CreateBuilder(secondDefinition);
            first._publicParents.Add(second);
            second._publicParents.Add(first);

            LogAssert.Expect(LogType.Error, CyclicError);
            LogAssert.Expect(LogType.Error, CyclicError);
            BuildWithoutRefresh(first);

            // references are collected for every definition, so error is reported twice
            AssertReferences(firstDefinition, secondDefinition);
            AssertReferences(extraDefinition, secondDefinition);
        }

        [Test]
        public void Cycle_WithInherit_DoesNotReportError()
        {
            var firstDefinition = CreateDefinition("First");
            var secondDefinition = CreateDefinition("Second");

            var first = CreateBuilder(firstDefinition);
            var second = CreateBuilder(secondDefinition);
            first._publicParents.Add(second);
            second._publicParents.Add(first);
            first._inheritMode = AssemblyInheritMode.Inherit;

            // without recursion there is nothing to cut, unexpected error fails this test
            BuildWithoutRefresh(first);

            AssertReferences(firstDefinition, secondDefinition);
        }

        [Test]
        public void Diamond_IsNotReportedAsCycle()
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

            // same builder through two branches is not a cycle, unexpected error fails this test
            BuildWithoutRefresh(child);

            AssertReferences(childDefinition, leftDefinition, rightDefinition, topDefinition);
        }
    }
}
