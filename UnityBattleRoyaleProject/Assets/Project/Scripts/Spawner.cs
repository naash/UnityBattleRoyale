using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Spawner : NetworkBehaviour {

    [SerializeField] private GameObject prefab;

	void Start () {
        GameObject instance = Instantiate(prefab, transform.position, transform.rotation, transform.parent);

        NetworkServer.Spawn(instance);

        Destroy(gameObject);
	}
}
