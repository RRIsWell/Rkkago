using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Properties;
using Unity.Services.Multiplayer;
using UnityEngine.UIElements;

public class CreateSessionVM : IDisposable
{
    SessionObserver m_SessionObserver;
    ISession m_Session;
    
    public bool CanRegisterSession
    {
        get => m_CanRegisterSession;
        private set
        {
            if (m_CanRegisterSession == value)
                return;

            m_CanRegisterSession = value;
            Notify();
        }
    }
    bool m_CanRegisterSession = true;
    
    /// <summary>
    /// Session 이름이 한 글자라도 입력되었는지
    /// </summary>
    public bool HasSessionName
    {
        get => m_HasSessionName;
        private set
        {
            if (m_HasSessionName == value)
                return;

            m_HasSessionName = value;
            Notify();
        }
    }
    bool m_HasSessionName;
    
    /// <summary>
    /// Session 이름
    /// </summary>
    public string SessionName
    {
        get => m_SessionName;
        private set
        {
            if (m_SessionName == value)
                return;

            m_SessionName = value;

            HasSessionName = m_SessionName != "";
            Notify();
        }
    }
    string m_SessionName;
    
    /// <summary>
    /// 초대 코드(세션 코드)
    /// </summary>
    public string SessionCode
    {
        get => m_SessionCode;
        private set
        {
            if (m_SessionCode == value)
                return;

            m_SessionCode = value;
            Notify();
        }
    }
    string m_SessionCode;

    public CreateSessionVM(string sessionType)
    {
        m_SessionObserver = new SessionObserver(sessionType);

        m_SessionObserver.AddingSessionStarted += OnAddingSessionStarted;
        m_SessionObserver.SessionAdded += OnSessionAdded;
        m_SessionObserver.AddingSessionFailed += OnAddingSessionFailed;

        if (m_SessionObserver.Session != null)
        {
            OnSessionAdded(m_SessionObserver.Session);
        }
    }
    void OnAddingSessionFailed(AddingSessionOptions session, SessionException exception) => CanRegisterSession = true;
    void OnAddingSessionStarted(AddingSessionOptions session) => CanRegisterSession = false;

    public void SetSessionName(string newName)
    {
        if (SessionName == newName)
            return;

        SessionName = newName;
        Notify();
    }
    
    void OnSessionAdded(ISession session)
    {
        m_Session = session;
        m_Session.RemovedFromSession += OnSessionRemoved;
        m_Session.Deleted += OnSessionRemoved;
        CanRegisterSession = false;
    }

    void OnSessionRemoved()
    {
        m_Session.RemovedFromSession -= OnSessionRemoved;
        m_Session.Deleted -= OnSessionRemoved;
        m_Session = null;
        CanRegisterSession = true;
    }

    public bool AreMultiplayerServicesInitialized()
    {
        return MultiplayerService.Instance != null;
    }

    /// <summary>
    /// 세션 생성
    /// </summary>
    /// <param name="sessionOptions"></param>
    /// <returns></returns>
    public async Task<IHostSession> CreateSessionAsync(SessionOptions sessionOptions)
    {
        sessionOptions.Name = SessionName;
        IHostSession session = await MultiplayerService.Instance.CreateSessionAsync(sessionOptions);
        SessionCode = session.Code;

        return session;
    }

    public void Dispose()
    {
        if (m_SessionObserver != null)
        {
            m_SessionObserver.AddingSessionStarted -= OnAddingSessionStarted;
            m_SessionObserver.SessionAdded -= OnSessionAdded;
            m_SessionObserver.AddingSessionFailed -= OnAddingSessionFailed;
            m_SessionObserver.Dispose();
            m_SessionObserver = null;
        }
        if (m_Session != null)
        {
            m_Session.RemovedFromSession -= OnSessionRemoved;
            m_Session.Deleted -= OnSessionRemoved;
            m_Session = null;
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

