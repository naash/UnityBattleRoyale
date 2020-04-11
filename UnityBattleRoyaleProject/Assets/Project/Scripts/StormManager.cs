using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class StormManager : NetworkBehaviour {
    public delegate void ShrinkHandler();
    public event ShrinkHandler OnShrink;

    [SerializeField] private float[] shrinkTimes;
    [SerializeField] private float[] distancesFromCenter;
    [SerializeField] private GameObject[] stormObjects;

    private float timer = 0;
    private int stormIndex = -1;

    private bool shouldShrink;
    public bool ShouldShrink {
        set {
            shouldShrink = value;
        }
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (!isServer) return;

        if (!shouldShrink) return;

        // Update the storm timer
        timer += Time.deltaTime;
        for (int i = 0; i < shrinkTimes.Length; i++) {
            float currentShrinkTime = shrinkTimes[i];
            if (timer > currentShrinkTime && stormIndex < i) {
                // The storm area is going to shrink
                stormIndex = i;

                float targetDistance = distancesFromCenter[i];
                foreach (GameObject stormObject in stormObjects) {
                    stormObject.GetComponent<StormObject>().MoveToDistance(targetDistance);
                }

                // Alert
                if (OnShrink != null) {
                    OnShrink();
                }
            }
        }
	}
}
