using UnityEngine;

public class DuelMovementRestrictor : MonoBehaviour
{
    public static DuelState CurrentState { get; private set; } = DuelState.Free;
 
    private PlayerController _controller;
 
    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }
 
    public void SetState(DuelState newState)
    {
        CurrentState = newState;
        ApplyState(newState);
    }
 
    private void ApplyState(DuelState state)
    {
        switch (state)
        {
            case DuelState.FullLock:
                break;
 
            case DuelState.WalkOnly:
                break;
            
            case DuelState.Free:
                break;
        }
    }
    
    public static bool CanMove()
        => CurrentState == DuelState.WalkOnly || CurrentState == DuelState.Free;
    
    public static bool CanMoveBackward()
        => CurrentState == DuelState.Free;
    
    public static bool CanRotateCamera()
        => CurrentState == DuelState.Free;
}