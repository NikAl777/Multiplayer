using FishNet.Object;
using FishNet.Connection;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _attackRange = 3f;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private KeyCode _attackKey = KeyCode.Mouse0;

    [Header("References")]
    [SerializeField] private PlayerNetwork _playerNetwork;
    private Camera _playerCamera;
    private float _lastAttackTime;
    [SerializeField] private float _attackCooldown = 0.5f;

    private void Update()
    {
        if (!base.Owner.IsLocalClient || !_playerNetwork.IsAlive.Value) return;

        if (Input.GetKeyDown(_attackKey) && Time.time - _lastAttackTime >= _attackCooldown)
        {
            TryAttack();
            _lastAttackTime = Time.time;
        }
    }

    public void TryAttack()
    {
        if (!IsOwner || _playerNetwork == null) return;
        if (FindTarget(out PlayerNetwork target))
            DealDamage(target.NetworkObject, _damage);
    }

    private bool FindTarget(out PlayerNetwork target)
    {
        target = null;
        if (_playerCamera == null)
        {
            _playerCamera = Camera.main;
            if (_playerCamera == null) return false;
        }

        Ray ray = _playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, _attackRange, _targetLayer))
        {
            target = hit.collider.GetComponentInParent<PlayerNetwork>();
            return target != null && target.IsSpawned;
        }
        return false;
    }

    [ServerRpc]
    private void DealDamage(NetworkObject targetObject, int damage, NetworkConnection conn = null)
    {
        if (targetObject == null) return;

        PlayerNetwork targetPlayer = targetObject.GetComponent<PlayerNetwork>();
        if (targetPlayer == null || targetPlayer == _playerNetwork) return;

        float distance = Vector3.Distance(transform.position, targetObject.transform.position);
        if (distance > _attackRange) return;

        int newHp = Mathf.Max(0, targetPlayer.HP.Value - damage);
        targetPlayer.HP.Value = newHp;
        Debug.Log($"{gameObject.name} dealt {damage} to {targetPlayer.gameObject.name}. HP = {newHp}");
    }
}