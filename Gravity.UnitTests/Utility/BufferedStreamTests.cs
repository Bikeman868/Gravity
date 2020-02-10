using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using NUnit.Framework;

namespace Gravity.UnitTests.Utility
{
    [TestFixture]
    public class BufferedStreamTests: Moq.Modules.TestBase
    {
        private System.IO.MemoryStream _stream;
        private BufferedStream _bufferedStream;

        [SetUp]
        public void SetUp()
        {
            _stream = new System.IO.MemoryStream();
            _bufferedStream = new BufferedStream(_stream, SetupMock<IBufferPool>(), 4096, 4096);
        }

        [TearDown]
        public void TearDown()
        {
            _bufferedStream.Dispose();
        }

        [Test]
        public void Should_stream_reads()
        {
        }

        [Test]
        public void Should_stream_writes()
        {
        }
    }
}
