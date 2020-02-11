using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using NUnit.Framework;
using System.Text;

namespace Gravity.UnitTests.Utility
{
    [TestFixture]
    public class BufferedStreamTests: Moq.Modules.TestBase
    {
        private readonly int[] _bufferSizes = { 5, 20, 32, 100, 5000 };
        private readonly Encoding _encoding = Encoding.ASCII;
        private const string _testMessage = "ABCDEFG";

        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(20)]
        [TestCase(55)]
        [TestCase(406)]
        [TestCase(3217)]
        public void Should_stream_reads(int iterations)
        {
            foreach (var readLength in _bufferSizes)
            {
                foreach (var bufferLength in _bufferSizes)
                {
                    var stream = new System.IO.MemoryStream();
                    FillStream(stream, iterations);
                    stream.Position = 0;

                    using (var bufferedStream = new BufferedStream(stream, SetupMock<IBufferPool>(), bufferLength, 0))
                    {
                        Assert.AreEqual(iterations, TestUnmodifiedStream(bufferedStream, readLength));
                    }
                }
            }
        }

        [Test]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(20)]
        [TestCase(55)]
        [TestCase(406)]
        [TestCase(3217)]
        public void Should_stream_writes(int iterations)
        {
            foreach (var bufferLength in _bufferSizes)
            {
                var stream = new System.IO.MemoryStream();
                using (var bufferedStream = new BufferedStream(stream, SetupMock<IBufferPool>(), 0, bufferLength))
                {
                    FillStream(bufferedStream, iterations);
                    bufferedStream.Close();
                    stream.Position = 0;
                    Assert.AreEqual(iterations, TestUnmodifiedStream(stream, (int)stream.Length));
                }
            }
        }

        private void FillStream(System.IO.Stream stream, int iterations)
        {
            var bytes = _encoding.GetBytes(_testMessage);

            for (var i = 0; i < iterations; i++)
                stream.Write(bytes, 0, bytes.Length);
        }

        private int TestUnmodifiedStream(System.IO.Stream stream, int bufferSize)
        {
            var stringBuilder = new StringBuilder();
            var buffer = new byte[bufferSize];

            do
            {
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                stringBuilder.Append(_encoding.GetString(buffer, 0, bytesRead));
            } while (true);

            var message = stringBuilder.ToString();
            var iterations = 0;
            var j = 0;

            for (var i = 0; i < message.Length; i++)
            {
                Assert.AreEqual(_testMessage[j], message[i]);
                if (++j == _testMessage.Length)
                {
                    j = 0;
                    iterations++;
                }
            }

            return iterations;
        }
    }
}
