# Option Panel Left-Dim Entrance Design

## Goal

When the player clicks the Restore Truth button, the left 1240-by-1080 scene area gradually darkens. The option buttons appear only after the dimming completes. Closing the option panel remains immediate and restores the scene brightness without an exit animation.

## Existing Flow

The existing request path remains unchanged:

`CaseBoardPanel.OnTruthClicked` -> `CaseBoradPresenter.HandleTruthClick` -> `FlowController.ShowOption(true)` -> `UIManager.ShowPanel<OptionPanel>`

`OptionPanel.prefab` already contains a `Mask` image sized and positioned over the left scene area. Its image color has an alpha of 0.392, which remains the final darkness level. `OptionsGroup` already contains the three option buttons.

## Architecture

`OptionPanel` owns its entrance animation. The global `ScreenFader` is not used because it covers the full screen and would also darken the right-side case board.

The prefab gains separate `CanvasGroup` components on `Mask` and `OptionsGroup`. `OptionPanel` receives serialized references to those groups and a serialized entrance duration with a default of 0.5 seconds.

The mask image keeps its existing color. Animating the mask `CanvasGroup.alpha` from 0 to 1 therefore produces the existing 39.2% black overlay as its final visual state.

## Opening Sequence

On opening the panel:

1. Cancel any previous entrance coroutine owned by the instance.
2. Set the mask group alpha to 0 while leaving it able to intercept raycasts over the left scene area.
3. Set the options group alpha to 0, disable its interaction, and disable its raycast blocking.
4. Animate the mask group alpha from 0 to 1 over the configured duration using `Time.unscaledDeltaTime`.
5. Set the options group alpha to 1, enable its interaction, and enable its raycast blocking.

The option buttons appear immediately after the dimming finishes; they do not receive a second fade animation.

## Closing and Interruption

Closing continues to call `UIManager.HidePanel("option_panel")`. The panel is released immediately, so the left scene returns to full brightness without animation.

If the player clicks Restore Truth again while the entrance animation is running, the panel closes immediately. Destroying or disabling the panel stops its coroutine. Reopening creates a fresh panel instance and replays the entrance sequence from the beginning.

## Initialization Safety

`OptionPanel` moves its button-reference initialization from `Start` to `Awake` and calls `base.Awake()`. This removes the existing ordering dependency between `OptionPanel.Start` and `OptionPresenter.Start`, ensuring option text binding always receives a populated button list.

If either required `CanvasGroup` reference is missing, `OptionPanel` logs an error and falls back to showing the mask and options immediately so the player is not trapped on a non-interactive panel.

## Scope

Files to modify during implementation:

- `Assets/Scripts/UI/OptionPanel.cs`
- `Assets/Prefabs/UI/OptionPanel.prefab`

No changes are required in the case scenes, `CaseBoardPanel`, `CaseBoradPresenter`, `FlowController`, `OptionPresenter`, or `ScreenFader`.

## Verification

- Clicking Restore Truth gradually dims only the left scene area.
- The right case board remains visually unchanged and usable.
- Option buttons remain invisible and non-interactive until dimming completes.
- After 0.5 seconds, all configured option texts appear and the buttons work normally.
- Closing before or after completion is immediate and restores the left scene brightness.
- Reopening replays the entrance from the beginning.
- The entrance still completes when `Time.timeScale` is zero.
- Missing prefab references produce a clear error and fall back to a usable immediate-open state.
