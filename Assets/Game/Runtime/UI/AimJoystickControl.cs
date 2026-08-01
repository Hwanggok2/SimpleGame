using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleGame
{
    public sealed class AimJoystickControl :
        MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler
    {
        private const int NoPointer = int.MinValue;

        [SerializeField] private RectTransform touchArea;
        [SerializeField] private RectTransform knob;

        private PlayerRoot player;
        private int activePointerId = NoPointer;

        public RectTransform TouchArea => touchArea;
        public RectTransform Knob => knob;
        public Vector2 NormalizedInput { get; private set; }
        public bool IsHeld => activePointerId != NoPointer;

        public void Configure(
            RectTransform configuredTouchArea,
            RectTransform configuredKnob)
        {
            touchArea = configuredTouchArea;
            knob = configuredKnob;
            ResetVisual();
        }

        public void Initialize(PlayerRoot configuredPlayer)
        {
            ReleaseAim();
            player = configuredPlayer;
        }

        public void CancelInput()
        {
            ReleaseAim();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null ||
                activePointerId != NoPointer ||
                player == null ||
                !player.BeginControlInput())
            {
                return;
            }

            activePointerId = eventData.pointerId;
            UpdateInput(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData == null ||
                eventData.pointerId != activePointerId)
            {
                return;
            }

            UpdateInput(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData == null ||
                eventData.pointerId != activePointerId)
            {
                return;
            }

            ReleaseAim();
        }

        public static Vector2 NormalizePadOffset(
            Vector2 localOffset,
            float padRadius)
        {
            if (padRadius <= 0f)
            {
                return Vector2.zero;
            }

            return Vector2.ClampMagnitude(
                localOffset / padRadius,
                1f);
        }

        private void OnDisable()
        {
            ReleaseAim();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                ReleaseAim();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                ReleaseAim();
            }
        }

        private void UpdateInput(PointerEventData eventData)
        {
            if (touchArea == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    touchArea,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            float padRadius =
                Mathf.Min(
                    touchArea.rect.width,
                    touchArea.rect.height) *
                0.5f;
            NormalizedInput = NormalizePadOffset(
                localPoint,
                padRadius);
            if (knob != null)
            {
                float knobRadius =
                    Mathf.Max(
                        knob.rect.width,
                        knob.rect.height) *
                    0.5f;
                float knobTravel =
                    Mathf.Max(0f, padRadius - knobRadius);
                knob.anchoredPosition =
                    NormalizedInput * knobTravel;
            }

            player.SetControlInput(NormalizedInput);
        }

        private void ReleaseAim()
        {
            activePointerId = NoPointer;
            NormalizedInput = Vector2.zero;
            ResetVisual();
            if (player != null)
            {
                player.EndControlInput();
            }
        }

        private void ResetVisual()
        {
            if (knob != null)
            {
                knob.anchoredPosition = Vector2.zero;
            }
        }
    }
}
