using System;
using UnityEngine;

public class RigPositioner : MonoBehaviour
{
    [NonSerialized]
    public float x = 0, y = 100, z = 0;

    public Vector3 pos
    {
        get => new(x, y, z);
        set
        {
            x = value.x;
            y = value.y;
            z = value.z;
        }
    }

    void Update()
    {
        transform.position = new(x, y, z);
    }
}