# SFX Consistency Design

## Goal

Unify the perceived loudness and playback behavior of the six production SFX within a Game Jam-sized implementation window. Preserve the existing call sites and Addressable keys, keep the original recordings recoverable, and avoid a larger audio-system rewrite.

## Scope

This change covers:

- the six WAV files directly under `Assets/Audio`;
- centralized per-clip gain and category configuration;
- an AudioMixer hierarchy for BGM and SFX;
- same-clip concurrency and retrigger rules;
- BGM/SFX settings routed through exposed mixer parameters;
- automated editor tests for configuration and concurrency policy, plus manual listening checks.

This change does not cover:

- replacing the recordings with a new sound library;
- randomized variants, spatial audio, reverb design, or side-chain ducking;
- changing existing public `StartPlaySound` call sites;
- the test audio under `Assets/Audio/Test`.

## Baseline

The production files currently differ by 14.4 dB in active-section mean level and by 9.6 dB in peak level. They also mix 44.1/48 kHz material, mono/stereo material, and envelopes ranging from a roughly 45 ms transient to a six-second result sound.

Every SFX call currently creates a new `AudioSource`, applies the same global linear volume, and plays without an AudioMixer route or same-clip concurrency limit. Consequently, source differences pass directly to the output, and long clips can stack when an interaction is repeated.

## Selected Approach

Use light offline source processing together with a small runtime policy layer:

1. Preserve an untouched copy of every production WAV outside Unity's imported `Assets` tree.
2. Process the existing WAV paths so their GUIDs and Addressable keys remain stable.
3. Add a mixer hierarchy and route each source by SFX category.
4. Keep gain, cooldown, voice limit, and retrigger behavior in one centralized profile table.
5. Leave all callers on `StartPlaySound(name, isLoop, callback)`.

This is preferred over source-only normalization because it also fixes interaction-dependent stacking. It is preferred over a full audio catalog and source-pool rewrite because the project has only six production clips and a short delivery window.

## Source Processing

Original files will be copied to `SourceAssets/Audio/Originals` before any WAV is replaced. The current files under `Assets/Audio` retain their paths and `.meta` files.

The first-pass processing matrix is:

| Clip | Offline gain | Trim |
| --- | ---: | --- |
| `SFX_01_07_09` | +6 dB | Remove the detected trailing silence after about 0.320 s and add a short fade-out. |
| `SFX_02_11_14` | -4 dB | Preserve its full 2.5 s duration; control repeat behavior at runtime. |
| `SFX_03_08` | -3 dB | Remove the final roughly 1.08 s of silence and add a short fade-out. |
| `SFX_04_05_06` | 0 dB | Retain about 20 ms after the active transient instead of the current long silent tail. |
| `SFX_10_12` | 0 dB | Apply the same processing as `SFX_04_05_06`; the source files are currently identical. |
| `SFX_13` | -3 dB | Remove about 61 ms of leading silence and about 806 ms of trailing silence, with edge fades. |

Processing must retain PCM WAV output and must not introduce a peak above -1 dBFS. Exact trim boundaries use zero crossings plus 10-30 ms fades so no click is introduced. If a trim audibly damages a natural tail during review, the trim is backed off; gain and runtime policy remain applicable independently.

## Mixer Architecture

Create one mixer with this hierarchy:

```text
Master
|-- BGM
`-- SFX
    |-- UI
    |-- Interaction
    `-- Result
```

The `BGM` and parent `SFX` attenuation parameters are exposed for settings. Slider values use a logarithmic conversion: zero maps to the mute floor, while non-zero values map through `20 * log10(value)`.

The parent SFX bus receives a safety compressor with an initial threshold near -3 dB, ratio near 8:1, 1-3 ms attack, 60-100 ms release, and no makeup gain. These are guardrail values, not a substitute for per-clip leveling, and can be backed off if transient character is audibly damaged.

## SFX Profiles

Each known SFX key resolves to a profile containing:

- category/mixer group;
- runtime gain multiplier, defaulting to unity after offline processing;
- same-key maximum concurrent voices;
- cooldown in seconds;
- retrigger behavior (`Restart` or `Ignore`).

Initial policies are:

| Category | Same-key voices | Cooldown | Retrigger |
| --- | ---: | ---: | --- |
| UI | 1 | 0.07 s | Restart the current voice. |
| Interaction | 1 | 0.10 s | Restart the current voice. |
| Result | 1 | For the active playback | Ignore the duplicate trigger. |

The global SFX voice cap is eight, with category priority `Result > Interaction > UI`. If the cap is reached, a new voice may reclaim the oldest non-looping voice from a lower-priority category. A voice never reclaims an equal- or higher-priority voice; when no eligible lower-priority voice exists, the new trigger is ignored. Looping voices are never reclaimed by this policy.

Profiles map the existing keys as follows:

- `sfx_01_07_09`: UI
- `sfx_02_11_14`: UI
- `sfx_03_08`: Interaction
- `sfx_04_05_06`: Interaction
- `sfx_10_12`: Result
- `sfx_13`: Result

## Playback Data Flow

1. A caller passes the existing key to `StartPlaySound`.
2. `AudioManager` resolves the profile before loading or playing the clip.
3. The concurrency policy decides whether to start, restart, ignore, or reclaim a voice.
4. The source receives the profile's mixer group and gain, then starts playback.
5. Active voice state records the key and start time.
6. Completion or explicit stop removes the voice from both the active-source list and per-key tracking.

An unknown key uses a conservative fallback profile routed to the parent SFX group with unity gain, one same-key voice, and `Ignore` retrigger behavior. It emits one warning per unknown key rather than failing playback.

Addressable load failure must destroy the temporary source, remove pending state, and log the requested key. The callback is invoked only after playback actually starts, preserving its current meaning.

## Settings Behavior

Existing saved BGM and SFX values remain normalized floats in the range 0-1. `SetBGMVolume` and `SetSoundVolume` update the exposed mixer attenuation parameters. Existing sources no longer receive the global setting directly; source volume is reserved for per-profile gain.

This preserves the current save format and UI contract while providing a perceptually smoother volume curve.

## Verification

Automated/editor verification covers:

- every production Addressable SFX key has a profile;
- all profiles have a mixer category, non-negative cooldown, and valid voice limit;
- logarithmic slider conversion maps 1 to 0 dB, 0.5 to approximately -6.02 dB, and 0 to the mute floor;
- duplicate-trigger policy returns the expected restart/ignore decision;
- an Addressable load failure does not leave a tracked source behind.

Offline audio verification reports duration, channels, sample rate, active mean, and peak for all processed WAVs. Processed peaks must remain at or below -1 dBFS.

Manual acceptance scenarios are:

1. Play all six clips sequentially at a fixed SFX setting.
2. Rapidly trigger one UI action ten times and confirm it does not accumulate loudness.
3. Exercise item pickup/drop, success, and failure flows.
4. Verify no trim click, abrupt natural-tail cut, or unnecessary long silence is audible.
5. Verify on both headphones and laptop speakers that UI feedback remains clear without masking dialogue.
6. Save and reload BGM/SFX settings and confirm the perceived levels are restored.

Within a semantic category, active-section mean levels should be within roughly 4 dB unless the sound is intentionally emphasized. The master meter should retain about 1 dB of peak headroom during the acceptance scenarios.

## Rollback

The original WAVs in `SourceAssets/Audio/Originals` provide a direct source rollback without changing GUIDs or Addressable keys. Mixer routing and profile policy are isolated inside the audio subsystem, so they can be disabled independently from source processing if a regression is found.
