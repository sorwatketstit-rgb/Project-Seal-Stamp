using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace ProjectDawn.CozyBuilder
{
    public static class ListExtension
    {
        public static bool IsNull<T>(this List<T> value) where T : UnityEngine.Object
        {
            if (value == null)
                return true;
            if (value.Count == 0)
                return true;
            foreach (var item in value)
            {
                if (item == null)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// GC-free, hassle-free GetComponentsInChildren.
    /// Example:
    /// 
    /// using (var renderers = transform.parent?.GetComponentsInChildrenScoped<MeshRenderer>())
    ///     foreach (var renderer in renderers.List)
    ///         Debug.Log($"Hello from {renderer} renderer");
    /// </summary>
    public static class GameObjectExt
    {
        public static ComponentScope<T> GetComponentsInChildrenScoped<T>(this Transform gameObject, bool includeInactive = false)
        {
            var scope = new ComponentScope<T> { List = ListPool<T>.Get() };
            gameObject.GetComponentsInChildren(includeInactive, scope.List);
            return scope;
        }

        public static ComponentScope<T> GetComponentsInChildrenScoped<T>(this MonoBehaviour gameObject, bool includeInactive = false)
        {
            var scope = new ComponentScope<T> { List = ListPool<T>.Get() };
            gameObject.GetComponentsInChildren(includeInactive, scope.List);
            return scope;
        }

        public static ComponentScope<T> GetComponentsScoped<T>(this Transform gameObject, bool includeInactive = false)
        {
            var scope = new ComponentScope<T> { List = ListPool<T>.Get() };
            gameObject.GetComponents(scope.List);
            return scope;
        }

        public static ComponentScope<T> GetComponentsScoped<T>(this MonoBehaviour gameObject, bool includeInactive = false)
        {
            var scope = new ComponentScope<T> { List = ListPool<T>.Get() };
            gameObject.GetComponents(scope.List);
            return scope;
        }

        public struct ComponentScope<T> : IDisposable
        {
            public List<T> List;

            public static implicit operator List<T>(ComponentScope<T> data) => data.List;

            public void Dispose() => ListPool<T>.Release(List);
        }
    }
}