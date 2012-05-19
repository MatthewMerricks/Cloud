using System;

namespace Dialog.Abstractions.Wpf.ServiceLocation
{
    public interface ISimpleServiceLocator
    {
        T Get<T>();
        Type Get(Type type);
        T Get<T>(string key);
        void Inject<T>(T instance);
        void InjectAsSingleton<T>(T instance);
        void Register<TInterface, TImplementor>() where TImplementor : TInterface;
        void Register<TInterface, TImplementor>(string key) where TImplementor : TInterface;
        void RegisterAsSingleton<TInterface, TImplementor>() where TImplementor : TInterface;
    }
}