using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed class WorldChunkGrid : MonoBehaviour
    {
        private const int GridRadius = 1;

        [SerializeField] private Transform target;
        [SerializeField] private Vector2 chunkWorldSize =
            new(20.48f, 20.48f);
        [SerializeField] private List<WorldChunk> chunks = new();

        private Vector2Int centerCoordinate;

        public IReadOnlyList<WorldChunk> Chunks => chunks;
        public Vector2Int CenterCoordinate => centerCoordinate;

        public void Configure(
            Transform followTarget,
            Vector2 size,
            IEnumerable<WorldChunk> activeChunks)
        {
            target = followTarget;
            chunkWorldSize = size;
            chunks = new List<WorldChunk>(activeChunks);
            Recenter(true);
        }

        private void LateUpdate()
        {
            Recenter(false);
        }

        private void Recenter(bool force)
        {
            if (target == null || chunks.Count != 9)
            {
                return;
            }

            Vector2Int nextCenter = new(
                Mathf.RoundToInt(target.position.x / chunkWorldSize.x),
                Mathf.RoundToInt(target.position.y / chunkWorldSize.y));
            if (!force && nextCenter == centerCoordinate)
            {
                return;
            }

            centerCoordinate = nextCenter;
            var required = new HashSet<Vector2Int>();
            for (int y = -GridRadius; y <= GridRadius; y++)
            {
                for (int x = -GridRadius; x <= GridRadius; x++)
                {
                    required.Add(centerCoordinate + new Vector2Int(x, y));
                }
            }

            var reusable = new Queue<WorldChunk>();
            foreach (WorldChunk chunk in chunks)
            {
                if (!required.Remove(chunk.Coordinate))
                {
                    reusable.Enqueue(chunk);
                }
            }

            foreach (Vector2Int coordinate in required)
            {
                reusable.Dequeue().Place(coordinate, chunkWorldSize);
            }
        }
    }
}
