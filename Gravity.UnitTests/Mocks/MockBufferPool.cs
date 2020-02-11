using Gravity.Server.Interfaces;
using Moq.Modules;
using System;

namespace Gravity.UnitTests.Mocks
{
    public class MockBufferPool : ConcreteImplementationProvider<IBufferPool>, IBufferPool
    {
        private Random _random = new Random();

        protected override IBufferPool GetImplementation(IMockProducer mockProducer)
        {
            return this;
        }

        public byte[] Get(int? size = null)
        {
            return new byte[size ?? 1024];
        }

        public byte[] GetAtLeast(int minimumSize)
        {
            return new byte[minimumSize + _random.Next(10)];
        }

        public void Reuse(byte[] buffer)
        {
        }
    }
}
