using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class DuelSpawner : MonoBehaviourPunCallbacks
{
    [Header("Spawn Settings")]
    [Tooltip("Center of the arena. Spawn points radiate outward from here.")]
    [SerializeField] private Transform arenaCenter;

    [Tooltip("Distance from center where players spawn.")]
    [SerializeField] private float spawnRadius = 5f;

    [Header("Pre-Duel Sequence Timings (seconds)")]
    [Tooltip("How long players face the center before turning away.")]
    [SerializeField] private float faceCenterDuration = 1.5f;

    [Tooltip("How long players can walk (no camera, no backward) before GO.")]
    [SerializeField] private float restrictedWalkDuration = 3f;

    [Tooltip("How long the GO! text shows before full control is given.")]
    [SerializeField] private float goDuration = 0.8f;

    private DuelMovementRestrictor _restrictor;
    private Transform _localPlayerTransform;
    private Vector3 _centerPoint;

    private void Awake()
    {
        if (arenaCenter != null)
            _centerPoint = arenaCenter.position;
        else
            _centerPoint = Vector3.zero;
    }

    private void Start()
    {
        StartCoroutine(InitAfterSpawn());
    }

    private IEnumerator InitAfterSpawn()
    {
        yield return new WaitUntil(() => GameManager.Instance != null && GameManager.Instance.LocalPlayer != null);

        _localPlayerTransform = GameManager.Instance.LocalPlayer.transform;
        
        _restrictor = _localPlayerTransform.GetComponent<DuelMovementRestrictor>();
        if (_restrictor == null)
            _restrictor = _localPlayerTransform.gameObject.AddComponent<DuelMovementRestrictor>();
        
        TeleportToSlot();
        
        StartCoroutine(PreDuelSequence());
    }

    private int GetLocalSlotIndex()
    {
        List<Player> sorted = new List<Player>(PhotonNetwork.PlayerList);
        sorted.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));
        return sorted.FindIndex(p => p.IsLocal);
    }

    private Vector3 GetSpawnPoint(int slotIndex, int totalPlayers)
    {
        float angleStep = 360f / totalPlayers;
        float angle = slotIndex * angleStep;
        float rad = angle * Mathf.Deg2Rad;
        return _centerPoint + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * spawnRadius;
    }

    private void TeleportToSlot()
    {
        int total = PhotonNetwork.CurrentRoom.PlayerCount;
        int slot  = GetLocalSlotIndex();

        Vector3 spawnPos = GetSpawnPoint(slot, total);
        
        spawnPos.y = _localPlayerTransform.position.y;
        _localPlayerTransform.position = spawnPos;
        
        FaceCenter();
    }

    private void FaceCenter()
    {
        Vector3 dir = (_centerPoint - _localPlayerTransform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            _localPlayerTransform.rotation = Quaternion.LookRotation(dir);
    }

    private void FaceAwayFromCenter()
    {
        Vector3 dir = (_localPlayerTransform.position - _centerPoint);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            _localPlayerTransform.rotation = Quaternion.LookRotation(dir);
    }
    
    private IEnumerator PreDuelSequence()
    {
        _restrictor.SetState(DuelState.FullLock);
        
        FaceCenter();
        yield return new WaitForSeconds(faceCenterDuration);
        
        FaceAwayFromCenter();
        
        _restrictor.SetState(DuelState.WalkOnly);
        yield return new WaitForSeconds(restrictedWalkDuration);
        
        _restrictor.SetState(DuelState.Free);
        
    }

}