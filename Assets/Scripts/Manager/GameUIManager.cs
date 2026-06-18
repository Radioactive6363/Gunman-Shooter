using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Photon.Pun;

public class GameUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI stateInfoText;
    [SerializeField] private TextMeshProUGUI countdownText;
    
    [Header("Panel Elements")]
    [SerializeField] private GameObject endMatchPanel;
    [SerializeField] private TextMeshProUGUI winnerText;

    private int _gameStartTimer;

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            _gameStartTimer = GameManager.Instance.TimeTillDuel;
        }
        if (countdownText != null) countdownText.text = "";
        if (endMatchPanel != null) endMatchPanel.SetActive(false);
        if (stateInfoText != null) stateInfoText.text = "Waiting Player...";
        if (winnerText != null) winnerText.text = "";
    }

    private void HandleGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.WaitingForPlayers:
                if (stateInfoText != null) stateInfoText.text = "Waiting for Players to Ready Up...";
                if (countdownText != null) countdownText.text = "";
                break;

            case GameState.Preparation:
                if (stateInfoText != null) stateInfoText.text = "¡Ready for Duel!";
                StartCoroutine(CountDownCoroutine(_gameStartTimer));
                break;

            case GameState.Duel:
                if (stateInfoText != null) stateInfoText.text = "¡Duel On Course!";
                if (countdownText != null) countdownText.text = "";
                break;

            case GameState.PostDuel:
                if (stateInfoText != null) stateInfoText.text = "Match Ended...";
                break;

            case GameState.MatchOver:
                if (stateInfoText != null) stateInfoText.text = "End of Match";
                ShowEndMatchUI();
                break;
        }
    }

    private IEnumerator CountDownCoroutine(int duration)
    {
        int timer = duration;
        while (timer > 0)
        {
            if (countdownText != null) countdownText.text = timer.ToString();
            yield return new WaitForSeconds(1f);
            timer--;
        }

        if (countdownText != null)
        {
            countdownText.text = "¡GO!";
            yield return new WaitForSeconds(0.8f);
            countdownText.text = "";
        }
    }

    private void ShowEndMatchUI()
    {
        if (endMatchPanel != null)
        {
            endMatchPanel.SetActive(true);
            if (winnerText != null) winnerText.text = "¡Match Ended!";
        }
    }
}