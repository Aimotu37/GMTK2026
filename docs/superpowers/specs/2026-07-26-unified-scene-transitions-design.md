# Unified Scene Transitions Design

## Goal

Apply a consistent black-screen transition to every player-facing content-scene change while revealing each destination only after its first visible UI or dialogue content is ready.

## Scope

Included transitions:

- Bootstrap loading view to main menu
- Main menu to opening story
- Opening story to case one
- Case to next case
- Case to ending
- Ending to main menu
- In-game settings/death screen to main menu
- Existing Continue Game paths

Debug jump, forced-ending shortcuts, and the existing retry fallback remain unchanged.

## Architecture

- `ScenesManager` remains a UI-agnostic additive scene loader.
- `GameManager` owns player-facing transition orchestration and a shared transition guard.
- `ScreenFader` exposes whether it is already opaque and uses unscaled time so transitions cannot stall when gameplay time is paused.
- Each destination owns its reveal moment:
  - Main menu reveals after `main_menu_panel` is mounted.
  - Opening and ending reveal after the first dialogue line has been assigned.
  - Cases reveal after case UI initialization and either the case story line or the no-story exploration fallback is ready.
- The bootstrap path continues to use `BootstrapLoadingView`, not `ScreenFader`, and hides it only after the main menu panel is ready.

## Failure Handling

- A missing fader aborts a source transition without unloading the visible scene.
- A failed content-scene load recovers to the main menu while black, then reveals the ready menu.
- Missing opening dialogue skips directly to case one while black.
- Missing ending dialogue returns to the main menu while black.
- Missing case story or story panel reveals the initialized exploration UI rather than remaining black.
- The shared transition guard resets on every success and failure exit.

## Verification

Per user direction, this change adds no new EditMode or PlayMode regression-test design. Verification consists of compiling `Assembly-CSharp.csproj`, checking all scene-switch call sites, and providing a manual scene-transition checklist.
