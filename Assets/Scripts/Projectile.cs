using FishNet.Object;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 20f;
    [SerializeField] private int _damage = 20;
    [SerializeField] private float _lifeTime = 3f;

    private float _spawnTime;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _spawnTime = Time.time;
    }

    private void Update()
    {
        transform.position += transform.forward * _speed * Time.deltaTime;

        if (base.IsServerInitialized && Time.time > _spawnTime + _lifeTime)
        {
            base.ServerManager.Despawn(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServerInitialized) return;

        if (other.TryGetComponent<PlayerNetwork>(out PlayerNetwork target))
        {
            if (target.OwnerId == base.OwnerId) return;

            int newHp = Mathf.Max(0, target.HP.Value - _damage);
            target.HP.Value = newHp;
        }

        base.ServerManager.Despawn(gameObject);
    }
}