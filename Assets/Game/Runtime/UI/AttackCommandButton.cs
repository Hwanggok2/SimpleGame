using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleGame
{
    public sealed class AttackCommandButton :
        MonoBehaviour,
        IPointerDownHandler
    {
        private Action callback;

        public void Bind(Action configuredCallback)
        {
            callback = configuredCallback;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            callback?.Invoke();
        }
    }
}
