using System;

namespace Gravity.Server.Interfaces
{
    internal interface IFactory
    {
        T Create<T>();
        object Create(Type t);
    }
}