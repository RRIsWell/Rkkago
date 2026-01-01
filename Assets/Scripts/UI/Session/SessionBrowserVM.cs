using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Properties;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UIElements;

public class SessionBrowserVM : IDisposable
{
    private SessionObserver m_SessionObserver;
    private ServiceObserver<IMultiplayerService> m_ServiceObserver;
    
    private bool m_SelectedAndAvailable;
    private bool m_CanRefresh;
    private int m_SelectedSessionIndex;

    private ISession m_Session;
    private List<SessionInfoVM> m_Sessions;
    
    public List<SessionInfoVM> Sessions
    {
        get => m_Sessions;
        set
        {
            if (m_Sessions == value)
            {
                return;
            }

            m_Sessions = value;
            Notify();
        }
    }
    
    /// <summary>
    /// 플레이어가 선택한 방 index
    /// </summary>
    public int SelectedSessionIndex
    {
        get => m_SelectedSessionIndex;
        set
        {
            if (value >= 0 && value < Sessions.Count)
            {
                m_SelectedSessionIndex = value;
                SelectedAndAvailable = true;
            }
            else
            {
                SelectedAndAvailable = false;
            }
        }
    }

    public string GetSelectedSessionId()
    {
        if (SelectedSessionIndex >= 0 && SelectedSessionIndex < Sessions.Count)
        {
            return Sessions[SelectedSessionIndex].Id;
        }
        return null;
    }
    
    public bool SelectedAndAvailable
    {
        get => m_SelectedAndAvailable;
        set
        {
            var newValue = value;

            if (value && m_Session != null && m_Session.Id == GetSelectedSessionId())
            {
                newValue = false;
            }

            if (m_SelectedAndAvailable != newValue)
            {
                m_SelectedAndAvailable = newValue;
                Notify();
            }
        }
    }
    
    public bool CanRefresh
    {
        get => m_CanRefresh;
        set
        {
            if (m_CanRefresh == value)
            {
                return;
            }

            m_CanRefresh = value;
            Notify();
        }
    }


    public SessionBrowserVM(string sessionType)
    {
        Sessions = new List<SessionInfoVM>();

        m_SessionObserver = new SessionObserver(sessionType);
        m_SessionObserver.SessionAdded += OnSessionAdded;

        if (m_SessionObserver.Session != null)
        {
            OnSessionAdded(m_SessionObserver.Session);
        }
        
        if (UnityServices.Instance != null)
        {
            m_ServiceObserver = new ServiceObserver<IMultiplayerService>();
            if (m_ServiceObserver.Service != null)
            {
                CanRefresh = true;
            }
            else
            {
                CanRefresh = false;
                m_ServiceObserver.Initialized += OnServicesInitialized;
            }
        }
    }

    void OnServicesInitialized(IMultiplayerService service)
    {
        m_ServiceObserver.Initialized -= OnServicesInitialized;
        CanRefresh = true;
    }

    /// <summary>
    /// 세션 리스트 정보 업데이트 (다시 불러옴)
    /// </summary>
    /// <param name="numberOfMaxSessions"></param>
    internal async Task UpdateSessionListAsync(int numberOfMaxSessions)
    {
        // if there is no connection to MultiplayerService, do not try to refresh
        if (!CanRefresh)
        {
            Debug.LogWarning("Cannot refresh session list." +
                "Multiplayer Services are not initialized." +
                "You can initialize them with default settings by adding a " +
                "ServicesInitialization and PlayerAuthentication components in your scene.");
            return;
        }

        try
        {
            CanRefresh = false;
            var queryResult = await MultiplayerService.Instance
                .QuerySessionsAsync(new QuerySessionsOptions
                {
                    SortOptions = new List<SortOption>
                    {
                        new (SortOrder.Descending,SortField.Name)
                    }
                });

            // properly dispose the sessionInfo view models first
            foreach (var session in Sessions)
            {
                session.Dispose();
            }

            Sessions.Clear();
            for (var i = 0; (i < Math.Min(queryResult.Sessions.Count, numberOfMaxSessions)); i++)
            {
                Sessions.Add(new SessionInfoVM(queryResult.Sessions[i]));
            }
            
            CanRefresh = true;
            // reset selection
            SelectedSessionIndex = -1;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to update session list: {ex.Message}");
        }
    }

    private void OnSessionAdded(ISession newSession)
    {
        m_Session = newSession;
        m_Session.RemovedFromSession += OnSessionRemoved;
        m_Session.Deleted += OnSessionRemoved;
        if (m_Session.Id == GetSelectedSessionId())
        {
            SelectedAndAvailable = false;
        }
    }
    private void OnSessionRemoved()
    {
        var lastSessionId = m_Session.Id;
        CleanupSession();
        if (lastSessionId == GetSelectedSessionId())
        {
            SelectedAndAvailable = true;
        }
    }

    private void CleanupSession()
    {
        m_Session.RemovedFromSession -= OnSessionRemoved;
        m_Session.Deleted -= OnSessionRemoved;
        m_Session = null;
    }

    public void Dispose()
    {
        if (m_SessionObserver != null)
        {
            m_SessionObserver.SessionAdded -= OnSessionAdded;
            m_SessionObserver.Dispose();
            m_SessionObserver = null;
        }

        if (m_ServiceObserver != null)
        {
            m_ServiceObserver.Dispose();
            m_ServiceObserver = null;
        }

        if (m_Session != null)
        {
            CleanupSession();
        }
    }

    /// <summary>
    /// 값 변경 시 호출되는 이벤트
    /// </summary>
    public event Action<string> PropertyChanged;

    private void Notify(string propertyName = null)
    {
        PropertyChanged?.Invoke(propertyName);
    }
}
