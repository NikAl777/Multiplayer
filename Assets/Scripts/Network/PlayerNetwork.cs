using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;

using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private GameObject _visualModel;
    [SerializeField] private Transform[] _spawnPoints;

    public readonly SyncVar<string> Nickname = new SyncVar<string>(string.Empty);
    public readonly SyncVar<int> HP = new SyncVar<int>(100);
    public readonly SyncVar<bool> IsAlive = new SyncVar<bool>(true);
    public readonly SyncVar<int> Score = new SyncVar<int>(0);

    public event Action<int> OnScoreChangedEvent;
    public event Action<string> OnNicknameChangedEvent;
    public event Action<int> OnHPChangedEvent;

    private bool _isRespawning;

    private void OnNicknameChanged(string oldVal, string newVal, bool asServer)
    {
        OnNicknameChangedEvent?.Invoke(newVal);
        if (Application.isEditor && !string.IsNullOrEmpty(newVal))
            gameObject.name = $"[{newVal}] Player";
    }

    private void OnHPChanged(int oldVal, int newVal, bool asServer)
    {
        OnHPChangedEvent?.Invoke(newVal);

        /*if (!base.IsServerInitialized)
            return;

        if (newVal <= 0 && IsAlive.Value)
        {
            IsAlive.Value = false;
            HP.Value = 0; // гарантируем 0
        }*/
    }

    private void OnIsAliveChanged(bool oldVal, bool newVal, bool asServer)
    {
        // Визуализация
        if (_visualModel != null)
            _visualModel.SetActive(newVal);
        else
        {
            foreach (var rend in GetComponentsInChildren<MeshRenderer>())
                rend.enabled = newVal;
        }

        // Запуск респауна только на сервере при переходе в false, однократно
        if (base.IsServerInitialized && !newVal && !_isRespawning)
        {
            _isRespawning = true;
            StartCoroutine(RespawnRoutine());
        }

    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Хост: локальный клиент и сервер в одном процессе — сразу ставим ник с экрана меню.
        // ServerRpc с клиента хоста иногда приходит позже или не в том порядке; SyncVar должен задаваться на сервере.
        if (Owner != null && Owner.IsLocalClient)
            ApplyServerNickname(ConnectionUI.PlayerNickname);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Nickname.OnChange += OnNicknameChanged;
        HP.OnChange += OnHPChanged;
        IsAlive.OnChange += OnIsAliveChanged;
        Score.OnChange += OnScoreChanged;

        // Хост уже получил ник в OnStartServer; удалённым клиентам шлём ServerRpc.
        if (base.Owner != null && base.Owner.IsLocalClient && !IsHostStarted)
            StartCoroutine(DelayedNicknameRpc());
    }

    public override void OnStopNetwork()
    {
        Nickname.OnChange -= OnNicknameChanged;
        HP.OnChange -= OnHPChanged;
        IsAlive.OnChange -= OnIsAliveChanged;
        Score.OnChange -= OnScoreChanged;
        base.OnStopNetwork();
    }

    private void OnScoreChanged(int oldVal, int newVal, bool asServer)
    {
        OnScoreChangedEvent?.Invoke(newVal);
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(5f);

        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            int idx = UnityEngine.Random.Range(0, _spawnPoints.Length);
            transform.position = _spawnPoints[idx].position;
        }

        HP.Value = 100;
        var shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
            shooting.CurrentAmmo.Value = shooting._maxAmmo;

        IsAlive.Value = true;
        _isRespawning = false;
    }

    private IEnumerator DelayedNicknameRpc()
    {
        yield return new WaitForSeconds(0.1f);   // ждём, пока ConnectionUI точно проинициализирован
        if (!base.IsOwner) yield break;

        string nick = ConnectionUI.PlayerNickname;
        SetNickname(nick);
    }

    private void ApplyServerNickname(string nickname)
    {
        string trimmed = nickname == null ? string.Empty : nickname.Trim();
        string safe = string.IsNullOrWhiteSpace(trimmed)
            ? $"Player_{base.Owner?.ClientId ?? 0}"
            : trimmed.Substring(0, Mathf.Min(30, trimmed.Length));
        Nickname.Value = safe;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetNickname(string nickname)
    {
        ApplyServerNickname(nickname);
    }

    public void TakeDamage(int damage, PlayerNetwork attacker)
    {
        if (!base.IsServerInitialized || !IsAlive.Value) return;

        // Вычитаем здоровье безопасным образом
        HP.Value = Mathf.Max(0, HP.Value - damage);

        // Если здоровье кончилось — фиксируем смерть и запускаем возрождение
        if (HP.Value <= 0)
        {
            IsAlive.Value = false;
            // 1. НАЧИСЛЕНИЕ ОЧКОВ: проверяем, что убийца существует и это не самоубийство
            if (attacker != null && attacker != this)
            {
                attacker.Score.Value += 1; 
                Debug.Log($"[Server] Игрок {attacker.Nickname.Value} убил {Nickname.Value}. Счет убийцы стал: {attacker.Score.Value}");
            }
            else
            {
                Debug.Log($"[Server] Игрок {Nickname.Value} погиб, но убийца не определен (null) или это самоубийство.");
            }

            
            if (!_isRespawning)
            {
                _isRespawning = true;
                StartCoroutine(RespawnRoutine()); 
            }
        }
    }
}