using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] private float health;
    [SerializeField] private float hitSmoothness;
    [SerializeField] protected float damage;

    protected Rigidbody enemyRigidbody;
    private float targetScale = 1f;

	void Awake()
	{
        enemyRigidbody = transform.GetComponent<Rigidbody>();
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
        if (health > 0)
        {
            transform.localScale = Vector3.one * 0.9f;
        }
        health -= amount;

        if (health <= 0)
        {
            targetScale = 0;
            Destroy(gameObject, 1f);
        }
        return 0;
    }
}
