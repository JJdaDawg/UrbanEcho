using UrbanEcho.Helpers;

namespace UrbanEcho.Models
{
    public class VehicleSettings
    {
        private float length;
        private float width;
        private float acceleration;
        private float deceleration;
        private float slowDownfactor;//number from 0 to 1 multiplied by deceleration for slowing down on turns
        private float turnSpeed;
        private float lookAheadValueForSteerTowardsLane;
        private bool validType = false;

        //Have variable for length of each car type so style can scale image correct
        public static readonly float CarLength = 5.0f;//Helper.DoMapCorrection(4.0f);

        public VehicleSettings(string carType)
        {
            if (carType == "RegularCar")
            {
                length = CarLength;
                width = 2.0f;//Helper.DoMapCorrection(2.0f);

                acceleration = 2.0f;// Helper.DoMapCorrection(0.5f * Helper.NumberOfVehicleGroups);
                deceleration = 2.0f;// Helper.DoMapCorrection(3.0f * Helper.NumberOfVehicleGroups);
                slowDownfactor = 0.25f;//number from 0 to 1 multiplied by deceleration for slowing down on turns
                turnSpeed = 4.0f;
                lookAheadValueForSteerTowardsLane = 15.0f;

                validType = true;
            }
        }

        public float GetLength()
        {
            return length;
        }

        public float GetWidth()
        {
            return width;
        }

        public float GetAcceleration()
        {
            return acceleration;
        }

        public float GetDeceleration()
        {
            return deceleration;
        }

        public float GetSlowDownfactor()
        {
            return slowDownfactor;
        }

        public float GetTurnSpeed()
        {
            return turnSpeed;
        }

        public float GetLookAheadValueForSteerTowardsLane()
        {
            return lookAheadValueForSteerTowardsLane;
        }

        public bool IsValid()
        {
            return validType;
        }

        //from google normal car 4.0m long and width 2.0m
    }
}