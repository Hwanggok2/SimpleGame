using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleGame
{
    public enum ControlLayoutDragTarget
    {
        None,
        Joystick,
        Attack
    }

    public sealed class ControlLayoutDragSurface : MonoBehaviour,
        ICanvasRaycastFilter,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler
    {
        private RectTransform joystick;
        private RectTransform attack;
        private Action<ControlLayoutDragTarget, Vector2, Camera> moved;
        private ControlLayoutDragTarget activeTarget;
        private int activePointerId = int.MinValue;
        private Vector2 grabOffset;
        private bool dragEnabled;

        public void Configure(
            RectTransform joystickControl,
            RectTransform attackControl,
            Action<ControlLayoutDragTarget, Vector2, Camera> onMoved)
        {
            joystick = joystickControl;
            attack = attackControl;
            moved = onMoved;
        }

        public void SetDragEnabled(bool enabled)
        {
            dragEnabled = enabled;
            if (!enabled)
            {
                CancelDrag();
            }
        }

        public bool IsRaycastLocationValid(
            Vector2 screenPoint,
            Camera eventCamera)
        {
            return dragEnabled &&
                (Contains(joystick, screenPoint, eventCamera) ||
                 Contains(attack, screenPoint, eventCamera));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!dragEnabled || activeTarget != ControlLayoutDragTarget.None)
            {
                return;
            }

            bool overJoystick = Contains(
                joystick,
                eventData.position,
                eventData.pressEventCamera);
            bool overAttack = Contains(
                attack,
                eventData.position,
                eventData.pressEventCamera);
            if (!overJoystick && !overAttack)
            {
                return;
            }

            activeTarget = ResolveTarget(
                eventData.position,
                eventData.pressEventCamera,
                overJoystick,
                overAttack);
            activePointerId = eventData.pointerId;
            RectTransform control = GetControl(activeTarget);
            Vector2 center = RectTransformUtility.WorldToScreenPoint(
                eventData.pressEventCamera,
                control.TransformPoint(control.rect.center));
            grabOffset = eventData.position - center;
            Move(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId)
            {
                Move(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId)
            {
                CancelDrag();
            }
        }

        private void Move(PointerEventData eventData)
        {
            if (activeTarget == ControlLayoutDragTarget.None)
            {
                return;
            }

            moved?.Invoke(
                activeTarget,
                eventData.position - grabOffset,
                eventData.pressEventCamera);
        }

        private void CancelDrag()
        {
            activeTarget = ControlLayoutDragTarget.None;
            activePointerId = int.MinValue;
            grabOffset = Vector2.zero;
        }

        private ControlLayoutDragTarget ResolveTarget(
            Vector2 screenPoint,
            Camera eventCamera,
            bool overJoystick,
            bool overAttack)
        {
            if (!overAttack)
            {
                return ControlLayoutDragTarget.Joystick;
            }

            if (!overJoystick)
            {
                return ControlLayoutDragTarget.Attack;
            }

            float joystickDistance = DistanceToCenter(
                joystick,
                screenPoint,
                eventCamera);
            float attackDistance = DistanceToCenter(
                attack,
                screenPoint,
                eventCamera);
            return joystickDistance <= attackDistance
                ? ControlLayoutDragTarget.Joystick
                : ControlLayoutDragTarget.Attack;
        }

        private RectTransform GetControl(ControlLayoutDragTarget target)
        {
            return target == ControlLayoutDragTarget.Joystick
                ? joystick
                : attack;
        }

        private static bool Contains(
            RectTransform control,
            Vector2 screenPoint,
            Camera eventCamera)
        {
            return control != null &&
                control.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    control,
                    screenPoint,
                    eventCamera);
        }

        private static float DistanceToCenter(
            RectTransform control,
            Vector2 screenPoint,
            Camera eventCamera)
        {
            Vector2 center = RectTransformUtility.WorldToScreenPoint(
                eventCamera,
                control.TransformPoint(control.rect.center));
            return (screenPoint - center).sqrMagnitude;
        }
    }
}
