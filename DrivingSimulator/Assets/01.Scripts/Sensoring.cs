using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sensoring : MonoBehaviour
{

    public List<Transform> hit = new List<Transform>();
    public int hitCount = 0;
    public bool isPlayerInvolved = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Vehicle") && !other.CompareTag("Player"))
            return;
        hit.Add(other.transform.parent);
        hitCount += 1;
        if (other.CompareTag("Player")) isPlayerInvolved = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Vehicle") && !other.CompareTag("Player"))
            return;
        hit.RemoveAt(hit.Count - 1);
        hitCount -= 1;
        if (other.CompareTag("Player")) isPlayerInvolved = false;
    }
}
