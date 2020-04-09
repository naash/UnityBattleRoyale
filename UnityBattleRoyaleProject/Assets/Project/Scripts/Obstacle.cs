using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Obstacle : NetworkBehaviour, IDamageable {

    [SerializeField] private float initialHealth;
    [SerializeField] private int cost;
    [SerializeField] private float hitSmoothness;

    private Renderer obstacleRenderer;
    private int targetScale = 1;

    private Health health;

    public int Cost {
        get {
            return cost;
        }
    }

    private Collider obstacleCollider;

	// Use this for initialization
	void Awake () {
        obstacleCollider = GetComponentInChildren<Collider>();
        obstacleRenderer = GetComponentInChildren<Renderer>();

        health = GetComponent<Health>();
        health.Value = initialHealth;
        health.OnHealthChanged += OnHealthChanged;
	}
	
	// Update is called once per frame
	void Update () {
        transform.localScale = new Vector3(
            Mathf.Lerp(transform.localScale.x, targetScale, hitSmoothness * Time.deltaTime),
            Mathf.Lerp(transform.localScale.y, targetScale, hitSmoothness * Time.deltaTime),
            Mathf.Lerp(transform.localScale.z, targetScale, hitSmoothness * Time.deltaTime)
        );
	}

    public void SetPositioningMode () {
        // Start with the obstacle collider disabled.
        obstacleCollider.enabled = false;

        // Make the obstacle transparent.
        obstacleRenderer.material.color = new Color(
            obstacleRenderer.material.color.r,
            obstacleRenderer.material.color.g,
            obstacleRenderer.material.color.b,
            0.5f
        );
    }

    public void Place () {
        // Enable the collider.
        obstacleCollider.enabled = true;

        // Make the obstacle opaque.
        obstacleRenderer.material.color = new Color(
            obstacleRenderer.material.color.r,
            obstacleRenderer.material.color.g,
            obstacleRenderer.material.color.b,
            1.0f
        );
    }

    public int Damage(float amount)
    {
        health.Damage(amount);
        return 0;
    }

    private void OnHealthChanged (float newHealth) {
        transform.localScale = Vector3.one * 0.8f;

        if (newHealth < 0.01f) {
            targetScale = 0;

            if (isServer) {
                Destroy(gameObject, 1.0f);
            }
        }
    }
}
