using UnityEngine;

namespace SimpleGame
{
    public sealed class EnemyFacing : MonoBehaviour
    {
        [SerializeField] private Vector2 direction = Vector2.down;

        public Vector2 Direction => direction;

        public void Face(Vector2 targetPosition)
        {
            Vector2 next = targetPosition - (Vector2)transform.position;
            if (next.sqrMagnitude > 0.0001f)
            {
                direction = next.normalized;
            }
        }
    }
}
