using FishNet.Managing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _clientButton;
    [SerializeField] private GameObject _uiPanel;
    [SerializeField] private NetworkManager _networkManager;

    public static string PlayerNickname { get; private set; } = "Player";

    private void Awake()
    {
        _hostButton.onClick.AddListener(() => StartConnection(true));
        _clientButton.onClick.AddListener(() => StartConnection(false));
    }

    public void StartConnection(bool asHost)
    {
        SaveNickname();

        if (asHost)
        {
            _networkManager.ServerManager.StartConnection();
            _networkManager.ClientManager.StartConnection();
        }
        else
        {
            _networkManager.ClientManager.StartConnection();
        }

        _uiPanel.SetActive(false);
    }

    public void SaveNickname()
    {
        string raw = _nicknameInput != null ? _nicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(raw) ? "Player" : raw.Trim();
    }
}