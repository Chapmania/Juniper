﻿using System.Collections;

namespace UnityEngine
{
    public static class MonoBehaviourExt
    {
        /// <summary>
        /// A shortcut for <code>SetActive(true)</code>.
        /// </summary>
        /// <remarks>
        /// Also useful for feeding to higher-order functions that expect
        /// parameterless functions.
        /// </remarks>
        /// <param name="parent">Parent.</param>
        public static void Activate(this MonoBehaviour parent)
        {
            parent.gameObject.Activate();
            parent.enabled = true;
        }

        public static object Run(this MonoBehaviour parent, IEnumerator routine)
        {
            if (Application.isPlaying)
            {
                return parent.StartCoroutine(routine);
            }
#if UNITY_EDITOR
            else
            {
                return new UnityEditor.EditorCoroutine(routine);
            }
#endif
        }
    }
}