# Audio Console Cleanup Design

## Goal

Eliminate the confirmed Unity Console errors caused by AudioMixer validation, transient AudioListener absence, and invalid `MonoBehaviour` construction without changing WAV content, Addressable keys, public audio APIs, or saved settings.

## Mixer Validation

`GameAudio.mixer` is a stable committed asset. Domain reload must never delete or regenerate it. `GameAudioMixerBuilder` becomes a manually invoked, read-only validator under `Tools/Audio/Validate Game Audio Mixer`.

Group lookup filters `AudioMixer.FindMatchingGroups` results by exact `group.name`, because the Unity API also returns descendant paths for the query `Master`. Validation checks the exact hierarchy, exposed BGM/SFX parameters, Compressor presence, three distinct non-zero parameter GUIDs, and values `Threshold=-3`, `Attack=10`, `Release=80`. Failure throws an explanatory exception but performs no asset mutation.

## AudioListener Ownership

The persistent scene owns the single enabled `AudioListener` for the full game lifetime. Every additive content scene keeps its listener component disabled. Current audio sources are two-dimensional, so listener position does not affect playback.

This prevents the no-listener gaps created when the current content scene is unloaded before the replacement scene finishes loading, while also avoiding multiple-listener warnings.

## Unrelated Console Error

`FlowController.HandleCluePopup` initializes its temporary `BasePanel` reference to `null`; it never constructs a `MonoBehaviour` with `new`. The existing callback remains responsible for assigning the loaded panel before use.

## Verification

- EditMode tests use exact-name Mixer group lookup and validate Compressor GUIDs and values.
- Scene configuration tests require one enabled listener in `Scene_Persistent` and zero enabled listeners in every additive content scene.
- Fresh compilation produces no errors.
- A fresh Unity play/scene-transition log contains none of: `Master found 6`, `guid != UnityGUID()`, `There are no audio listeners`, or `trying to create a MonoBehaviour using the 'new' keyword`.
- The Mixer file hash remains unchanged across a domain reload and manual validation.

## Scope Boundary

This increment does not modify AudioManager cache architecture, async-generation handling, processed audio bytes, Addressables configuration, scene cameras, or gameplay flow beyond replacing the invalid `BasePanel` constructor call.
