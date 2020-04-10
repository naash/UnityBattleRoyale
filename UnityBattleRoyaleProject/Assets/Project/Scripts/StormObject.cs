using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StormObject : MonoBehaviour
{
    [SerializeField] float initialDistance;
    [SerializeField] float shrinkSmoothness = 0.01f;
    private float targetDistance;

    // Start is called before the first frame update
    void Start()
    {
        targetDistance = initialDistance;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 direction = transform.position.normalized;
        Vector3 targetPosition = direction * targetDistance;

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * shrinkSmoothness);
    }

    public void MoveToDistance(float distance)
    {
        targetDistance = distance;
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.GetComponent<Player>() != null)
        {
            other.GetComponent<Player>().StormDamage();
        }
    }
}
