using System.Collections.Generic;
using UnityEngine;

public enum LogType { Info, Warning, Error }

public class GameLogger : MonoBehaviour
{
    public static GameLogger Instance;

    [SerializeField] private bool showInBuild = true;
    [SerializeField] Vector2 location = new Vector2(0, 0);
    [SerializeField] Vector2 boxSize = new Vector2(420, 300);
    private readonly List<(string message, LogType type)> logs = new();
    private Vector2 scrollPos;
    private bool visible = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
        
        Application.logMessageReceived += OnUnityLog;
    }
    
    void OnDestroy()
    {
        Application.logMessageReceived -= OnUnityLog;
    }

    private void OnUnityLog(string message, string stackTrace, UnityEngine.LogType type)
    {
        var mapped = type switch
        {
            UnityEngine.LogType.Error or UnityEngine.LogType.Exception => LogType.Error,
            UnityEngine.LogType.Warning => LogType.Warning,
            _ => LogType.Info
        };
        Add(message, mapped);
    }

    public void Add(string message, LogType type = LogType.Info)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        logs.Add(($"[{timestamp}] {message}", type));

        if (logs.Count > 100) logs.RemoveAt(0); // límite de memoria
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) visible = !visible;
    }

    void OnGUI()
    {
        if (!visible) return;
        if (!showInBuild && !Application.isEditor) return;
        
        GUI.Box(new Rect(location.x, location.y, boxSize.x, boxSize.y), "Logger (F1)");
        scrollPos = GUILayout.BeginScrollView(scrollPos,
            GUILayout.Width(boxSize.x), GUILayout.Height(boxSize.y));

        foreach (var (msg, type) in logs)
        {
            GUI.color = type switch
            {
                LogType.Error   => Color.red,
                LogType.Warning => Color.yellow,
                _               => Color.white
            };
            GUILayout.Label(msg);
        }

        GUI.color = Color.white;
        GUILayout.EndScrollView();
    }
}