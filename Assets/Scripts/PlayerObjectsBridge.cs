using UnityEngine;
using Unity.Netcode;

public class PlayerObjectsBridge : MonoBehaviour
{
    
    public GameObjectStorage Storage {get; set;}
    public GameObject mainCameraTemp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if(NetworkManager.Singleton.LocalClient!=null){
            if(NetworkManager.Singleton.LocalClient.PlayerObject!=null){
                Storage=NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<GameObjectStorage>();
                mainCameraTemp.SetActive(false);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Storage == null&&NetworkManager.Singleton.LocalClient!=null)
        {
            if(NetworkManager.Singleton.LocalClient.PlayerObject!=null){
                Storage=NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<GameObjectStorage>();
                mainCameraTemp.SetActive(false);
                Debug.Log("Found playerobject");
            }
        }
    }
}
