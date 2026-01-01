using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blocks.Common;
using Blocks.Sessions;
using Blocks.Sessions.Common;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Blocks.Sessions.Common;
using Unity.Multiplayer.Center.Common;
using Unity.Services.Authentication;

public class SessionBrowser : MonoBehaviour
{
    [Header("UI")]
    public Transform sessionListParent;    // 세션 목록 출력 위치
    public GameObject sessionItemPrefab;   // 세션 리스트 프리팹
    public Button refreshButton;
    
    private SessionBrowserVM viewModel;
    private List<GameObject> createdItems = new();

    public event Action<bool> SessionItemBtnOnClick;

    [SerializeField]
    private SessionSettings sessionSettings;
    public SessionSettings SessionSettings
    {
        get => sessionSettings;
        set
        {
            if (sessionSettings == value)
                return;

            sessionSettings = value;
        }
    }

    private void Awake()
    {
        viewModel = new SessionBrowserVM(SessionSettings?.sessionType);
        viewModel.PropertyChanged += OnViewModelChanged;

        refreshButton.onClick.AddListener(OnRefreshButtonClicked);
    }

    private void OnEnable()
    {
        OnRefreshButtonClicked();
    }

    /// <summary>
    /// UI 리스트 새로고침 버튼 눌렀을 때
    /// </summary>
    private async void OnRefreshButtonClicked()
    {
        await viewModel.UpdateSessionListAsync(20);
        RefreshListUI();
    }

    /// <summary>
    /// 각 방 눌렀을 때 -> 코드로 참여 화면으로 넘어감
    /// </summary>
    /// <param name="index"></param>
    private void OnSessionButtonClicked(int index)
    {
        viewModel.SelectedSessionIndex = index;
        Debug.Log($"Selected: {index}, Session Name: {viewModel.Sessions[index].Name}");
        
        if (!viewModel.SelectedAndAvailable)
        {
            Debug.LogError("Selected session is no longer selected.");
            return;
        }
        
        // UI 전환
        SessionItemBtnOnClick?.Invoke(true);
    }
    
    /// <summary>
    /// ViewModel 값 변경 감지
    /// </summary>
    /// <param name="propertyName"></param>
    void OnViewModelChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(SessionBrowserVM.Sessions):
                RefreshListUI();
                break;

            case nameof(SessionBrowserVM.CanRefresh):
                refreshButton.interactable = viewModel.CanRefresh;
                break;
        }
    }
    
    /// <summary>
    /// UI 리스트 갱신
    /// </summary>
    void RefreshListUI()
    {
        foreach (var go in createdItems)
            Destroy(go);

        createdItems.Clear();

        for (int i = 0; i < viewModel.Sessions.Count; i++)
        {
            var item = Instantiate(sessionItemPrefab, sessionListParent);
            createdItems.Add(item);

            var controller = item.GetComponent<SessionItem>();
            controller.SetData(viewModel.Sessions[i]);

            int index = i;
            Button button = item.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnSessionButtonClicked(index));
        }
    }

    private void OnDestroy()
    {
        viewModel?.Dispose();
    }
}
