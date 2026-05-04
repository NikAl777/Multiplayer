using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _cooldown = 0.4f;
    [SerializeField] public int _maxAmmo = 10;

    private float _lastShotTime;
    private PlayerNetwork _playerNetwork;

    public readonly SyncVar<int> CurrentAmmo = new SyncVar<int>(10);
    public event Action<int> OnAmmoChangedEvent;

    private void OnAmmoChanged(int oldVal, int newVal, bool asServer)
    {
        OnAmmoChangedEvent?.Invoke(newVal);
    }

    public override void OnStarClient()
    {
        _playerNetwork = GetComponent<PlayerNetwork>();

        if (base.IsServerInitialized)
            CurrentAmmo.Value = _maxAmmo;

        CurrentAmmo.OnChange += OnAmmoChanged;
    }

    public override void OnStopNetwork()
    {
        CurrentAmmo.OnChange -= OnAmmoChanged;
    }

    private void Update()
    {
        if (!base.Owner.IsLocalClient || !_playerNetwork.IsAlive.Value) return;
        if (Input.GetKeyDown(KeyCode.Space))
            Shoot(_firePoint.position, _firePoint.forward);
    }

    [ServerRpc]
    private void Shoot(Vector3 pos, Vector3 dir, NetworkConnection conn = null)
    {
        if (!_playerNetwork.IsAlive.Value || CurrentAmmo.Value <= 0) return;
        if (Time.time < _lastShotTime + _cooldown) return;

        _lastShotTime = Time.time;
        CurrentAmmo.Value--;

        GameObject go = Instantiate(_projectilePrefab, pos + dir * 1.2f, Quaternion.LookRotation(dir));
        base.ServerManager.Spawn(go, conn);
    }
}