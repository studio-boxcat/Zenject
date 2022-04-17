using System;
using System.Collections.Generic;
using System.Diagnostics;
using ModestTree;
using Zenject.Internal;
using System.Linq;
using TypeExtensions = ModestTree.TypeExtensions;

#if !NOT_UNITY3D
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#endif

namespace Zenject
{
    internal static class BindingUtil
    {
#if !NOT_UNITY3D

        [Conditional("DEBUG")]
        public static void AssertIsValidPrefab(UnityEngine.Object prefab)
        {
            Assert.That(!ZenUtilInternal.IsNull(prefab), "Received null prefab during bind command");

#if UNITY_EDITOR
            // Unfortunately we can't do this check because asset bundles return PrefabType.None here
            // as discussed here: https://github.com/svermeulen/Zenject/issues/269#issuecomment-323419408
            //Assert.That(PrefabUtility.GetPrefabType(prefab) == PrefabType.Prefab,
                //"Expected prefab but found game object with name '{0}' during bind command", prefab.name);
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertIsValidGameObject(GameObject gameObject)
        {
            Assert.That(!ZenUtilInternal.IsNull(gameObject), "Received null game object during bind command");

#if UNITY_EDITOR
            // Unfortunately we can't do this check because asset bundles return PrefabType.None here
            // as discussed here: https://github.com/svermeulen/Zenject/issues/269#issuecomment-323419408
            //Assert.That(PrefabUtility.GetPrefabType(gameObject) != PrefabType.Prefab,
                //"Expected game object but found prefab instead with name '{0}' during bind command", gameObject.name);
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertIsNotComponent(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                AssertIsNotComponent(type);
            }
        }

        [Conditional("DEBUG")]
        public static void AssertIsNotComponent<T>()
        {
            AssertIsNotComponent(typeof(T));
        }

        [Conditional("DEBUG")]
        public static void AssertIsNotComponent(Type type)
        {
            Assert.That(!type.DerivesFrom(typeof(Component)),
                "Invalid type given during bind command.  Expected type '{0}' to NOT derive from UnityEngine.Component", type);
        }

        [Conditional("DEBUG")]
        public static void AssertDerivesFromUnityObject(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                AssertDerivesFromUnityObject(type);
            }
        }

        [Conditional("DEBUG")]
        public static void AssertDerivesFromUnityObject<T>()
        {
            AssertDerivesFromUnityObject(typeof(T));
        }

        [Conditional("DEBUG")]
        public static void AssertDerivesFromUnityObject(Type type)
        {
            Assert.That(type.DerivesFrom<UnityEngine.Object>(),
                "Invalid type given during bind command.  Expected type '{0}' to derive from UnityEngine.Object", type);
        }

        [Conditional("DEBUG")]
        public static void AssertTypesAreNotComponents(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                AssertIsNotComponent(type);
            }
        }

        [Conditional("DEBUG")]
        public static void AssertIsValidResourcePath(string resourcePath)
        {
            Assert.That(!string.IsNullOrEmpty(resourcePath), "Null or empty resource path provided");

            // We'd like to validate the path here but unfortunately there doesn't appear to be
            // a way to do this besides loading it
        }

        [Conditional("DEBUG")]
        public static void AssertIsInterfaceOrScriptableObject(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                AssertIsInterfaceOrScriptableObject(type);
            }
        }

        [Conditional("DEBUG")]
        public static void AssertIsInterfaceOrScriptableObject<T>()
        {
            AssertIsInterfaceOrScriptableObject(typeof(T));
        }

        [Conditional("DEBUG")]
        public static void AssertIsInterfaceOrScriptableObject(Type type)
        {
            Assert.That(type.DerivesFrom(typeof(ScriptableObject)) || type.IsInterface,
                "Invalid type given during bind command.  Expected type '{0}' to either derive from UnityEngine.ScriptableObject or be an interface", type);
        }

        [Conditional("DEBUG")]
        public static void AssertIsInterfaceOrComponent(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                AssertIsInterfaceOrComponent(type);
            }
        }

        [Conditional("DEBUG")]
        public static void AssertIsInterfaceOrComponent<T>()
        {
            AssertIsInterfaceOrComponent(typeof(T));
        }

        [Conditional("DEBUG")]
        public static void AssertIsInterfaceOrComponent(Type type)
        {
            Assert.That(type.DerivesFrom(typeof(Component)) || type.IsInterface,
                "Invalid type given during bind command.  Expected type '{0}' to either derive from UnityEngine.Component or be an interface", type);
        }

        [Conditional("DEBUG")]
        public static void AssertIsComponent(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                AssertIsComponent(type);
            }
        }

        [Conditional("DEBUG")]
        public static void AssertIsComponent<T>()
        {
            AssertIsComponent(typeof(T));
        }

        [Conditional("DEBUG")]
        public static void AssertIsComponent(Type type)
        {
            Assert.That(type.DerivesFrom(typeof(Component)),
                "Invalid type given during bind command.  Expected type '{0}' to derive from UnityEngine.Component", type);
        }
#else
        public static void AssertTypesAreNotComponents(IEnumerable<Type> types)
        {
        }

        public static void AssertIsNotComponent(Type type)
        {
        }

        public static void AssertIsNotComponent<T>()
        {
        }

        public static void AssertIsNotComponent(IEnumerable<Type> types)
        {
        }
#endif

        [Conditional("DEBUG")]
        public static void AssertTypesAreNotAbstract(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                AssertIsNotAbstract(type);
            }
        }

        [Conditional("DEBUG")]
        public static void AssertIsNotAbstract(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                AssertIsNotAbstract(type);
            }
        }

        [Conditional("DEBUG")]
        public static void AssertIsNotAbstract<T>()
        {
            AssertIsNotAbstract(typeof(T));
        }

        [Conditional("DEBUG")]
        public static void AssertIsNotAbstract(Type type)
        {
            Assert.That(!type.IsAbstract,
                "Invalid type given during bind command.  Expected type '{0}' to not be abstract.", type);
        }

        [Conditional("DEBUG")]
        public static void AssertIsDerivedFromType(Type concreteType, Type parentType)
        {
            Assert.That(concreteType.DerivesFromOrEqual(parentType),
                "Invalid type given during bind command.  Expected type '{0}' to derive from type '{1}'", concreteType, parentType);
        }

        [Conditional("DEBUG")]
        public static void AssertConcreteTypeListIsNotEmpty(IEnumerable<Type> concreteTypes)
        {
            Assert.That(concreteTypes.Count() >= 1,
                "Must supply at least one concrete type to the current binding");
        }

        [Conditional("DEBUG")]
        public static void AssertIsDerivedFromTypes(
            IEnumerable<Type> concreteTypes, IEnumerable<Type> parentTypes, InvalidBindResponses invalidBindResponse)
        {
            if (invalidBindResponse == InvalidBindResponses.Assert)
            {
                AssertIsDerivedFromTypes(concreteTypes, parentTypes);
            }
            else
            {
                Assert.IsEqual(invalidBindResponse, InvalidBindResponses.Skip);
            }
        }

        [Conditional("DEBUG")]
        public static void AssertIsDerivedFromTypes(IEnumerable<Type> concreteTypes, IEnumerable<Type> parentTypes)
        {
            foreach (var concreteType in concreteTypes)
            {
                AssertIsDerivedFromTypes(concreteType, parentTypes);
            }
        }

        [Conditional("DEBUG")]
        public static void AssertIsDerivedFromTypes(Type concreteType, IEnumerable<Type> parentTypes)
        {
            foreach (var parentType in parentTypes)
            {
                AssertIsDerivedFromType(concreteType, parentType);
            }
        }

        [Conditional("DEBUG")]
        public static void AssertInstanceDerivesFromOrEqual(object instance, IEnumerable<Type> parentTypes)
        {
            if (!ZenUtilInternal.IsNull(instance))
            {
                foreach (var baseType in parentTypes)
                {
                    AssertInstanceDerivesFromOrEqual(instance, baseType);
                }
            }
        }

        [Conditional("DEBUG")]
        public static void AssertInstanceDerivesFromOrEqual(object instance, Type baseType)
        {
            if (!ZenUtilInternal.IsNull(instance))
            {
                Assert.That(instance.GetType().DerivesFromOrEqual(baseType),
                    "Invalid type given during bind command.  Expected type '{0}' to derive from type '{1}'", instance.GetType(), baseType);
            }
        }

        public static IProvider CreateCachedProvider(IProvider creator)
        {
            return new CachedProvider(creator);
        }
    }
}
