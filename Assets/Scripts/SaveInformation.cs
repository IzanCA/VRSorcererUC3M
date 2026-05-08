// SaveInformation.cs
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class SpellRecord
{
    public string spellName;
    public int timesCompleted;
    public int timesSkipped;
    public float bestSimilarity;
    public float worstSimilarity = 1f;
}

[Serializable]
public class SessionData
{
    public string sessionDate;
    public int totalCompleted;
    public int totalSkipped;
    public string bestGesture;
    public float bestGestureScore;
    public string worstGesture;
    public float worstGestureScore = 1f;
    public List<SpellRecord> spellRecords = new();
}

public class SaveInformation : MonoBehaviour
{
    [SerializeField] private RandomSpellGenerator spellGenerator;

    [Header("Custom Save")]
    [SerializeField] private TMP_InputField saveNameInputField;
    [SerializeField] private Button saveWithNameButton;

    private SessionData session = new();
    private string SavePath => Path.Combine(Application.persistentDataPath, "session.json");

    void Start()
    {
        session.sessionDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        foreach (var name in spellGenerator.spellNames)
            session.spellRecords.Add(new SpellRecord { spellName = name, worstSimilarity = 1f });

        if (saveWithNameButton != null)
            saveWithNameButton.onClick.AddListener(SaveWithCustomName);
    }

    public void OnSpellCompleted(string spellName, float similarity)
    {
        session.totalCompleted++;

        var record = GetOrCreateRecord(spellName);
        record.timesCompleted++;

        if (similarity > record.bestSimilarity)
            record.bestSimilarity = similarity;
        if (similarity < record.worstSimilarity)
            record.worstSimilarity = similarity;

        if (similarity > session.bestGestureScore)
        {
            session.bestGestureScore = similarity;
            session.bestGesture = spellName;
        }
        if (similarity < session.worstGestureScore)
        {
            session.worstGestureScore = similarity;
            session.worstGesture = spellName;
        }

        Save();
    }

    public void OnSpellSkipped(string spellName)
    {
        session.totalSkipped++;
        GetOrCreateRecord(spellName).timesSkipped++;
        Save();
    }

    private void SaveWithCustomName()
    {
        string customName = saveNameInputField != null ? saveNameInputField.text.Trim() : "";

        if (string.IsNullOrEmpty(customName))
        {
            Debug.LogWarning("El campo de nombre está vacío, guardando como session.json");
            Save();
            return;
        }

        // Limpia caracteres inválidos para nombre de archivo
        foreach (char c in Path.GetInvalidFileNameChars())
            customName = customName.Replace(c.ToString(), "");

        string customPath = Path.Combine(Application.persistentDataPath, customName + ".json");
        string json = JsonUtility.ToJson(session, true);
        File.WriteAllText(customPath, json);
        Debug.Log("Guardado con nombre personalizado en: " + customPath);
    }

    private void Save()
    {
        string json = JsonUtility.ToJson(session, true);
        File.WriteAllText(SavePath, json);
        Debug.Log("Guardado en: " + SavePath);
    }

    private SpellRecord GetOrCreateRecord(string spellName)
    {
        var record = session.spellRecords.Find(r => r.spellName == spellName);
        if (record != null) return record;

        record = new SpellRecord { spellName = spellName, worstSimilarity = 1f };
        session.spellRecords.Add(record);
        return record;
    }

    public SessionData GetSession() => session;

    void OnApplicationQuit()
    {
        Save();
        
    }
}