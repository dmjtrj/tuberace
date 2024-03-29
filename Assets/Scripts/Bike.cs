using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Race
{
    /// <summary>
    /// 
    /// Model
    /// ������ ������
    /// 
    /// </summary>
    [System.Serializable]
    public class BikeParameters
    {
        [Range(0.0f, 10.0f)]
        public float mass;

        [Range(0.0f, 100.0f)]
        public float thrust;

        public float afterburnerThrust;

        [Range(0.0f, 1000.0f)]
        public float agility;
        public float maxSpeed;

        public float afterburnerMaxSpeedBonus;

        public float afterburnerCoolSpeed;
        public float afterburnerHeatGeneration; // per second
        public float afterburnerMaxHeat;

        //������������ �������� ��������
        public float maxSpeedRoll;

        [Range(0.0f, 1.0f)]
        public float linearDrag;

        [Range(0.0f, 1.0f)]
        public float collisionBounceFactor;

        public bool afterburner;

        public GameObject m_EngineModelId;
        public GameObject m_HullModel;
    }

    /// <summary>
    /// 
    /// ���������� (��������)
    /// Controller
    /// 
    /// </summary>
    public class Bike : MonoBehaviour
    {
        [SerializeField] private BikeParameters m_BikeParametersInitial;

        /// <summary>
        /// 
        /// Viev
        /// �������������
        /// 
        /// </summary>

        [SerializeField] private BikeViewController m_VisualController;
        

        /// <summary>
        ///  ���������� ����� �����. ���������������. �� -1 �� +1.
        /// </summary>
        private float m_ForwardThrustAxis;

        /// <summary>
        /// ��������� �������� ������ ����.
        /// </summary>
        /// <param name="val"></param>
        public void SetForwardThrustAxis(float val) => m_ForwardThrustAxis = val;

        /// <summary>
        ///  ���������� ����������� ����� � ������. ���������������. �� -1 �� +1.
        /// </summary>
        private float m_HorizontalThrustAxis;
        /// <summary>
        /// ��������� �������� ����������.
        /// </summary>
      
        /// <summary>
        /// ���/���� ��� ����������
        /// </summary>
        public bool EnableAfterburner { get; set; }

        public void SetHorizontalThrustAxis(float val) => m_HorizontalThrustAxis = val;

        [SerializeField] private RaceTrack m_Track;
        
        public RaceTrack GetTrack => m_Track;

        private float m_Distance;
        private float m_Velocity;
        [Range(0.0f, 360f)]
        [SerializeField] private float m_RollAngle;

        public float Distance => m_Distance;
        public float Velocity => m_Velocity;
        public float RollAngle => m_RollAngle;
        public float GetDistance()
        {
            return m_Distance;
        }

        //������� �������� ��������
        private float m_RollSpeed;

        public void Update()
        {
            UpdateAfterburnerHeat();
            BikePhysics();
        }

        private float m_AfterburnerHeat;
        
        public float GetNormalizedHeat()
        {
            if (m_BikeParametersInitial.afterburnerMaxHeat > 0)
                return m_AfterburnerHeat / m_BikeParametersInitial.afterburnerMaxHeat;

            return 0;
        }

        public void CoolAfterburner()
        {
            m_AfterburnerHeat = 0;
        }

        private void UpdateAfterburnerHeat()
        {
            // calc heat dissipation;
            m_AfterburnerHeat -= m_BikeParametersInitial.afterburnerCoolSpeed * Time.deltaTime;
            
            if (m_AfterburnerHeat < 0)
                m_AfterburnerHeat = 0;

            // Chech max heat
            // ***
        }

        private void BikePhysics()
        {
            // S=v * dt
            // F=m * a
            // V=v0 + a * dt
            float dt = Time.deltaTime;
            //float dv = dt * m_ForwardThrustAxis * m_BikeParametersInitial.thrust;

            float F_thrustMax = m_BikeParametersInitial.thrust;
            float Vmax = m_BikeParametersInitial.maxSpeed;
            float F = m_ForwardThrustAxis * m_BikeParametersInitial.thrust;

            if (EnableAfterburner && ConsumeFuelForAfterburner(1.0f * Time.deltaTime))
            {
                m_AfterburnerHeat += m_BikeParametersInitial.afterburnerHeatGeneration * Time.deltaTime;

                F += m_BikeParametersInitial.afterburnerThrust;

                Vmax += m_BikeParametersInitial.afterburnerMaxSpeedBonus;
                F_thrustMax += m_BikeParametersInitial.afterburnerThrust;
            }

            // drag
            F += -m_Velocity * (F_thrustMax / Vmax);
            
            float dv = dt * F;


            // F=ma
            // F_thrust
            // F_drag
            // F = F_thrust - F_drag
            // F_drag = -V * K_drag

            // F = F_thrust - V * K_drag
            // V * K_drag = F_thrust
            // K_drag = f_thrust / Vmax


            m_Velocity += dv;

            //m_Velocity = Mathf.Clamp(m_Velocity, -m_BikeParametersInitial.maxSpeed, m_BikeParametersInitial.maxSpeed);

            float dS = m_Velocity * dt;

            // collision 
            if (Physics.Raycast(transform.position, transform.forward, dS))
            {
                m_Velocity = -m_Velocity * m_BikeParametersInitial.collisionBounceFactor;
                dS = m_Velocity * dt;
                // ��� ������������ � ������������ � ����� ����� ��������
                m_AfterburnerHeat = m_BikeParametersInitial.afterburnerMaxHeat;
            }

            m_PrevDistance = m_Distance;
            m_Distance += dS;

            //m_Velocity += -m_Velocity * m_BikeParametersInitial.linearDrag * dt;

            m_Distance = Mathf.Clamp(m_Distance, 0, m_Track.GetTrackLength());

            // ���� ���� ��������� �������� ����� ������, �� �������� �������������� � 0
            if (m_Distance == m_Track.GetTrackLength() || m_Distance == 0)
            {
                m_Velocity = 0;
            }

            // ��������� �������� �������� � ������ ���������������� �����
            float dv_roll = dt * m_HorizontalThrustAxis * m_BikeParametersInitial.agility;
            // ��������� ������� �������� ��������
            m_RollSpeed += dv_roll;
            // ����������� ������� �������� � ������������ � ����������� ���������
            m_RollSpeed = Mathf.Clamp(m_RollSpeed, -m_BikeParametersInitial.maxSpeedRoll, m_BikeParametersInitial.maxSpeedRoll);
            // ������������� �������� �������� � ������������ � �������
            m_RollSpeed += -m_RollSpeed * m_BikeParametersInitial.linearDrag * dt;
            // ������ ���� �������� � ������ ���������� ������� ��������
            m_RollAngle += m_RollSpeed * dt;
            if (m_RollAngle > 360)
            {
                m_RollAngle = 0;
            }
            else if (m_RollAngle < 0)
            {
                m_RollAngle = 360;
            }

            Vector3 bikePos = m_Track.GetPosition(m_Distance);
            Vector3 bikeDir = m_Track.GetDirection(m_Distance);

            Quaternion q = Quaternion.AngleAxis(m_RollAngle, Vector3.forward);
            Vector3 trackOffset = q * (Vector3.up * m_Track.Radius);
            
            transform.position = bikePos - trackOffset;
            transform.rotation = Quaternion.LookRotation(bikeDir, trackOffset);
        }
        
        

        private float m_PrevDistance;
        public float GetPrevDistance()
        {
            return m_PrevDistance;
        }
        // 0 - 100
        private float m_Fuel;

        public float GetFuel()
        {
            return m_Fuel;
        }

        public void AddFuel(float amount)
        {
            m_Fuel += amount;

            m_Fuel = Mathf.Clamp(m_Fuel, 0, 100);
        }

        public void Braking()
        {
            m_Velocity = 0;
        }

        public bool ConsumeFuelForAfterburner(float amount)
        {
            if (m_Fuel <= amount)
                return false;

            m_Fuel -= amount;

            return true;
        }

        public static readonly string Tag = "Bike";
    }
}

