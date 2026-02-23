using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Multiplayer;
using Unity.Netcode;

public class BackButtonUI : MonoBehaviour
{
    public GameObject currentHUD;
    public GameObject prevHUD;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnBack);
    }

    private async void OnBack()
    {
        currentHUD.SetActive(false);
        prevHUD.SetActive(true);
    }
}