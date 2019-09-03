using System;

using Juniper.Input;
using Juniper.Events;

using UnityEngine;
using UnityEngine.EventSystems;

using InputButton = UnityEngine.EventSystems.PointerEventData.InputButton;

namespace Juniper.Input.Pointers
{
    public class MappedButton<ButtonIDType>
        where ButtonIDType : struct
    {
        private const float THRESHOLD_CLICK = 0.4f;
        private const float THRESHOLD_LONG_PRESS = 2f;

        public ButtonIDType button;
        public int buttonNumber;
        private string buttonName;

        private float buttonDownTime;
        private float dragDistance;
        private bool mayLongPress;

        public event Action<Interaction> InteractionNeeded;

        public event Func<ButtonIDType, bool> ButtonDownNeeded;

        public event Func<ButtonIDType, bool> ButtonUpNeeded;

        public event Func<ButtonIDType, bool> ButtonPressedNeeded;

        public event Func<int, PointerEventData, PointerEventData> ClonedPointerEventNeeded;

        private readonly ButtonEvent buttonEvent;

        public InputButton? UnityInputButton
        {
            get
            {
                if (buttonEvent.inputButton == InputEventButton.None)
                {
                    return null;
                }
                else
                {
                    return (InputButton)buttonEvent.inputButton;
                }
            }
        }

        public MappedButton(ButtonIDType btn, InputEventButton inputBtn, GameObject eventParent)
        {
            SetButton(btn);
            var key = ButtonEvent.MakeKey(button);
            var btns = eventParent.GetComponents<ButtonEvent>();
            buttonEvent = Array.Find(btns, e => e.Key == key)
                ?? Array.Find(btns, e => e.inputButton == inputBtn)
                ?? eventParent.AddComponent<ButtonEvent>();
            buttonEvent.Key = key;
            buttonEvent.inputButton = inputBtn;
        }

        public void Destroy()
        {
            buttonEvent.DestroyImmediate();
        }

        private void SetButton(ButtonIDType value)
        {
            button = value;
            buttonName = value.ToString();
            buttonNumber = Convert.ToInt32(value);
        }

        public bool IsDown
        {
            get; private set;
        }

        public bool IsUp
        {
            get; private set;
        }

        public bool IsPressed
        {
            get; private set;
        }

        public bool IsDragging
        {
            get; private set;
        }

        public IEventSystemHandler Process(PointerEventData eventData, float pixelDragThresholdSquared)
        {
            if (buttonEvent.buttonValueName != buttonName)
            {
                SetButton(buttonEvent.GetButtonValue<ButtonIDType>());
            }

            IsPressed = ButtonPressedNeeded(button);
            var evtData = ClonedPointerEventNeeded(buttonEvent.GetInstanceID(), eventData);
            evtData.button = (InputButton)buttonEvent.inputButton;

            TestUpDown(evtData);
            TestDrag(evtData, pixelDragThresholdSquared);

            return evtData.pointerEnter?.GetComponent<IEventSystemHandler>();
        }

        private void TestUpDown(PointerEventData evtData)
        {
            IsUp = ButtonUpNeeded(button);
            IsDown = ButtonDownNeeded(button);
            if (IsDown)
            {
                mayLongPress = true;
                buttonDownTime = Time.unscaledTime;
                dragDistance = 0;
                evtData.rawPointerPress = evtData.pointerEnter;
                evtData.pressPosition = evtData.position;
                evtData.pointerPressRaycast = evtData.pointerCurrentRaycast;
                evtData.eligibleForClick = true;
                evtData.pointerPress = ExecuteEvents.ExecuteHierarchy(evtData.pointerEnter, evtData, ExecuteEvents.pointerDownHandler);
                evtData.pointerDrag = ExecuteEvents.ExecuteHierarchy(evtData.pointerEnter, evtData, ExecuteEvents.initializePotentialDrag);
                buttonEvent.OnDown(evtData);
                if (evtData.pointerPress != null)
                {
                    InteractionNeeded(Interaction.Pressed);
                }
            }
            
            var deltaTime = Time.unscaledTime - buttonDownTime;
            evtData.eligibleForClick = deltaTime < THRESHOLD_CLICK;

            if (IsUp)
            {
                ExecuteEvents.ExecuteHierarchy(evtData.pointerPress, evtData, ExecuteEvents.pointerUpHandler);
                buttonEvent.OnUp(evtData);
                if (evtData.pointerPress != null)
                {
                    InteractionNeeded(Interaction.Released);
                }

                var target = evtData.pointerCurrentRaycast.gameObject;
                if (evtData.eligibleForClick)
                {
                    ++evtData.clickCount;
                    evtData.clickTime = Time.unscaledTime;

                    evtData.selectedObject = ExecuteEvents.ExecuteHierarchy(evtData.pointerPress, evtData, ExecuteEvents.pointerClickHandler);
                    buttonEvent.OnClick(evtData);
                    if (evtData.pointerPress != null)
                    {
                        InteractionNeeded(Interaction.Clicked);
                    }
                }
                else if (evtData.pointerDrag != null)
                {
                    ExecuteEvents.ExecuteHierarchy(target, evtData, ExecuteEvents.dropHandler);
                }

                evtData.pointerPress = null;
                evtData.rawPointerPress = null;
            }
            else if (IsPressed && mayLongPress)
            {
                if (deltaTime < THRESHOLD_LONG_PRESS)
                {
                    ExecuteEvents.ExecuteHierarchy(evtData.pointerPress, evtData, LongPressEvents.longPressUpdateHandler);
                }
                else
                {
                    mayLongPress = false;
                    ExecuteEvents.ExecuteHierarchy(evtData.pointerPress, evtData, LongPressEvents.longPressHandler);
                    buttonEvent.OnLongPress(evtData);
                    if (evtData.pointerPress != null)
                    {
                        InteractionNeeded(Interaction.Clicked);
                    }
                }
            }
        }

        private void TestDrag(PointerEventData evtData, float pixelDragThresholdSquared)
        {
            if (evtData.pointerDrag != null && evtData.IsPointerMoving())
            {
                var wasDragging = evtData.dragging;
                if (!IsPressed)
                {
                    evtData.dragging = false;
                }
                else if (!evtData.useDragThreshold)
                {
                    evtData.dragging = true;
                }
                else
                {
                    dragDistance += evtData.delta.sqrMagnitude;
                    evtData.dragging = dragDistance > pixelDragThresholdSquared;
                }

                if (evtData.dragging)
                {
                    if (evtData.pointerPress != null && evtData.pointerPress != evtData.pointerDrag)
                    {
                        ExecuteEvents.Execute(evtData.pointerPress, evtData, ExecuteEvents.pointerUpHandler);
                        InteractionNeeded(Interaction.Released);

                        evtData.eligibleForClick = false;
                        evtData.pointerPress = null;
                        evtData.rawPointerPress = null;
                    }

                    if (!wasDragging)
                    {
                        mayLongPress = false;
                        ExecuteEvents.ExecuteHierarchy(evtData.pointerDrag, evtData, ExecuteEvents.beginDragHandler);
                        InteractionNeeded(Interaction.DraggingStarted);
                        IsDragging = true;
                    }

                    evtData.pointerDrag = ExecuteEvents.ExecuteHierarchy(evtData.pointerDrag ?? evtData.pointerPress, evtData, ExecuteEvents.dragHandler);
                    InteractionNeeded(Interaction.Dragged);
                }
                else if (wasDragging && !IsPressed)
                {
                    ExecuteEvents.ExecuteHierarchy(evtData.pointerDrag, evtData, ExecuteEvents.endDragHandler);
                    InteractionNeeded(Interaction.DraggingEnded);
                    evtData.pointerDrag = null;
                    IsDragging = false;
                }
            }
        }
    }
}
