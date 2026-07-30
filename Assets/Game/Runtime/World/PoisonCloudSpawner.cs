using System.Collections;
using UnityEngine;

namespace SimpleGame
{
    public sealed class PoisonCloudSpawner : MonoBehaviour
    {
        [SerializeField] private PrototypeGameSession session;
        [SerializeField] private PlayerRoot player;
        [SerializeField] private MushroomPoisonCloud cloudPrefab;
        [SerializeField] private Transform cloudRoot;

        public void Configure(
            PrototypeGameSession configuredSession,
            PlayerRoot configuredPlayer,
            MushroomPoisonCloud configuredPrefab,
            Transform configuredRoot)
        {
            session = configuredSession;
            player = configuredPlayer;
            cloudPrefab = configuredPrefab;
            cloudRoot = configuredRoot;
        }

        public void Schedule(Vector2 position)
        {
            if (cloudPrefab != null)
            {
                StartCoroutine(SpawnAfterDelay(position));
            }
        }

        private IEnumerator SpawnAfterDelay(Vector2 position)
        {
            yield return new WaitForSeconds(
                MushroomPoisonCloud.SpawnDelay);
            if (session == null ||
                player == null ||
                cloudPrefab == null ||
                !session.IsPlaying)
            {
                yield break;
            }

            MushroomPoisonCloud cloud = Instantiate(
                cloudPrefab,
                position,
                Quaternion.identity,
                cloudRoot);
            cloud.Configure(player);
        }
    }
}
