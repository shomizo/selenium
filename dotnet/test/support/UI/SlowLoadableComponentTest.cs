using System;
using NUnit.Framework;
using System.Threading.Tasks;

namespace OpenQA.Selenium.Support.UI
{
    [TestFixture]
    public class SlowLoadableComponentTest
    {

        [Test]
        public void TestShouldDoNothingIfComponentIsAlreadyLoaded()
        {
            try
            {
                Task.Run( async() => await new DetonatingSlowLoader().Load()).Wait();
            }
            catch (Exception)
            {
                Assert.Fail("Did not expect load to be called");
            }
        }

        [Test]
        public void TestShouldCauseTheLoadMethodToBeCalledIfTheComponentIsNotAlreadyLoaded()
        {
            int numberOfTimesThroughLoop = 1;
            SlowLoading slowLoading = new SlowLoading(TimeSpan.FromSeconds(1), new SystemClock(), numberOfTimesThroughLoop).Load().Result;

            Assert.AreEqual(numberOfTimesThroughLoop, slowLoading.GetLoopCount());
        }

        [Test]
        public void TestTheLoadMethodShouldOnlyBeCalledOnceIfTheComponentTakesALongTimeToLoad()
        {
            try
            {
                Task.Run(async () => await new OnlyOneLoad(TimeSpan.FromSeconds(5), new SystemClock(), 5).Load()).Wait();
            }
            catch (Exception)
            {
                Assert.Fail("Did not expect load to be called more than once");
            }
        }

        [Test]
        public void TestShouldThrowAnErrorIfCallingLoadDoesNotCauseTheComponentToLoadBeforeTimeout()
        {
            FakeClock clock = new FakeClock();
            try
            {
                Task.Run(async() => await new BasicSlowLoader(TimeSpan.FromSeconds(2), clock).Load()).Wait();
                Assert.Fail();
            }
            catch (WebDriverTimeoutException)
            {
                // We expect to time out
            }
        }

        [Test]
        public void TestShouldCancelLoadingIfAnErrorIsDetected()
        {
            HasError error = new HasError();

            try
            {
                Task.Run(async () => await error.Load()).Wait();
                Assert.Fail();
            }
            catch (CustomException)
            {
                // This is expected
            }
        }


        private class DetonatingSlowLoader : SlowLoadableComponent<DetonatingSlowLoader>
        {

            public DetonatingSlowLoader() : base(TimeSpan.FromSeconds(1), new SystemClock()) { }

            protected override void ExecuteLoad()
            {
                throw new Exception("Should never be called");
            }

            protected override bool EvaluateLoadedStatus()
            {
                return true;
            }
        }

        private class SlowLoading : SlowLoadableComponent<SlowLoading>
        {

            private readonly int counts;
            private long loopCount;

            public SlowLoading(TimeSpan timeOut, SystemClock clock, int counts)
                : base(timeOut, clock)
            {
                this.counts = counts;
            }

            protected override void ExecuteLoad()
            {
                // Does nothing
            }

            protected override bool EvaluateLoadedStatus()
            {
                if (loopCount > counts)
                {
                    throw new Exception();
                }

                loopCount++;
                return true;
            }

            public long GetLoopCount()
            {
                return loopCount;
            }
        }

        private class OnlyOneLoad : SlowLoading
        {

            private bool loadAlreadyCalled;

            public OnlyOneLoad(TimeSpan timeout, SystemClock clock, int counts) : base(timeout, clock, counts) { }

            protected override void ExecuteLoad()
            {
                if (loadAlreadyCalled)
                {
                    throw new Exception();
                }
                loadAlreadyCalled = true;
            }
        }

        private class BasicSlowLoader : SlowLoadableComponent<BasicSlowLoader>
        {

            private readonly FakeClock clock;
            public BasicSlowLoader(TimeSpan timeOut, FakeClock clock)
                : base(timeOut, clock)
            {
                this.clock = clock;
            }

            protected override void ExecuteLoad()
            {
                // Does nothing
            }

            protected override bool EvaluateLoadedStatus()
            {
                // Cheat and increment the clock here, because otherwise it's hard to
                // get to.
                clock.TimePasses(TimeSpan.FromSeconds(1));
                return false; // Never loads
            }
        }

        private class HasError : SlowLoadableComponent<HasError>
        {

            public HasError() : base(TimeSpan.FromSeconds(1000), new FakeClock()) { }

            protected override void ExecuteLoad()
            {
                // does nothing
            }

            protected override bool EvaluateLoadedStatus()
            {
                return false;       //never loads
            }

            protected override void HandleErrors()
            {
                throw new CustomException();
            }
        }

        private class CustomException : Exception
        {

        }
    }

}
