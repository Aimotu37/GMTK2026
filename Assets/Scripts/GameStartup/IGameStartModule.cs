using System.Collections;

public interface IGameStartModule
{
    string ModuleName { get; }
    float ProgressWeight { get; }
    bool IsInitialized { get; }

    IEnumerator Initialize();
}
