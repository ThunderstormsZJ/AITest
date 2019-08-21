using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WestWorld;

public class WestWorldManager : MonoBehaviour
{
    float DeltaTime = 0;
    Miner Jack;
    MinerWife Rose;

    // Start is called before the first frame update
    void Start()
    {
        Jack = new Miner((int)EntityName.Miner_Jack);
        Rose = new MinerWife((int)EntityName.Rose);

        EntityManager.Instance.RegisterEntity(Jack);
        EntityManager.Instance.RegisterEntity(Rose);

        StartCoroutine(TimeCount());
    }

    IEnumerator TimeCount()
    {
        while (true)
        {
            MessageDispatch.Instance.Start();
            yield return new WaitForSeconds(1);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (DeltaTime>1)
        {
            Jack.Update();
            Rose.Update();
            DeltaTime = 0;
        }
        DeltaTime += Time.deltaTime;
    }
}
