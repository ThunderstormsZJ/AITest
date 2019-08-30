using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 根据BoxCollider 创建包围盒检测
/// </summary>
public class AIColliderCheck : MonoBehaviour
{
    public float BoxExtendLength = 2;

    BoxCollider boxCollider;
    Bounds bounds = new Bounds();

    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    void UpdateBounds()
    {
        if (boxCollider != null)
        {
            bounds.center = boxCollider.bounds.center;
            bounds.size = Vector3.Scale(boxCollider.size, boxCollider.transform.lossyScale);

            Vector3 boxLenVec = new Vector3(0, 0, BoxExtendLength);
            bounds.size += boxLenVec;
            //bounds.center += boxLenVec;
        }
    }

    void Update()
    {
        UpdateBounds();

        // 构造一个包围盒
        if (bounds != null)
        {
            Collider[] colliders = Physics.OverlapBox(bounds.center, bounds.extents, transform.rotation, LayerMask.GetMask("Env_Tree"));
            foreach (Collider item in colliders)
            {
                Debug.Log(item.name);
            }
        }

    }

    private void OnDrawGizmos()
    {
        if (bounds != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.matrix = Matrix4x4.TRS(bounds.center, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, bounds.size);
        }
    }
}
