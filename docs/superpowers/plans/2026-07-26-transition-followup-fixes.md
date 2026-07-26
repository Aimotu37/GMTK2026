# Scene Transition Follow-up Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the visible gap after opening dialogue and add black coverage to snapshot-based case retry.

**Architecture:** `GameManager` continues to own transition ordering and the shared transition guard. `FlowController.FlowInit(false)` remains responsible for revealing the restored case after its UI is ready; `ScenesManager` and the no-snapshot fallback are unchanged.

**Tech Stack:** Unity 2022.3, C#, coroutines, CanvasGroup fades.

## Global Constraints

- Do not modify `ScenesManager`.
- Do not modify the no-snapshot retry fallback.
- Do not add EditMode or PlayMode regression tests.
- Preserve unrelated dirty-worktree changes.

---

### Task 1: Preserve opening dialogue through fade-out

**Files:**
- Modify: `Assets/Scripts/Managers/GameManager.cs`

**Interfaces:**
- Consumes: `FadeToBlack(float)`, `ScreenFader.FadeOut(float)`
- Produces: opening completion ordering of fade-out, panel hide, then case load

- [x] Remove the early `HidePanel("story_dialogue_panel")` from `PlayOpeningLines`.
- [x] Hide `story_dialogue_panel` immediately after `FadeOut` completes in `FadeToBlackThenStartNewGameRoutine`.
- [x] Confirm missing-dialogue paths still proceed to case one while black.

### Task 2: Cover snapshot retry with black

**Files:**
- Modify: `Assets/Scripts/Managers/GameManager.cs`
- Modify: `Assets/Scripts/UI/DeathPanelPresenter.cs`
- Modify: `Assets/Scripts/Interaction/InteractionController.cs`

**Interfaces:**
- Consumes: `TryBeginSceneTransition()`, `GetOrLoadScreenFader(...)`, `FlowInit(false)`
- Produces: `bool RetryCurrentCase()` and guarded `RetryCurrentCaseRoutine(Scene, GameObject)`

- [x] Return `false` for invalid-scene and no-snapshot fallback paths so the presenter preserves its original immediate hide.
- [x] Start the shared transition guard only after a valid snapshot is found.
- [x] Move snapshot mutation into a coroutine that fades fully to black first.
- [x] Hide `death_panel` only after full opacity, restore the snapshot, release the guard, and call `FlowInit(false)`.
- [x] Make `DeathPanelPresenter.HandleTetry` hide immediately only when `RetryCurrentCase()` returns `false`.
- [x] Make `InteractionController.UIEmptyClick` use the same conditional hide for its case-failure retry path.
- [x] On fader load failure, release the guard and retain the death panel.

### Task 3: Verify

**Files:**
- Verify production files only

**Interfaces:**
- Produces: compilation and static call-order evidence

- [x] Build `Assembly-CSharp.csproj` with zero compiler errors.
- [x] Confirm the opening dialogue hide occurs after fade-out.
- [x] Confirm only the snapshot retry branch starts a fade transition.
- [x] Confirm `ScenesManager` has no diff.
