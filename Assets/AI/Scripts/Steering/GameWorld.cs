using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

public class GameWorld : MonoBehaviour
{
    public Transform[] ObstaclesByHide; // 可以用来躲避的物体
    public PathCreator pathCreator; // 路径
    public Transform TargetPicker { get; private set; }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SeekTarget(Transform target)
    {
        TargetPicker = target;
    }
}
