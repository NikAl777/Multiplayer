using FishNet.Object;
using UnityEngine;
using System.Collections;

public class PickupManager : NetworkBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 10f;

    private void Start()
    {
        
        if (!base.IsServerStarted) return;
        foreach (var point in _spawnPoints)
            SpawnPickup(point.position);
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
        go.GetComponent<HealthPickup>().Init(this);
        ServerManager.Spawn(go);
    }
}