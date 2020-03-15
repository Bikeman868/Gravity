using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using NUnit.Framework;
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

            var index = 0L;
            var sb = new StringBuilder();
            while (index < _byteArray.Length)
            {
                _byteArray.GetReadBuffer(index, out var buffer, out var bufferOffset, out var count);
                sb.Append(_encoding.GetString(buffer, bufferOffset, count));
                index += count;
            }

            Assert.AreEqual(_testMessages[0] + _testMessages[1] + _testMessages[2], sb.ToString());
        }

        private void Append(string message)
        {
            var bytes = _encoding.GetBytes(message);
            _byteArray.Append(bytes, 0, bytes.Length);
        }
    }
}
