# Unified Scene Transitions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give every player-facing content-scene change a consistent black-screen cover and destination-ready reveal.

**Architecture:** `GameManager` orchestrates transitions while `ScenesManager` stays UI-agnostic. `ScreenFader` supplies idempotent unscaled fades, and opening, ending, case, main-menu, and bootstrap flows reveal only after their first visible content is ready.

**Tech Stack:** Unity 2022.3, C#, coroutines, Addressables, additive scene loading.

## Global Constraints

- Do not modify save version, save fields, scene names, or Addressables keys.
- Do not add new EditMode or PlayMode regression tests for this change.
- Do not change Debug jump or forced-ending shortcuts.
- Preserve user changes already present in the dirty worktree.

---

### Task 1: Make black-screen fades idempotent

**Files:**
- Modify: `Assets/Scripts/UI/ScreenFader.cs`

**Interfaces:**
- Produces: `bool IsOpaque`, existing `FadeOut(float)`, existing `FadeIn(float)`

- [x] Add an opacity property based on the cached `CanvasGroup` alpha.
- [x] Make an already-opaque fade-out complete without an animation delay.
- [x] Drive both fade loops with `Time.unscaledDeltaTime`.

### Task 2: Centralize player-facing scene transitions

**Files:**
- Modify: `Assets/Scripts/Managers/GameManager.cs`
- Modify: `Assets/Scripts/UI/MainMenuPresenter.cs`

**Interfaces:**
- Produces: guarded opening, next-case, ending, and main-menu transition paths
- Consumes: `ScreenFader.IsOpaque`, `ScenesManager.SwitchGameScene`

- [x] Replace the continue-only guard with a shared scene-transition guard.
- [x] Add a main-menu-to-opening coroutine that fades out before switching.
- [x] Make `NextCase` fade out internally before changing case or entering the ending.
- [x] Make `LoadEndingScene` guarantee black before loading and defer reveal to ending dialogue.
- [x] Make the public parameterless `LoadMainMenu` perform a full transition while retaining an internal no-transition loading path for bootstrap and black-screen recovery.
- [x] Route `MainMenuPresenter.HandleNewGameClicked` through the new opening transition.

### Task 3: Reveal destinations only when ready

**Files:**
- Modify: `Assets/Scripts/Managers/GameManager.cs`
- Modify: `Assets/Scripts/Flow/FlowController.cs`

**Interfaces:**
- Produces: opening, ending, and case initialization paths that call fade-in at destination readiness

- [x] Reveal the opening scene after assigning the first opening dialogue line.
- [x] Reveal the ending scene after assigning the ending dialogue line.
- [x] Keep the screen black when opening or ending dialogue is unavailable and continue to the next valid destination.
- [x] Wait for both case board and demon panels before advancing case initialization.
- [x] Reveal a case after its story line is assigned or after no-story exploration setup completes.
- [x] Remove the local fade-out from the truth flow because `NextCase` now owns it.

### Task 4: Correct bootstrap reveal timing

**Files:**
- Modify: `Assets/Scripts/GameStartup/GameBootstrap.cs`

**Interfaces:**
- Consumes: the no-transition main-menu load completion callback
- Produces: bootstrap loading view hidden only after the main menu is ready

- [x] Keep `BootstrapLoadingView` visible while loading the main-menu scene and panel.
- [x] Call `HandleSceneLoaded` from the actual main-menu completion callback instead of before loading starts.

### Task 5: Compile and audit

**Files:**
- Verify production files only

**Interfaces:**
- Produces: compiler output, scene-switch call-site audit, and manual verification checklist

- [x] Build `Assembly-CSharp.csproj` with zero compiler errors.
- [x] Search every `LoadSceneAsync` and `SwitchGameScene` caller and confirm each player-facing path is covered or intentionally bootstrap/debug-only.
- [x] Record manual checks for startup, opening, case one, next case, ending, return to menu, and rapid repeated input.

## Manual Verification Checklist

- Startup: the bootstrap loading view stays visible until the main-menu panel is ready.
- New game: the main menu fades fully to black before the opening scene replaces it.
- Opening: the first dialogue line is assigned before fade-in; the transition to case one is covered by black.
- Continue: saved case UI and its first story line are ready before fade-in.
- Next case: truth/success content remains covered while the next case scene loads; the next case reveals from its first ready state.
- Ending: the ending scene remains black until its dialogue line is assigned, then returns to the ready main menu through a black transition.
- Main-menu returns: settings and death paths fade with unscaled time, including while gameplay is paused.
- Repeated input: repeated transition clicks during an active transition do not start a second scene switch.
