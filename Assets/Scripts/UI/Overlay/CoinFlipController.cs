using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class CoinFlipController : MonoBehaviour
{
    [SerializeField] private GameObject coinFlipPrefab; // CoinFlipUI 프리팹

    private CoinFlipUI _ui;
    private bool _bound;

    // TurnManager에서 받은 결과 저장
    private bool _hasResult;
    private bool _isHeads;
    private ulong _p1LeftId;
    private ulong _p2RightId;

    private void OnEnable()
    {
        StartCoroutine(WaitAndBind());
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnSeatsDecided -= OnSeatsDecided;

        _bound = false;
    }

    private IEnumerator WaitAndBind()
    {
        while (GameManager.Instance == null)
            yield return null;

        if (!_bound)
        {
            _bound = true;
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }

        while (TurnManager.Instance == null)
            yield return null;

        TurnManager.Instance.OnSeatsDecided -= OnSeatsDecided;
        TurnManager.Instance.OnSeatsDecided += OnSeatsDecided;
    }

    private void OnSeatsDecided(bool isHeads, ulong p1LeftId, ulong p2RightId)
    {
        _hasResult = true;
        _isHeads = isHeads;
        _p1LeftId = p1LeftId;
        _p2RightId = p2RightId;
    }

    private void OnGameStateChanged(GameState oldState, GameState newState)
    {
        if (newState == GameState.CoinFlip)
        {
            ShowAndPlay();
        }
        else
        {
            Hide();
        }
    }

    private void EnsureUI()
    {
        if (_ui != null) return;

        if (coinFlipPrefab == null)
        {
            Debug.LogError("[CoinFlip] coinFlipPrefab이 할당되지 않았습니다.");
            return;
        }

        var go = Instantiate(coinFlipPrefab);
        _ui = go.GetComponentInChildren<CoinFlipUI>(true);

        if (_ui == null)
            Debug.LogError("[CoinFlip] CoinFlipUI 컴포넌트를 프리팹에서 찾지 못했습니다.");
    }

    private void ShowAndPlay()
    {
        EnsureUI();
        if (_ui == null) return;

        // 결과가 아직 없으면(거의 없겠지만) 안전 기본값
        bool isHeads = _hasResult ? _isHeads : true;

        // starter 계산
        ulong starterId;
        if (_hasResult)
            starterId = isHeads ? _p1LeftId : _p2RightId;
        else
        {
            // fallback: 로컬이 host면 선공처럼 보이게
            starterId = (NetworkManager.Singleton != null) ? NetworkManager.ServerClientId : 0;
        }

        bool amIStarter = (NetworkManager.Singleton != null &&
                           NetworkManager.Singleton.IsListening &&
                           NetworkManager.Singleton.LocalClientId == starterId);

        _ui.Play(isHeads, amIStarter);
    }

    private void Hide()
    {
        if (_ui != null)
            _ui.Hide();
    }
}