using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra el top N del leaderboard de Dreamlo en una tabla del menu.
/// Asigna en el inspector 10 filas (objetos con Rank/Name/Score Text).
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    [Header("Filas del leaderboard (asignar en el inspector)")]
    [SerializeField] private LeaderboardRow[] rows;

    [Header("Config")]
    [SerializeField] private int topCount = 10;

    private void OnEnable()
    {
        RefreshLeaderboard();
    }

    /// <summary>Llamar tambien desde un boton "Refrescar" si queres.</summary>
    public void RefreshLeaderboard()
    {
        Debug.Log("[LeaderboardUI] RefreshLeaderboard llamado.");

        if (DreamloManager.Instance == null)
        {
            Debug.LogWarning("[LeaderboardUI] No hay DreamloManager en la escena.");
            return;
        }

        DreamloManager.Instance.OnLeaderboardLoaded += PopulateRows;
        DreamloManager.Instance.GetLeaderboard(topCount);
    }

    private void OnDisable()
    {
        if (DreamloManager.Instance != null)
            DreamloManager.Instance.OnLeaderboardLoaded -= PopulateRows;
    }

    private void PopulateRows(List<DreamloEntry> entries)
    {
        Debug.Log($"[LeaderboardUI] PopulateRows recibió {entries.Count} entradas.");
        DreamloManager.Instance.OnLeaderboardLoaded -= PopulateRows;

        for (int i = 0; i < rows.Length; i++)
        {
            if (i < entries.Count)
            {
                rows[i].root.SetActive(true);
                rows[i].SetData(i + 1, entries[i].name, entries[i].score);
            }
            else
            {
                rows[i].root.SetActive(false);
            }
        }
    }
}

[System.Serializable]
public class LeaderboardRow
{
    [Tooltip("El GameObject completo de la fila, para poder ocultarla si sobra.")]
    public GameObject root;
    public Text rankText;
    public Text nameText;
    public Text scoreText;

    public void SetData(int rank, string playerName, int score)
    {
        rankText.text = rank.ToString();
        nameText.text = playerName;
        scoreText.text = score.ToString();
    }
}
