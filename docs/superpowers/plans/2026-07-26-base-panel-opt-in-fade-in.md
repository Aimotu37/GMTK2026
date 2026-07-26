# BasePanel Opt-In Fade-In Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fade the complete case-success and case-failure panels in over `0.3` seconds when opened, without adding fade-out or changing other panels.

**Architecture:** `BasePanel` owns an opt-in entrance animation driven by a root `CanvasGroup` and `Time.unscaledDeltaTime`. `SuccessPanel` and `DeathPanel` enable the serialized option and receive root groups; all other panels retain the existing immediate-open behavior because the option defaults to false.

**Tech Stack:** Unity 2022.3, C#, uGUI `CanvasGroup`, coroutines, serialized prefab YAML.

## Global Constraints

- Implement the reusable behavior directly in `BasePanel`; do not add a `FadeInPanel` class.
- Fade in only when a panel opens; closing remains immediate with no fade-out.
- Enable the behavior only for `SuccessPanel.prefab` and `DeathPanel.prefab`.
- Use `Time.unscaledDeltaTime` and an initial duration of exactly `0.3` seconds.
- Disable interaction and raycasts until the fade reaches alpha one.
- Missing root `CanvasGroup` configuration must fall back to a fully visible, interactive panel and log a warning.
- Preserve unrelated working-tree changes.

## File Map

- `Assets/Scripts/UI/BasePanel.cs`: owns opt-in serialized settings, coroutine lifecycle, alpha updates, interaction gating, and fallback behavior.
- `Assets/Prefabs/UI/SuccessPanel.prefab`: adds a root `CanvasGroup` and enables the inherited fade-in settings.
- `Assets/Prefabs/UI/DeathPanel.prefab`: adds a root `CanvasGroup` and enables the inherited fade-in settings.

---

### Task 1: Implement the opt-in BasePanel entrance lifecycle

**Files:**
- Modify: `Assets/Scripts/UI/BasePanel.cs`

**Interfaces:**
- Consumes: existing `BasePanel.Awake()`, `BasePanel.Open(object data = null)`, `BasePanel.Close()`, Unity `CanvasGroup`, and coroutine APIs.
- Produces: serialized `_fadeInOnOpen: bool`, serialized `_fadeInDuration: float`, and private deterministic fade lifecycle helpers.

- [ ] **Step 1: Establish a compile baseline**

Run:

```powershell
dotnet build .\Assembly-CSharp.csproj --no-restore
```

Expected: the existing runtime assembly compiles successfully before the change. If unrelated dirty work causes a failure, record it and continue only when the failure does not involve `BasePanel.cs`.

- [ ] **Step 2: Add the fade configuration and cached state**

Add `using System.Collections;`, then add these members near the start of `BasePanel`:

```csharp
private const float DefaultFadeInDuration = 0.3f;

[SerializeField]
private bool _fadeInOnOpen;

[SerializeField, Min(0f)]
private float _fadeInDuration = DefaultFadeInDuration;

private CanvasGroup _canvasGroup;
private Coroutine _fadeInCoroutine;
```

At the end of `Awake`, cache only the root group and prepare opted-in panels before their first rendered frame:

```csharp
_canvasGroup = GetComponent<CanvasGroup>();
if (!_fadeInOnOpen)
{
    return;
}

if (_canvasGroup == null)
{
    Debug.LogWarning($"{name} has fade-in enabled but no root CanvasGroup. Opening immediately.", this);
    return;
}

SetCanvasGroupState(0f, false);
```

- [ ] **Step 3: Integrate animation startup and cancellation with panel lifecycle**

Update `Open` so panel-specific data is applied before the entrance starts:

```csharp
IsOpen = true;
gameObject.SetActive(true);
OnOpen(data);
BeginFadeInIfEnabled();
```

Update `Close` to cancel and normalize the entrance immediately before existing close behavior:

```csharp
StopFadeInAndRestore();
OnClose();
IsOpen = false;
gameObject.SetActive(false);
```

- [ ] **Step 4: Add the coroutine and state helpers**

Add these private methods before `CacheComponents<T>`:

```csharp
private void BeginFadeInIfEnabled()
{
    if (!_fadeInOnOpen || _canvasGroup == null)
    {
        return;
    }

    if (_fadeInCoroutine != null)
    {
        StopCoroutine(_fadeInCoroutine);
        _fadeInCoroutine = null;
    }

    SetCanvasGroupState(0f, false);

    if (_fadeInDuration <= 0f)
    {
        SetCanvasGroupState(1f, true);
        return;
    }

    _fadeInCoroutine = StartCoroutine(FadeInRoutine(_fadeInDuration));
}

private IEnumerator FadeInRoutine(float duration)
{
    float elapsed = 0f;
    while (elapsed < duration)
    {
        elapsed += Time.unscaledDeltaTime;
        _canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
        yield return null;
    }

    SetCanvasGroupState(1f, true);
    _fadeInCoroutine = null;
}

private void StopFadeInAndRestore()
{
    if (_fadeInCoroutine != null)
    {
        StopCoroutine(_fadeInCoroutine);
        _fadeInCoroutine = null;
    }

    if (_canvasGroup != null)
    {
        SetCanvasGroupState(1f, true);
    }
}

private void SetCanvasGroupState(float alpha, bool interactive)
{
    _canvasGroup.alpha = alpha;
    _canvasGroup.interactable = interactive;
    _canvasGroup.blocksRaycasts = interactive;
}
```

- [ ] **Step 5: Compile the runtime assembly**

Run:

```powershell
dotnet build .\Assembly-CSharp.csproj --no-restore
```

Expected: build succeeds with no errors involving `BasePanel`.

- [ ] **Step 6: Commit the BasePanel behavior**

```powershell
git add -- Assets/Scripts/UI/BasePanel.cs
git commit -m "feat: add opt-in panel fade in"
```

### Task 2: Opt in the success and failure prefabs

**Files:**
- Modify: `Assets/Prefabs/UI/SuccessPanel.prefab`
- Modify: `Assets/Prefabs/UI/DeathPanel.prefab`

**Interfaces:**
- Consumes: inherited `_fadeInOnOpen` and `_fadeInDuration` fields from Task 1.
- Produces: a root `CanvasGroup` and enabled `0.3`-second fade configuration on each target panel.

- [ ] **Step 1: Add a CanvasGroup to each panel root**

On the same GameObject as `SuccessPanel`, add a `CanvasGroup` initialized as:

```yaml
m_Enabled: 1
m_Alpha: 1
m_Interactable: 1
m_BlocksRaycasts: 1
m_IgnoreParentGroups: 0
```

Repeat on the same GameObject as `DeathPanel`. Do not remove or repurpose the existing `CanvasGroup` components on the `HintText` and retry-button child objects.

- [ ] **Step 2: Enable the inherited settings on SuccessPanel**

Add the following serialized values to the `SuccessPanel` script component, before its existing `truthBg`, `hintText`, and `successBg` fields:

```yaml
_fadeInOnOpen: 1
_fadeInDuration: 0.3
```

- [ ] **Step 3: Enable the inherited settings on DeathPanel**

Add the following serialized values to the `DeathPanel` script component:

```yaml
_fadeInOnOpen: 1
_fadeInDuration: 0.3
```

- [ ] **Step 4: Verify serialized structure and compile**

Run:

```powershell
rg -n "_fadeInOnOpen: 1|_fadeInDuration: 0.3|CanvasGroup:" Assets/Prefabs/UI/SuccessPanel.prefab Assets/Prefabs/UI/DeathPanel.prefab
dotnet build .\Assembly-CSharp.csproj --no-restore
```

Expected: each target panel has the two enabled settings, each root has a new `CanvasGroup`, existing child groups remain, and the runtime assembly builds successfully.

- [ ] **Step 5: Commit the prefab opt-in**

```powershell
git add -- Assets/Prefabs/UI/SuccessPanel.prefab Assets/Prefabs/UI/DeathPanel.prefab
git commit -m "feat: fade in case result panels"
```

### Task 3: Verify player-visible behavior

**Files:**
- Verify: `Assets/Scripts/UI/BasePanel.cs`
- Verify: `Assets/Prefabs/UI/SuccessPanel.prefab`
- Verify: `Assets/Prefabs/UI/DeathPanel.prefab`

**Interfaces:**
- Consumes: completed BasePanel lifecycle and prefab opt-in.
- Produces: verification evidence for entrance timing, interaction gating, immediate closing, fallback safety, and unaffected panels.

- [ ] **Step 1: Inspect the final scoped diff**

Run:

```powershell
git diff --check
git diff -- Assets/Scripts/UI/BasePanel.cs Assets/Prefabs/UI/SuccessPanel.prefab Assets/Prefabs/UI/DeathPanel.prefab
```

Expected: no whitespace errors, no fade-out code, no `FlowController` changes, and no unrelated files in the scoped diff.

- [ ] **Step 2: Run the manual Unity checklist**

In Play Mode:

1. Trigger case success and confirm the complete success overlay fades from alpha zero to one in approximately `0.3` seconds.
2. Attempt to click during the entrance and confirm no click is accepted before full opacity.
3. Close or advance from the success panel and confirm it disappears immediately.
4. Trigger case failure and repeat the opacity, click-gating, and immediate-close checks.
5. Set `Time.timeScale = 0`, trigger either result panel, and confirm the entrance still completes.
6. Open an unaffected panel such as settings and confirm it still appears immediately.
7. On a temporary duplicate in Play Mode, remove the root `CanvasGroup`, reopen the panel, confirm the warning is logged and the panel remains visible, then exit Play Mode without saving the temporary change.

- [ ] **Step 3: Record final verification**

Report the compile result, scoped diff result, and manual checklist result. If Unity Play Mode is unavailable, explicitly mark the seven manual checks as pending rather than claiming them as passed.
