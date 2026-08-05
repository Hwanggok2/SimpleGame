# Repository Agent Rules

## Preserve user-authored Unity assets

Existing `.prefab`, `.unity`, `.asset`, animation, material, and UI files are
user-authored assets. Their hierarchy and visual values must be preserved unless
the user explicitly asks to redesign or replace them.

- Before creating an asset, check whether the target path already exists.
- A generator may save to a path only when no asset exists there, or when the
  user explicitly requests full replacement of that exact asset.
- Never treat a missing script, missing serialized reference, old schema, or
  failed validation as permission to regenerate an existing asset.
- Upgrade existing prefabs in place with `PrefabUtility.LoadPrefabContents`:
  add or configure only the required component and serialized references, then
  save the same prefab without changing its hierarchy, `RectTransform`, text,
  colors, sprites, fonts, materials, or animation data.
- If the expected child structure is missing, stop and report the missing path.
  Do not fall back to a `Create*Prefab` method.
- `Create*Prefab` methods are fallback creation paths for genuinely missing
  assets, not repair paths for existing assets.
- Before and after a prefab migration, compare the asset diff. A reference-only
  migration must not produce broad hierarchy or visual-property changes.
- When visual preservation matters, verify representative serialized values and
  inspect or render the result before reporting completion.

## Incident to avoid: ControlSettingsPanel overwrite

On 2026-08-05, `EnsureControlSettingsPanelPrefab` treated the absence of the new
`ControlSettingsPanelView` as a broken prefab and called
`CreateControlSettingsPanelPrefab`. That generator saved to the existing shared
prefab path and replaced a manually designed control-settings UI. Discarding the
parent `PauseDetailsPanel` could not restore it because the shared control panel
was a separate prefab asset.

For this and similar cases, restore the original asset first and attach the new
View/component references in place. Never reconstruct the visual prefab merely
to add code ownership or serialized links.
