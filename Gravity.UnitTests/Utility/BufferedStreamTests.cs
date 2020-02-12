using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using NUnit.Framework;
using System.Diagnostics;
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

                    var streamPos = 0L;

                    using (var bufferedStream = new BufferedStream(
                        stream, 
                        SetupMock<IBufferPool>(), 
                        bufferLength, 
                        bs => 
                        {
                            Assert.IsTrue(bs.BufferedReadStart <= streamPos);
                            streamPos = bs.BufferedReadStart + bs.BufferedReadLength;
                        }, 
                        0, 
                        null))
                    {
                        Assert.AreEqual(iterations, TestUnmodifiedStream(bufferedStream, readLength));
                    }
                }
            }
        }

        [Test]
        [TestCase(2)]
        [TestCase(9)]
        [TestCase(23)]
        [TestCase(71)]
        [TestCase(399)]
        [TestCase(1754)]
        public void Should_replace_bytes_in_stream_reads(int iterations)
        {
            foreach (var readLength in _bufferSizes)
            {
                TestContext.Out.WriteLine($"Read length {readLength}");

                foreach (var bufferLength in _bufferSizes)
                {
                    TestContext.Out.WriteLine($"Buffer length {bufferLength}");

                    var stream = new System.IO.MemoryStream();
                    FillStream(stream, iterations);
                    stream.Position = 0;

                    var streamPos = 0L;

                    using (var bufferedStream = new BufferedStream(
                        stream, 
                        SetupMock<IBufferPool>(), 
                        bufferLength, 
                        bs => 
                        {
                            var count = bs.BufferedReadLength - (int)(streamPos - bs.BufferedReadStart);
                            if (count > 0)
                            {
                                var buffer = new byte[count];
                                bs.GetReadBytes(streamPos, count, buffer, 0);

                                for (var i = 0; i < count; i++) buffer[i] += 1;

                                bs.ReplaceReadBytes(streamPos, count, buffer, 0, count);

                                streamPos += count;
                            }
                        }, 
                        0, 
                        null))
                    {
                        Assert.AreEqual(iterations, TestIncrementedStream(bufferedStream, readLength));
                    }
                }
            }
        }

        [Test]
        [TestCase(1)]
        [TestCase(6)]
        [TestCase(23)]
        [TestCase(71)]
        [TestCase(512)]
        [TestCase(1745)]
        public void Should_insert_bytes_in_stream_reads(int iterations)
        {
        }

        [Test]
        [TestCase(1)]
        [TestCase(6)]
        [TestCase(23)]
        [TestCase(71)]
        [TestCase(512)]
        [TestCase(1745)]
        public void Should_delete_bytes_in_stream_reads(int iterations)
        {
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
                var streamPos = 0L;
                using (var bufferedStream = new BufferedStream(
                    stream, 
                    SetupMock<IBufferPool>(), 
                    0, 
                    null, 
                    bufferLength, 
                    bs => 
                    { 
                        Assert.IsTrue(bs.BufferedWriteStart <= streamPos);
                        streamPos = bs.BufferedWriteStart + bs.BufferedWriteLength;
                    }))
                {
                    FillStream(bufferedStream, iterations);
                    bufferedStream.Close();
                    stream.Position = 0;
                    Assert.AreEqual(iterations, TestUnmodifiedStream(stream, (int)stream.Length));
                }
            }
        }

        [Test]
        [TestCase(2)]
        [TestCase(9)]
        [TestCase(23)]
        [TestCase(71)]
        [TestCase(399)]
        [TestCase(1754)]
        public void Should_replace_bytes_in_stream_writes(int iterations)
        {
            foreach (var bufferLength in _bufferSizes)
            {
                var stream = new System.IO.MemoryStream();
                var streamPos = 0L;
                using (var bufferedStream = new BufferedStream(
                    stream, 
                    SetupMock<IBufferPool>(), 
                    0, 
                    null, 
                    bufferLength, 
                    bs => 
                    { 
                        var count = bs.BufferedWriteLength - (int)(streamPos - bs.BufferedWriteStart);
                        if (count > 0)
                        {
                            var buffer = new byte[count];
                            bs.GetWrittenBytes(streamPos, count, buffer, 0);

                            for (var i = 0; i < count; i++) buffer[i] += 1;

                            bs.ReplaceWrittenBytes(streamPos, count, buffer, 0, count);

                            streamPos += count;
                        }
                    }))
                {
                    FillStream(bufferedStream, iterations);
                    bufferedStream.Close();
                    stream.Position = 0;
                    Assert.AreEqual(iterations, TestIncrementedStream(stream, (int)stream.Length));
                }
            }
        }

        [Test]
        [TestCase(1)]
        [TestCase(6)]
        [TestCase(23)]
        [TestCase(71)]
        [TestCase(512)]
        [TestCase(1745)]
        public void Should_insert_bytes_in_stream_writes(int iterations)
        {
        }

        [Test]
        [TestCase(1)]
        [TestCase(6)]
        [TestCase(23)]
        [TestCase(71)]
        [TestCase(512)]
        [TestCase(1745)]
        public void Should_delete_bytes_in_stream_writes(int iterations)
        {
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

        private int TestIncrementedStream(System.IO.Stream stream, int bufferSize)
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
                Assert.AreEqual(_testMessage[j] + 1, message[i] + 0);
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
