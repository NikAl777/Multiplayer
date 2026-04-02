using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _clientButton;
    [SerializeField] private TextMeshProUGUI _statusText;

    public static string PlayerNickname { get; private set; } = "Player";

    private void OnEnable()
    {
        // Подписка на кнопки
        if (_hostButton != null)
            _hostButton.onClick.AddListener(StartAsHost);
        if (_clientButton != null)
            _clientButton.onClick.AddListener(StartAsClient);

        // Подписка на события NetworkManager
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
            NetworkManager.Singleton.OnClientStopped += OnClientStopped;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }
    }

    private void OnDisable()
    {
        // Отписка от кнопок
        if (_hostButton != null)
            _hostButton.onClick.RemoveListener(StartAsHost);
        if (_clientButton != null)
            _clientButton.onClick.RemoveListener(StartAsClient);

        // Отписка от событий NetworkManager
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }

    public void StartAsHost()
    {
        SaveNickname();
        if (NetworkManager.Singleton == null)
        {
            _statusText.text = "NetworkManager не найден";
            return;
        }

        if (NetworkManager.Singleton.StartHost())
            _statusText.text = "Хост запущен";
        else
            _statusText.text = "Ошибка запуска хоста";
    }

    public void StartAsClient()
    {
        SaveNickname();
        if (NetworkManager.Singleton == null)
        {
            _statusText.text = "NetworkManager не найден";
            return;
        }

        if (NetworkManager.Singleton.StartClient())
            _statusText.text = "Подключение к хосту...";
        else
            _statusText.text = "Ошибка запуска клиента";
    }

    private void SaveNickname()
    {
        string rawValue = _nicknameInput != null ? _nicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
    }

    private void OnClientStarted() => _statusText.text = "Клиент подключён";
    private void OnClientStopped(bool _) => _statusText.text = "Клиент отключён";
    private void OnServerStarted() => _statusText.text = "Сервер запущен";
}