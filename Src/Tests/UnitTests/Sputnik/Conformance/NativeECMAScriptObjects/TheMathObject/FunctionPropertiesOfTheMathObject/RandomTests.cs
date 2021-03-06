// <auto-generated />
namespace IronJS.Tests.UnitTests.Sputnik.Conformance.NativeECMAScriptObjects.TheMathObject.FunctionPropertiesOfTheMathObject
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class RandomTests : SputnikTestFixture
    {
        public RandomTests()
            : base(@"Conformance\15_Native_ECMA_Script_Objects\15.8_The_Math_Object\15.8.2_Function_Properties_of_the_Math_Object\15.8.2.14_random")
        {
        }

        [Test]
        [Category("Sputnik Conformance")]
        [Category("ECMA 15.8.2.14")]
        [TestCase("S15.8.2.14_A1.js", Description = "Math.random() returns a number value with positive sign, greater than or equal to 0 but less than 1")]
        public void MathRandomReturnsANumberValueWithPositiveSignGreaterThanOrEqualTo0ButLessThan1(string file)
        {
            RunFile(file);
        }
    }
}