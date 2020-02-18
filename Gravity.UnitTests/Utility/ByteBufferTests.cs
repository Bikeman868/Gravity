using Gravity.Server.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravity.UnitTests.Utility
{
    [TestFixture]
    public class ByteBufferTests: Moq.Modules.TestBase
    {
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void Should_calculate_properties()
        {
            const int size = 10;
            var bb = new ByteBuffer(new byte[size]);

            Assert.IsNotNull(bb.Data);
            Assert.AreEqual(size, bb.Data.Length);
            Assert.AreEqual(size, bb.Size);
            Assert.AreEqual(0, bb.Start);
            Assert.AreEqual(size, bb.End);
            Assert.AreEqual(size, bb.Length);
        }

        [Test]
        public void Should_append_up_to_size()
        {
            const int size = 10;
            var bb = new ByteBuffer(new byte[size], 0);

            Assert.AreEqual(size, bb.Size);
            Assert.AreEqual(0, bb.Length);

            Assert.IsTrue(bb.CanAppend(1));
            Assert.IsTrue(bb.CanAppend(size));
            Assert.IsFalse(bb.CanAppend(size+1));

            bb.Append(new byte[] { 1, 2, 3 }, 0, 3);

            Assert.AreEqual(size, bb.Size);
            Assert.AreEqual(3, bb.Length);
            Assert.AreEqual(0, bb.Start);
            Assert.AreEqual(3, bb.End);

            Assert.IsTrue(bb.CanAppend(1));
            Assert.IsTrue(bb.CanAppend(size - 3));
            Assert.IsFalse(bb.CanAppend(size - 2));

            bb.Append(new byte[] { 4, 5, 6 }, 0, 3);

            Assert.AreEqual(size, bb.Size);
            Assert.AreEqual(6, bb.Length);
            Assert.AreEqual(0, bb.Start);
            Assert.AreEqual(6, bb.End);

            var dest = new byte[6];
            bb.Get(0, 6, dest, 0);

            Assert.AreEqual(1, dest[0]);
            Assert.AreEqual(2, dest[1]);
            Assert.AreEqual(3, dest[2]);
            Assert.AreEqual(4, dest[3]);
            Assert.AreEqual(5, dest[4]);
            Assert.AreEqual(6, dest[5]);
        }

        [Test]
        public void Should_prepend_up_to_size()
        {
            const int size = 10;
            var bb = new ByteBuffer(new byte[size], 0);

            Assert.AreEqual(size, bb.Size);
            Assert.AreEqual(0, bb.Length);

            Assert.IsTrue(bb.CanPrepend(1));
            Assert.IsTrue(bb.CanPrepend(size));
            Assert.IsFalse(bb.CanPrepend(size+1));

            bb.Prepend(new byte[] { 4, 5, 6 }, 0, 3);

            Assert.AreEqual(size, bb.Size);
            Assert.AreEqual(3, bb.Length);
            Assert.AreEqual(size - 3, bb.Start);
            Assert.AreEqual(size, bb.End);

            Assert.IsTrue(bb.CanPrepend(1));
            Assert.IsTrue(bb.CanPrepend(size - 3));
            Assert.IsFalse(bb.CanPrepend(size - 2));

            bb.Prepend(new byte[] { 1, 2, 3 }, 0, 3);

            Assert.AreEqual(size, bb.Size);
            Assert.AreEqual(6, bb.Length);
            Assert.AreEqual(size - 6, bb.Start);
            Assert.AreEqual(size, bb.End);

            var dest = new byte[6];
            bb.Get(0, 6, dest, 0);

            Assert.AreEqual(1, dest[0]);
            Assert.AreEqual(2, dest[1]);
            Assert.AreEqual(3, dest[2]);
            Assert.AreEqual(4, dest[3]);
            Assert.AreEqual(5, dest[4]);
            Assert.AreEqual(6, dest[5]);
        }
    }
}
