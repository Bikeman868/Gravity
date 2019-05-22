using System;
using Gravity.Server.Interfaces;

namespace Gravity.Server.Utility
{
    internal class Factory: IFactory
    {
        public static Func<Type, object> IocContainer;

        T IFactory.Create<T>()
        {
            return (T)IocContainer(typeof (T));
        }

        object IFactory.Create(Type t)
        {
            return IocContainer(t);
        }
    }
}