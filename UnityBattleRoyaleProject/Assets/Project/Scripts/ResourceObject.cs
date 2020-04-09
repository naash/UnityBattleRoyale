using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ResourceObject : NetworkBehaviour, IDamageable {

    [SerializeField] private int resourceAmount;
    [SerializeField] private float amountOfHits;
    [SerializeField] private float hitScale;
    [SerializeField] private float hitSmoothness;

    private float hits;
    private float targetScale;
    private Health health;

    public float HealthValue { get { return health.Value; } }
    public int ResourceAmount { get { return resourceAmount; } }

	// Use this for initialization
	void Start () {
        targetScale = 1;

        health = GetComponent<Health>();
        health.Value = amountOfHits;
        health.OnHealthChanged += OnHealthChanged;
	}

    // Update is called once per frame
    void Update()
    {
        transform.localScale = new Vector3(
            Mathf.Lerp(transform.localScale.x, targetScale, Time.deltaTime * hitSmoothness),
            Mathf.Lerp(transform.localScale.y, targetScale, Time.deltaTime * hitSmoothness),
            Mathf.Lerp(transform.localScale.z, targetScale, Time.deltaTime * hitSmoothness)
        );
    }

    public int Damage(float amount)
    {
        health.Damage(amount);
        if (health.Value < 0.01f) return resourceAmount;
        else return 0;
    }

    private void OnHealthChanged (float newHealth) {
        transform.localScale = Vector3.one * hitScale;

        if (newHealth < 0.01f) {
            targetScale = 0;

            if (isServer) {
                Destroy(gameObject, 1);
            }
        }
    }
}
