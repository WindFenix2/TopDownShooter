using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionObject_CarDeliveryZone : MonoBehaviour
{


    private void OnTriggerEnter(Collider other)
    {
        Car car = other.GetComponent<Car>();

        if(car != null)
            car.GetComponent<MissionObject_CarToDeliver>().InvokeOnCarDelivery();
    }
}
