using UnityEngine;

namespace SimpleGame
{
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;

        public void Configure(float speed)
        {
            moveSpeed = Mathf.Max(0.1f, speed);
        }

        public bool StepTowards(Vector2 destination, float stoppingDistance)
        {
            Vector2 current = transform.position;
            if (Vector2.Distance(current, destination) <= stoppingDistance)
            {
                return true;
            }

            Vector2 next = Vector2.MoveTowards(
                current,
                destination,
                moveSpeed * Time.deltaTime);
            transform.position = new Vector3(next.x, next.y, transform.position.z);
            return Vector2.Distance(next, destination) <= stoppingDistance;
        }
    }
}
