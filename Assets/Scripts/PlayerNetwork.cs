using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using System;
using UnityEditor.MemoryProfiler;
using System.Collections;
using TMPro;

public class PlayerNetwork : NetworkBehaviour
{
    // Сетевые переменные 
    [SerializeField] private GameObject _visualModel;

    /// Никнейм: читают все, пишет только сервер.
    /// FixedString32Bytes — сетевой-сериализуемый тип для строк.

    public readonly NetworkVariable<FixedString32Bytes> Nickname = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    
    /// Здоровье: читают все, пишет только сервер.
    
    public readonly NetworkVariable<int> HP = new(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );


    public NetworkVariable<bool> IsAlive = new(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] private Transform[] _spawnPoints;

    // События для UI


    /// Вызывается на всех клиентах при изменении никнейма.

    public event Action<string> OnNicknameChanged;

    
    /// Вызывается на всех клиентах при изменении здоровья.
    
    public event Action<int> OnHealthChanged;

    // Инициализация 
    public override void OnNetworkSpawn()
    {
        // Подписка на изменения сетевых переменных
        Nickname.OnValueChanged += OnNicknameValueChanged;
        HP.OnValueChanged += OnHealthValueChanged;
        IsAlive.OnValueChanged += OnIsAliveChanged;

        // Если объект уже заспавнен — сразу уведомляем подписчиков
        if (IsSpawned)
        {
            OnNicknameValueChanged(default, Nickname.Value);
            OnHealthValueChanged(100, HP.Value);
        }

        // Только локальный владелец отправляет свой ник на сервер
        if (IsOwner)
        {
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
        }
    }

    public override void OnNetworkDespawn()
    {
        // Отписка от событий для предотвращения утечек памяти
        Nickname.OnValueChanged -= OnNicknameValueChanged;
        HP.OnValueChanged -= OnHealthValueChanged;
        IsAlive.OnValueChanged -= OnIsAliveChanged;

    }

    //  ServerRpc: отправка ника на сервер 

    
    /// Клиент вызывает этот метод, чтобы передать свой никнейм серверу.
    /// RequireOwnership = false позволяет вызывать даже до полного спавна.
    
    [ServerRpc(RequireOwnership = false)]
    private void SubmitNicknameServerRpc(string nickname)
    {
        // ⚠️ Сервер — единственный источник истины: нормализуем входные данные
        string rawValue = nickname ?? string.Empty;
        string safeValue = string.IsNullOrWhiteSpace(rawValue)
            ? $"Player_{OwnerClientId}"
            : rawValue.Trim();

        // Ограничиваем длину (на случай, если клиент обойдёт проверку)
        if (safeValue.Length > 32)
            safeValue = safeValue.Substring(0, 32);

        // Записываем в NetworkVariable — изменение автоматически уйдёт всем клиентам
        Nickname.Value = safeValue;

        Debug.Log($"[Server] Player {OwnerClientId} registered as \"{safeValue}\"");
    }

    // Обработчики изменений 

    private void OnNicknameValueChanged(FixedString32Bytes previous, FixedString32Bytes current)
    {
        // Конвертируем FixedString32Bytes → string для удобства
        string currentString = current.ToString();
        OnNicknameChanged?.Invoke(currentString);

        // Обновляем имя объекта в редакторе для удобной отладки
        if (Application.isEditor && !string.IsNullOrEmpty(currentString))
            gameObject.name = $"[{currentString}] Player";
    }

    private void OnHealthValueChanged(int previous, int current)
    {
        if (!IsServer) return;
        OnHealthChanged?.Invoke(current);

        // Логика при смерти (срабатывает только при переходе через 0)
        if (current <= 0 && IsAlive.Value)
        {
            if (current <= 0 && IsAlive.Value)
            {
                // Прямой вызов вместо ServerRpc (мы уже на сервере)
                Debug.Log($"[Server] Player {Nickname.Value} (ID: {OwnerClientId}) defeated!");
                if (HP.Value > 0) HP.Value = 0; // страховка

                IsAlive.Value = false;
                StartCoroutine(RespawnRoutine());
            }
        }
    }


    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(3f);

        // Выбрать случайную точку респавна
        int idx = UnityEngine.Random.Range(0, _spawnPoints.Length);
        transform.position = _spawnPoints[idx].position;

        HP.Value = 100;
        // Сброс патронов (если есть компонент PlayerShooting)
        var shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
        {
            shooting.CurrentAmmo.Value = shooting._maxAmmo; // нужно передать _maxAmmo или хранить его в PlayerNetwork
        }
        IsAlive.Value = true;
    }


    private void OnIsAliveChanged(bool prev, bool next)
    {
        // Показываем/скрываем модель на всех клиентах
        // Студент реализует самостоятельно
        if (_visualModel != null)
        {
            _visualModel.SetActive(next);
        }
        else
        {
            // Альтернатива: найти MeshRenderer на себе или дочерних объектах
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in renderers)
            {
                rend.enabled = next;
            }
        }
    }


    //  Публичные методы для чтения 

    
    /// Безопасное получение никнейма (работает с любого клиента).
    
    public string GetNickname() => Nickname.Value.ToString();

    
    /// Безопасное получение текущего здоровья (работает с любого клиента).
    
    public int GetCurrentHealth() => HP.Value;

    
    
    
    
}