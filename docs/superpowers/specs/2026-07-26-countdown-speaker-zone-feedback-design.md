# Countdown and Speaker-Zone Feedback Design

## Goal

Make successful item use feel physical and make every real loss of speaking attempts immediately legible.

- When the speaking-attempt count decreases, keep the old number visible, enlarge it, return it to its normal size, then replace it with the new number while applying a short shake and a UI particle burst.
- When an item is accepted by the speaker zone, shake the speaker-zone visual.

## Scope

The feedback applies to every count decrease, including the normal item cost and curse-driven multi-count loss. Initialization, retry/reset, and count increases update immediately without feedback. A curse that changes `4` directly to `2` plays one `4 -> 2` feedback sequence rather than two stepped sequences.

The feature does not change item acceptance, count calculation, failure conditions, clue flow, audio behavior, or save data. It adds no tweening or UI-particle dependency.

## Architecture

### Count feedback ownership

`DemonPanel` remains the UI endpoint for `GameEvents.CountChanged`. It stores whether a count has been displayed and the last displayed value.

- The first received value initializes the text directly.
- A value equal to or greater than the displayed value updates directly.
- A lower value delegates the transition from the displayed value to the new value to a dedicated `CountDecreaseFeedback` component.

The existing `CountChanged<int>` event remains unchanged. Comparing the last displayed value keeps initialization and reset behavior in the presentation layer and avoids coupling `GameManager` to visual effects. The currently unused `CounterManager` is outside this change.

`CountDecreaseFeedback` owns the animation coroutine, the number's baseline transform, and a small pool of UI particle images. Its public operation accepts the old and new values; consumers do not manage animation timing.

### Speaker-zone feedback ownership

`SpeakerZone` exposes a successful-drop feedback operation and delegates the animation to a `SpeakerZoneFeedback` component. `InteractionController` retains the active `SpeakerZone` reference when `SetSpeakerZone` is called.

On an accepted drop, `InteractionController` first calls `Item.OnDragEnd(true)`. This captures the stable speaker-zone position for the item's existing snap animation. It then triggers speaker-zone feedback. The ordering prevents a shaking transform from changing the item's snap target.

The speaker-zone feedback shakes only a configured visual transform. The collider and magnetic target remain stationary. Existing scenes may default that visual to the speaker zone's `SpriteRenderer` transform, but the target is serialized so a child visual can be assigned without changing code.

## Animation Behavior

### Count decrease sequence

1. Keep the old number visible at baseline position and scale.
2. Ease from scale `1.0` to a configurable enlarged scale, initially `1.25`.
3. Ease back to scale `1.0`.
4. At the impact point, replace the old value with the final new value.
5. Start a short decaying shake and emit one radial UI particle burst at the same time.
6. Restore the exact baseline position and scale when the effect finishes.

The initial timing target is approximately `0.10s` enlarge, `0.10s` return, and `0.14s` impact shake. Scale, durations, shake amplitude, particle count, spread, color, lifetime, and distance are serialized for tuning in the prefab.

### Speaker-zone sequence

The accepted-drop response is a short decaying local-position shake, initially about `0.16s`. It does not scale or rotate the zone and always restores the exact baseline local position.

### Interruption policy

Both feedback components use latest-value-wins behavior. If the same effect is triggered again while running, it stops the active coroutine, restores the baseline transform, and starts one fresh effect for the latest result. Count text is synchronized before restarting so rapid events cannot leave a stale displayed value.

Disabling or destroying either object also restores its baseline transform and hides any active UI particles.

## UI Particle Design

The project UI root uses a Screen Space Overlay canvas, so the count burst uses ordinary UI `Image` elements rather than a world-space `ParticleSystem`. A small fixed pool is created or referenced below the count container. Each burst places particles around the number, moves them radially, shrinks them, and fades them out.

This keeps clipping and draw order predictable and avoids per-trigger instantiate/destroy allocations. The particles do not receive raycasts.

## Assets and Scene Changes

- `DemonPanel.cs`: track displayed count and route decreases to feedback.
- New `CountDecreaseFeedback.cs`: run count scale, impact shake, and pooled UI burst.
- `DemonPanel.prefab`: add/configure the count feedback and particle container.
- `InteractionController.cs`: retain the `SpeakerZone` component and trigger feedback after the item has captured its snap target.
- `SpeakerZone.cs`: expose the successful-drop feedback entry point.
- New `SpeakerZoneFeedback.cs`: shake and restore the configured visual transform.
- Case scenes `1001`, `1002`, and `1003`, plus the interaction test scene: attach/configure consistent speaker-zone feedback and, where needed, separate the visual from the stationary collider/target.

Existing unrelated working-tree changes in the case scenes, `InteractionController`, and `GameManager` must be preserved.

## Error Handling

- Missing count feedback: update the text immediately and log a clear warning once; gameplay continues.
- Missing speaker-zone feedback: accept the item normally without visual feedback.
- Missing speaker visual target: resolve the local `SpriteRenderer` transform; if unavailable, skip the shake and warn.
- Invalid or repeated count values: never animate an increase or no-op update.

Feedback must use unscaled time so a short UI response can complete consistently if time scale is adjusted by an overlay or pause transition.

## Verification

Automated checks should cover the presentation decisions: first value does not animate, decrease animates once, increase/reset does not animate, and a multi-count decrease produces one transition to the final value. Component-level checks should verify that interruption and disable restore baseline transforms.

Manual Play Mode verification should cover:

- A normal accepted item produces one count effect and one speaker-zone shake.
- A rejected item produces neither effect.
- A curse changing the count by more than one plays one old-to-final sequence.
- Retry/reset shows the restored count immediately without effects.
- Repeated accepted drops do not accumulate transform offsets or leave particles visible.
- All three case scenes and the interaction test scene use consistent parameters.
