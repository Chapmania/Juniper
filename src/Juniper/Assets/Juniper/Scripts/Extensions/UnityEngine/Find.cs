﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine.SceneManagement;

namespace UnityEngine
{
    public static class Find
    {

        /// <summary>
        /// Find any object in any scene that is of a certain type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static T Any<T>(Func<T, bool> filter)
        {
            return All(filter).FirstOrDefault();
        }

        public static bool Any<T>(Func<T, bool> filter, out T value)
        {
            value = Any(filter);
            return value != default;
        }

        /// <summary>
        /// Find any object in any scene that is of a certain type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Any<T>()
        {
            return All<T>().FirstOrDefault();
        }

        public static bool Any<T>(out T value)
        {
            value = Any<T>();
            return value != default;
        }

        /// <summary>
        /// Find a component that is loosely related to another component.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T FindClosest<T>(this Component obj)
        {
            var v = obj.GetComponent<T>();

            if (v == default)
            {
                obj.GetComponentInChildren<T>();
            }

            if (v == default)
            {
                v = obj.GetComponentInParent<T>();
            }

            if (v == default)
            {
                v = Any<T>();
            }

            return v;
        }

        public static bool FindClosest<T>(this Component obj, out T value)
        {
            value = obj.FindClosest<T>();
            return value != default;
        }

        /// <summary>
        /// Find all objects in any scene that is of a certain type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> All<T>()
        {
            var objs = Resources.FindObjectsOfTypeAll<Object>();
            for (var i = 0; i < objs.Length; ++i)
            {
                var o = objs[i];
                if (o is T obj)
                {
                    yield return obj;
                }
            }
        }

        public static void All<T>(ref List<T> container)
        {
            if (container == null)
            {
                container = new List<T>();
            }
            container.AddRange(All<T>());
        }

        /// <summary>
        /// Find all objects in any scene that is of a certain type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static IEnumerable<T> All<T>(Func<T, bool> filter)
        {
            foreach (var obj in All<T>())
            {
                if (filter(obj))
                {
                    yield return obj;
                }
            }
        }

        /// <summary>
        /// Find all objects in any scene that is of a certain type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static void All<T>(Func<T, bool> filter, ref List<T> container)
        {
            if(container == null)
            {
                container = new List<T>();
            }
            container.AddRange(All(filter));
        }

        /// <summary>
        /// Find all objects in the specified scene that is of a certain type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scene"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindAll<T>(this Scene scene)
        {
            if (scene.isLoaded || !string.IsNullOrEmpty(scene.name))
            {
                foreach (var o in scene.GetRootGameObjects())
                {
                    foreach (var c in o.GetComponentsInChildren<T>(true))
                    {
                        yield return c;
                    }
                }
            }
        }

        public static void FindAll<T>(this Scene scene, ref List<T> container)
        {
            if (container == null)
            {
                container = new List<T>();
            }
            container.AddRange(scene.FindAll<T>());
        }

        /// <summary>
        /// Find all objects in the specified scene that is of a certain type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scene"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindAll<T>(this Scene scene, Func<T, bool> filter)
        {
            foreach (var c in scene.FindAll<T>())
            {
                if (filter(c))
                {
                    yield return c;
                }
            }
        }

        public static void FindAll<T>(this Scene scene, Func<T, bool> filter, ref List<T> container)
        {
            if (container == null)
            {
                container = new List<T>();
            }
            container.AddRange(scene.FindAll(filter));
        }
    }
}
