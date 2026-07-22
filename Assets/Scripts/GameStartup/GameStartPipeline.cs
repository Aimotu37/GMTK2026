using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameStartPipeline
{
    private readonly IReadOnlyList<IGameStartModule> _modules;

    public GameStartPipeline(IReadOnlyList<IGameStartModule> modules)
    {
        _modules = modules ?? throw new ArgumentNullException(nameof(modules));
    }

    public IEnumerator Run(
        Action<GameStartProgress> onProgress,
        Action<GameInitializationFailure> onFailure)
    {
        float totalWeight = GetTotalWeight();
        float completedWeight = 0f;

        foreach (IGameStartModule module in _modules)
        {
            IEnumerator initialization = module.Initialize();
            while (true)
            {
                bool hasNext;
                object current = null;

                try
                {
                    hasNext = initialization.MoveNext();
                    if (hasNext)
                    {
                        current = initialization.Current;
                    }
                }
                catch (Exception exception)
                {
                    onFailure?.Invoke(
                        new GameInitializationFailure(module.ModuleName, exception.Message));
                    yield break;
                }

                if (!hasNext)
                {
                    break;
                }

                yield return current;
            }

            completedWeight += Mathf.Max(0f, module.ProgressWeight);
            float progress = totalWeight > 1f ? completedWeight / totalWeight : 1f;
            onProgress?.Invoke(new GameStartProgress(module.ModuleName, progress));
            yield return null;
        }
    }

    private float GetTotalWeight()
    {
        float totalWeight = 0f;
        foreach (IGameStartModule module in _modules)
        {
            totalWeight += Mathf.Max(0f, module.ProgressWeight);
        }

        return totalWeight;
    }
}
