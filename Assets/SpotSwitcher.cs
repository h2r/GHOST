using System.Collections;
using System.Collections.Generic;
//using UnityEngine;

//public class SpotSwitcher : MonoBehaviour
//{
//    // Start is called before the first frame update
//    void Start()
//    {

//    }

//    // Update is called once per frame
//    void Update()
//    {

//    }
//}
using UnityEngine;

public class SpotSwitcher : MonoBehaviour
{
    public GameObject spot;
    public GameObject spot2;
    /*public GameObject ros;
    public GameObject ros2;
    public GameObject con;
    public GameObject con2;*/

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwitchSpots();
        }
    }

    private void SwitchSpots()
    {
        if (spot != null && spot2 != null)
        {
            bool isSpotActive = spot.activeSelf;
            spot.SetActive(!isSpotActive);
            spot2.SetActive(isSpotActive);
           /* ros.SetActive(!isSpotActive);
            ros2.SetActive(isSpotActive);
            con.SetActive(!isSpotActive);
            con2.SetActive(isSpotActive);*/
        }
    }
}