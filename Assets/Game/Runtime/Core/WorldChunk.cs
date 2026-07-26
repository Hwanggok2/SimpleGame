using UnityEngine;

namespace SimpleGame
{
    public sealed class WorldChunk : MonoBehaviour
    {
        [SerializeField] private Vector2Int coordinate;

        public Vector2Int Coordinate => coordinate;

        public void Place(Vector2Int newCoordinate, Vector2 worldSize)
        {
            coordinate = newCoordinate;
            transform.position = new Vector3(
                coordinate.x * worldSize.x,
                coordinate.y * worldSize.y,
                transform.position.z);
            name = $"MapChunk_{coordinate.x}_{coordinate.y}";
        }
    }
}
