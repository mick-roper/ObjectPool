using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ObjectPool.Tests
{
    [TestClass]
    public class PoolTests
    {
        [TestMethod]
        public void Pool_is_sealed()
        {
            var isSealed = typeof(Pool<>).IsSealed;

            Assert.IsTrue(isSealed);
        }

        [TestMethod]
        public void Pool_implements_IDisposable()
        {
            var implements = typeof(IDisposable).IsAssignableFrom(typeof(Pool<>));

            Assert.IsTrue(implements);
        }
    }
}
