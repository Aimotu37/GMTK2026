# Audio Stability Bugfix Design

## Goal

Repair the confirmed AudioMixer serialization failure and close the runtime cache and asynchronous-lifecycle gaps without changing existing gameplay call sites, Addressable keys, saved settings, or processed WAV files.

## Root Cause

`GameAudioMixerBuilder` constructs Unity's built-in Compressor through private editor reflection and asks the effect for parameter GUIDs before Unity has created its parameter definitions. All three lookups return the zero GUID, so Threshold, Attack, and Release overwrite one snapshot entry. The generated mixer therefore contains one `80` value instead of three distinct `-3`, `10`, and `80` values.

The `[InitializeOnLoad]` recovery path then deletes an invalid mixer, attempts the same broken generation again, and leaves the newly invalid asset in place when validation throws. The existing EditMode regression test compiles but was not executed because another Unity process held the project lock.

## Selected Mixer Approach

Treat `Assets/Resources/Audio/GameAudio.mixer` as a stable, committed Unity asset. Create or repair it once through supported Unity editor serialization and retain its `.meta` GUID. Remove automatic deletion and regeneration from domain reloads.

`GameAudioMixerBuilder` becomes a read-only validator with an explicit menu command. It checks:

- the exact `Master/BGM` and `Master/SFX/UI|Interaction|Result` hierarchy;
- exposed `BGMVolumeDb` and `SFXVolumeDb` parameters;
- a Compressor on SFX;
- distinct, non-zero GUIDs for Threshold, Attack, and Release;
- values Threshold `-3 dB`, Attack `10 ms`, and Release `80 ms`.

Validation failure logs a precise error and never deletes or mutates the mixer. If automated repair is retained for local development, it may only copy a known-good template asset while preserving the destination `.meta`; it must not construct effects through private APIs.

## Runtime Cache and Lifecycle

`AudioManager` separates BGM and SFX caches so the same short name cannot resolve across namespaces. Both cache entries retain the existing loaded `AudioClip` objects and release each Addressables reference exactly once.

BGM receives a load-generation token matching the existing SFX generation guard. Stopping/releasing audio invalidates pending BGM callbacks. `ReleaseAudioSources` stops BGM, clears `bgmSource.clip`, stops all SFX, invalidates both load generations, then releases and clears both caches.

SFX admission becomes two-phase:

1. Before loading, reject pending same-key duplicates and requests that cannot ever fit the policy.
2. After the clip loads successfully, re-evaluate capacity and only then reclaim a lower-priority active voice immediately before starting the new source.

This prevents a lower-priority sound from being cut while the replacement is still loading or may fail.

## Testing

Pure policy tests remain in `GMTK.Audio.EditorTests`. Mixer tests add serialization assertions for three distinct non-zero parameter GUIDs. Runtime state decisions that do not require Addressables move into pure helpers so cache namespace, generation acceptance, and capacity/reclaim timing can be tested without scene objects.

Final verification requires:

- fresh compilation of runtime, game, editor, and test assemblies;
- a complete EditMode Test Runner execution with XML showing zero failures;
- read-only mixer serialization verification;
- WAV format/peak verification and unchanged `.meta` hashes;
- manual listening for sequential clips, rapid UI triggers, result transitions, and settings reload.

Compilation alone is not accepted as evidence that EditMode tests passed.

## Scope Boundary

The processed WAV bytes and their backups remain unchanged. No new audio pooling framework, spatial audio system, randomized variations, or public API migration is introduced.
