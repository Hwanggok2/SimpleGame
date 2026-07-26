using UnityEngine;

namespace SimpleGame
{
    public sealed class PrototypeArenaVisual : MonoBehaviour
    {
        [SerializeField] private MapBounds mapBounds;
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private SpriteRenderer lane;

        public void Configure(
            MapBounds bounds,
            SpriteRenderer configuredBackground,
            SpriteRenderer configuredLane)
        {
            mapBounds = bounds;
            background = configuredBackground;
            lane = configuredLane;

            Vector2 size = mapBounds.Max - mapBounds.Min;
            background.transform.localScale = new Vector3(size.x, size.y, 1f);
            background.transform.position = (mapBounds.Min + mapBounds.Max) * 0.5f;

            lane.transform.localScale =
                new Vector3(size.x * 0.28f, size.y, 1f);
            lane.transform.position = (mapBounds.Min + mapBounds.Max) * 0.5f;
        }
    }
}
