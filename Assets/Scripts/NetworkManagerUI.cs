using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button serverBnt;
    [SerializeField] private Button hostBnt;
    [SerializeField] private Button clientBnt;

    private void Start()
    {
        serverBnt.onClick.AddListener(() => NetworkManager.Singleton.StartServer());
        hostBnt.onClick.AddListener(() => NetworkManager.Singleton.StartHost());
        clientBnt.onClick.AddListener(() => NetworkManager.Singleton.StartClient());
    }
}
