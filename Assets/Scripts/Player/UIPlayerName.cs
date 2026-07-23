using System;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class UIPlayerName : MonoBehaviourPun
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text playerName;

    [Header("Colors & Visuals")]
    [SerializeField] private Color aliveColor = Color.green;
    [SerializeField] private Color deadColor = Color.red;
    private PlayerHealth _health;

    private void Awake()
    {
        _health = GetComponentInParent<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (_health != null) 
            _health.OnDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        if (_health != null) 
            _health.OnDeath -= HandlePlayerDeath;
    }

    private void Start()
    {
        playerName.text = photonView.Owner.NickName;
        playerName.color = aliveColor;
    }

    private void HandlePlayerDeath()
    {
        playerName.color = deadColor;
        playerName.text += " (FALLEN)";
    }
}