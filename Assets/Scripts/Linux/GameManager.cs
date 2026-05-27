using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private int _requiredPlayers = 2;

    public readonly SyncVar<GameState> CurrentState = new SyncVar<GameState>(GameState.WaitingForPlayers);
    public readonly SyncVar<int> ConnectedPlayers = new SyncVar<int>(0);
    public readonly SyncVar<float> MatchTimer = new SyncVar<float>(60f);
    

    public static GameManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public enum GameState
    {
        WaitingForPlayers,
        InProgress,
        ShowingResults
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        ServerManager.OnRemoteConnectionState += OnPlayerConnectionChanged;
    }

    public override void OnStopServer()
    {
        if (ServerManager != null)
            ServerManager.OnRemoteConnectionState -= OnPlayerConnectionChanged;
        base.OnStopServer();
    }

    // Обработка подключений и ОТКЛЮЧЕНИЙ
    private void OnPlayerConnectionChanged(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            ConnectedPlayers.Value++;
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            ConnectedPlayers.Value--;

            // Защита: если мы в игре, а кто-то ливнул, и нас стало меньше минимума — прерываем матч
            if (CurrentState.Value == GameState.InProgress && ConnectedPlayers.Value < _requiredPlayers)
            {
                Debug.Log("[Server] Игрок отключился. Недостаточно игроков. Досрочно завершаем матч...");
                EndMatch();
            }
        }

        // При любом изменении состава проверяем, можем ли мы стартовать (если мы в лобби)
        CheckStartMatch();
    }

    private void Update()
    {
        if (!base.IsServerInitialized) return;

        // Таймер тикает ТОЛЬКО во время самой игры
        if (CurrentState.Value == GameState.InProgress)
        {
            MatchTimer.Value -= Time.deltaTime;

            if (MatchTimer.Value <= 0f)
            {
                EndMatch();
            }
        }
    }

    // Единая точка входа для старта матча
    private void CheckStartMatch()
    {
        if (CurrentState.Value == GameState.WaitingForPlayers && ConnectedPlayers.Value >= _requiredPlayers)
        {
            StartMatch();
        }
    }

    private void StartMatch()
    {
        MatchTimer.Value = 60f; // Сбрасываем время для нового матча
        CurrentState.Value = GameState.InProgress;
        Debug.Log("[Server] Match started!");
    }

    private void EndMatch()
    {
        // Защита от двойного вызова (если таймер кончился одновременно с выходом игрока)
        if (CurrentState.Value == GameState.ShowingResults) return;

        CurrentState.Value = GameState.ShowingResults;
        Debug.Log("[Server] Match ended! Showing results...");

        // Ждем 5 секунд на экране результатов, потом возвращаем всех в лобби
        Invoke(nameof(ResetToLobby), 5f);
    }

    private void ResetToLobby()
    {
        Debug.Log("[Server] Returning to lobby...");
        CurrentState.Value = GameState.WaitingForPlayers;

        // Восстанавливаем здоровье и статус оставшимся игрокам
        foreach (NetworkConnection conn in ServerManager.Clients.Values)
        {
            foreach (NetworkObject nob in conn.Objects)
            {
                PlayerNetwork pn = nob.GetComponent<PlayerNetwork>();
                if (pn != null)
                {
                    pn.HP.Value = 100;
                    pn.IsAlive.Value = true;
                    pn.Score.Value = 0; 
                }
            }
        }

        // ИСПРАВЛЕНИЕ БАГА: Пытаемся начать матч только в том случае, 
        // если после экрана результатов с нами всё еще достаточно игроков
        CheckStartMatch();
    }
}