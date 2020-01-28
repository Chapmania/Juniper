using System;

namespace UnityEngine.Events
{
    /// <summary>
    /// Extensions to UnityEngine.Events.UnityEvent.
    /// </summary>
    public static class UnityEventExt
    {
        /// <summary>
        /// Attach an event listener that is only fired once, and then removed.
        /// </summary>
        /// <param name="evt">   Evt.</param>
        /// <param name="action">Action.</param>
        public static void Once(this UnityEvent evt, Action action)
        {
            if (evt is null)
            {
                throw new ArgumentNullException(nameof(evt));
            }

            void subAct()
            {
                evt.RemoveListener(subAct);
                action();
            }

            evt.AddListener(subAct);
        }
    }
}
