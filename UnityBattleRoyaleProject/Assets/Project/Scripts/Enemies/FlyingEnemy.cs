using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEnemy : Enemy {
    [Header("Flying")]
    [SerializeField] private float distanceFromFloor;
    [SerializeField] private float hoverSmoothness;
    [SerializeField] private float bounceAmplitude;
    [SerializeField] private float bounceSpeed;

    [Header("Chasing")]
    [SerializeField] private float chasingRange;
    [SerializeField] private float chasingSpeed;
    [SerializeField] private float chasingSmoothness;

    [Header("Attacking")]
    [SerializeField] private float attackingRange;
    [SerializeField] private float bounceBackSpeed;

    private float bounceAngle;
    private Player target;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        // Chase a player.
        Chase();
	}

	private void FixedUpdate() {
        // Make the enemy fly.
        Fly();
	}

	private void Fly () {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit))
        {
            // Get the ground position.
            Vector3 targetPosition = hit.point;

            // Move the enemy up.
            targetPosition = new Vector3(
                targetPosition.x,
                targetPosition.y + distanceFromFloor,
                targetPosition.z
            );

            // Swing the enemy.
            bounceAngle += Time.deltaTime * bounceSpeed;
            float offset = Mathf.Cos(bounceAngle) * bounceAmplitude;
            targetPosition = new Vector3(
                targetPosition.x,
                targetPosition.y + offset,
                targetPosition.z
            );

            // Move the enemy towards the target (if any).
            if (target != null && Vector3.Distance(transform.position, target.transform.position) <= attackingRange) {
                targetPosition = new Vector3(targetPosition.x, target.transform.position.y + 1, targetPosition.z);
            }

            // Apply the position.
            transform.position = new Vector3(
                Mathf.Lerp(transform.position.x, targetPosition.x, Time.deltaTime * hoverSmoothness),
                Mathf.Lerp(transform.position.y, targetPosition.y, Time.deltaTime * hoverSmoothness),
                Mathf.Lerp(transform.position.z, targetPosition.z, Time.deltaTime * hoverSmoothness)
            );
        }
    }

    private void Chase () {
        Vector3 targetVelocity = Vector3.zero;

        // Find a player.
        if (target == null) {
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, chasingRange / 2, Vector3.down);
            foreach (RaycastHit hit in hits) {
                if (hit.transform.GetComponent<Player>() != null) {
                    target = hit.transform.GetComponent<Player>();
                }
            }
        }

        // Check if target is too far away.
        if (target != null && Vector3.Distance(transform.position, target.transform.position) > chasingRange) {
            target = null;
        }

        // Chase the target (if any).
        if (target != null) {
            Vector3 direction = (target.transform.position - transform.position).normalized;

            // Remove the vertical component of the direction.
            direction = new Vector3(direction.x, 0, direction.z);
            direction.Normalize();

            // Calculate target velocity.
            targetVelocity = direction * chasingSpeed;
        }

        // Make the enemy move.
        enemyRigidbody.velocity = new Vector3(
            Mathf.Lerp(enemyRigidbody.velocity.x, targetVelocity.x, Time.deltaTime * chasingSmoothness),
            Mathf.Lerp(enemyRigidbody.velocity.y, targetVelocity.y, Time.deltaTime * chasingSmoothness),
            Mathf.Lerp(enemyRigidbody.velocity.z, targetVelocity.z, Time.deltaTime * chasingSmoothness)
        );
    }

	private void OnCollisionEnter (Collision collision)
	{
        if (collision.gameObject.GetComponent<IDamageable>() != null) {
            collision.gameObject.GetComponent<IDamageable>().Damage(damage);

            Vector3 direction = (transform.position - collision.gameObject.transform.position).normalized;
            enemyRigidbody.velocity = direction * bounceBackSpeed;
        }
	}
}
