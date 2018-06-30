using System.Collections.Generic;
using System.Linq;

using Microsoft.ProgramSynthesis.Matching.Text;
using Microsoft.ProgramSynthesis.Wrangling.Constraints;
using NUnit.Framework;

namespace FlashProfileDemo {
    using Constraint = Constraint<string, bool>;

    [TestFixture]
    public static class ProfilingTests {
        [Test]
        [TestCaseSource(typeof(TestCase), nameof(TestCase.LoadTestCasesFromRelativeDir), new object[] { "tests/homo" })]
        [TestCaseSource(typeof(TestCase), nameof(TestCase.LoadTestCasesFromRelativeDir), new object[] { "tests/hetero" })]
        [TestCaseSource(typeof(TestCase), nameof(TestCase.LoadTestCasesFromRelativeDir), new object[] { "tests/homo.simple" })]
        public static void TestBestDescription(List<Constraint> constraints, List<string> expectedResult, string info) {
            TestContext.Write(info);
            IEnumerable<string> description = Learner.Instance.Learn(constraints)?.Description() ?? Enumerable.Empty<string>();
            CollectionAssert.AreEquivalent(expectedResult, description);
        }
    }

}