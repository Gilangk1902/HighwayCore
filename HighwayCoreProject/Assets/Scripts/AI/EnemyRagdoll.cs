using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRagdoll : MonoBehaviour
{
    public Rigidbody[] rbs;
    public Transform pivot;
    public Animator anim;
    public float upTime;

    public void Init(Transform rig, Vector3 velocity)
    {
        pivot.position = rig.position;
        CopyRotation(rig, pivot);
        foreach(Rigidbody rb in rbs)
        {
            rb.angularVelocity = Vector3.zero;
            rb.velocity = velocity;
        }
        StartCoroutine(Die());
    }

    public void CopyRotation(Transform from, Transform to)
    {
        to.rotation = from.rotation;
        int count = Mathf.Min(from.childCount, to.childCount);
        for(int i = 0; i < count; i++)
        {
            CopyRotation(from.GetChild(i), to.GetChild(i));
        }
    }

    IEnumerator Die()
    {
        yield return new WaitForSeconds(upTime);
        anim.Update(0f);
        gameObject.SetActive(false);
    }

    [ContextMenu("SetRigidbodies")]
    public void SetRigidbodies()
    {
        rbs = GetComponentsInChildren<Rigidbody>();
    }
}
