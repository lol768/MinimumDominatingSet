using NUnit.Framework;
using Shouldly;

namespace MinimumDominatingSet.Tests
{
    [TestFixture]
    public class AutomatedTests
    {

        /*
         * Key to test diagrams:
         *  (n) Nth node.
         *  <n> Nth node, known to appear in a min-size dominating set.
         */

        [Test]
        public void TestExampleProblemOne()
        {

            /*
             <1>      <3>
                     /   \
                   <5>   [2]
                   / \
                 [6] [4]
            */

            var arr = new int?[] {null, 2, null, 4, 2, 4};
            var ds = new MdsFinder(arr).ComputeSet();
            ds.Length.ShouldBe(3);
        }

        /// <summary>
        /// This one broke the original algorithm.
        /// </summary>
        [Test]
        public void TestCounterExample()
        {

            /*  [1]
                 |
                <2>
                 |
                [3]
                 |
                [4]
                 |
                <5>
                 |
                [6] */

            var arr = new int?[] {null, 0, 1, 2, 3, 4};

            var ds = new MdsFinder(arr).ComputeSet();
            ds.Length.ShouldBe(2);
        }

        [Test]
        public void TestExampleProblemTwo()
        {

            /*  [1]
                 |
                <2>
                 |
                [3]
                 |
                <4>
                 |
                [5] */

            var arr = new int?[] {null, 0, 1, 2, 3};

            var ds = new MdsFinder(arr).ComputeSet();
            ds.Length.ShouldBe(2);
        }

        [Test]
        public void TestExampleProblemThree()
        {

            /* <1> <2> <3> <4> <5> */

            var arr = new int?[] {null, null, null, null, null};

            var ds = new MdsFinder(arr).ComputeSet();
            ds.Length.ShouldBe(5);
        }

        [Test]
        public void TestExampleProblemFour()
        {

            /* [6]----<1>-----[5]
                      /|\
                     / | \
                    /  |  \
                   /   |   \
                 <2>   |   <4>
                 / \  [3]  / \
               [7] [8]   [9] [10] */

            var arr = new int?[] {null, 0, 0, 0, 0, 0, 1, 1, 3, 3};

            var ds = new MdsFinder(arr).ComputeSet();
            ds.Length.ShouldBe(3);
        }

    }
}