using UnityEngine;

public class DuelRestrictor : MonoBehaviour
{
    public static DuelState CurrentState { get; private set; }
        = DuelState.Free;

    public void SetState(DuelState newState)
    {
        CurrentState = newState;
    }

    public static bool CanMove()
        => CurrentState != DuelState.FullLock;

    public static bool CanMoveBackward()
        => CurrentState == DuelState.Free;

    public static bool CanRotateCamera()
        => CurrentState == DuelState.Free;
}