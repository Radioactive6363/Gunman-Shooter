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

    private Coroutine _countdownCoroutine;

    private void OnEnable()
    {
        GameManager.OnGameStateChanged      += HandleGameStateChanged;
        GameManager.OnDuelCountdownStarted  += HandleCountdownStarted;
        GameManager.OnLobbyCountdownTick     += HandleLobbyCountdownTick;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged      -= HandleGameStateChanged;
        GameManager.OnDuelCountdownStarted  -= HandleCountdownStarted;
        GameManager.OnLobbyCountdownTick     -= HandleLobbyCountdownTick;
    }

    private void Start()
    {
        if (countdownText != null)  countdownText.text  = "";
        if (endMatchPanel != null)  endMatchPanel.SetActive(false);
        if (stateInfoText != null)  stateInfoText.text  = "Waiting Player...";
        if (winnerText != null)     winnerText.text     = "";
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
    
    private void HandleCountdownStarted(int duration)
    {
        if (_countdownCoroutine != null)
            StopCoroutine(_countdownCoroutine);
        _countdownCoroutine = StartCoroutine(CountDownCoroutine(duration));
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
        _countdownCoroutine = null;
    }
    
    private void HandleLobbyCountdownTick(int remaining)
    {
        if (remaining == -1)
        {
            if (countdownText != null) countdownText.text = "";
        }
        else if (remaining == 0)
        {
            if (countdownText != null) countdownText.text = "¡GO!";
        }
        else
        {
            if (countdownText != null) countdownText.text = remaining.ToString();
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