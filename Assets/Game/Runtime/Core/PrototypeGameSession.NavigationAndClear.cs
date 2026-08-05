using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimpleGame
{
    public sealed partial class PrototypeGameSession
    {
        public const string LobbySceneName = "Lobby";
        public const float ClearSlowMotionScale = 0.2f;
        public const float ClearZoomMultiplier = 0.65f;

        public void RestartRun()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ReturnToLobby()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(LobbySceneName);
        }

        private void BeginClear(Vector2 defeatedBossPosition)
        {
            if (state == GameRunState.Clear)
            {
                return;
            }

            state = GameRunState.Clear;
            pendingCardSelections = 0;
            pendingBossRewardSelections = 0;
            selectingStartingCards = false;
            currentCardChoices.Clear();
            currentCardHistory.Clear();
            SetCardChoicesInteractable(false);
            PauseVisibilityChanged?.Invoke(false);
            CardSelectionVisibilityChanged?.Invoke(false);
            StartCoroutine(PlayClearSequence(defeatedBossPosition));
        }

        private IEnumerator PlayClearSequence(Vector2 focusPosition)
        {
            Time.timeScale = ClearSlowMotionScale;
            CameraFollowController follow = worldCamera != null
                ? worldCamera.GetComponent<CameraFollowController>()
                : null;
            Vector3 startPosition = worldCamera != null
                ? worldCamera.transform.position
                : Vector3.zero;
            float startSize = worldCamera != null
                ? worldCamera.orthographicSize
                : 1f;
            if (follow != null)
            {
                follow.enabled = false;
            }

            Vector3 focusCameraPosition = new(
                focusPosition.x,
                focusPosition.y,
                startPosition.z);
            yield return AnimateClearCamera(
                startPosition,
                focusCameraPosition,
                startSize,
                startSize * ClearZoomMultiplier,
                0.55f);
            yield return new WaitForSecondsRealtime(0.65f);
            yield return AnimateClearCamera(
                focusCameraPosition,
                startPosition,
                startSize * ClearZoomMultiplier,
                startSize,
                0.55f);

            if (follow != null)
            {
                follow.enabled = true;
                follow.SnapToTarget();
            }

            Time.timeScale = 0f;
            ClearVisibilityChanged?.Invoke(true);
        }

        private IEnumerator AnimateClearCamera(
            Vector3 fromPosition,
            Vector3 toPosition,
            float fromSize,
            float toSize,
            float duration)
        {
            if (worldCamera == null)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed / duration));
                worldCamera.transform.position = Vector3.Lerp(
                    fromPosition,
                    toPosition,
                    progress);
                worldCamera.orthographicSize = Mathf.Lerp(
                    fromSize,
                    toSize,
                    progress);
                yield return null;
            }
        }
    }
}
