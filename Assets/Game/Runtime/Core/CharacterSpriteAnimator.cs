using System;
using UnityEngine;

namespace SimpleGame
{
    public sealed class CharacterSpriteAnimator : MonoBehaviour
    {
        private enum PlaybackState
        {
            Idle,
            Move,
            Guard,
            Attack,
            Hurt
        }

        [SerializeField] private SpriteRenderer spriteRenderer;

        private static Sprite[] lightBanditFrames;
        private static Sprite[] goblinIdleFrames;
        private static Sprite[] goblinMoveFrames;
        private static Sprite[] goblinAttackFrames;
        private static Sprite[] goblinHurtFrames;
        private static Sprite[] skeletonIdleFrames;
        private static Sprite[] skeletonMoveFrames;
        private static Sprite[] skeletonGuardFrames;
        private static Sprite[] skeletonAttackFrames;
        private static Sprite[] skeletonHurtFrames;

        private Sprite[] idleFrames;
        private Sprite[] moveFrames;
        private Sprite[] guardFrames;
        private Sprite[] attackFrames;
        private Sprite[] hurtFrames;
        private PlaybackState state;
        private PlaybackState requestedState;
        private float idleFps;
        private float moveFps;
        private float guardFps;
        private float attackFps;
        private float hurtFps;
        private float frameElapsed;
        private int frameIndex;
        private bool oneShot;
        private bool configured;

        public bool ConfigureLightBandit(SpriteRenderer target)
        {
            Sprite[] frames = LoadFrames(
                ref lightBanditFrames,
                "Bandits - Pixel Art/Sprites/LightBandit");
            if (frames.Length < 34)
            {
                return false;
            }

            Configure(
                target,
                Slice(frames, 0, 4),
                Slice(frames, 8, 8),
                Slice(frames, 0, 4),
                Slice(frames, 16, 8),
                Slice(frames, 32, 2),
                4f,
                10f,
                4f,
                10f,
                8f);
            return true;
        }

        public bool ConfigureGoblin(SpriteRenderer target)
        {
            Sprite[] idle = LoadFrames(
                ref goblinIdleFrames,
                "Monsters Creatures Fantasy/Sprites/Goblin/Idle");
            Sprite[] move = LoadFrames(
                ref goblinMoveFrames,
                "Monsters Creatures Fantasy/Sprites/Goblin/Run");
            Sprite[] attack = LoadFrames(
                ref goblinAttackFrames,
                "Monsters Creatures Fantasy/Sprites/Goblin/Attack1");
            Sprite[] hurt = LoadFrames(
                ref goblinHurtFrames,
                "Monsters Creatures Fantasy/Sprites/Goblin/Take Hit");
            if (idle.Length == 0 ||
                move.Length == 0 ||
                attack.Length == 0 ||
                hurt.Length == 0)
            {
                return false;
            }

            Configure(
                target,
                idle,
                move,
                idle,
                attack,
                hurt,
                12f,
                12f,
                12f,
                16f,
                12f);
            return true;
        }

        public bool ConfigureSkeleton(SpriteRenderer target)
        {
            Sprite[] idle = LoadFrames(
                ref skeletonIdleFrames,
                "Monsters Creatures Fantasy/Sprites/Skeleton/Idle");
            Sprite[] move = LoadFrames(
                ref skeletonMoveFrames,
                "Monsters Creatures Fantasy/Sprites/Skeleton/Walk");
            Sprite[] guard = LoadFrames(
                ref skeletonGuardFrames,
                "Monsters Creatures Fantasy/Sprites/Skeleton/Shield");
            Sprite[] attack = LoadFrames(
                ref skeletonAttackFrames,
                "Monsters Creatures Fantasy/Sprites/Skeleton/Attack1");
            Sprite[] hurt = LoadFrames(
                ref skeletonHurtFrames,
                "Monsters Creatures Fantasy/Sprites/Skeleton/Take Hit");
            if (idle.Length == 0 ||
                move.Length == 0 ||
                guard.Length == 0 ||
                attack.Length == 0 ||
                hurt.Length == 0)
            {
                return false;
            }

            Configure(
                target,
                idle,
                move,
                guard,
                attack,
                hurt,
                12f,
                12f,
                12f,
                16f,
                12f);
            return true;
        }

        public void SetMoving(Vector2 direction)
        {
            Face(direction);
            requestedState = PlaybackState.Move;
            if (!oneShot)
            {
                Play(PlaybackState.Move, false);
            }
        }

        public void SetIdle()
        {
            requestedState = PlaybackState.Idle;
            if (!oneShot)
            {
                Play(PlaybackState.Idle, false);
            }
        }

        public void SetGuard(Vector2 direction)
        {
            Face(direction);
            requestedState = PlaybackState.Guard;
            if (!oneShot)
            {
                Play(PlaybackState.Guard, false);
            }
        }

        public void Face(Vector2 direction)
        {
            if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.01f)
            {
                spriteRenderer.flipX = direction.x < 0f;
            }
        }

        public void PlayAttack(Vector2 direction)
        {
            Face(direction);
            Play(PlaybackState.Attack, true);
        }

        public void PlayHurt(Vector2 direction)
        {
            Face(direction);
            Play(PlaybackState.Hurt, true);
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            Sprite[] frames = CurrentFrames;
            if (frames.Length <= 1)
            {
                return;
            }

            frameElapsed += Time.deltaTime;
            float frameDuration = 1f / CurrentFps;
            while (frameElapsed >= frameDuration)
            {
                frameElapsed -= frameDuration;
                frameIndex++;
                if (frameIndex >= frames.Length)
                {
                    if (oneShot)
                    {
                        oneShot = false;
                        Play(requestedState, true);
                        return;
                    }

                    frameIndex = 0;
                }

                spriteRenderer.sprite = frames[frameIndex];
            }
        }

        private Sprite[] CurrentFrames => state switch
        {
            PlaybackState.Move => moveFrames,
            PlaybackState.Guard => guardFrames,
            PlaybackState.Attack => attackFrames,
            PlaybackState.Hurt => hurtFrames,
            _ => idleFrames
        };

        private float CurrentFps => state switch
        {
            PlaybackState.Move => moveFps,
            PlaybackState.Guard => guardFps,
            PlaybackState.Attack => attackFps,
            PlaybackState.Hurt => hurtFps,
            _ => idleFps
        };

        private void Configure(
            SpriteRenderer target,
            Sprite[] idle,
            Sprite[] move,
            Sprite[] guard,
            Sprite[] attack,
            Sprite[] hurt,
            float idleRate,
            float moveRate,
            float guardRate,
            float attackRate,
            float hurtRate)
        {
            spriteRenderer = target;
            idleFrames = idle;
            moveFrames = move;
            guardFrames = guard;
            attackFrames = attack;
            hurtFrames = hurt;
            idleFps = idleRate;
            moveFps = moveRate;
            guardFps = guardRate;
            attackFps = attackRate;
            hurtFps = hurtRate;
            requestedState = PlaybackState.Idle;
            configured = true;
            Play(PlaybackState.Idle, true);
        }

        private void Play(PlaybackState nextState, bool restart)
        {
            if (!configured || (!restart && state == nextState))
            {
                return;
            }

            state = nextState;
            oneShot = nextState == PlaybackState.Attack ||
                nextState == PlaybackState.Hurt;
            frameElapsed = 0f;
            frameIndex = 0;
            spriteRenderer.sprite = CurrentFrames[0];
        }

        private static Sprite[] LoadFrames(
            ref Sprite[] cache,
            string resourcePath)
        {
            if (cache != null)
            {
                return cache;
            }

            cache = Resources.LoadAll<Sprite>(resourcePath);
            Array.Sort(
                cache,
                (left, right) =>
                    GetFrameIndex(left.name).CompareTo(GetFrameIndex(right.name)));
            return cache;
        }

        private static Sprite[] Slice(Sprite[] source, int start, int count)
        {
            var frames = new Sprite[count];
            Array.Copy(source, start, frames, 0, count);
            return frames;
        }

        private static int GetFrameIndex(string spriteName)
        {
            int separator = spriteName.LastIndexOf('_');
            return separator >= 0 &&
                int.TryParse(spriteName[(separator + 1)..], out int index)
                    ? index
                    : 0;
        }
    }
}
