using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Race
{
    public class RaceController : MonoBehaviour
    {
        [SerializeField] private RaceTrack m_Track;

        [SerializeField] private int m_MaxLaps;
        public int MaxLaps => m_MaxLaps;

        // gameplay mode
        // race - laps
        // time
        public enum Racemode
        {
           Laps,
           Time,
           LastStanding
        }

        [SerializeField] private Racemode m_RaceMode;

        [SerializeField] private UnityEvent m_EventRaceStart;
        [SerializeField] private UnityEvent m_EventRaceFinished;

        [SerializeField] private Bike[] m_Bikes;
        public Bike[] Bikes => m_Bikes;

        [SerializeField] private int m_CountdownTimer;
        public int CountdownTimer => m_CountdownTimer;

        private float m_CountTimer;
        public float CountTimer => m_CountTimer;

        public float time1;

        public bool IsRaceActive { get; private set; }

        [SerializeField] private RaceCondition[] m_Conditions;

        public void StartRace()
        {
            m_ActiveBikes = new List<Bike>(m_Bikes);
            m_FinishedBikes = new List<Bike>();

            IsRaceActive = true;

            m_CountTimer = m_CountdownTimer;

            foreach (var c in m_Conditions)
                c.OnRaceStart();

            foreach (var b in m_Bikes)
            {
                b.OnRaceStart();
            }

            m_EventRaceStart?.Invoke();
        }

        public void EndRace()
        {
            IsRaceActive = false;
            foreach (var c in m_Conditions)
                c.OnRaceEnd();
        }

        private void Start()
        {
            StartRace();
        }

        private void Update()
        {
            UpdateBikeRacePositions();
            UpdateRacePrestart();
            UpdateConditions();
        }
        
        private void UpdateRacePrestart()
        {
            if (CountTimer > 1)
            {
                m_CountTimer -= Time.deltaTime;

                if (m_CountTimer < 1)
                {
                    foreach (var e in m_Bikes)
                    {
                        e.IsMovementControlsActive = true;
                    }
                }
            }
            else
                m_CountTimer -= Time.deltaTime;
        }

        private void UpdateConditions()
        {
            if (IsRaceActive)
                return;
            
            foreach(var c in m_Conditions)
            {
                if (!c.IsTriggered)
                    return;
            }

            //race ends
            EndRace();

            m_EventRaceFinished?.Invoke();
        }

        private List<Bike> m_ActiveBikes;
        private List<Bike> m_FinishedBikes;

        [SerializeField] private RaceResultsViewController m_RaceResultsViewController;

        private void UpdateBikeRacePositions()
        {
            if(m_ActiveBikes.Count == 0)
            {
                EndRace();
                return;
            }

            foreach(var v in m_ActiveBikes)
            {
                if (m_FinishedBikes.Contains(v))
                    continue;

                float dist = v.GetDistance();
                float totalRaceDistance = m_MaxLaps * m_Track.GetTrackLength();

                
                if (dist > totalRaceDistance)
                {
                    m_FinishedBikes.Add(v);
                    v.Statistics.RacePlace = m_FinishedBikes.Count;
                    v.OnRaceEnd();

                    if(v.IsPlayerBike)
                    {
                        m_RaceResultsViewController.Show(v.Statistics);
                    }
                }
            }
        }
    }
}