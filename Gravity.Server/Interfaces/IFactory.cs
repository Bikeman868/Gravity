namespace Gravity.Server.Interfaces
{
    internal interface IFactory
    {
        T Create<T>();
    }
}