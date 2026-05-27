using FishNet;
using UnityEngine;

/// <summary> Горячие клавиши для отладки сети; в релизе отключите компонент на объекте. </summary>
public class NetworkTest : MonoBehaviour
{
    private void Update()
    {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
        return;
#endif
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