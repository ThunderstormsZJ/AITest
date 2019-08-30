using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 手动操作
public class AIThirdPersonUserController : MonoBehaviour
{
    public float vDire;
    public float hDire;
    public Vector3 moveVel;

    void Start()
    {
        
    }

    void Update()
    {
       vDire = Input.GetAxis("Vertical");
       hDire = Input.GetAxis("Horizontal");

        Vector2 mapVec = MapSquareToDisc(new Vector2(vDire, hDire));
        moveVel = mapVec.x * Vector3.forward + mapVec.y * Vector3.right;
    }

    protected Vector2 MapSquareToDisc(Vector2 vec)
    {
        Vector2 discVec = Vector2.zero;

        discVec.x = vec.x * Mathf.Sqrt(1 - Mathf.Pow(vec.y, 2) / 2);
        discVec.y = vec.y * Mathf.Sqrt(1 - Mathf.Pow(vec.x, 2) / 2);

        return discVec;
    }
}
