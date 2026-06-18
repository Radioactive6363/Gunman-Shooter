using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI stateInfoText;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI SVeventTxt;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Image deathOverlayImage;

    private Coroutine _phaseCoroutine;
    private Coroutine _serverMessageCoroutine;

    private void OnEnable()
    {
        DuelSpawner.OnPhaseInstruction += HandlePhaseInstruction;
        GameManager.OnLobbyCountdownTick += HandleLobbyCountdownTick;
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        
        GameManager.OnServerMessageDeclared += HandleServerMessage;
        
        GameManager.OnRoundWinnerDeclared += HandleRoundWinner;
        GameManager.OnMatchWinnerDeclared += HandleMatchWinner;
        
        GameManager.OnMatchCancelled += HandleMatchCancelled;
        PlayerHealth.OnLocalDeath += HandleLocalDeath;
    }

    private void OnDisable()
    {
        DuelSpawner.OnPhaseInstruction -= HandlePhaseInstruction;
        GameManager.OnLobbyCountdownTick -= HandleLobbyCountdownTick;
        GameManager.OnGameStateChanged -= HandleGameStateChanged;

        GameManager.OnServerMessageDeclared -= HandleServerMessage;

        GameManager.OnRoundWinnerDeclared -= HandleRoundWinner;
        GameManager.OnMatchWinnerDeclared -= HandleMatchWinner;
        
        GameManager.OnMatchCancelled -= HandleMatchCancelled;
        PlayerHealth.OnLocalDeath -= HandleLocalDeath;
    }

    private void Awake()
    {
        if (countdownText != null)  countdownText.text  = "";
        if (stateInfoText != null)  stateInfoText.text  = "Loading...";
        if (SVeventTxt != null)     SVeventTxt.text     = "";
    }
    
    private void Start()
    {
        UpdateWeaponNameUI();
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.Preparation || newState == GameState.Duel)
        {
            if (deathOverlayImage != null) deathOverlayImage.gameObject.SetActive(false);
        }
        
        if (newState == GameState.Duel)
        {
            if (countdownText != null) countdownText.text = "";
            if (gameObject.activeInHierarchy) StartCoroutine(ClearDrawTextRoutine());
        }
        
        else if (newState == GameState.WaitingForPlayers)
        {
            if (stateInfoText != null) 
            {
                stateInfoText.color = Color.white;
                stateInfoText.text = "Waiting for Players to Ready Up...";
            }
        }
    }
    
    private IEnumerator ClearDrawTextRoutine()
    {
        yield return new WaitForSeconds(1f);
        if (stateInfoText != null && stateInfoText.text == "DRAW!") 
        {
            stateInfoText.text = "";
        }
    }
    
    private void HandleRoundWinner(string winnerName)
    {
        if (stateInfoText != null) 
        {
            stateInfoText.color = Color.yellow;
            stateInfoText.text = $"¡{winnerName} has survived the round!";
        }
    }

    private void HandleMatchWinner(string winnerName)
    {
        if (stateInfoText != null) 
        {
            stateInfoText.color = Color.green;
            stateInfoText.text = $"¡Final Victory!\nWinner is: {winnerName}";
        }
        if (countdownText != null) countdownText.text = "";
    }
    
    private void HandleServerMessage(string message)
    {
        if (SVeventTxt == null) return;

        SVeventTxt.text = message;

        if (_serverMessageCoroutine != null) StopCoroutine(_serverMessageCoroutine);
        _serverMessageCoroutine = StartCoroutine(ClearServerMessageRoutine());
    }

    private IEnumerator ClearServerMessageRoutine()
    {
        yield return new WaitForSeconds(3.5f);
        if (SVeventTxt != null) SVeventTxt.text = "";
    }
    
    private void HandlePhaseInstruction(string instruction, float duration)
    {
        if (stateInfoText != null) 
        {
            stateInfoText.color = Color.white;
            stateInfoText.text = instruction;
        }

        if (_phaseCoroutine != null) StopCoroutine(_phaseCoroutine);
        
        if (duration > 0)
        {
            _phaseCoroutine = StartCoroutine(DecimalCountdownRoutine(duration));
        }
        else
        {
            if (countdownText != null) countdownText.text = "";
        }
    }

    private IEnumerator DecimalCountdownRoutine(float duration)
    {
        float timer = duration;
        while (timer > 0)
        {
            if (countdownText != null) countdownText.text = timer.ToString("F1");
            yield return null;
            timer -= Time.deltaTime;
        }
        
        if (countdownText != null) countdownText.text = "";
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
    
    private void UpdateWeaponNameUI()
    {
        if (weaponNameText == null) return;
        
        LevelWeaponConfig levelConfig = FindObjectOfType<LevelWeaponConfig>();
        
        if (levelConfig != null)
        {
            weaponNameText.text = $"Current Weapon: {levelConfig.allowedWeapon.ToString()}";
        }
        else
        {
            weaponNameText.text = "";
        }
    }
    
    private void HandleMatchCancelled()
    {
        if (stateInfoText != null) 
        {
            stateInfoText.color = Color.red;
            stateInfoText.text = "Game Canceled\nNot Enough Players.";
        }
        
        if (countdownText != null) countdownText.text = "";
    }
    
    private void HandleLocalDeath()
    {
        if (deathOverlayImage != null)
        {
            deathOverlayImage.gameObject.SetActive(true);
        }
    }
    
}