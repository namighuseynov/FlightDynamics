using UnityEngine;

namespace FlightDynamics
{
    public class AeroEnvironment : MonoBehaviour
    {
        public static AeroEnvironment Instance { get; private set; }

        public DensityModel densityModel = DensityModel.ISA_Troposphere;
        public float seaLevelY = 0f;
        public float rho0 = 1.225f;
        public float expScaleHeight = 8500f;
        public Vector3 globalWind = Vector3.zero;
        public float gustAmplitude = 0f;
        public float gustFrequencyHz = 0.2f;
        public float turbulenceAmplitude = 0f;
        public float turbulenceSpatialScale = 50f;
        public float turbulenceTimeScale = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        public AirSample Sample(Vector3 worldPos, float timeSeconds)
        {
            float altitude = worldPos.y - seaLevelY;
            if (altitude < 0f) altitude = 0f;

            AirSample s = new AirSample();
            s.altitudeM = altitude;

            GetAtmosphere(altitude, out float rho, out float tempK, out float pressPa);
            s.density = rho;
            s.temperatureK = tempK;
            s.pressurePa = pressPa;

            s.wind = GetWind(worldPos, timeSeconds, altitude);
            return s;
        }

        public float GetAirDensity(float altitudeM)
        {
            GetAtmosphere(altitudeM, out float rho, out _, out _);
            return rho;
        }

        public Vector3 GetWind(Vector3 worldPos, float timeSeconds, float altitudeM)
        {
            // 1) Base wind
            Vector3 wind = globalWind;

            // 2) (gust)
            if (gustAmplitude > 0f && gustFrequencyHz > 0f)
            {
                float w = 2f * Mathf.PI * gustFrequencyHz;
                float g1 = Mathf.Sin(w * timeSeconds);
                float g2 = Mathf.Cos(w * timeSeconds * 0.73f);
                float g3 = Mathf.Sin(w * timeSeconds * 1.37f);
                Vector3 gust = new Vector3(g1, 0f, g2 + 0.5f * g3).normalized * gustAmplitude;
                wind += gust;
            }

            // 3) (Perlin)
            if (turbulenceAmplitude > 0f && turbulenceSpatialScale > 0.001f)
            {
                float inv = 1f / turbulenceSpatialScale;
                float t = timeSeconds * turbulenceTimeScale;

                // PerlinNoise -> [0..1], in [-1..1]
                float nx = (Mathf.PerlinNoise(worldPos.z * inv + 17.1f, t + 3.3f) * 2f - 1f);
                float ny = (Mathf.PerlinNoise(worldPos.x * inv + 9.2f, t + 8.8f) * 2f - 1f);
                float nz = (Mathf.PerlinNoise(worldPos.y * inv + 5.7f, t + 1.1f) * 2f - 1f);

                Vector3 turb = new Vector3(nx, ny * 0.3f, nz);
                wind += turb * turbulenceAmplitude;
            }

            return wind;
        }

        private void GetAtmosphere(float altitudeM, out float rho, out float tempK, out float pressPa)
        {
            // sea level standart
            const float T0 = 288.15f;     // K
            const float P0 = 101325f;     // Pa
            const float g0 = 9.80665f;    // m/s^2
            const float R = 287.05287f;  // J/(kg*K)
            const float L = -0.0065f;    // K/m (until 11 km)

            float h = altitudeM;
            if (h < 0f) h = 0f;

            switch (densityModel)
            {
                case DensityModel.Constant:
                    rho = rho0;
                    tempK = T0;
                    pressPa = P0;
                    break;

                case DensityModel.Exponential:
                    // rho = rho0 * exp(-h/H)
                    rho = rho0 * Mathf.Exp(-h / Mathf.Max(1f, expScaleHeight));
                    tempK = T0;
                    pressPa = P0;
                    break;

                default: // ISA troposphere
                         // T = T0 + L*h
                    tempK = T0 + L * Mathf.Min(h, 11000f);

                    // P = P0 * (T/T0)^(g0/(R*L))
                    float exponent = g0 / (R * L);
                    pressPa = P0 * Mathf.Pow(tempK / T0, exponent);

                    // rho = P / (R*T)
                    rho = pressPa / (R * tempK);
                    break;
            }
        }

    }

}