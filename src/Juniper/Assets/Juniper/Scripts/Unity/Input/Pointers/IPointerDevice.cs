using System;

using UnityEngine;
using UnityEngine.EventSystems;

namespace Juniper.Unity.Input.Pointers
{
    public interface IPointerDevice
    {
        Transform transform { get;  }

        IEventSystemHandler EventTarget
        {
            get; set;
        }

        IInputModule InputModule
        {
            get;
        }

        /// <summary>
        /// Unique pointer identifiers keep the pointer events cached in Unity's Event System.
        /// </summary>
        /// <value>The pointer identifier.</value>
        int PointerDataID
        {
            get;
        }

        string ProbeName
        {
            get;
        }

        Type ButtonType
        {
            get;
        }

        bool isActiveAndEnabled
        {
            get;
        }

        bool IsConnected
        {
            get;
        }

        bool IsDisabled
        {
            get;
        }

        bool IsEnabled
        {
            get;
        }

        bool IsDragging
        {
            get;
        }

        PhysicsRaycaster Raycaster
        {
            get;
        }

        float MaximumPointerDistance
        {
            get;
        }

        float MinimumPointerDistance
        {
            get;
        }

        Vector3 Origin
        {
            get;
        }

        Vector3 LastOrigin
        {
            get;
        }

        Vector3 OriginDelta
        {
            get;
        }

        Vector3 WorldPoint
        {
            get;
        }

        Vector3 LastWorldPoint
        {
            get;
        }

        Vector3 WorldDelta
        {
            get;
        }

        Vector3 SmoothedWorldPoint
        {
            get;
        }

        Vector3 LastSmoothedWorldPoint
        {
            get;
        }

        Vector3 SmoothedWorldPointDelta
        {
            get;
        }

        Vector3 Direction
        {
            get;
        }

        Vector3 LastDirection
        {
            get;
        }

        Vector2 ViewportPoint
        {
            get;
        }

        Vector2 LastViewportPoint
        {
            get;
        }

        Vector2 ViewportDelta
        {
            get;
        }

        Vector2 ScreenPoint
        {
            get;
        }

        Vector2 LastScreenPoint
        {
            get;
        }

        Vector2 ScreenDelta
        {
            get;
        }

        Vector2 ScrollDelta
        {
            get;
        }

        Vector3 ViewportFromWorld(Vector3 worldPoint);

        Vector3 ViewportFromScreen(Vector3 screenPoint);

        Vector3 ScreenFromWorld(Vector3 worldPoint);

        Vector3 ScreenFromViewport(Vector3 viewportPoint);

        Vector3 WorldFromScreen(Vector3 screenPoint);

        Vector3 WorldFromViewport(Vector3 viewportPoint);

        bool IsButtonDown(PointerEventData.InputButton button);

        bool IsButtonPressed(PointerEventData.InputButton button);

        bool IsButtonUp(PointerEventData.InputButton button);

        void Process(PointerEventData evtData, float pixelDragThresholdSquared);

        void OnProbeFound();
    }
}
