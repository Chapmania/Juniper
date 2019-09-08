using System;
using System.Collections;
using System.Linq;

using Juniper.Input;
using Juniper.Progress;

using UnityEngine;
using UnityEngine.Events;

namespace Juniper
{
    /// <summary>
    /// A subscene is a game object loaded from another scene. The scenes all get loaded at runtime
    /// and then you can make different parts of it visible on the fly. This procedure deactivates
    /// any subscenes that are not the desired subscene, calling any Exit functions along the way. In
    /// the new scene, TransitionController Enter functions are called as well. It is suitable for
    /// running in a coroutine to track when the end of the switching process occurs.
    /// </summary>
    public abstract class AbstractStateController : MonoBehaviour
    {
        /// <summary>
        /// An event that occurs when the controller is starting to go through the Enter transition.
        /// Prefer <see cref="Entering"/> if you are programmatically adding event handlers at
        /// runtime. If you are adding event handlers in the Unity Editor, prefer <see
        /// cref="onEntering"/>. If you are waiting for this event in a subclass of StateController,
        /// prefer overriding the <see cref="OnEnable"/> method.
        /// </summary>
        public UnityEvent onEntering = new UnityEvent();

        /// <summary>
        /// An event that occurs when the controller has finished going through the Enter transition.
        /// Prefer <see cref="Entered"/> if you are programmatically adding event handlers at
        /// runtime. If you are adding event handlers in the Unity Editor, prefer <see
        /// cref="onEntered"/>. If you are waiting for this event in a subclass of StateController,
        /// prefer overriding the <see cref="OnEntered"/> method.
        /// </summary>
        public UnityEvent onEntered = new UnityEvent();

        /// <summary>
        /// An event that occurs when the controller is starting to go through the Exit transition.
        /// Prefer <see cref="Exiting"/> if you are programmatically adding event handlers at
        /// runtime. If you are adding event handlers in the Unity Editor, prefer <see
        /// cref="onExiting"/>. If you are waiting for this event in a subclass of StateController,
        /// prefer overriding the <see cref="OnExiting"/> method.
        /// </summary>
        public UnityEvent onExiting = new UnityEvent();

        /// <summary>
        /// An event that occurs when the controller has finished going through the Exit transition.
        /// Prefer <see cref="Exited"/> if you are programmatically adding event handlers at runtime.
        /// If you are adding event handlers in the Unity Editor, prefer <see cref="onExited"/>. If
        /// you are waiting for this event in a subclass of StateController, prefer overriding the
        /// <see cref="OnDisable"/> method.
        /// </summary>
        public UnityEvent onExited = new UnityEvent();

        /// <summary>
        /// An event that occurs when the controller is starting to go through the Enter transition.
        /// Prefer <see cref="Entering"/> if you are programmatically adding event handlers at
        /// runtime. If you are adding event handlers in the Unity Editor, prefer <see
        /// cref="onEntering"/>. If you are waiting for this event in a subclass of StateController,
        /// prefer overriding the <see cref="OnEnable"/> method.
        /// </summary>
        public event EventHandler Entering;

        /// <summary>
        /// An event that occurs when the controller has finished going through the Enter transition.
        /// Prefer <see cref="Entered"/> if you are programmatically adding event handlers at
        /// runtime. If you are adding event handlers in the Unity Editor, prefer <see
        /// cref="onEntered"/>. If you are waiting for this event in a subclass of StateController,
        /// prefer overriding the <see cref="OnEntered"/> method.
        /// </summary>
        public event EventHandler Entered;

        /// <summary>
        /// An event that occurs when the controller is starting to go through the Exit transition.
        /// Prefer <see cref="Exiting"/> if you are programmatically adding event handlers at
        /// runtime. If you are adding event handlers in the Unity Editor, prefer <see
        /// cref="onExiting"/>. If you are waiting for this event in a subclass of StateController,
        /// prefer overriding the <see cref="OnExiting"/> method.
        /// </summary>
        public event EventHandler Exiting;

        /// <summary>
        /// An event that occurs when the controller has finished going through the Exit transition.
        /// Prefer <see cref="Exited"/> if you are programmatically adding event handlers at runtime.
        /// If you are adding event handlers in the Unity Editor, prefer <see cref="onExited"/>. If
        /// you are waiting for this event in a subclass of StateController, prefer overriding the
        /// <see cref="OnDisable"/> method.
        /// </summary>
        public event EventHandler Exited;

        /// <summary>
        /// Whether or not the transition is running in the forward or backward direction (which is
        /// important for Fade(in|out) and Shrink(in|out) transitions).
        /// </summary>
        [ReadOnly]
        public Direction _curState;

        public Direction State
        {
            get { return _curState; }
            private set
            {
                lastState = _curState;
                _curState = value;

                if(State != lastState)
                {
                    if(State == Direction.Forward)
                    {
                        OnEntering();
                    }
                    else if(State == Direction.Reverse)
                    {
                        OnExiting();
                    }
                    else if(lastState == Direction.Forward)
                    {
                        OnEntered();
                    }
                    else if(lastState == Direction.Reverse)
                    {
                        OnExited();
                    }
                }
            }
        }

        /// <summary>
        /// The value of <see cref="State"/> from the last frame, to check for changes in the value.
        /// </summary>
        private Direction lastState;

        /// <summary>
        /// Used to avoid firing the status update events for the SkipEnter/SkipExit functions.
        /// </summary>
        private bool skipEvents;

        /// <summary>
        /// Returns true when <see cref="State"/> is <see cref="Direction.Stopped"/>
        /// </summary>
        public virtual bool IsComplete
        {
            get
            {
                return State == Direction.Stopped;
            }
        }

        /// <summary>
        /// A method for the IsComplete property to be used with the <see cref="Waiter"/> object.
        /// </summary>
        /// <returns></returns>
        public bool HasCompleted()
        {
            return IsComplete;
        }

        /// <summary>
        /// Returns true when <see cref="State"/> is not <see cref="Direction.Stopped"/>
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return !IsComplete;
            }
        }

        /// <summary>
        /// Returns true when the state controller is in the "Entered" state.
        /// </summary>
        public bool IsEntered
        {
            get
            {
                return isActiveAndEnabled;
            }
        }

        /// <summary>
        /// Returns true when the state controller is in the "Exited" state.
        /// </summary>
        public bool IsExited
        {
            get
            {
                return !isActiveAndEnabled;
            }
        }

#if UNITY_EDITOR

        public string GetStatus(string label)
        {
            var fields = new[]{
                enabled ? "enabled" : "",
                IsEntered ? "entered" : "",
                IsExited ? "exited" : "",
                IsComplete ? "complete" : "",
                skipEvents ? "skip events": ""
            };
            var full = string.Join(", ", fields.Where(x => !string.IsNullOrEmpty(x)));
            return $"{label}: {name} is ({lastState} -> {State}) = {full}";
        }

#endif

        protected virtual void Update()
        {

        }

        private void SetState(IProgress prog, Direction nextState)
        {
            prog?.Report(0);
            if (!isActiveAndEnabled)
            {
                this.SetTreeActive(true);
            }
            enabled = true;
            State = nextState;
        }

        /// <summary>
        /// Fire the OnEnable event and perform the Enter transition.
        /// </summary>
        public virtual void Enter(IProgress prog = null)
        {
            SetState(prog, Direction.Forward);
        }

        /// <summary>
        /// Fire the OnExiting event and perform the Exit transition.
        /// </summary>
        public virtual void Exit(IProgress prog = null)
        {
            SetState(prog, Direction.Reverse);
        }

        /// <summary>
        /// Finish the transition, whatever is going on.
        /// </summary>
        protected virtual void Complete()
        {
            State = Direction.Stopped;
        }

        /// <summary>
        /// Jump to the fully entered state without firing any events.
        /// </summary>
        public void SkipEnter()
        {
            skipEvents = true;
            Enter();
            Update();
            Complete();
            Update();
            skipEvents = false;
        }

        public void SkipExit()
        {
            skipEvents = true;
            Exit();
            Update();
            Complete();
            Update();
            skipEvents = false;
        }

        public IEnumerator EnterCoroutine(IProgress prog = null)
        {
            Enter(prog);
            while (!IsComplete)
            {
                yield return null;
            }
        }

        public IEnumerator ExitCoroutine(IProgress prog = null)
        {
            Exit(prog);
            while (!IsComplete)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Called as the object goes from Inactive to Active. Prefer <see cref="Entering"/> if you
        /// are programmatically adding event handlers at runtime. If you are adding event handlers
        /// in the Unity Editor, prefer <see cref="onEntering"/>. If you are waiting for this event
        /// in a subclass of StateController, prefer overriding the <see cref="OnEnable"/> method.
        /// </summary>
        protected virtual void OnEntering()
        {
            if (!skipEvents)
            {
                onEntering?.Invoke();
                Entering?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Calls the <see cref="onEntered"/> and <see cref="Entered"/> events, if they are valid.
        /// Prefer <see cref="Entered"/> if you are programmatically adding event handlers at
        /// runtime. If you are adding event handlers in the Unity Editor, prefer <see
        /// cref="onEntered"/>. If you are waiting for this event in a subclass of StateController,
        /// prefer overriding the <see cref="OnEntered"/> method.
        /// </summary>
        protected virtual void OnEntered()
        {
            if (!skipEvents)
            {
                onEntered?.Invoke();
                Entered?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Calls the <see cref="onExiting"/> and <see cref="Exiting"/> events, if they are valid.
        /// Prefer <see cref="Exiting"/> if you are programmatically adding event handlers at
        /// runtime. If you are adding event handlers in the Unity Editor, prefer <see
        /// cref="onExiting"/>. If you are waiting for this event in a subclass of StateController,
        /// prefer overriding the <see cref="OnExiting"/> method.
        /// </summary>
        protected virtual void OnExiting()
        {
            if (!skipEvents)
            {
                onExiting?.Invoke();
                Exiting?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Calls the <see cref="onExited"/> and <see cref="Exited"/> events, if they are valid.
        /// Prefer <see cref="Exited"/> if you are programmatically adding event handlers at runtime.
        /// If you are adding event handlers in the Unity Editor, prefer <see cref="onExited"/>. If
        /// you are waiting for this event in a subclass of StateController, prefer overriding the
        /// <see cref="OnExited"/> method.
        /// </summary>
        protected virtual void OnExited()
        {
            if (!skipEvents)
            {
                onExited?.Invoke();
                Exited?.Invoke(this, EventArgs.Empty);
            }
            enabled = false;
        }
    }
}