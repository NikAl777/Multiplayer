using FishNet.Object;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 20f;
    [SerializeField] private int _damage = 20;
    [SerializeField] private float _lifeTime = 3f;

    private float _spawnTime;
    private PlayerNetwork _spawner;

    public void Initialize(PlayerNetwork spawner)
    {
        _spawner = spawner;
    }
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
            // Защита от дружественного огня по самому себе
            if (_spawner != null && target == _spawner) return;

            // Вместо ручного изменения HP, вызываем единый метод на сервере
            target.TakeDamage(_damage,_spawner);
        }

        base.ServerManager.Despawn(gameObject);
    }
}