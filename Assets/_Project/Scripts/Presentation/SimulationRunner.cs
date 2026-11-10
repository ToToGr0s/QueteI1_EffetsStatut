using System.Collections.Generic;
using UnityEngine;

namespace QueteI1.StatusEffects.Presentation
{
    /// <summary>
    /// Relie la scène à l'horloge de simulation : pause, ralenti et pas-à-pas pour observer le système.
    /// Le découpage en pas fixes est fait par <see cref="SimulationClock"/>, commun à toutes les entités.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class SimulationRunner : MonoBehaviour
    {
        [SerializeField] List<EntityBehaviour> entities = new List<EntityBehaviour>();
        [SerializeField, Range(0f, 4f)] float timeScale = 1f;

        SimulationClock clock;

        public float SimulationTime => clock != null ? clock.Time : 0f;
        public bool Paused { get; set; }
        public EffectEventLog Log { get; private set; }
        public IReadOnlyList<EntityBehaviour> Entities => entities;

        public float TimeScale
        {
            get => timeScale;
            set => timeScale = Mathf.Clamp(value, 0f, 4f);
        }

        void Awake()
        {
            clock = new SimulationClock();
            Log = new EffectEventLog(() => clock.Time);
            foreach (var entity in entities)
            {
                entity.Initialize(Log);
                clock.Add(entity.Model);
            }
            Log.Add("Système", EffectEventType.Info, "simulation démarrée");
        }

        void Update()
        {
            if (!Paused)
                Step(Time.deltaTime * timeScale);
        }

        public void Step(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            clock.Advance(deltaTime);
            foreach (var entity in entities)
                entity.Move(deltaTime);
        }

        public void ResetAll()
        {
            foreach (var entity in entities)
                entity.ResetEntity();
        }

        internal void EditorSetup(IEnumerable<EntityBehaviour> sceneEntities) =>
            entities = new List<EntityBehaviour>(sceneEntities);
    }
}
