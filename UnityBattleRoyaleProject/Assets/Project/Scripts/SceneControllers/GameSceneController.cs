using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneController : MonoBehaviour {

    // Use this for initialization
	void Start () {
        
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnPlayerDied () {
        Invoke("ReloadScene", 3);
    }

    private void ReloadScene () {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
