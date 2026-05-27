using FishNet.Object;
using UnityEngine;
using System.Collections;

public class PickupManager : NetworkBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 10f;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (_healthPickupPrefab == null || _spawnPoints == null) return;
        foreach (Transform point in _spawnPoints)
        {
            if (point != null)
                SpawnPickup(point.position);
        }
    }

    public void OnPickedUp(Vector3 position) => StartCoroutine(RespawnAfterDelay(position));

    private IEnumerator RespawnAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(_respawnDelay);
        SpawnPickup(position);
    }

    private void SpawnPickup(Vector3 position)
    {
        GameObject go = Instantiate(_healthPickupPrefab, position, Quaternion.identity);
        if (go.TryGetComponent(out HealthPickup pickup))
            pickup.Init(this);
        ServerManager.Spawn(go);
    }
}