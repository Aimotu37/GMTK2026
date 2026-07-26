using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class SaveManager : SingletonMono<SaveManager>
{
    private const int CurrentSaveVersion = 3;
    private const string SaveFileName = "save.sav";
    private const string TempSaveFileName = "temp.sav";

    private readonly List<ISaveable> _saveables = new List<ISaveable>();
    private bool _isSaving;

    private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto
    };

    //初始化变量
    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;

    private bool _initFailed;

    public IEnumerator InitializeAsync()
    {
        if (_isInitialized)
        {
            yield break;
        }

        _initFailed = false;

        if (_initFailed)
        {
            _isInitialized = false;
            Debug.LogError(this.name + " Initialization Failed.");
        }
        else
        {
            _isInitialized = true;
            Debug.Log(this.name + " Initialization Successful.");
        }
    }


    public void RegisterSaveable(ISaveable saveable)
    {
        if (!_saveables.Contains(saveable))
            _saveables.Add(saveable);
    }

    public void UnregisterSaveable(ISaveable saveable)
    {
        if (_saveables.Contains(saveable))
            _saveables.Remove(saveable);
    }

    public void SaveGameData()
    {
        if (_isSaving) return;
        _isSaving = true;

        try
        {
            var snapshot = new SavedFileData
            {
                saveVersion = CurrentSaveVersion,
                savedAtUtc = DateTime.UtcNow.ToString("o")
            };

            foreach (var saveable in _saveables)
            {
                var data = saveable.SaveData();
                if (data != null)
                    snapshot.savedDataDictionary[saveable.SavedId] = data;
            }

            if (snapshot.savedDataDictionary.Count == 0)
            {
                Debug.LogWarning("No data to save.");
                return;
            }

            // 序列化
            string json = JsonConvert.SerializeObject(snapshot, Formatting.Indented, SerializerSettings);

            string savePath = GetSaveFilePath();
            string tempPath = GetTempFilePath();
            File.WriteAllText(tempPath, json);

            if (File.Exists(savePath))
                File.Replace(tempPath, savePath, null);
            else
                File.Move(tempPath, savePath);

            Debug.Log("Game saved.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Save failed: {e.Message}");
        }
        finally
        {
            _isSaving = false;
        }
    }

    public bool LoadGameData()
    {
        string path = GetSaveFilePath();
        if (!File.Exists(path))
        {
            Debug.LogError("No save file found.");
            return false;
        }
        try
        {
            string json = File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<SavedFileData>(json, SerializerSettings);

            if (data == null || data.savedDataDictionary == null || data.saveVersion != CurrentSaveVersion)
            {
                Debug.LogError("Invalid or unsupported save version.");
                return false;
            }

            foreach (var saveable in _saveables)
            {
                if (data.savedDataDictionary.TryGetValue(saveable.SavedId, out var saveData))
                    saveable.LoadData(saveData);
            }

            Debug.Log("Game loaded.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Load failed: {e.Message}");
            return false;
        }
    }

    public bool HasGameSavedData()
    {
        string path = GetSaveFilePath();
        if (!File.Exists(path)) return false;

        try
        {
            string json = File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<SavedFileData>(json, SerializerSettings);
            if (data == null ||
                data.saveVersion != CurrentSaveVersion ||
                data.savedDataDictionary == null ||
                !data.savedDataDictionary.TryGetValue("Game_Saved", out ISavedData savedData))
            {
                return false;
            }

            return savedData is GameProgressSavedData gameData &&
                   GameManager.Instance.IsValidCaseId(gameData.currentCaseId);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool DeleteGameData()
    {
        try
        {
            string savePath = GetSaveFilePath();
            string tempPath = GetTempFilePath();
            if (File.Exists(savePath)) File.Delete(savePath);
            if (File.Exists(tempPath)) File.Delete(tempPath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Delete failed: {e.Message}");
            return false;
        }
    }

    private string GetSaveFilePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }

    private string GetTempFilePath()
    {
        return Path.Combine(Application.persistentDataPath, TempSaveFileName);
    }
}
