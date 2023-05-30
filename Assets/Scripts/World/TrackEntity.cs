using System;
using System.Collections.Generic;
using UnityEngine;

public enum SportType
{
    SprintTrack,
    BoxerTrack,
    WaterTrack,
    SandTrack,
    ClimbingTrack,
    SnowTrack,
    SprintObstaclesTrack,
    Finish,
    Start,
    DontMatter
}

public class TrackEntity : MonoBehaviour
{
    [SerializeField] private SportType trackType;
    [SerializeField] private Transform beginPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private Vector3 posOffset;
    [SerializeField] private List<ATrackEvent> trackEvents = new List<ATrackEvent>();

    private bool _isPlayerSwitchedRight;

    public static event Action OnRightSwitchPlayer;

    #region Properties

    public Vector3 PosOffset => posOffset;
    public bool IsPlayerSwitchedRight => _isPlayerSwitchedRight;
    public SportType TrackType => trackType;
    public Transform BeginPoint => beginPoint;
    public Transform EndPoint => endPoint;

    #endregion


    private void OnTriggerEnter(Collider other)
    {
        if (GameController.CurrentState == GameState.Core)
        {
            if (other.TryGetComponent(out PlayerRunner runner))
            {
                runner.CheckTrack(true);
                runner.PlayTrackTypeParticle(trackType);
            }

        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (GameController.CurrentState == GameState.Core)
        {
            if (other.TryGetComponent(out ARunner runner))
            {
                runner.StopTrackTypeParticles();
                runner.StopRunnerSpecialParticles();
            }
        }
    }


    private void OnTriggerStay(Collider other)
    {
        if(GameController.CurrentState == GameState.Core)
        {
            if (other.TryGetComponent(out ARunner runner))
                    runner.CheckTrack(true);

            if (other.TryGetComponent(out PlayerRunner player))
            {
                if(!_isPlayerSwitchedRight && player.Type == trackType )
                {
                    _isPlayerSwitchedRight = true;
                    OnRightSwitchPlayer?.Invoke();
                }
            }

        }
    }

    public void UnsubscribeFromEvents()
    {
        foreach (var t in trackEvents)
            t.Unsubscribe();
    }
    
}
