using TMPro;
using UnityEngine;
using FishNet;

public class GameLoopUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _inGamePanel;
    [SerializeField] private GameObject _resultsPanel;

    [Header("Lobby UI Elements")]
    [SerializeField] private TMP_Text _lobbyStatusText;

    [Header("In-Game UI Elements")]
    [SerializeField] private TMP_Text _timerText;

    [Header("Results UI Elements")]
    [SerializeField] private TMP_Text _resultsText;

    private void Update()
    {
        // Если GameManager ещё не появился на сцене, ничего не делаем
        if (GameManager.Instance == null) return;

        // ХИТРАЯ ПРОВЕРКА: Если клиент не запущен И сервер не запущен (мы еще в меню)
        if (!InstanceFinder.ClientManager.Started && !InstanceFinder.ServerManager.Started)
        {
            // Выключаем все игровые панели, чтобы они не мешали ConnectionUI
            _lobbyPanel.SetActive(false);
            _inGamePanel.SetActive(false);
            _resultsPanel.SetActive(false);
            return; // Выходим из метода, не доходя до логики GameManager
        }

        GameManager.GameState state = GameManager.Instance.CurrentState.Value;

        // Включаем нужную панель в зависимости от состояния игры
        _lobbyPanel.SetActive(state == GameManager.GameState.WaitingForPlayers);
        _inGamePanel.SetActive(state == GameManager.GameState.InProgress);
        _resultsPanel.SetActive(state == GameManager.GameState.ShowingResults);

        // Обновляем текстовые данные
        if (state == GameManager.GameState.WaitingForPlayers)
        {
            _lobbyStatusText.text = $"Waiting for players: {GameManager.Instance.ConnectedPlayers.Value} / 2";
        }
        else if (state == GameManager.GameState.InProgress)
        {
            _timerText.text = $"Time Left: {Mathf.CeilToInt(GameManager.Instance.MatchTimer.Value)}s";
        }
        else if (state == GameManager.GameState.ShowingResults)
        {
            // Собираем таблицу лидеров со всех игроков на сцене
            string resultsStr = "MATCH OVER\n\nFINAL SCORES:\n";
            PlayerNetwork[] allPlayers = FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);

            foreach (var p in allPlayers)
            {
                resultsStr += $"{p.Nickname.Value}: {p.Score.Value} Frags\n";
            }
            _resultsText.text = resultsStr;
        }
    }
}