using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class ConnectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private TMP_InputField _nicknameInput;

    // Сохраняем ник локально до появления сетевого объекта игрока.
    public static string PlayerNickname { get; private set; } = "Player";

    private void Awake()
    {
        // Проверяем наличие обязательных компонентов
        if (_statusText == null)
            Debug.LogWarning("[ConnectionUI] StatusText not assigned!");
    }

    private void OnEnable()
    {
        // Subscribe to network events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        // Subscribe to button clicks
        if (_hostButton != null)
            _hostButton.onClick.AddListener(OnHostConnected);
        if (_joinButton != null)
            _joinButton.onClick.AddListener(OnJoinClient);
    }

    private void OnDisable()
    {
        // Unsubscribe from network events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        // Unsubscribe from button clicks
        if (_hostButton != null)
            _hostButton.onClick.RemoveListener(OnHostConnected);
        if (_joinButton != null)
            _joinButton.onClick.RemoveListener(OnJoinClient);
    }

    // Сетевые события 

    private void OnServerStarted()
    {
        UpdateStatus("Host started");
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsClient && clientId == NetworkManager.Singleton.LocalClientId)
        {
            UpdateStatus($"Connected as \"{PlayerNickname}\"");
        }
        else if (NetworkManager.Singleton.IsServer)
        {
            UpdateStatus($"Client {clientId} connected");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            UpdateStatus("Disconnected");
        }
    }

    // Обработчики кнопок 

    private void OnHostConnected()
    {
        SaveNickname();
        NetworkManager.Singleton.StartHost();
        UpdateStatus($"Hosting as \"{PlayerNickname}\"...");
    }

    private void OnJoinClient()
    {
        SaveNickname();
        NetworkManager.Singleton.StartClient();
        UpdateStatus($"Connecting as \"{PlayerNickname}\"...");
    }

    //  Вспомогательные методы 

    private void SaveNickname()
    {
        // Нормализуем ввод, чтобы сервер не получил пустую строку.
        string rawValue = _nicknameInput != null ? _nicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
    }

    private void UpdateStatus(string message)
    {
        if (_statusText != null)
            _statusText.text = message;
        Debug.Log($"[ConnectionUI] {message}");
    }

    //  Публичные методы для внешних вызовов 

    
    // Вызывается из других скриптов для запуска хоста с сохранением никнейма
    public void StartAsHost() => OnHostConnected();

   
    // Вызывается из других скриптов для подключения к хосту с сохранением никнейма
    
    public void StartAsClient() => OnJoinClient();

    
    // Обновляет никнейм игрока вручную (если нужно изменить после инициализации)
   
    public void UpdateNickname(string newNickname)
    {
        PlayerNickname = string.IsNullOrWhiteSpace(newNickname) ? "Player" : newNickname.Trim();
        if (_nicknameInput != null)
            _nicknameInput.text = PlayerNickname;
    }
}