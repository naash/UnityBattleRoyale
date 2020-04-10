using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class StormManager : NetworkBehaviour
{
    public delegate void ShrinkHandler();
    public event ShrinkHandler OnShrink;

    [SerializeField] private float[] shrinkTimes;
    [SerializeField] private float[] distancesFromCenter;
    [SerializeField] private GameObject[] stormObjects;
    private float timer = 0;
    private int stormIndex = -1;

    private bool shouldShrink = false;
    
    public bool ShouldShrink
    {
        set
        {
            shouldShrink = value;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!isServer) return;

        if(!shouldShrink)
        {
            return;
        }

        timer += Time.deltaTime;

        for(int i =0; i < shrinkTimes.Length; i++)
        {
            float currentShrinkTime = shrinkTimes[i];

            if(timer > currentShrinkTime && stormIndex < i)
            {
                //Stomr area will shrink
                stormIndex = i;

                float targetDistance = distancesFromCenter[i];

                foreach(GameObject stormObj in stormObjects)
                {
                    stormObj.GetComponent<StormObject>().MoveToDistance(targetDistance);
                }

                //Alert
                OnShrink?.Invoke();
            }
        }
    }
}
