using UnityEngine;

namespace SimpleGame
{
    public sealed class PrototypeArenaVisual : MonoBehaviour
    {
        [SerializeField] private MapBounds mapBounds;

        public void Configure(MapBounds bounds)
        {
            mapBounds = bounds;
        }

        private void Awake()
        {
            if (mapBounds == null)
            {
                return;
            }

            Vector2 size = mapBounds.Max - mapBounds.Min;
            SpriteRenderer background = PrototypeVisualFactory.CreateSprite(
                transform,
                "ArenaBackground",
                new Color(0.18f, 0.38f, 0.16f),
                size,
                -200);
            background.transform.position = (mapBounds.Min + mapBounds.Max) * 0.5f;

            SpriteRenderer lane = PrototypeVisualFactory.CreateSprite(
                transform,
                "CastleLane",
                new Color(0.62f, 0.48f, 0.25f),
                new Vector2(size.x * 0.28f, size.y),
                -190);
            lane.transform.position = (mapBounds.Min + mapBounds.Max) * 0.5f;
        }
    }
}
