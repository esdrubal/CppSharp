﻿using CppSharp;
using CppSharp.Passes;
using NUnit.Framework;

namespace Generator.Tests.Passes
{
    [TestFixture]
    public class TestPasses : HeaderTestFixture
    {
        private PassBuilder passBuilder;

        [TestFixtureSetUp]
        public void Init()
        {
        }

        [SetUp]
        public void Setup()
        {
            ParseLibrary("Passes.h");
            passBuilder = new PassBuilder(Driver);
        }

        [Test]
        public void TestCheckFlagEnumsPass()
        {
            var @enum = Library.Enum("FlagEnum");
            Assert.IsFalse(@enum.IsFlags);

            var @enum2 = Library.Enum("FlagEnum2");
            Assert.IsFalse(@enum2.IsFlags);

            passBuilder.CheckFlagEnums();
            passBuilder.RunPasses();

            Assert.IsTrue(@enum.IsFlags);
            Assert.IsFalse(@enum2.IsFlags);
        }

        [Test]
        public void TestFunctionToInstancePass()
        {
            var c = Library.Class("Foo");

            Assert.IsNull(c.Method("Start"));

            passBuilder.FunctionToInstanceMethod();
            passBuilder.RunPasses();

            Assert.IsNotNull(c.Method("Start"));
        }

        [Test]
        public void TestFunctionToStaticPass()
        {
            var c = Library.Class("Foo");

            Assert.IsFalse(Library.Function("FooStart").ExplicityIgnored);
            Assert.IsNull(c.Method("Start"));

            passBuilder.FunctionToStaticMethod();
            passBuilder.RunPasses();

            Assert.IsTrue(Library.Function("FooStart").ExplicityIgnored);
            Assert.IsNotNull(c.Method("Start"));
        }

        [Test]
        public void TestCaseRenamePass()
        {
            var c = Library.Class("TestRename");

            var method = c.Method("lowerCaseMethod");
            var field = c.Field("lowerCaseField");

            passBuilder.RenameDeclsUpperCase(RenameTargets.Any);
            passBuilder.RunPasses();

            Assert.That(method.Name, Is.EqualTo("LowerCaseMethod"));
            Assert.That(field.Name, Is.EqualTo("LowerCaseField"));
        }

        [Test]
        public void TestCleanEnumItemNames()
        {
            Library.GenerateEnumFromMacros("TestEnumItemName", "TEST_ENUM_ITEM_NAME_(.*)");

            var @enum = Library.Enum("TestEnumItemName");
            Assert.IsNotNull(@enum);

            passBuilder.RemovePrefix("TEST_ENUM_ITEM_NAME_", RenameTargets.EnumItem);
            passBuilder.CleanInvalidDeclNames();
            passBuilder.RunPasses();

            Assert.That(@enum.Items[0].Name, Is.EqualTo("_0"));
        }

        [Test]
        public void TestStructInheritance()
        {

        }
    }
}
