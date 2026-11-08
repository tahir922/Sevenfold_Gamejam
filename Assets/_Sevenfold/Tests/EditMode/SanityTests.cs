using NUnit.Framework;

namespace Sevenfold.Tests
{
    public class SanityTests
    {
        [Test]
        public void SanityCheck_OnePlusOneEqualsTwo()
        {
            Assert.AreEqual(2, 1 + 1);
        }
    }
}
