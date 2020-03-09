using Gravity.Server.Interfaces;
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

            Check(bb, new byte[] { 1, 2, 3 });

            Assert.IsTrue(bb.CanAppend(1));
            Assert.IsTrue(bb.CanAppend(size - 3));
            Assert.IsFalse(bb.CanAppend(size - 2));

            bb.Append(new byte[] { 4, 5, 6 }, 0, 3);

            Assert.AreEqual(size, bb.Size);
            Assert.AreEqual(6, bb.Length);
            Assert.AreEqual(0, bb.Start);
            Assert.AreEqual(6, bb.End);

            Check(bb, new byte[] { 1, 2, 3, 4, 5, 6 });
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

            Check(bb, new byte[] { 4, 5, 6 });

            Assert.IsTrue(bb.CanPrepend(1));
            Assert.IsTrue(bb.CanPrepend(size - 3));
            Assert.IsFalse(bb.CanPrepend(size - 2));

            bb.Prepend(new byte[] { 1, 2, 3 }, 0, 3);

            Assert.AreEqual(size, bb.Size);
            Assert.AreEqual(6, bb.Length);
            Assert.AreEqual(size - 6, bb.Start);
            Assert.AreEqual(size, bb.End);

            Check(bb, new byte[] { 1, 2, 3, 4, 5, 6 });
        }

        [Test]
        public void Should_replace_at_start_append()
        {
            const int size = 10;
            var bb = new ByteBuffer(new byte[size], 0);

            bb.Append(new byte[] { 1, 2, 3, 4, 5, 6 }, 0, 6);

            var offset = 0;
            var count = 3;
            var replacementStart = 0;
            var replacementCount = 3;

            Assert.IsTrue(bb.CanReplace(offset, count, replacementCount));

            bb.Replace(new byte[] { 8, 9, 10}, ref offset, ref count, ref replacementStart, ref replacementCount);

            Assert.AreEqual(3, offset);
            Assert.AreEqual(0, count);
            Assert.AreEqual(3, replacementStart);
            Assert.AreEqual(0, replacementCount);

            Check(bb, new byte[] { 8, 9, 10, 4, 5, 6 });
        }

        [Test]
        public void Should_replace_at_end_append()
        {
            const int size = 10;
            var bb = new ByteBuffer(new byte[size], 0);

            bb.Append(new byte[] { 1, 2, 3, 4, 5, 6 }, 0, 6);

            var offset = 3;
            var count = 3;
            var replacementStart = 0;
            var replacementCount = 3;

            Assert.IsTrue(bb.CanReplace(offset, count, replacementCount));

            bb.Replace(new byte[] { 8, 9, 10}, ref offset, ref count, ref replacementStart, ref replacementCount);

            Assert.AreEqual(0, offset);
            Assert.AreEqual(0, count);
            Assert.AreEqual(3, replacementStart);
            Assert.AreEqual(0, replacementCount);

            Check(bb, new byte[] { 1, 2, 3, 8, 9, 10 });
        }

        [Test]
        public void Should_replace_at_start_prepend()
        {
            const int size = 10;
            var bb = new ByteBuffer(new byte[size], 0);

            bb.Prepend(new byte[] { 1, 2, 3, 4, 5, 6 }, 0, 6);

            var offset = 0;
            var count = 3;
            var replacementStart = 0;
            var replacementCount = 3;

            Assert.IsTrue(bb.CanReplace(offset, count, replacementCount));

            bb.Replace(new byte[] { 8, 9, 10}, ref offset, ref count, ref replacementStart, ref replacementCount);

            Assert.AreEqual(3, offset);
            Assert.AreEqual(0, count);
            Assert.AreEqual(3, replacementStart);
            Assert.AreEqual(0, replacementCount);

            Check(bb, new byte[] { 8, 9, 10, 4, 5, 6 });
        }

        [Test]
        public void Should_replace_at_end_prepend()
        {
            const int size = 10;
            var bb = new ByteBuffer(new byte[size], 0);

            bb.Prepend(new byte[] { 1, 2, 3, 4, 5, 6 }, 0, 6);

            var offset = 3;
            var count = 3;
            var replacementStart = 0;
            var replacementCount = 3;

            Assert.IsTrue(bb.CanReplace(offset, count, replacementCount));

            bb.Replace(new byte[] { 8, 9, 10}, ref offset, ref count, ref replacementStart, ref replacementCount);

            Assert.AreEqual(0, offset);
            Assert.AreEqual(0, count);
            Assert.AreEqual(3, replacementStart);
            Assert.AreEqual(0, replacementCount);

            Check(bb, new byte[] { 1, 2, 3, 8, 9, 10 });
        }

        [Test]
        public void Should_replace_overlapping_end()
        {
            const int size = 10;
            var bb = new ByteBuffer(new byte[size], 0);

            bb.Append(new byte[] { 1, 2, 3, 4, 5, 6 }, 0, 6);

            var offset = 4;
            var count = 3;
            var replacementStart = 0;
            var replacementCount = 3;

            Assert.IsTrue(bb.CanReplace(offset, count, replacementCount));

            bb.Replace(new byte[] { 8, 9, 10}, ref offset, ref count, ref replacementStart, ref replacementCount);

            Assert.AreEqual(0, offset);
            Assert.AreEqual(1, count);
            Assert.AreEqual(2, replacementStart);
            Assert.AreEqual(1, replacementCount);

            Check(bb, new byte[] { 1, 2, 3, 4, 8, 9 });
        }

        [Test]
        public void Should_replace_larger_buffer()
        {
            const int size = 10;
            var bb = new ByteBuffer(new byte[size], 0);

            bb.Append(new byte[] { 1, 2, 3, 4, 5, 6 }, 0, 6);

            var offset = 2;
            var count = 10;
            var replacementStart = 0;
            var replacementCount = 10;

            Assert.IsTrue(bb.CanReplace(offset, count, replacementCount));

            bb.Replace(new byte[] { 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 }, 
                ref offset, ref count, ref replacementStart, ref replacementCount);

            Assert.AreEqual(0, offset);
            Assert.AreEqual(6, count);
            Assert.AreEqual(4, replacementStart);
            Assert.AreEqual(6, replacementCount);

            Check(bb, new byte[] { 1, 2, 8, 9, 10, 11 });
        }

        [Test]
        public void Should_insert_within()
        {
            var bufferPool = SetupMock<IBufferPool>();

            const int size = 10;
            var bb1 = new ByteBuffer(new byte[size], 0);

            bb1.Append(new byte[] { 1, 2, 3, 4 }, 0, 4);
            var bb2 = bb1.Insert(1, 2, bufferPool, new byte[] { 8, 9, 10 }, 0, 3);

            Check(bb1, bb2, new byte[] { 1, 8, 9, 10, 4 });
        }

        [Test]
        public void Should_insert_at_end_append()
        {
            var bufferPool = SetupMock<IBufferPool>();

            const int size = 10;
            var bb1 = new ByteBuffer(new byte[size], 0);

            bb1.Append(new byte[] { 1, 2, 3, 4 }, 0, 4);
            var bb2 = bb1.Insert(2, 2, bufferPool, new byte[] { 8, 9, 10, 11, 12 }, 0, 5);

            Check(bb1, bb2, new byte[] { 1, 2, 8, 9, 10, 11, 12 });
        }

        [Test]
        public void Should_insert_at_start_append()
        {
            var bufferPool = SetupMock<IBufferPool>();

            const int size = 10;
            var bb1 = new ByteBuffer(new byte[size], 0);

            bb1.Append(new byte[] { 1, 2, 3, 4 }, 0, 4);
            var bb2 = bb1.Insert(0, 2, bufferPool, new byte[] { 8, 9, 10, 11, 12 }, 0, 5);

            Check(bb1, bb2, new byte[] { 8, 9, 10, 11, 12, 3, 4 });
        }

        [Test]
        public void Should_insert_at_end_prepend()
        {
            var bufferPool = SetupMock<IBufferPool>();

            const int size = 10;
            var bb1 = new ByteBuffer(new byte[size], 0);

            bb1.Prepend(new byte[] { 1, 2, 3, 4 }, 0, 4);
            var bb2 = bb1.Insert(2, 2, bufferPool, new byte[] { 8, 9, 10, 11, 12 }, 0, 5);

            Check(bb1, bb2, new byte[] { 1, 2, 8, 9, 10, 11, 12 });
        }

        [Test]
        public void Should_insert_at_start_prepend()
        {
            var bufferPool = SetupMock<IBufferPool>();

            const int size = 10;
            var bb1 = new ByteBuffer(new byte[size], 0);

            bb1.Prepend(new byte[] { 1, 2, 3, 4 }, 0, 4);
            var bb2 = bb1.Insert(0, 2, bufferPool, new byte[] { 8, 9, 10, 11, 12 }, 0, 5);

            Check(bb1, bb2, new byte[] { 8, 9, 10, 11, 12, 3, 4 });
        }

        [Test]
        public void Should_insert_large_at_end()
        {
            var bufferPool = SetupMock<IBufferPool>();

            const int size = 10;
            var bb1 = new ByteBuffer(new byte[size], 0);

            bb1.Append(new byte[] { 1, 2, 3, 4 }, 0, 4);
            var bb2 = bb1.Insert(2, 2, bufferPool, new byte[] { 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 }, 0, 10);

            Check(bb1, bb2, new byte[] { 1, 2, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 });
        }

        [Test]
        public void Should_insert_large_in_middle()
        {
            var bufferPool = SetupMock<IBufferPool>();

            const int size = 10;
            var bb1 = new ByteBuffer(new byte[size], 0);

            bb1.Append(new byte[] { 1, 2, 3, 4 }, 0, 4);
            var bb2 = bb1.Insert(1, 2, bufferPool, new byte[] { 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 }, 0, 10);

            Check(bb1, bb2, new byte[] { 1, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 4 });
        }

        [Test]
        public void Should_delete_within()
        {
            const int size = 10;
            var bb = new ByteBuffer(new byte[size], 0);

            bb.Append(new byte[] { 1, 2, 3, 4, 5, 6 }, 0, 6);

            var offset = 2;
            var count = 2;
            var replacementStart = 0;
            var replacementCount = 0;

            Assert.IsTrue(bb.CanReplace(offset, count, replacementCount));

            bb.Replace(null, ref offset, ref count, ref replacementStart, ref replacementCount);

            Assert.AreEqual(0, offset);
            Assert.AreEqual(0, count);
            Assert.AreEqual(0, replacementStart);
            Assert.AreEqual(0, replacementCount);
            Assert.AreEqual(4, bb.Length);

            Check(bb, new byte[] { 1, 2, 5, 6 });
        }

        [Test]
        public void Should_delete_at_end()
        {
            const int size = 10;
            var bb = new ByteBuffer(new byte[size], 0);

            bb.Append(new byte[] { 1, 2, 3, 4, 5, 6 }, 0, 6);

            var offset = 3;
            var count = 3;
            var replacementStart = 0;
            var replacementCount = 0;

            Assert.IsTrue(bb.CanReplace(offset, count, replacementCount));

            bb.Replace(null, ref offset, ref count, ref replacementStart, ref replacementCount);

            Assert.AreEqual(0, offset);
            Assert.AreEqual(0, count);
            Assert.AreEqual(0, replacementStart);
            Assert.AreEqual(0, replacementCount);
            Assert.AreEqual(3, bb.Length);

            Check(bb, new byte[] { 1, 2, 3 });
        }

        [Test]
        public void Should_delete_at_start()
        {
            const int size = 10;
            var bb = new ByteBuffer(new byte[size], 0);

            bb.Append(new byte[] { 1, 2, 3, 4, 5, 6 }, 0, 6);

            var offset = 0;
            var count = 3;
            var replacementStart = 0;
            var replacementCount = 0;

            Assert.IsTrue(bb.CanReplace(offset, count, replacementCount));

            bb.Replace(null, ref offset, ref count, ref replacementStart, ref replacementCount);

            Assert.AreEqual(0, offset);
            Assert.AreEqual(0, count);
            Assert.AreEqual(0, replacementStart);
            Assert.AreEqual(0, replacementCount);
            Assert.AreEqual(3, bb.Length);
            Assert.AreEqual(3, bb.HeadSize);

            Check(bb, new byte[] { 4, 5, 6 });
        }

        [Test]
        public void Should_delete_spanning_buffers()
        {
            const int size = 10;

            var bb1 = new ByteBuffer(new byte[size], 0);
            var bb2 = new ByteBuffer(new byte[size], 0);

            bb1.Append(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
            bb2.Append(new byte[] { 6, 7, 8, 9, 10 }, 0, 5);

            var replacementBytes = new byte[] { 90, 91, 92 };

            var offset = 4;
            var count = 5;
            var replacementStart = 0;
            var replacementCount = replacementBytes.Length;

            Assert.IsTrue(bb1.CanReplace(offset, count, replacementCount));

            bb1.Replace(replacementBytes, ref offset, ref count, ref replacementStart, ref replacementCount);

            Assert.IsTrue(bb2.CanReplace(offset, count, replacementCount));

            bb2.Replace(replacementBytes, ref offset, ref count, ref replacementStart, ref replacementCount);

            Check(bb1, bb2, new byte[] { 1, 2, 3, 4, 90, 91, 92, 10 });
        }

        [Test]
        public void Should_delete_large()
        {
            const int size = 10;
            var bb = new ByteBuffer(new byte[size], 0);

            bb.Append(new byte[] { 1, 2, 3, 4, 5, 6 }, 0, 6);

            var offset = 3;
            var count = 8;
            var replacementStart = 0;
            var replacementCount = 0;

            Assert.IsTrue(bb.CanReplace(offset, count, replacementCount));

            bb.Replace(null, ref offset, ref count, ref replacementStart, ref replacementCount);

            Assert.AreEqual(0, offset);
            Assert.AreEqual(5, count);
            Assert.AreEqual(0, replacementStart);
            Assert.AreEqual(0, replacementCount);
            Assert.AreEqual(3, bb.Length);

            Check(bb, new byte[] { 1, 2, 3 });
        }

        private void Check(ByteBuffer bb1, ByteBuffer bb2, byte[] expected)
        {
            if (bb2 == null)
                Assert.AreEqual(expected.Length, bb1.Length);
            else
                Assert.AreEqual(expected.Length, bb1.Length + bb2.Length);

            var actual = new byte[expected.Length];
            bb1.Get(0, bb1.Length, actual, 0);

            if (bb2 != null)
                bb2.Get(0, bb2.Length, actual, bb1.Length);

            for (var i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], actual[i]);
        }

        private void Check(ByteBuffer bb, byte[] expected)
        {
            Check(bb, null, expected);
        }
    }
}
