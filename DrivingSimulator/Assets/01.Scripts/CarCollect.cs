using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarCollect : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.transform.parent.CompareTag("Vehicle"))
            return;

        if (other.CompareTag("CollectorException"))
            return;

        Debug.Log(this.transform.name + " collected " + other.transform.parent.name + " !!");
        other.transform.parent.gameObject.SetActive(false);
    }
}
