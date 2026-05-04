using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System;
using System.Collections;
using FishNet.Connection;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private GameObject _visualModel;
    [SerializeField] private Transform[] _spawnPoints;

    public readonly SyncVar<string> Nickname = new SyncVar<string>(string.Empty);
    public readonly SyncVar<int> HP = new SyncVar<int>(100);
    public readonly SyncVar<bool> IsAlive = new SyncVar<bool>(true);

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

        if (!base.IsServerInitialized)
            return;

        if (newVal <= 0 && IsAlive.Value)
        {
            IsAlive.Value = false;
            HP.Value = 0; // гарантируем 0
        }
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

    public override void OnStartClient()
    {
        base.OnStarClient();
        Nickname.OnChange += OnNicknameChanged;
        HP.OnChange += OnHPChanged;
        IsAlive.OnChange += OnIsAliveChanged;

        if (base.Owner.IsLocalClient && base.Owner!=null)
            StartCoroutine(DelayedNicknameRpc());
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

    [ServerRpc(RequireOwnership = false)]
    public void SetNickname(string nickname)
    {
        string safe = string.IsNullOrWhiteSpace(nickname)
            ? $"Player_{base.Owner?.ClientId ?? 0}"
            : nickname.Trim().Substring(0, Mathf.Min(30, nickname.Length));
        Nickname.Value = safe;
    }


}