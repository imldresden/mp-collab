// ------------------------------------------------------------------------------------
// <copyright file="Services.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using IMLD.MixedReality.Network;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Core
{
    /// <summary>
    /// This class makes core services available to other components.
    /// </summary>
    public class ServiceLocator
    {
        /// <summary>
        /// The Instance property to implement the Singleton pattern.
        /// </summary>
        public static ServiceLocator Instance
        {
            get
            {
                return LazyServiceLocator.Value;
            }
        }

        public bool CheckDependencies(IService service)
        {
            if (service != null)
            {
                foreach (var dependency in service.Dependencies)
                {
                    if (dependency != null && !Services.ContainsKey(dependency))
                    {
                        Debug.LogError("Dependency of type " + dependency + " missing in service " + service.GetType());
                        return false;
                    }
                }
            }

            return true;
        }

        public T Get<T>()
        {
            if (Services.ContainsKey(typeof(T)))
            {
                return (T)Services[typeof(T)];
            }
            else
            {
                throw new ArgumentException("No service found of type " + typeof(T).FullName);
            }
        }

        public bool TryGet<T>(out T value)
        {
            if (Services.ContainsKey(typeof(T)))
            {
                value = (T)Services[typeof(T)];
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public void Register(Type type , object obj)
        {
            if (type.IsAssignableFrom(obj.GetType()))
            {
                Services.Add(type, obj);
            }
        }

        private Dictionary<Type, object> Services = new Dictionary<Type, object>();

        private ServiceLocator()
        {

        }

        private static readonly Lazy<ServiceLocator> LazyServiceLocator = new Lazy<ServiceLocator>(() => new ServiceLocator());
    }
}