using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using Cinemachine;
public class FinishTrack : ATrackEvent
{
    [SerializeField] private GameObject pedestalCam;
    [Header("Pedestal")]
    [SerializeField] private float multiplierOffset;
    [SerializeField] private float pedestalRiseSpeed;
    [SerializeField] private Transform risingPedestal;
    [Header("Move Points")]
    [SerializeField] private Transform pedestalMovePoint;
    [SerializeField] private Transform runnerUpPoint;
    [SerializeField] private Transform firstPlacePoint;
    [SerializeField] private Transform secondPlacePoint;
    [SerializeField] private Transform thirdPlacePoint;
    [SerializeField] private Transform fourthPlacePoint;
    [SerializeField] private Transform topPlatformPoint;

    [SerializeField] private UnityEvent Onx10Platform;
    [SerializeField] private UnityEvent OnPlatformRise;

    public static event Action OnCupEarned;

    private int _coinsMultiplier = 1;
    private int _positionIndex = 0;
    private bool _isColliding;

    private Vector3 _initPedestalPos;

    private void Awake()
    {
        _initPedestalPos = risingPedestal.transform.localPosition;
        GameController.OnRestartLevel += OnRestart;
    }

    private void OnRestart()
    {
        Init();
    }

    public override void Init()
    {
        risingPedestal.transform.localPosition = _initPedestalPos;
        _coinsMultiplier = 1;
        _positionIndex = 0;
        StopAllCoroutines();
    }


    public override void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out ARunner r))
        {
            r.StopRunnerSpecialParticles();
            if (r.IsFinished)
                return;

            r.IsFinished = true;
            _positionIndex++;
            r.SetFinishPosition(_positionIndex);
            r.State = RunnerState.Finish;
            Debug.Log($"{r.gameObject.name} is Finishend in {r.FinishIndex} place");
            StartCoroutine(JumpToPedestal(r, PedestalPos(r.FinishIndex).position));
        }
    }

    private Transform PedestalPos(int pos)
    {
        Transform point = null;

        switch(pos)
        {
            case 1:
                point = firstPlacePoint;
                break;

            case 2:
                point = secondPlacePoint;
                break;
            case 3:
                point = thirdPlacePoint;
                break;
            case 4:
                point = fourthPlacePoint;
                break;

        }
        return point;
    }

    private IEnumerator JumpToPedestal(ARunner r, Vector3 dir)
    {
        r.transform.LookAt(dir);
        r.transform.DOMove(dir, 1);
        yield return new WaitForSeconds(1.2f);
        r.SetFinishAnimation();
        r.Body.isKinematic = true;
        r.transform.DORotate(new Vector3(0, -180, 0),1);
        StartCoroutine(RiseFirstPlace(r));
    }

    private IEnumerator RiseFirstPlace(ARunner runner)
    {
        float xMultiplier = 0f;
        while (_positionIndex == 1)
        {

            if (runner is PlayerRunner)
                pedestalCam.gameObject.SetActive(true);


            var pedestalT = risingPedestal.transform.position;
            var runnerT = runner.transform.position;

            risingPedestal.transform.position = Vector3.MoveTowards(pedestalT, pedestalMovePoint.position, pedestalRiseSpeed * Time.deltaTime);
            runner.transform.position = Vector3.MoveTowards(runnerT, runnerUpPoint.position, pedestalRiseSpeed * Time.deltaTime);
            xMultiplier += Time.deltaTime * multiplierOffset;
            _coinsMultiplier = Mathf.RoundToInt(xMultiplier);
            if (risingPedestal.transform.position == pedestalMovePoint.position && runner.FinishIndex == 1 && runner as PlayerRunner)
            {
                PlayerRunner player = runner as PlayerRunner;
                pedestalCam.gameObject.SetActive(false);

                player.TurnOnFinishCamera();
                _coinsMultiplier = 10;
                StopAllCoroutines();
                TopPlatform(runner);
                OnCupEarned?.Invoke();
            }
            yield return null;
        }
        while (_positionIndex != 4)
            yield return null;

        CheckPlayerPos(runner);
    }

    private void TopPlatform(ARunner runner)
    {
        runner.RunnerAnimator.Play("Normal Sprint");
        Onx10Platform?.Invoke();
        var seq = DOTween.Sequence();
        seq.Append(runner.transform.DOMove(topPlatformPoint.position, 1.3f));
        seq.Join(runner.transform.DORotate(new Vector3(0, topPlatformPoint.position.y, 0), 1));
        seq.OnComplete(() =>Onx10PlayerWin(runner));

    }

    private void Onx10PlayerWin(ARunner runner)
    {
        int endMultiplier = _coinsMultiplier < 1 ? 1 : _coinsMultiplier;
        int totalPoints = endMultiplier * GameController.SessionScore;

        Debug.Log(totalPoints);
        runner.RunnerAnimator.Play("Victory");
        GameController.Data.Cups++;
        GameController.Data.Coins += totalPoints;
        GameController.CurrentState = GameState.Win;
        GameController.Data.WinsToNextRank++;
        SaveController.SaveData();
    }


    private void CheckPlayerPos(ARunner runner)
    {
        runner.CheckPosition();
        runner.Body.isKinematic = false;

        if(runner is PlayerRunner)
        {
            if(runner.FinishIndex == 1)
            {
                int endMultiplier = _coinsMultiplier < 1 ? 1 : _coinsMultiplier;
                int totalPoints = endMultiplier * GameController.SessionScore;
                GameController.Data.Coins += totalPoints;
            }
            pedestalCam.gameObject.SetActive(false);
        }    

    }


    public override void Unsubscribe()
    {
        _positionIndex = 0;
        _coinsMultiplier = 1;
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        GameController.OnRestartLevel -= OnRestart;
    }
}
