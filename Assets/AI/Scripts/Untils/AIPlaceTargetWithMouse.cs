using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIPlaceTargetWithMouse : MonoBehaviour
{

    public float targetOffset = 0.5f;
    public GameObject setTargetOn;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!Input.GetButtonDown("Fire1"))
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 500, LayerMask.GetMask("Ground", "Env_Rock", "Env_Tree")))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 10);
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                //Debug.Log(hit.point+hit.collider.name);
                transform.position = hit.point + targetOffset * hit.normal;

                if (setTargetOn != null)
                {
                    setTargetOn.SendMessage("SeekTarget", transform);
                }
            }

        }
    }
  
}
