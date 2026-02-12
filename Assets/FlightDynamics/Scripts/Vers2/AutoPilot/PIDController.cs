namespace FlightDynamics.Vers2
{
    [System.Serializable]
    public class PIDController
    {
        public float pGain = 0.8f;
        public float iGain = 0.1f;
        public float dGain = 0.2f;

        private float _integral;
        private float _lastError;

        public float Update(float error, float dt)
        {
            float pTerm = error * pGain;

            _integral += error * dt;
            float iTerm = _integral * iGain;

            float dTerm = ((error - _lastError) / dt) * dGain;
            _lastError = error;

            return pTerm + iTerm + dTerm;
        }
        public void Reset()
        {
            _integral = 0;
            _lastError = 0;
        }
    }
}


