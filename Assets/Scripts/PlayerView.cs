using TMPro;
using Unity.Netcode;
using UnityEngine;
using System;
using Unity.Collections;
using System.Collections;

public class PlayerView : NetworkBehaviour
{

    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private PlayerShooting _playerShooting;
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _ammoText;
    [SerializeField] private TMP_Text _respawnTimerText;

    private Coroutine _respawnCoroutine;

    public override void OnNetworkSpawn()
    {
        // Подписываемся на изменения только после сетевого спавна объекта.
        _playerNetwork.Nickname.OnValueChanged += OnNicknameChanged;
        _playerNetwork.HP.OnValueChanged += OnHpChanged;
        _playerNetwork.IsAlive.OnValueChanged += OnIsAliveChanged;

        // Подписка на патроны (если есть PlayerShooting)
        if (_playerShooting != null)
        {
            _playerShooting.OnAmmoChanged += OnAmmoChanged;
            OnAmmoChanged(_playerShooting.CurrentAmmo.Value);
        }

        // Сразу рисуем текущее состояние, чтобы UI не ждал первого сетевого события.
        OnNicknameChanged(default, _playerNetwork.Nickname.Value);
        OnHpChanged(0, _playerNetwork.HP.Value);

        // Скрыть таймер респавна при спавне
        if (_respawnTimerText != null)
            _respawnTimerText.gameObject.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        // Отписка обязательна, чтобы не оставлять "висячие" обработчики.
        _playerNetwork.Nickname.OnValueChanged -= OnNicknameChanged;
        _playerNetwork.HP.OnValueChanged -= OnHpChanged;
        _playerNetwork.IsAlive.OnValueChanged -= OnIsAliveChanged;

        if (_playerShooting != null)
            _playerShooting.OnAmmoChanged -= OnAmmoChanged;

    }

    private void OnNicknameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        _nicknameText.text = newValue.ToString();
    }

    private void OnHpChanged(int oldValue, int newValue)
    {
        _hpText.text = $"HP: {newValue}";
    }

    private void OnAmmoChanged(int newAmmo)
    {
        _ammoText.text = $"Ammo: {newAmmo}";
    }

    private void OnIsAliveChanged(bool previous, bool isAlive)
    {
        // Показываем таймер респавна только для локального игрока, если он мёртв
        if (IsOwner && !isAlive)
        {
            StartRespawnTimer();
        }
        else if (IsOwner && isAlive)
        {
            StopRespawnTimer();
        }
    }

    private void StartRespawnTimer()
    {
        if (_respawnCoroutine != null)
            StopCoroutine(_respawnCoroutine);
        _respawnCoroutine = StartCoroutine(RespawnTimerCoroutine());
    }

    private void StopRespawnTimer()
    {
        if (_respawnCoroutine != null)
        {
            StopCoroutine(_respawnCoroutine);
            _respawnCoroutine = null;
        }
        if (_respawnTimerText != null)
            _respawnTimerText.gameObject.SetActive(false);
    }

    private IEnumerator RespawnTimerCoroutine()
    {
        if (_respawnTimerText != null)
        {
            _respawnTimerText.gameObject.SetActive(true);
            float timer = 3f; // длительность респавна (должна совпадать с серверной)
            while (timer > 0f)
            {
                _respawnTimerText.text = $"Respawn in {timer:F1}";
                timer -= Time.deltaTime;
                yield return null;
            }
            _respawnTimerText.text = "Respawning...";
        }
        // Ждём фактического респавна (сервер установит IsAlive = true)
        // Таймер скроется в OnIsAliveChanged при isAlive = true
    }



}