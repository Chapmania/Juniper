using System;
using System.Linq;

using Juniper.Input.Speech;

using UnityEngine;
using UnityEngine.Events;

namespace Juniper.Unity.Input.Speech
{
    public class KeywordRecognizer :
#if UNITY_WSA
        UWPKeywordRecognizer
#elif UNITY_STANDALONE_WIN
        NETFXKeywordRecognizer
#else
        NoKeywordRecognizer
#endif
    {

    }

    /// <summary>
    /// A class that implements basic functionality for systems that manage speech recognition and
    /// route out the events associated with recognizing keywords.
    /// </summary>
    public abstract class AbstractKeywordRecognizer : MonoBehaviour, IKeywordRecognizer
    {
        /// <summary>
        /// The keywords for which to listen.
        /// </summary>
        protected string[] keywords;

        /// <summary>
        /// Respond to the speech recognition system having detected a keyword. You should only use
        /// this version in the Unity Editor. If you are programmatically attaching event listeners,
        /// you should preferr <see cref="KeywordRecognized"/>.
        /// </summary>
        public StringEvent onKeywordRecognized = new StringEvent();

        /// <summary>
        /// Respond to the speech recognition system having detected a keyword. You should only use
        /// this version if you are programmatically attaching event listeners. If you are attaching
        /// events in the Unity Editor, you should preferr <see cref="onKeywordRecognized"/>.
        /// </summary>
        public event EventHandler<KeywordRecognizedEventArgs> KeywordRecognized;

        /// <summary>
        /// Invokes <see cref="onKeywordRecognized"/> and <see cref="KeywordRecognized"/>.
        /// </summary>
        /// <param name="keyword">Keyword.</param>
        protected void OnKeywordRecognized(string keyword)
        {
            onKeywordRecognized?.Invoke(keyword);
            KeywordRecognized?.Invoke(this, new KeywordRecognizedEventArgs(keyword));
        }

        /// <summary>
        /// Find all of the keyword-responding components that are currently active in the scene,
        /// collect up their keywords, and register them return them as a set-array to be registered
        /// with the speech recognition system.
        /// </summary>
        /// <returns>The active keywords.</returns>
        public void RefreshKeywords()
        {
            this.WithLock(() =>
            {
                keywords = (from comp in ComponentExt.FindAll<MonoBehaviour>()
                            where comp is IKeywordTriggered
                            let trigger = (IKeywordTriggered)comp
                            where trigger.Keywords != null
                            from keyword in trigger.Keywords
                            where !string.IsNullOrEmpty(keyword)
                            select keyword)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToArray();
            });
        }

        /// <summary>
        /// Initialize the speech recognition system.
        /// </summary>
        public void OnEnable()
        {
            TearDown();
            Setup();
        }

        /// <summary>
        /// Tear down the speech recognition system.
        /// </summary>
        public void OnDisable()
        {
            TearDown();
        }

        protected abstract void Setup();

        protected abstract void TearDown();
    }
}
