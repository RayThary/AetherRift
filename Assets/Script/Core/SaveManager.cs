using System;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SaveFileName = "game_progress.json";

    private GameProgressManager progressManager;
    private bool isLoading;
    private bool isAutoSaveEnabled;

    public string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);
    public bool HasSaveData => File.Exists(SaveFilePath);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        TryGetProgressManager();
    }

    private void OnDestroy()
    {
        DisableAutoSave();

        if (Instance == this)
            Instance = null;
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused && isAutoSaveEnabled)
            SaveGame();
    }

    private void OnApplicationQuit()
    {
        if (isAutoSaveEnabled)
            SaveGame();
    }

    public bool SaveGame()
    {
        if (isLoading || !TryGetProgressManager())
            return false;

        return WriteSaveData();
    }

    public bool LoadGame()
    {
        if (!TryGetProgressManager() || !File.Exists(SaveFilePath))
            return false;

        DisableAutoSave();
        isLoading = true;

        try
        {
            string json = File.ReadAllText(SaveFilePath);

            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("저장 파일이 비어 있습니다.");

            GameProgressData loadedData = JsonUtility.FromJson<GameProgressData>(json);

            if (loadedData == null)
                throw new InvalidDataException("저장 데이터를 변환할 수 없습니다.");

            progressManager.ApplyLoadedProgress(loadedData);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SaveManager] 불러오기에 실패했습니다.\n{exception.Message}", this);
            return false;
        }
        finally
        {
            isLoading = false;
        }

        EnableAutoSave();
        return true;
    }

    public bool CreateNewGame()
    {
        if (!TryGetProgressManager())
            return false;

        DisableAutoSave();
        isLoading = true;

        try
        {
            progressManager.CreateNewProgress();

            if (!WriteSaveData())
                return false;
        }
        finally
        {
            isLoading = false;
        }

        EnableAutoSave();
        return true;
    }

    public bool DeleteSaveData()
    {
        try
        {
            if (File.Exists(SaveFilePath))
                File.Delete(SaveFilePath);

            DisableAutoSave();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveManager] 저장 파일 삭제에 실패했습니다.\n{exception.Message}", this);
            return false;
        }
    }

    private bool WriteSaveData()
    {
        GameProgressData currentData = progressManager.CurrentData;

        if (currentData == null)
        {
            Debug.LogWarning("[SaveManager] 저장할 진행 데이터가 없습니다.", this);
            return false;
        }

        try
        {
            currentData.EnsureValid();
            string json = JsonUtility.ToJson(currentData, true);
            File.WriteAllText(SaveFilePath, json);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveManager] 저장에 실패했습니다.\n{exception.Message}", this);
            return false;
        }
    }

    private void EnableAutoSave()
    {
        if (isAutoSaveEnabled || !TryGetProgressManager())
            return;

        progressManager.ProgressChanged += HandleProgressChanged;
        isAutoSaveEnabled = true;
    }

    private void DisableAutoSave()
    {
        if (progressManager != null && isAutoSaveEnabled)
            progressManager.ProgressChanged -= HandleProgressChanged;

        isAutoSaveEnabled = false;
    }

    private void HandleProgressChanged()
    {
        if (!isLoading)
            SaveGame();
    }

    private bool TryGetProgressManager()
    {
        if (progressManager == null)
            progressManager = GameProgressManager.Instance;

        if (progressManager != null)
            return true;

        Debug.LogError("[SaveManager] GameProgressManager를 찾지 못했습니다.", this);
        return false;
    }
}
