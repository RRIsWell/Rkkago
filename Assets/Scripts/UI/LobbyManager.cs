using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [Header("UI HUD")]
    public GameObject startHUD;
    public GameObject createSessionHUD;
    public GameObject joinCodeHUD;
    public GameObject joinByCodeHUD;
    public GameObject sessionListHUD;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startHUD.GetComponent<StartUGUI>().CreateBtnOnClick -= OnActiveCreateSessionHUD;
        startHUD.GetComponent<StartUGUI>().CreateBtnOnClick += OnActiveCreateSessionHUD;
        startHUD.GetComponent<StartUGUI>().JoinBtnOnClick -= OnActiveSessionListHUD;
        startHUD.GetComponent<StartUGUI>().JoinBtnOnClick += OnActiveSessionListHUD;

        CreateSession cs = createSessionHUD.GetComponent<CreateSession>();
        cs.CreateSessioinBtnOnClick -= OnActiveJoinCodeHUD;
        cs.CreateSessioinBtnOnClick += OnActiveJoinCodeHUD;
        joinCodeHUD.GetComponent<ShowJoinCode>().OnDisableJoinCode -= cs.DestroyCurrSession;
        joinCodeHUD.GetComponent<ShowJoinCode>().OnDisableJoinCode += cs.DestroyCurrSession;
        
        sessionListHUD.GetComponent<SessionBrowser>().SessionItemBtnOnClick -= OnActiveJoinByCodeHUD;
        sessionListHUD.GetComponent<SessionBrowser>().SessionItemBtnOnClick += OnActiveJoinByCodeHUD;
        
        SoundManager.Instance.PlayBGM(BGMName.로비);
    }

    private void OnActiveStartHUD(bool active)
    {
        startHUD.SetActive(active);
        SoundManager.Instance.PlaySFX(SFXName.버튼클릭);
        
        // 나머지 UI 비활성화
        createSessionHUD.SetActive(false);
        joinCodeHUD.SetActive(false);
        joinByCodeHUD.SetActive(false);
        sessionListHUD.SetActive(false);
    }

    private void OnActiveCreateSessionHUD(bool active)
    {
        createSessionHUD.SetActive(active);
        SoundManager.Instance.PlaySFX(SFXName.버튼클릭);
        SoundManager.Instance.PlaySFX(SFXName.UI보이기);
        
        // 나머지 UI 비활성화
        startHUD.SetActive(false);
        joinCodeHUD.SetActive(false);
        joinByCodeHUD.SetActive(false);
        sessionListHUD.SetActive(false);
    }
    
    private void OnActiveJoinCodeHUD(bool active)
    {
        joinCodeHUD.SetActive(active);
        SoundManager.Instance.PlaySFX(SFXName.버튼클릭);
        SoundManager.Instance.PlaySFX(SFXName.UI보이기);
        
        // 나머지 UI 비활성화
        startHUD.SetActive(false);
        createSessionHUD.SetActive(false);
        joinByCodeHUD.SetActive(false);
        sessionListHUD.SetActive(false);
    }

    private void OnActiveJoinByCodeHUD(bool active)
    {
        joinByCodeHUD.SetActive(active);
        SoundManager.Instance.PlaySFX(SFXName.버튼클릭);
        SoundManager.Instance.PlaySFX(SFXName.UI보이기);
        
        // 나머지 UI 비활성화
        startHUD.SetActive(false);
        createSessionHUD.SetActive(false);
        joinCodeHUD.SetActive(false);
        sessionListHUD.SetActive(false);
    }

    private void OnActiveSessionListHUD(bool active)
    {
        sessionListHUD.SetActive(active);
        SoundManager.Instance.PlaySFX(SFXName.버튼클릭);
        SoundManager.Instance.PlaySFX(SFXName.UI보이기);
        
        // 나머지 UI 비활성화
        startHUD.SetActive(false);
        createSessionHUD.SetActive(false);
        joinCodeHUD.SetActive(false);
        joinByCodeHUD.SetActive(false);
    }
    
}
