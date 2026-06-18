using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class DuelSpawner : MonoBehaviourPunCallbacks
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float spawnRadius = 5f;
    
    [Header("Pre-Duel Sequence (sec)")]
    [SerializeField] private float faceCenterDuration    = 1.5f;
    [SerializeField] private float restrictedWalkDuration = 3f;
    [SerializeField] private float rotationSmoothTime    = 0.4f;

    private DuelRestrictor _restrictor;
    private Transform      _localPlayerTransform;
    private Vector3        _centerPoint;

    private void Awake()
    {
        _centerPoint = arenaCenter != null ? arenaCenter.position : Vector3.zero;
    }

    private void Start()
    {
        StartCoroutine(InitAfterSpawn());
    }

    private IEnumerator InitAfterSpawn()
    {
        yield return new WaitUntil(() =>
            GameManager.Instance != null && GameManager.Instance.LocalPlayer != null);

        _localPlayerTransform = GameManager.Instance.LocalPlayer.transform;

        _restrictor = _localPlayerTransform.GetComponent<DuelRestrictor>();
        if (_restrictor == null)
            _restrictor = _localPlayerTransform.gameObject.AddComponent<DuelRestrictor>();
        
        float totalPreDuelTime = faceCenterDuration + restrictedWalkDuration;
        GameManager.Instance.SetDuelStartDelay(totalPreDuelTime);

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
        float angle     = slotIndex * angleStep;
        float rad       = angle * Mathf.Deg2Rad;
        return _centerPoint + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * spawnRadius;
    }

    private void TeleportToSlot()
    {
        int total = PhotonNetwork.CurrentRoom.PlayerCount;
        int slot  = GetLocalSlotIndex();

        Vector3 spawnPos = GetSpawnPoint(slot, total);
        spawnPos.y = _localPlayerTransform.position.y;
        _localPlayerTransform.position = spawnPos;
        
        SnapFaceCenter();
    }

    private void SnapFaceCenter()
    {
        Vector3 dir = _centerPoint - _localPlayerTransform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            _localPlayerTransform.rotation = Quaternion.LookRotation(dir);
    }

    private Quaternion GetRotationFacingCenter()
    {
        Vector3 dir = _centerPoint - _localPlayerTransform.position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dir) : _localPlayerTransform.rotation;
    }

    private Quaternion GetRotationFacingAway()
    {
        Vector3 dir = _localPlayerTransform.position - _centerPoint;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dir) : _localPlayerTransform.rotation;
    }

    private IEnumerator SmoothRotateTo(Quaternion target)
    {
        float elapsed = 0f;
        Quaternion start = _localPlayerTransform.rotation;

        while (elapsed < rotationSmoothTime)
        {
            elapsed += Time.deltaTime;
            _localPlayerTransform.rotation = Quaternion.Slerp(start, target, elapsed / rotationSmoothTime);
            yield return null;
        }
        _localPlayerTransform.rotation = target;
    }

    private IEnumerator PreDuelSequence()
    {
        _restrictor.SetState(DuelState.FullLock);
        
        yield return new WaitForSeconds(faceCenterDuration);
        
        yield return StartCoroutine(SmoothRotateTo(GetRotationFacingAway()));

        GameManager.Instance.StartDuelCountdown();
        _restrictor.SetState(DuelState.WalkOnly);
        yield return new WaitForSeconds(restrictedWalkDuration);
        
        _restrictor.SetState(DuelState.Free);
    }
}