using System.Collections;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 스킬 정보 창을 관리하는 컨트롤러.
/// - 매칭 후 / 돌이 떨어질 때마다 자동으로 스킬 정보 표시
/// - ShowCurrentSkillInfo() 호출로 게임 중 언제든 현재 스킬 정보 표시
/// </summary>
public class SkillInfoController : NetworkBehaviour
{
    public static SkillInfoController Instance { get; private set; }

    [SerializeField] private GameObject skillInfoPrefab;
    [SerializeField] private float preShowDuration = 1f;
    [SerializeField] private float autoHideDuration = 2f;

    private SkillInfoUI _instance;
    private GameObject _skillActivateObj;
    private Coroutine _autoHideCoroutine;
    private Coroutine _displaySequenceCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        StartCoroutine(WaitAndBindGameState());
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        if (_autoHideCoroutine != null)
            StopCoroutine(_autoHideCoroutine);
    }
    
    [ClientRpc]
    public void ShowSkillInfoClientRpc()
    {
        // 각 클라이언트(호스트 포함)는 자기 자신의 로컬 스킬을 찾아 UI를 띄움
        ShowCurrentSkillInfo(true); 
    }

    private System.Collections.IEnumerator WaitAndBindGameState(){
        while (GameManager.Instance == null)
            yield return null;

        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    

    /// <summary>
    /// 현재 보유 스킬 정보 창을 띄운다.
    /// 게임 중 특정 함수를 호출해 스킬 정보를 보고 싶을 때 사용.
    /// </summary>
    /// <param name="autoHide">true면 일정 시간 후 자동으로 닫힘</param>
    public void ShowCurrentSkillInfo(bool autoHide = true)
    {
        var (skillIndex, _) = GetLocalPlayerCurrentSkill();
        ShowCurrentSkillInfoInternal(skillIndex, autoHide);
    }

    private void ShowCurrentSkillInfoInternal(int skillIndex, bool autoHide)
    {
        if (skillIndex < 0)
        {
            Debug.Log("[SkillInfo] 아직 부여된 스킬이 없습니다.");
            return;
        }

        var (_, skill) = GetLocalPlayerCurrentSkill();
        if (skill == null)
        {
            Debug.LogWarning("[SkillInfo] 스킬을 찾을 수 없습니다.");
            return;
        }

        EnsureInstance();
        if (_displaySequenceCoroutine != null)
            StopCoroutine(_displaySequenceCoroutine);

        _displaySequenceCoroutine =
            StartCoroutine(DisplaySequence(skill.Data.skillName, skill.Data.skillDescription, autoHide));
    }

    private IEnumerator DisplaySequence(SkillName skillName, string skillDesc, bool autoHide = true)
    {
        // 1. 초기 상태 설정 (둘 다 끄기)
        _instance.Hide();
        if (_skillActivateObj != null) _skillActivateObj.SetActive(true);

        // 2. 첫 번째 오브젝트 1초간 대기
        yield return new WaitForSeconds(preShowDuration);

        // 3. 첫 번째 오브젝트 끄고 메인 스킬 UI 켜기
        if (_skillActivateObj != null) _skillActivateObj.SetActive(false);
        _instance.Show(skillName, skillDesc);

        // 4. autoHide가 true일 때만 일정 시간 후 메인 UI도 닫기
        if (autoHide)
        {
            yield return new WaitForSeconds(autoHideDuration);
            _instance.Hide();
        }

        _displaySequenceCoroutine = null;
    }

    private IEnumerator AutoHideAfterDelay()
    {
        yield return new WaitForSeconds(autoHideDuration);
        Hide();
        _autoHideCoroutine = null;
    }

    /// <summary>
    /// 스킬 정보 창을 닫는다.
    /// </summary>
    public void Hide()
    {
        if (_displaySequenceCoroutine != null)
        {
            StopCoroutine(_displaySequenceCoroutine);
            _displaySequenceCoroutine = null;
        }
        if (_instance != null)
            _instance.Hide();
        if (_skillActivateObj != null)  _skillActivateObj.SetActive(false);
    }

    private void EnsureInstance()
    {
        if (_instance != null) return;

        if (skillInfoPrefab == null)
        {
            Debug.LogError("[SkillInfo] skillInfoPrefab이 할당되지 않았습니다.");
            return;
        }
        
        GameObject go = Instantiate(skillInfoPrefab);

        _instance = go.GetComponentInChildren<SkillInfoUI>();
        
        Transform activateTrans = go.transform.Find("SkillActivate");
        if (activateTrans != null)
        {
            _skillActivateObj = activateTrans.gameObject;
            _skillActivateObj.SetActive(false);
        }
        else
        {
            // 만약 자식의 자식 단계에 있다면 GetComponentsInChildren 사용
            _skillActivateObj = go.transform.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "SkillActivate")?.gameObject;
        }
    }

    /// <summary>
    /// 로컬 플레이어의 현재 스킬 반환 (skillIndex, skill). 없으면 (-1, null)
    /// </summary>
    private (int skillIndex, SkillBase skill) GetLocalPlayerCurrentSkill()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return (-1, null);

        ulong localId = NetworkManager.Singleton.LocalClientId;
        var stones = FindObjectsByType<StoneController>(FindObjectsSortMode.None);

        foreach (var sc in stones)
        {
            if (!sc.IsOwner) continue;

            int idx = sc.CurrentSkillIndex;
            if (idx < 0) continue;

            var skill = sc.SkillContainer.GetSkillByIndex(idx);
            return (idx, skill);
        }

        return (-1, null);
    }

    /// <summary>
    /// GameState 변경에 반응하여 스킬 정보 표시
    /// </summary>
    private void HandleGameStateChanged(GameState oldState, GameState newState)
    {
        if (newState == GameState.SkillInfo)
        {
            ShowCurrentSkillInfoInternal(GetLocalPlayerCurrentSkill().skillIndex, autoHide: true);
        }
        else if (newState == GameState.Playing)
        {
            Hide();
        }
    }

    private IEnumerator WaitForSkillAndShow()
    {
        // 스킬이 부여될 때까지 대기 (최대 5초)
        float elapsed = 0f;
        while (elapsed < 5f)
        {
            var (idx, _) = GetLocalPlayerCurrentSkill();
            if (idx >= 0)
            {
                ShowCurrentSkillInfoInternal(idx, autoHide: false);
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.Log("[SkillInfo] 매칭 후 스킬 대기 시간 초과");
    }
}