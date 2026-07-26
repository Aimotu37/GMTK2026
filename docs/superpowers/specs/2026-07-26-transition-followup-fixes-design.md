# Scene Transition Follow-up Fixes Design

## Goal

Fix two transition gaps: keep the opening dialogue visible until the screen is fully black, and cover normal snapshot-based case retry with a black transition.

## Scope

- Opening story completion to case one.
- Snapshot-based `RetryCurrentCase` behavior.
- Death panel dismissal timing from both retry entry points during snapshot retry.

The no-snapshot retry fallback and `ScenesManager` remain unchanged.

## Design

### Opening completion

`PlayOpeningLines` finishes the dialogue and invokes its completion callback without hiding the dialogue panel. `FadeToBlackThenStartNewGameRoutine` fades to full black first, then hides `story_dialogue_panel` and starts the first case. This matches the established next-case ordering.

### Snapshot retry

`RetryCurrentCase` returns whether a snapshot transition has taken ownership of death-panel dismissal. It validates the active scene and snapshot before starting a guarded coroutine. The coroutine loads the fader, fades fully to black, hides `death_panel`, resets the interaction controller, restores snapshot objects, and starts `FlowInit(false)`. The existing no-story case initialization reveals the case only after the case board and demon panels are ready.

When no snapshot exists, `RetryCurrentCase` runs the existing fallback and returns `false`, so `DeathPanelPresenter` and `InteractionController` keep their original immediate-dismiss behavior for that excluded path. A snapshot transition returns `true`, so both entry points leave the panel visible until `GameManager` hides it under black.

If the fader cannot be loaded, retry is aborted without hiding the death panel. The transition guard is released before case initialization hands reveal responsibility to `FlowController`.

## Verification

Per prior user direction, no new EditMode or PlayMode regression tests are added. Verify with production compilation, call-order inspection, and manual checks for opening completion and snapshot retry.
