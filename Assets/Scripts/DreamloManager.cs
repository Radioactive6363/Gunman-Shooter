using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Maneja la comunicacion con Dreamlo: sumar victorias por nickname
/// y obtener el top N del leaderboard para mostrarlo en el menu.
/// </summary>
public class DreamloManager : MonoBehaviour
{
    public static DreamloManager Instance;

    [Header("Dreamlo Codes")]
    [Tooltip("Codigo privado (add/delete). Nunca deberia quedar visible en un build publico sensible.")]
    [SerializeField] private string privateCode = "PRIVATE_CODE";

    [Tooltip("Codigo publico (lectura).")]
    [SerializeField] private string publicCode = "PUBLIC_CODE";

    /// <summary>Se dispara cuando el leaderboard termina de cargarse.</summary>
    public event Action<List<DreamloEntry>> OnLeaderboardLoaded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ---------------------------------------------------------------
    // SUMAR VICTORIA
    // ---------------------------------------------------------------

    /// <summary>
    /// Suma 1 victoria al nickname indicado. Busca el puntaje actual
    /// en el leaderboard y sube el total en 1 antes de enviarlo.
    /// </summary>
    public void AddWin(string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogWarning("[Dreamlo] Nickname vacio, no se envia victoria.");
            return;
        }

        StartCoroutine(AddWinRoutine(nickname));
    }

    private IEnumerator AddWinRoutine(string nickname)
    {
        int currentWins = 0;

        // 1) Traer el leaderboard completo (formato pipe) y buscar el jugador
        string listUrl = $"http://dreamlo.com/lb/{publicCode}/pipe";

        using (UnityWebRequest www = UnityWebRequest.Get(listUrl))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                List<DreamloEntry> entries = ParseLeaderboard(www.downloadHandler.text);
                DreamloEntry existing = entries.Find(e =>
                    string.Equals(e.name, nickname, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                    currentWins = existing.score;
            }
            else
            {
                Debug.LogWarning($"[Dreamlo] No se pudo leer el leaderboard antes de sumar: {www.error}");
            }
        }

        // 2) Enviar el nuevo total (actual + 1)
        int newWins = currentWins + 1;
        string addUrl = $"http://dreamlo.com/lb/{privateCode}/add/{UnityWebRequest.EscapeURL(nickname)}/{newWins}";

        using (UnityWebRequest www = UnityWebRequest.Get(addUrl))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
                Debug.Log($"[Dreamlo] {nickname} ahora tiene {newWins} victorias.");
            else
                Debug.LogError($"[Dreamlo] Error al actualizar puntaje de {nickname}: {www.error}");
        }
    }

    // ---------------------------------------------------------------
    // OBTENER LEADERBOARD
    // ---------------------------------------------------------------

    /// <summary>Pide el top N de jugadores por victorias.</summary>
    public void GetLeaderboard(int topCount = 10)
    {
        StartCoroutine(GetLeaderboardRoutine(topCount));
    }

    private IEnumerator GetLeaderboardRoutine(int topCount)
    {
        string url = $"http://dreamlo.com/lb/{publicCode}/pipe";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Dreamlo] Error al obtener leaderboard: {www.error}");
                yield break;
            }

            Debug.Log($"[Dreamlo] Respuesta cruda: '{www.downloadHandler.text}'");

            List<DreamloEntry> entries = ParseLeaderboard(www.downloadHandler.text);
            entries.Sort((a, b) => b.score.CompareTo(a.score));

            Debug.Log($"[Dreamlo] Entradas parseadas: {entries.Count}");

            if (entries.Count > topCount)
                entries = entries.GetRange(0, topCount);

            OnLeaderboardLoaded?.Invoke(entries);
        }
    }

    /// <summary>
    /// Parsea el formato "pipe" de Dreamlo: una linea de texto por jugador,
    /// campos separados por "|" -> name|score|seconds|text|date
    /// Evitamos JsonUtility porque cuando el leaderboard tiene 0 o 1
    /// jugadores, el JSON de Dreamlo cambia de forma (entry deja de ser
    /// array) y tira ArgumentException al parsear.
    /// </summary>
    private List<DreamloEntry> ParseLeaderboard(string raw)
    {
        List<DreamloEntry> result = new List<DreamloEntry>();

        if (string.IsNullOrWhiteSpace(raw))
            return result;

        string[] lines = raw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string trimmed = line.Trim('\r', ' ');
            if (string.IsNullOrEmpty(trimmed)) continue;

            string[] parts = trimmed.Split('|');
            if (parts.Length < 2) continue;

            if (int.TryParse(parts[1], out int score))
            {
                result.Add(new DreamloEntry
                {
                    name = parts[0],
                    score = score
                });
            }
        }

        return result;
    }
}

[Serializable]
public class DreamloEntry
{
    public string name;
    public int score;
}
