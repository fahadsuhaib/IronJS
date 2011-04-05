// <auto-generated />
namespace IronJS.Tests.UnitTests.Sputnik.Conformance.NativeECMAScriptObjects.NumberObjects.PropertiesOfNumberConstructor
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class NumberNEGATIVEINFINITYTests : SputnikTestFixture
    {
        public NumberNEGATIVEINFINITYTests()
            : base(@"Conformance\15_Native_ECMA_Script_Objects\15.7_Number_Objects\15.7.3_Properties_of_Number_Constructor\15.7.3.5_Number.NEGATIVE_INFINITY")
        {
        }

        [Test]
        [Category("Sputnik Conformance")]
        [Category("ECMA 15.7.3.5")]
        [TestCase("S15.7.3.5_A1.js", Description = "Number.NEGATIVE_INFINITY is -Infinity")]
        public void NumberNEGATIVE_INFINITYIsInfinity(string file)
        {
            RunFile(file);
        }

        [Test]
        [Category("Sputnik Conformance")]
        [Category("ECMA 15.7.3.5")]
        [TestCase("S15.7.3.5_A2.js", Description = "Number.NEGATIVE_INFINITY is ReadOnly")]
        public void NumberNEGATIVE_INFINITYIsReadOnly(string file)
        {
            RunFile(file);
        }

        [Test]
        [Category("Sputnik Conformance")]
        [Category("ECMA 15.7.3.5")]
        [TestCase("S15.7.3.5_A3.js", Description = "Number.NEGATIVE_INFINITY is DontDelete")]
        public void NumberNEGATIVE_INFINITYIsDontDelete(string file)
        {
            RunFile(file);
        }

        [Test]
        [Category("Sputnik Conformance")]
        [Category("ECMA 15.7.3.5")]
        [TestCase("S15.7.3.5_A4.js", Description = "Number.NEGATIVE_INFINITY has the attribute DontEnum")]
        public void NumberNEGATIVE_INFINITYHasTheAttributeDontEnum(string file)
        {
            RunFile(file);
        }
    }
}