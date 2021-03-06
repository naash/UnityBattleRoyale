﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Explosion : NetworkBehaviour {
    public void Explode(float range, float damage) {
        transform.GetChild(0).localScale = Vector3.one * range * 2;

        if (isServer)
        {
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, range, transform.up);
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.GetComponent<IDamageable>() != null)
                {
                    hit.transform.GetComponent<IDamageable>().Damage(damage);
                }
                if (hit.transform.GetComponentInParent<IDamageable>() != null)
                {
                    hit.transform.GetComponentInParent<IDamageable>().Damage(damage);
                }
            }

            Destroy(gameObject, 1.5f);
        }
    }
}
