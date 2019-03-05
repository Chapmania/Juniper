using Juniper.Input;
using Juniper.Input.Pointers;
using Juniper.Unity.Audio;
using Juniper.Unity.Display;
using Juniper.Unity.Events;
using Juniper.Unity.Haptics;
using Juniper.Unity.Statistics;

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;

using InputButton = UnityEngine.EventSystems.PointerEventData.InputButton;

namespace Juniper.Unity.Input.Pointers
{
    public abstract class AbstractPointerConfiguration<ButtonIDType>
        where ButtonIDType : struct
    {
        private readonly Dictionary<ButtonIDType, InputButton> nativeButtons = new Dictionary<ButtonIDType, InputButton>();

        protected void AddButton(ButtonIDType outButton, InputButton inButton)
        {
            nativeButtons.Add(outButton, inButton);
        }

        public void Install(ButtonMapper<ButtonIDType> mapper, GameObject eventParent)
        {
            mapper.Install(eventParent, nativeButtons);
        }

        public void Uninstall(GameObject eventParent)
        {
            foreach (var evt in eventParent.GetComponents<ButtonEvent>())
            {
                evt.Destroy();
            }
        }
    }

    public abstract class AbstractPointerDevice<ButtonIDType, HapticsType, ConfigType> :
        MonoBehaviour,
        IInstallable,
        IPointerDevice,
        IPointerButtons<ButtonIDType>
        where ButtonIDType : struct
        where HapticsType : AbstractHapticDevice
        where ConfigType : AbstractPointerConfiguration<ButtonIDType>, new()
    {
        protected static Vector2 SCREEN_MIDPOINT
        {
            get
            {
                return new Vector2(UnityEngine.Screen.width / 2, UnityEngine.Screen.height / 2);
            }
        }

        protected static readonly Vector2 VIEWPORT_MIDPOINT =
            0.5f * Vector2.one;

        protected static readonly ConfigType PointerConfig =
            new ConfigType();

        public Type ButtonType
        {
            get
            {
                return typeof(ButtonIDType);
            }
        }

        protected readonly ButtonMapper<ButtonIDType> nativeButtons = new ButtonMapper<ButtonIDType>();

        [ReadOnly]
        public bool Connected;

        /// <summary>
        /// The minimum distance from the camera at which to place the pointer.
        /// </summary>
        public float MinimumPointerDistance
        {
            get
            {
                return Mathf.Max(1.5f, 1.1f * DisplayManager.MainCamera.nearClipPlane);
            }
        }

        protected Vector3 pointerOffset;

        public float MaximumPointerDistance
        {
            get
            {
                return Mathf.Min(10f, 0.9f * DisplayManager.MainCamera.farClipPlane);
            }
        }

        public Material LaserPointerMaterial;

        public bool LockedOnTarget
        {
            get; set;
        }

        public IEventSystemHandler EventTarget
        {
            get; set;
        }

        /// <summary>
        /// Mouse wheel and touch-pad scroll. This is a 2-dimensional value, as even with a
        /// single-wheel scroll mouse, you can hold the SHIFT key to scroll in the horizontal direction.
        /// </summary>
        public Vector2 ScrollDelta
        {
            get; protected set;
        }

        public abstract bool IsConnected
        {
            get;
        }

        public bool IsEnabled
        {
            get
            {
                return isActiveAndEnabled && IsConnected;
            }
        }

        /// <summary>
        /// Returns true when the device is supposed to be disabled.
        /// </summary>
        /// <value><c>true</c> if is disabled; otherwise, <c>false</c>.</value>
        public bool IsDisabled
        {
            get
            {
                return !IsEnabled;
            }
        }

        public PhysicsRaycaster Raycaster
        {
            get
            {
                return probe?.Raycaster;
            }
        }

        /// <summary>
        /// The camera the pointer uses to point at Canvas objects.
        /// </summary>
        public Camera EventCamera
        {
            get
            {
                return probe?.EventCamera;
            }
        }

        /// <summary>
        /// Unique pointer identifiers keep the pointer events cached in Unity's Event System.
        /// </summary>
        /// <value>The pointer identifier.</value>
        public int PointerID
        {
            get; set;
        }

        public virtual bool AnyButtonPressed
        {
            get
            {
                return IsButtonPressed(InputButton.Left)
                    || IsButtonPressed(InputButton.Right)
                    || IsButtonPressed(InputButton.Middle);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Juniper.Input.PointerDevice"/> class.
        /// </summary>
        public virtual void Awake()
        {
            Install(false);

            eventManager = ComponentExt.FindAny<UnifiedInputModule>();
            stage = ComponentExt.FindAny<StageExtensions>();

            pointerOffset = MinimumPointerDistance * Vector3.forward;
            ProbeName = name;

            Haptics = this.EnsureComponent<HapticsType>();

            nativeButtons.ButtonDownNeeded += IsButtonDown;
            nativeButtons.ButtonUpNeeded += IsButtonUp;
            nativeButtons.ButtonPressedNeeded += IsButtonPressed;
            nativeButtons.ClonedPointerEventNeeded += Clone;
            nativeButtons.InteractionNeeded += PlayInteraction;

            eventManager.AddPointer(this);
        }

        public virtual void Install(bool reset)
        {
            reset &= Application.isEditor;

            PointerConfig.Install(nativeButtons, gameObject);
        }

        public virtual void Uninstall()
        {
            PointerConfig.Uninstall(gameObject);
        }

        public virtual void Reinstall()
        {
            Install(true);
        }

#if UNITY_EDITOR

        public void Reset()
        {
            Reinstall();
        }

#endif

        public bool showProbe = true;

        /// <summary>
        /// The name of the pointer.
        /// </summary>
        /// <value>The name.</value>
        private string _probeName;

        /// <summary>
        /// The cursor probe that shows the physical location of the current selection.
        /// </summary>
        protected Probe probe;

        private Probe FindProbe()
        {
            if (ProbeName != null)
            {
                return Probe.Ensure(transform, ProbeName);
            }
            else
            {
                return null;
            }
        }

        public virtual void SetProbe(Probe p)
        {
            if (probe != p)
            {
                if (probe != null)
                {
                    probe.gameObject.Destroy();
                }

                probe = p ?? FindProbe();
            }
        }

        public string ProbeName
        {
            get
            {
                return _probeName;
            }

            private set
            {
                if (value != ProbeName)
                {
                    if (probe != null)
                    {
                        probe.gameObject.Destroy();
                        probe = null;
                    }

                    _probeName = value;
                    SetProbe(FindProbe());
                }
            }
        }

        public Vector3 LastWorldPoint
        {
            get; private set;
        }

        public Vector2 ScreenDelta
        {
            get
            {
                return ScreenPoint - ScreenFromWorld(LastWorldPoint);
            }
        }

        public abstract Vector2 ScreenPoint
        {
            get;
        }

        public Vector2 ScreenFromWorld(Vector3 worldPoint)
        {
            return EventCamera.WorldToScreenPoint(worldPoint);
        }

        public Vector2 ScreenFromViewport(Vector2 viewportPoint)
        {
            return EventCamera.ViewportToScreenPoint(viewportPoint);
        }

        public abstract Vector2 ViewportPoint
        {
            get;
        }

        public Vector2 ViewportFromWorld(Vector3 worldPoint)
        {
            return EventCamera.WorldToViewportPoint(worldPoint);
        }

        public Vector2 ViewportFromScreen(Vector2 screenPoint)
        {
            return EventCamera.ScreenToViewportPoint(screenPoint);
        }

        public abstract Vector3 WorldPoint
        {
            get;
        }

        public Vector3 WorldFromScreen(Vector2 screenPoint)
        {
            return EventCamera.ScreenToWorldPoint((Vector3)screenPoint + pointerOffset);
        }

        public Vector3 WorldFromViewport(Vector2 viewportPoint)
        {
            return EventCamera.ViewportToWorldPoint((Vector3)viewportPoint + pointerOffset);
        }

#if UNITY_EDITOR
        private AbstractMotionFilter parent;
#endif

        private AbstractMotionFilter lastMotionFilter;
        public AbstractMotionFilter motionFilter;

        /// <summary>
        /// The target at and through which the pointer rays fire.
        /// </summary>
        /// <value>The interaction end point.</value>
        public Vector3 InteractionEndPoint
        {
            get
            {
                return motionFilter?.PredictedPosition ?? WorldPoint;
            }
        }

        /// <summary>
        /// The direction the pointer is pointing, from <see cref="InteractionOrigin"/> to <see cref="InteractionEndPoint"/>.
        /// </summary>
        /// <value>The interaction direction.</value>
        public Vector3 InteractionDirection
        {
            get
            {
                return (InteractionEndPoint - transform.position).normalized;
            }
        }

        /// <summary>
        /// Update the position of the pointer and the pointer probe. Also check to see if the
        /// configuration has been changed to hide the pointer probe.
        /// </summary>
        public void Update()
        {
            if (motionFilter != lastMotionFilter)
            {
#if UNITY_EDITOR
                parent = motionFilter;
#endif
                motionFilter = Instantiate(motionFilter);
                lastMotionFilter = motionFilter;
            }

#if UNITY_EDITOR
            motionFilter?.Copy(parent);
#endif

            Connected = IsConnected;

            if (probe != null)
            {
                probe.SetActive(IsEnabled && showProbe);
                probe.LaserPointerMaterial = LaserPointerMaterial;
            }

            if (IsEnabled)
            {
                motionFilter?.UpdateState(WorldPoint);
                InternalUpdate();
                probe?.AlignProbe(InteractionDirection, transform.up, MaximumPointerDistance);
            }
        }

        protected UnifiedInputModule eventManager;

        protected StageExtensions stage;

        public UnifiedInputModule InputModule
        {
            get; set;
        }

        public virtual bool IsDragging
        {
            get
            {
                return nativeButtons.AnyDragging;
            }
        }

        /// <summary>
        /// The haptic feedback system associated with the device. For touch pointers, this is the
        /// global haptic system. For motion controllers, each controller has its own haptic system.
        /// </summary>
        /// <value>The haptics.</value>
        public HapticsType Haptics
        {
            get; protected set;
        }

        private float finishTime;

        public virtual void PlayInteraction(Interaction action)
        {
            if (Time.time > finishTime)
            {
                finishTime = Time.time + InteractionAudio.Play(action, probe.Cursor, Haptics);
            }
        }

        private PointerEventData Clone(PointerEventData evtData, ButtonIDType button)
        {
            return InputModule?.Clone(evtData, nativeButtons.ToInt32(button));
        }

        public virtual void Process(PointerEventData evtData, float pixelDragThresholdSquared)
        {
            if (!IsDragging)
            {
                TestEnterExit(evtData);
            }

            EventTarget = ProcessButtons(evtData, pixelDragThresholdSquared);

            if (evtData.clickCount == -1)
            {
                evtData.clickCount = 0;
            }

            LastWorldPoint = evtData.pointerCurrentRaycast.worldPosition;

            probe?.SetCursor(
                evtData.pointerCurrentRaycast.gameObject != null,
                AnyButtonPressed,
                LastWorldPoint,
                Quaternion.LookRotation(evtData.pointerCurrentRaycast.worldNormal));
        }

        protected virtual IEventSystemHandler ProcessButtons(PointerEventData evtData, float pixelDragThresholdSquared)
        {
            return nativeButtons.Process(evtData, pixelDragThresholdSquared);
        }

        private void TestEnterExit(PointerEventData evtData)
        {
            var target = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(evtData.pointerCurrentRaycast.gameObject);
            if (target != evtData.pointerEnter)
            {
                evtData.clickCount = -1;
                if (evtData.pointerEnter != null)
                {
                    ExecuteEvents.ExecuteHierarchy(evtData.pointerEnter, evtData, ExecuteEvents.pointerExitHandler);
                    PlayInteraction(Interaction.Exited);
                    evtData.pointerEnter = null;
                }

                if (target != null)
                {
                    evtData.pointerEnter = ExecuteEvents.ExecuteHierarchy(target, evtData, ExecuteEvents.pointerEnterHandler);
                    PlayInteraction(Interaction.Entered);
                }
            }
        }

        public virtual bool IsButtonPressed(InputButton button)
        {
            return nativeButtons.IsButtonPressed(button);
        }

        public virtual bool IsButtonUp(InputButton button)
        {
            return nativeButtons.IsButtonUp(button);
        }

        public virtual bool IsButtonDown(InputButton button)
        {
            return nativeButtons.IsButtonDown(button);
        }

        public abstract bool IsButtonPressed(ButtonIDType button);

        public abstract bool IsButtonDown(ButtonIDType button);

        public abstract bool IsButtonUp(ButtonIDType button);

        protected abstract void InternalUpdate();
    }
}
