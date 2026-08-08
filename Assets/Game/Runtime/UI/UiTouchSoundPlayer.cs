using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SimpleGame
{
    public sealed class UiTouchSoundPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip touchClip;

        private readonly List<RaycastResult> raycastResults = new();
        private int lastPlayedFrame = -1;

        public AudioClip TouchClip => touchClip;

        public void Configure(AudioClip configuredTouchClip)
        {
            touchClip = configuredTouchClip;
        }

        public void Play()
        {
            if (lastPlayedFrame == Time.frameCount)
            {
                return;
            }

            if (GameManager.Instance.PlaySoundEffect(
                    GameAudioIds.UiTouch,
                    touchClip))
            {
                lastPlayedFrame = Time.frameCount;
            }
        }

        public static bool ShouldPlayFor(GameObject hitObject)
        {
            if (hitObject == null ||
                hitObject.GetComponentInParent<AimJoystickControl>() != null)
            {
                return false;
            }

            Selectable selectable =
                hitObject.GetComponentInParent<Selectable>();
            if (selectable == null ||
                !selectable.isActiveAndEnabled)
            {
                return false;
            }

            return selectable.GetComponent<AttackCommandButton>() == null;
        }

        private void Update()
        {
            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryPlayAt(Mouse.current.position.ReadValue(), -1);
            }

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return;
            }

            foreach (var touch in touchscreen.touches)
            {
                if (touch.press.wasPressedThisFrame)
                {
                    TryPlayAt(
                        touch.position.ReadValue(),
                        touch.touchId.ReadValue());
                }
            }
        }

        private void TryPlayAt(Vector2 screenPosition, int pointerId)
        {
            if (EventSystem.current == null)
            {
                return;
            }

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition,
                pointerId = pointerId
            };
            raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            if (raycastResults.Count == 0 ||
                !ShouldPlayFor(raycastResults[0].gameObject))
            {
                return;
            }

            Play();
        }
    }
}
