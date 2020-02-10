using System;

namespace Gravity.Server.Interfaces
{
    public interface IFactory
    {
        T Create<T>();
        object Create(Type t);
    }
}