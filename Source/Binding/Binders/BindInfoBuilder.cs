using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    public class BindInfoBuilder
    {
        BindInfo _bindInfo;

        public BindInfoBuilder()
        {
        }

        public BindInfoBuilder(BindInfo bindInfo)
        {
            _bindInfo = bindInfo;
        }

        public BindInfo GetBindInfo()
        {
            return _bindInfo;
        }

        public BindInfoBuilder BindSelf(Type concreteType)
        {
            Assert.IsNull(_bindInfo.ConcreteType);

            _bindInfo.ConcreteType = concreteType;
            _bindInfo.BindConcreteType = true;
            return this;
        }

        public BindInfoBuilder BindInterfaces(Type concreteType)
        {
            Assert.IsNull(_bindInfo.ConcreteType);

            _bindInfo.ConcreteType = concreteType;
            _bindInfo.BindInterfaces = true;
            return this;
        }

        public BindInfoBuilder BindInterfacesAndSelf(Type concreteType)
        {
            Assert.IsNull(_bindInfo.ConcreteType);

            _bindInfo.ConcreteType = concreteType;
            _bindInfo.BindConcreteType = true;
            _bindInfo.BindInterfaces = true;
            return this;
        }

        public BindInfoBuilder WithId(int identifier)
        {
            _bindInfo.Identifier = identifier;
            return this;
        }

        public BindInfoBuilder WithId(string identifier)
        {
            _bindInfo.Identifier = identifier.GetHashCode();
            return this;
        }

        public BindInfoBuilder WithArguments(params object[] args)
        {
            _bindInfo.Arguments = args;
            return this;
        }

        public BindInfoBuilder NonLazy()
        {
            _bindInfo.NonLazy = true;
            return this;
        }

        public BindInfoBuilder Lazy()
        {
            _bindInfo.NonLazy = false;
            return this;
        }

        public BindInfoBuilder FromComponentOn(GameObject gameObject)
        {
            Assert.IsNotNull(gameObject, "Received null game object during bind command");
            _bindInfo.ProviderFactory = (_, bindInfo) => new GetFromGameObjectComponentProvider(
                bindInfo.ConcreteType, gameObject);
            return this;
        }

        public BindInfoBuilder FromComponentOn(Func<DiContainer, GameObject> gameObjectGetter)
        {
            _bindInfo.ProviderFactory = (container, bindInfo) => new GetFromGameObjectGetterComponentProvider(
                container, bindInfo.ConcreteType, gameObjectGetter);
            return this;
        }

        public BindInfoBuilder FromNewComponentOn(GameObject gameObject)
        {
            Assert.IsNotNull(gameObject, "Received null game object during bind command");
            _bindInfo.ProviderFactory = (container, bindInfo) => new AddToExistingGameObjectComponentProvider(
                gameObject, container, bindInfo.ConcreteType, bindInfo.Arguments);
            return this;
        }

        public BindInfoBuilder FromNewComponentOn(Func<GameObject> gameObjectGetter)
        {
            _bindInfo.ProviderFactory = (container, bindInfo) => new AddToExistingGameObjectComponentProviderGetter(
                gameObjectGetter, container, bindInfo.ConcreteType, bindInfo.Arguments);
            return this;
        }

        public BindInfoBuilder FromMethod<TConcrete>(Func<TConcrete> method)
        {
            _bindInfo.ProviderFactory = (_, _) => new MethodProvider<TConcrete>(method);
            return this;
        }

        public BindInfoBuilder FromResolveGetter<TObj, TContract>(Func<TObj, TContract> method, int identifier = 0, InjectSources source = InjectSources.Any)
        {
            _bindInfo.ProviderFactory = (container, _) => new GetterProvider<TObj, TContract>(
                identifier, method, container, source);
            return this;
        }

        public BindInfoBuilder FromInstance(object instance)
        {
            _bindInfo.Instance = instance;
            return this;
        }
    }
}