using System;
using System.Collections.Generic;
using System.Linq;

using Juniper;

using UnityEngine.SceneManagement;

namespace UnityEngine
{
    /// <summary>
    /// Extension methods for Unity's Component class.
    /// </summary>
    public static class ComponentExt
    {
        /// <summary>
        /// ARCore looks for certain things as soon as the component is enabled, so we have to delay
        /// it. First, we disable the object hierarchy in question. Then, we execute the code to
        /// create the ARCore objects. Finally, we re-enable the hierarchy.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="act"></param>
        public static void WithLock(this Component src, Action act)
        {
            src.Deactivate();
            act();
            src.Activate();
        }

        /// <summary>
        /// Check to see if a component is valid (not-null) and activated.
        /// </summary>
        /// <param name="parent">The component to check</param>
        public static bool IsActivated(this Component parent)
        {
            return parent.gameObject.IsActivated();
        }

        /// <summary>
        /// Set the active state for the whole hierarchy from an object on up through its parent
        /// transforms. Useful for making absolutely sure a particular object is activated.
        /// </summary>
        /// <param name="parent">Parent.</param>
        /// <param name="active">If set to <c>true</c> active.</param>
        public static void SetTreeActive(this Component parent, bool active)
        {
            parent.gameObject.SetTreeActive(active);
        }

        /// <summary>
        /// Set the active state for the GameObject of any particular Component. Just a useful shortcut.
        /// </summary>
        /// <param name="parent">Parent.</param>
        /// <param name="active">If set to <c>true</c> active.</param>
        public static void SetActive(this Component parent, bool active)
        {
            parent.gameObject.SetActive(active);
        }

        /// <summary>
        /// A shortcut for
        /// <code>
        /// SetActive(true)
        /// </code>
        /// . Also useful for feeding to higher-order functions that expect parameterless functions.
        /// </summary>
        /// <param name="parent">Parent.</param>
        public static void Activate(this Component parent)
        {
            parent.gameObject.Activate();
        }

        /// <summary>
        /// A shortcut for
        /// <code>
        /// SetActive(true)
        /// </code>
        /// . Also useful for feeding to higher-order functions that expect parameterless functions.
        /// </summary>
        /// <param name="parent">Parent.</param>
        public static void Activate(this MonoBehaviour parent)
        {
            parent.gameObject.Activate();
            parent.enabled = true;
        }

        /// <summary>
        /// A shortcut for
        /// <code>
        /// SetActive(false)
        /// </code>
        /// . Also useful for feeding to higher-order functions that expect parameterless functions.
        /// </summary>
        /// <param name="parent">Parent.</param>
        public static void Deactivate(this Component parent)
        {
            parent.gameObject.Deactivate();
        }

        public static void Destroy(this Component obj)
        {
            Object.DestroyImmediate(obj);
        }

        /// <summary>
        /// Search through a series of Transforms and child transforms, defined as a set of
        /// forward-slash delimited names. Use ".." to select the parent transform.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <param name="path">  </param>
        /// <returns></returns>
        public static T Query<T>(this Component parent, string path)
        {
            return parent.transform.Query<T>(path);
        }

        /// <summary>
        /// Search through a series of Transforms and child transforms, defined as a set of
        /// forward-slash delimited names. Use ".." to select the parent transform.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <param name="path">  </param>
        /// <returns></returns>
        public static Transform Query(this Component parent, string path)
        {
            return parent.transform.Query(path);
        }

        /// <summary>
        /// Check to see if an object is associated with an active renderer.
        /// </summary>
        /// <returns>
        /// <c>true</c>, if the Component has a sibling Compoent that is an enabled Renderer,
        /// <c>false</c> otherwise.
        /// </returns>
        /// <param name="parent">Parent.</param>
        public static bool GetVisible(this Component parent)
        {
            var r = parent.GetComponent<Renderer>();
            return r?.enabled == true;
        }

        /// <summary>
        /// For any renderers on the <paramref name="parent"/>, sets its enabled property to
        /// <paramref name="visible"/>. For any child transforms, calls <see
        /// cref="SetActive(Component, bool)"/> with <paramref name="visible"/> as the value.
        /// </summary>
        /// <param name="parent"> </param>
        /// <param name="visible"></param>
        public static void SetVisible(this Component parent, bool visible)
        {
            var r = parent.GetComponent<Renderer>();
            if (r != null)
            {
                r.enabled = visible;
            }

            foreach (Transform child in parent.transform)
            {
                child.SetActive(visible);
            }
        }

        /// <summary>
        /// Attempts to get a component of type <typeparamref name="T"/> from <paramref name="obj"/>.
        /// If one is not available, this function creates one on the gameObject and returns that.
        /// </summary>
        /// <returns>The component.</returns>
        /// <param name="obj">Object.</param>
        /// <typeparam name="T">A subclass of type <see cref="Component"/>.</typeparam>
        public static PooledComponent<T> Ensure<T>(this Component obj, Predicate<T> predicate) where T : Component
        {
            return obj.gameObject.Ensure<T>(predicate);
        }

        public static PooledComponent<T> Ensure<T>(this Component obj)
            where T : Component
        {
            return obj.gameObject.Ensure<T>();
        }

        public static PooledComponent<Transform> EnsureParent(this Component obj, string name, params Transform[] exclusionList)
        {
            var parent = obj.transform.parent;
            if (parent == null || exclusionList.Contains(parent))
            {
                parent = new GameObject(name).transform;
                obj.transform.SetParent(parent, false);
                return new PooledComponent<Transform>(parent, true);
            }
            else
            {
                parent.name = name;
                return new PooledComponent<Transform>(parent, false);
            }
        }

        /// <summary>
        /// Checks to see if a gameObject has a particular component and, if it does, destroys it.
        /// </summary>
        /// <returns><c>true</c>, if component existed to be destroy, <c>false</c> otherwise.</returns>
        /// <param name="obj">The gameObject from which to remove the component.</param>
        /// <typeparam name="T">A subclass of type <see cref="Component"/>.</typeparam>
        public static bool Remove<T>(this Component obj) where T : Component
        {
            var o = obj.GetComponent<T>();
            o?.Destroy();
            return o != null;
        }

        /// <summary>
        /// Fill in a path of Transforms with other Transforms as necessary. Useful for creating
        /// complex Transform hierarchies very quickly.
        /// </summary>
        /// <returns>The transform.</returns>
        /// <param name="obj">   Object.</param>
        /// <param name="path">  Path.</param>
        /// <param name="create"></param>
        public static PooledComponent<T> Ensure<T>(this Component obj, string path, Func<GameObject> create)
            where T : Transform
        {
            return obj.Ensure<T>(path, null, create);
        }

        /// <summary>
        /// Fill in a path of Transforms with other Transforms as necessary. Useful for creating
        /// complex Transform hierarchies very quickly.
        /// </summary>
        /// <returns>The transform.</returns>
        /// <param name="obj">         Object.</param>
        /// <param name="path">        Path.</param>
        /// <param name="creationPath"></param>
        /// <param name="create">      </param>
        public static PooledComponent<T> Ensure<T>(this Component obj, string path, string creationPath = null, Func<GameObject> create = null)
            where T : Transform
        {
            if (creationPath == null)
            {
                creationPath = path;
            }

            var trans = obj.transform.Query(path);
            var isNew = trans == null;
            if (isNew)
            {
                var parts = creationPath.Split('/');
                var name = parts.LastOrDefault();

                if (create == null)
                {
                    create = () => new GameObject();
                }

                trans = create().transform;
                trans.name = name;

                Transform parent;
                if (parts.Length == 1)
                {
                    parent = obj.transform;
                }
                else if (parts.Length == 0 || (parts.Length == 2 && parts[0].Length == 0))
                {
                    parent = null;
                }
                else
                {
                    var parentPath = string.Join("/", parts
                        .Take(parts.Length - 1)
                        .ToArray());
                    parent = obj.Ensure<T>(parentPath);
                }

                trans.SetParent(parent, false);
            }

            return new PooledComponent<T>(trans.gameObject, isNew);
        }

        /// <summary>
        /// Find any object in any scene that is of a certain type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static T FindAny<T>(Func<T, bool> filter = null) where T : Component
        {
            return FindAll(filter).FirstOrDefault();
        }

        /// <summary>
        /// Find any object in the specified scene that is of a certain type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scene"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static T FindAny<T>(this Scene scene, Func<T, bool> filter = null) where T : Component
        {
            return scene.FindAll(filter).FirstOrDefault();
        }

        /// <summary>
        /// Find all objects in any scene that is of a certain type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindAll<T>(Func<T, bool> filter = null) where T : Component
        {
            for (var i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                foreach (var c in scene.FindAll(filter))
                {
                    yield return c;
                }
            }
        }

        /// <summary>
        /// Find all objects in the specified scene that is of a certain type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scene"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindAll<T>(this Scene scene, Func<T, bool> filter = null) where T : Component
        {
            if (scene.isLoaded || !string.IsNullOrEmpty(scene.name))
            {
                foreach (var o in scene.GetRootGameObjects())
                {
                    foreach (var c in o.GetComponentsInChildren<T>(true))
                    {
                        if (filter?.Invoke(c) != false)
                        {
                            yield return c;
                        }
                    }
                }
            }
        }
    }
}
