using UnityEngine;

namespace SimpleGame
{
    public sealed partial class FlyingSwordController
    {
        private void EnsureSlotCount(int count)
        {
            if (slots.Count >= count)
            {
                return;
            }

            EnsureVisualRoot();
            while (slots.Count < count)
            {
                SwordSlot slot = CreateSlot(slots.Count);
                if (slot == null)
                {
                    return;
                }

                slots.Add(slot);
            }
        }

        private SwordSlot CreateSlot(int index)
        {
            bool missingReadyIndicator = showReadyIndicators &&
                (readySwordVisuals == null ||
                 index < 0 ||
                 index >= readySwordVisuals.Length ||
                 readySwordVisuals[index] == null);
            if (index < 0 ||
                missingReadyIndicator ||
                attackVisualTemplate == null)
            {
                if (!missingVisualsLogged)
                {
                    Debug.LogError(
                        "Player prefab requires Flying_Sword1..3 " +
                        "and Flying_Sword_Attack visuals.",
                        this);
                    missingVisualsLogged = true;
                }

                return null;
            }

            var root = new GameObject(
                $"FlyingSwordAttackSlot_{index + 1}");
            root.transform.SetParent(visualRoot, false);

            GameObject attackVisual = Instantiate(
                attackVisualTemplate.gameObject,
                root.transform);
            attackVisual.name =
                $"Flying_Sword_Attack_{index + 1}";
            Transform attackTransform = attackVisual.transform;
            Vector3 templateScale =
                attackVisualTemplate.transform.localScale;
            float depthOffset =
                attackVisualTemplate.transform.localPosition.z;
            attackTransform.localPosition =
                new Vector3(0f, 0f, depthOffset);
            attackTransform.localRotation = Quaternion.identity;
            attackTransform.localScale =
                new Vector3(
                    templateScale.x,
                    0f,
                    templateScale.z);

            SpriteRenderer attackRenderer =
                attackVisual.GetComponent<SpriteRenderer>();
            attackRenderer.sortingOrder = Mathf.Max(
                VisualSortingOrder,
                attackRenderer.sortingOrder);
            float spriteHeight =
                attackRenderer.sprite != null
                    ? attackRenderer.sprite.bounds.size.y
                    : 1f;

            SpriteRenderer readyRenderer = showReadyIndicators
                ? readySwordVisuals[index]
                : null;
            if (readyRenderer != null)
            {
                readyRenderer.sortingOrder = Mathf.Max(
                    VisualSortingOrder,
                    readyRenderer.sortingOrder);
                readyRenderer.gameObject.SetActive(false);
            }

            attackVisual.SetActive(false);

            return new SwordSlot(
                root,
                readyRenderer != null
                    ? readyRenderer.gameObject
                    : null,
                attackVisual,
                attackRenderer,
                templateScale.x,
                templateScale.z,
                depthOffset,
                Mathf.Max(
                    Mathf.Epsilon,
                    spriteHeight));
        }

        private void EnsureVisualRoot()
        {
            if (visualRoot == null)
            {
                var rootObject = new GameObject(
                    $"FlyingSwordAttacks_{gameObject.name}");
                visualRoot = rootObject.transform;
            }

            Transform parent =
                owner != null ? owner.transform.parent : null;
            if (visualRoot.parent != parent)
            {
                visualRoot.SetParent(parent, true);
            }
        }

        private void ResolvePrefabVisuals()
        {
            if (showReadyIndicators &&
                (readySwordVisuals == null ||
                 readySwordVisuals.Length != MaximumSwordCount))
            {
                SpriteRenderer[] previous = readySwordVisuals;
                readySwordVisuals =
                    new SpriteRenderer[MaximumSwordCount];
                int copyCount = previous != null
                    ? Mathf.Min(
                        previous.Length,
                        readySwordVisuals.Length)
                    : 0;
                for (int index = 0;
                     index < copyCount;
                     index++)
                {
                    readySwordVisuals[index] =
                        previous[index];
                }
            }

            if (showReadyIndicators)
            {
                for (int index = 0;
                     index < readySwordVisuals.Length;
                     index++)
                {
                    SpriteRenderer readyRenderer =
                        readySwordVisuals[index];
                    if (readyRenderer != null)
                    {
                        readyRenderer.sortingOrder = Mathf.Max(
                            VisualSortingOrder,
                            readyRenderer.sortingOrder);
                        readyRenderer.gameObject.SetActive(false);
                    }
                }
            }

            if (attackVisualTemplate != null)
            {
                attackVisualTemplate.sortingOrder = Mathf.Max(
                    VisualSortingOrder,
                    attackVisualTemplate.sortingOrder);
                attackVisualTemplate.gameObject.SetActive(false);
            }
        }

        private void RefreshReadyIndicators()
        {
            if (!showReadyIndicators || readySwordVisuals == null)
            {
                return;
            }

            bool canShow =
                owner != null &&
                owner.IsAlive;
            for (int index = 0;
                 index < readySwordVisuals.Length;
                 index++)
            {
                SpriteRenderer readyRenderer =
                    readySwordVisuals[index];
                if (readyRenderer == null)
                {
                    continue;
                }

                bool ready = canShow &&
                    index < swordCountLevel &&
                    index < slots.Count &&
                    slots[index].State == SwordState.Ready;
                readyRenderer.gameObject.SetActive(ready);
            }
        }

        private static void RefreshAttackVisual(
            SwordSlot slot,
            float alpha = 1f)
        {
            float length = Mathf.Max(
                0f,
                Vector2.Dot(
                    (Vector2)slot.Transform.position -
                    slot.AttackOrigin,
                    slot.Direction));
            Transform attackTransform =
                slot.AttackVisual.transform;
            attackTransform.localPosition =
                new Vector3(
                    0f,
                    -length * 0.5f,
                    slot.AttackDepthOffset);
            attackTransform.localScale =
                new Vector3(
                    slot.AttackWidthScale,
                    length / slot.AttackSpriteHeight,
                    slot.AttackDepthScale);

            Color color = slot.AttackBaseColor;
            color.a *= Mathf.Clamp01(alpha);
            slot.AttackRenderer.color = color;
        }

        private static void RestoreAttackColor(
            SwordSlot slot)
        {
            slot.AttackRenderer.color =
                slot.AttackBaseColor;
        }

        private static void HideSlotVisuals(
            SwordSlot slot)
        {
            slot.IndicatorVisual?.SetActive(false);
            slot.AttackVisual.SetActive(false);
            RestoreAttackColor(slot);
        }

        private void CancelFlights()
        {
            foreach (SwordSlot slot in slots)
            {
                if (slot.State != SwordState.Approaching &&
                    slot.State != SwordState.Passing)
                {
                    continue;
                }

                slot.State = SwordState.Cooling;
                slot.PrimaryTarget = null;
                slot.HitEnemyGenerations.Clear();
                slot.AttackVisual.SetActive(false);
                RestoreAttackColor(slot);
                slot.ReadyAt = Mathf.Max(
                    slot.ReadyAt,
                    Time.time);
            }

            hitCandidates.Clear();
        }

        private void HideAllVisuals()
        {
            foreach (SwordSlot slot in slots)
            {
                HideSlotVisuals(slot);
            }

            if (showReadyIndicators && readySwordVisuals != null)
            {
                foreach (SpriteRenderer readyRenderer in
                         readySwordVisuals)
                {
                    if (readyRenderer != null)
                    {
                        readyRenderer.gameObject.SetActive(false);
                    }
                }
            }

            if (attackVisualTemplate != null)
            {
                attackVisualTemplate.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            CancelFlights();
            HideAllVisuals();
        }

        private static void SetWorldPosition(
            SwordSlot slot,
            Vector2 position)
        {
            Vector3 current = slot.Transform.position;
            slot.Transform.position = new Vector3(
                position.x,
                position.y,
                current.z);
        }

        private static void FaceDirection(
            Transform target,
            Vector2 direction)
        {
            float angle =
                Mathf.Atan2(direction.y, direction.x) *
                Mathf.Rad2Deg -
                90f;
            target.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnDestroy()
        {
            if (visualRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(visualRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(visualRoot.gameObject);
                }
            }

        }
    }
}
