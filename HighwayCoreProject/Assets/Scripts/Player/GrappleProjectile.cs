using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleProjectile : MonoBehaviour
{
    public float speed, maxDistance, approachRate, castRadius;
    public LayerMask hitMask;

    IProjectileSpawner spawner;
    TransformPoint grapplePoint;
    Vector3 offset, velocity, projPos, grapplePos;
    float distance;
    bool isFiring, retracting;

    public void Fire(Vector3 direction, Vector3 spawnPoint, IProjectileSpawner spawnr)
    {
        if(isFiring)
            return;
        isFiring = true;
        offset = transform.position - spawnPoint;
        velocity = direction * speed;
        projPos = spawnPoint;
        distance = 0f;
        spawner = spawnr;
        gameObject.SetActive(true);
    }
    public void Retract()
    {
        if(!enabled)
            return;
        
        isFiring = false;
        retracting = true;
    }
    public void Disable()
    {
        isFiring = false;
        retracting = false;
        grapplePoint.transform = null;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if(retracting)
        {
            grapplePos = Vector3.MoveTowards(grapplePos, transform.position, speed * Time.deltaTime);
            if(grapplePos == transform.position)
                Disable();
            return;
        }
        if(isFiring)
        {
            RaycastHit hit;
            if(!Physics.SphereCast(projPos, castRadius, velocity, out hit, velocity.magnitude * Time.deltaTime, hitMask))
            {
                projPos += velocity * Time.deltaTime;
                offset *= approachRate * Time.deltaTime;
                grapplePos = projPos + offset;
                distance += speed * Time.deltaTime;
                if(distance > maxDistance)
                {
                    Retract();
                    spawner.OnTargetNotFound();
                }
                return;
            }

            isFiring = false;
            grapplePoint = new TransformPoint(hit.transform, hit.point - hit.transform.position);
            spawner.SetHit(hit);
        }
        if(grapplePoint.transform == null)
        {
            Retract();
            return;
        }
        grapplePos = Vector3.MoveTowards(grapplePos, grapplePoint.worldPoint, speed * Time.deltaTime);
    }
}

public interface IProjectileSpawner
{
    void SetHit(RaycastHit hit);
    void OnTargetNotFound();
}