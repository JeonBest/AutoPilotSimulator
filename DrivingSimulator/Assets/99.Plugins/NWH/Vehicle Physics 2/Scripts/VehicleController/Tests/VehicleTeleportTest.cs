using UnityEngine;

namespace NWH.VehiclePhysics2.Tests
{
    public class VehicleTeleportTest : MonoBehaviour
    {
        public VehicleController targetVehicle;

        private Vector3 _initPos;
        private float   _timer;


        private void Awake()
        {
            _initPos = targetVehicle.transform.position;
        }


        private void Update()
        {
            _timer += Time.deltaTime;

            if (_timer > 5f)
            {
                targetVehicle.transform.position        = _initPos;
                targetVehicle.vehicleRigidbody.velocity = Vector3.zero;
                _timer                                  = 0;
            }
        }
    }
}