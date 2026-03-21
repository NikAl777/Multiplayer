using Unity.Netcode;
using UnityEngine;
public class NetworkTest : MonoBehaviour
{

    // Update is called once per frame
   private void Update()
    {
      if(Input.GetKeyDown(KeyCode.H))
      {
         NetworkManager.Singleton.StartHost();
         Debug.Log("Start Host");
      }

      if(Input.GetKeyDown(KeyCode.C))
      {
          NetworkManager.Singleton.StartClient();
          Debug.Log("Start Client");
      } 

      if(Input.GetKeyDown(KeyCode.S))
      {
          NetworkManager.Singleton.Shutdown();
          Debug.Log("Shutdown");
      }
    }
}
