using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using NUnit.Framework;
using System;
using System.Text;

namespace Gravity.UnitTests.Utility
{
    [TestFixture]
    public class FlexibleByteArrayTests: Moq.Modules.TestBase
    {
        private readonly int[] _bufferSizes = { 5, 20, 32, 100, 5000 };
        private readonly Encoding _encoding = Encoding.ASCII;
        private string[] _testMessages;

        private FlexibleByteArray _byteArray;

        [SetUp]
        public void SetUp()
        {
            _testMessages = new string[3];
            _testMessages[0] = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            _testMessages[1] = "TheQuickBrownFoxJumpsOverLazyDog";
            _testMessages[2] = "987654321098765432109876543210";

            _byteArray = new FlexibleByteArray(SetupMock<IBufferPool>());
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void Should_append_byte_arrays()
        {
            Append(_testMessages[0]);
            Append(_testMessages[1]);
            Append(_testMessages[2]);

            var sb = GetAsString();

            Assert.AreEqual(_testMessages[0] + _testMessages[1] + _testMessages[2], sb.ToString());
        }

        [Test]
        public void Should_append_to_buffers()
        {
            AppendBuffer(_testMessages[0]);
            AppendBuffer(_testMessages[1]);
            AppendBuffer(_testMessages[2]);

            var sb = GetAsString();

            Assert.AreEqual(_testMessages[0] + _testMessages[1] + _testMessages[2], sb.ToString());
        }

        [Test]
        public void Should_allow_random_access()
        {
            Append(_testMessages[0]);
            Append(_testMessages[1]);

            for (var i = 0; i < _testMessages[0].Length; i++)
                Assert.AreEqual((byte)(_testMessages[0][i]), _byteArray[i]);

            for (var i = 0; i < _testMessages[1].Length; i++)
                Assert.AreEqual((byte)(_testMessages[1][i]), _byteArray[i+_testMessages[0].Length]);

            _byteArray[3] = (byte)'0';

            var sb = GetAsString();

            var expected = _testMessages[0].Substring(0, 3) + '0' + _testMessages[0].Substring(4) + _testMessages[1];
            Assert.AreEqual(expected, sb.ToString());
        }

        [Test]
        public void Should_delete_bytes()
        {
            Append(_testMessages[0]);
            Append(_testMessages[1]);

            _byteArray.Delete(_testMessages[0].Length - 5, 10);
            var sb = GetAsString();

            var expected = _testMessages[0].Substring(0, _testMessages[0].Length - 5) + _testMessages[1].Substring(5);
            Assert.AreEqual(expected, sb.ToString());
        }

        [Test]
        public void Should_insert_bytes()
        {
            Append(_testMessages[0]);
            Insert(10, _testMessages[1]);
            var sb = GetAsString();

            var expected = _testMessages[0].Substring(0, 10) + _testMessages[1] + _testMessages[0].Substring(10);
            Assert.AreEqual(expected, sb.ToString());
        }

        [Test]
        public void Should_replace_bytes()
        {
            Append(_testMessages[0]);
            Replace(10, 5, _testMessages[1]);
            var sb = GetAsString();

            var expected = _testMessages[0].Substring(0, 10) + _testMessages[1] + _testMessages[0].Substring(15);
            Assert.AreEqual(expected, sb.ToString());
        }

        private void Append(string message)
        {
            var bytes = _encoding.GetBytes(message);
            _byteArray.Append(bytes, 0, bytes.Length);
        }

        private void Insert(long index, string message)
        {
            var bytes = _encoding.GetBytes(message);
            _byteArray.Insert(index, bytes, 0, bytes.Length);
        }

        private void Replace(long index, int count, string message)
        {
            var bytes = _encoding.GetBytes(message);
            _byteArray.Replace(index, count, bytes, 0, bytes.Length);
        }

        private void AppendBuffer(string message)
        {
            var bytes = _encoding.GetBytes(message);
            var updateFunc = _byteArray.GetAppendBuffer(bytes.Length, out var buffer, out var offset, out var count);
            Array.Copy(bytes, 0, buffer, offset,bytes.Length);
            updateFunc(bytes.Length);
        }

        private StringBuilder GetAsString()
        {
            var index = 0L;
            var sb = new StringBuilder();
            while (index < _byteArray.Length)
            {
                _byteArray.GetReadBuffer(index, out var buffer, out var bufferOffset, out var count);
                sb.Append(_encoding.GetString(buffer, bufferOffset, count));
                index += count;
            }

            return sb;
        }
    }
}
