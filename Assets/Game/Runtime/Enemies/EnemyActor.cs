using UnityEngine;

namespace SimpleGame
{
    public sealed class EnemyActor : EnemyBase
    {
        [SerializeField] private EnemyArchetype archetype;

        public override EnemyArchetype Archetype => archetype;

        public void ConfigureArchetype(
            EnemyArchetype configuredArchetype)
        {
            archetype = configuredArchetype;
        }
    }
}
