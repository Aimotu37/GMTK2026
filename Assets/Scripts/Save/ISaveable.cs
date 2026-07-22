public interface ISaveable
{
    string SavedId { get; }

    void RegisterSaveable()
    {
        SaveManager.Instance.RegisterSaveable(this);
    }

    void UnregisterSaveable()
    {
        SaveManager.Instance.UnregisterSaveable(this);
    }

    ISavedData SaveData();
    void LoadData(ISavedData data);
}
