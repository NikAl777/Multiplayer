using FishNet;
using UnityEngine;

public class NetworkTest : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            InstanceFinder.ServerManager.StartConnection();
            Debug.Log("Start Host");
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            InstanceFinder.ClientManager.StartConnection();
            Debug.Log("Start Client");
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if (InstanceFinder.IsServerStarted)
                InstanceFinder.ServerManager.StopConnection(true);
            else if (InstanceFinder.IsClientStarted)
                InstanceFinder.ClientManager.StopConnection();
            Debug.Log("Shutdown");
        }
    }
}