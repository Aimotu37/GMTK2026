# BasePanel Opt-In Fade-In Design

## Goal

Make the case-success and case-failure panels fade in when they appear, while keeping their existing immediate-close behavior and leaving every other panel unchanged by default.

## Scope

- Add reusable, opt-in fade-in behavior directly to `BasePanel`.
- Enable the behavior only on `SuccessPanel.prefab` and `DeathPanel.prefab`.
- Add a root `CanvasGroup` to both prefabs. Their existing `CanvasGroup` components belong to child text objects and cannot fade the whole panel.
- Do not add fade-out behavior.
- Do not change `FlowController`, `SuccessPanel`, `DeathPanel`, or their presenters unless implementation reveals a compile-time requirement.

## BasePanel Behavior

`BasePanel` receives serialized `fadeInOnOpen` and `fadeInDuration` settings. Existing panels keep `fadeInOnOpen` disabled by default, preserving their current behavior.

During `Awake`, the panel caches its root `CanvasGroup`. When `Open` activates an opted-in panel, it cancels any previous fade coroutine, sets the group transparent and non-interactive, and starts a fade from alpha zero to one. The animation uses `Time.unscaledDeltaTime` so it continues while gameplay time is paused.

When the fade completes, the group is set exactly to alpha one and interaction and raycasts are enabled. A non-positive duration completes immediately.

`Close` cancels an active fade and follows the existing immediate close path. It does not animate opacity. This also ensures a panel cannot retain a running coroutine or partially transparent state when closed during its entrance.

## Prefab Configuration

Enable the opt-in flag on:

- `Assets/Prefabs/UI/SuccessPanel.prefab`
- `Assets/Prefabs/UI/DeathPanel.prefab`

Both panels use the same initial recommended duration of `0.3` seconds. The serialized duration remains editable per prefab for visual tuning without code changes.

Each prefab also receives a `CanvasGroup` on the same root object as its `SuccessPanel` or `DeathPanel` component. Existing child-level groups remain unchanged because they serve separate text-level behavior.

## Data Flow

The existing flow remains intact:

1. `FlowController` requests the success or death panel through `UIManager.ShowPanel`.
2. `UIManager` mounts and opens the panel as before.
3. `BasePanel.Open` starts the opted-in local fade after activating the object.
4. The panel becomes clickable only after it is fully visible.
5. Existing `UIManager.HidePanel` calls close and release the panel immediately.

## Failure Handling

If fade-in is enabled but the root `CanvasGroup` is missing, `BasePanel` logs a warning and opens the panel fully visible and interactive. The panel must never remain invisible because of invalid prefab configuration.

If a new open request arrives while the entrance coroutine is running, the previous coroutine is stopped and the fade restarts deterministically from alpha zero.

## Verification

- Compile `Assembly-CSharp.csproj`.
- Trigger case success and confirm the entire success panel fades from transparent to opaque in approximately `0.3` seconds and cannot be clicked mid-fade.
- Trigger case failure and confirm the same behavior for the death panel.
- Close either panel and confirm it disappears immediately without fading out.
- Open representative unaffected panels and confirm they still appear immediately.
- Verify behavior while `Time.timeScale` is zero.
