using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QueteI1.StatusEffects.Presentation
{
    /// <summary>
    /// Suite d'applications d'effets jouée automatiquement, pour montrer le cumul de plusieurs
    /// effets sur la même entité. Les délais suivent le temps de simulation (pause, ralenti).
    /// </summary>
    public sealed class DemoScenario : MonoBehaviour
    {
        [Serializable]
        public struct Step
        {
            public EffectDefinition effect;
            [Tooltip("0 = Joueur, 1 = Ennemi")] public int targetIndex;
            [Min(0f)] public float delay;

            public Step(EffectDefinition effect, float delay, int targetIndex = 0)
            {
                this.effect = effect;
                this.delay = delay;
                this.targetIndex = targetIndex;
            }
        }

        [SerializeField] SimulationRunner runner;
        [SerializeField] List<Step> steps = new List<Step>();
        [Tooltip("Lance la démo au démarrage (aussi avec l'argument de ligne de commande -autodemo).")]
        [SerializeField] bool playOnStart;

        Coroutine routine;

        public bool IsPlaying { get; private set; }

        void Start()
        {
            if (playOnStart || Array.IndexOf(Environment.GetCommandLineArgs(), "-autodemo") >= 0)
                Play();
        }

        public void Play()
        {
            if (!IsPlaying && runner != null)
                routine = StartCoroutine(Run());
        }

        public void Stop()
        {
            if (!IsPlaying)
                return;

            StopCoroutine(routine);
            IsPlaying = false;
            runner.Log.Add("Démo", EffectEventType.Info, "démo interrompue");
        }

        IEnumerator Run()
        {
            IsPlaying = true;
            runner.Log.Add("Démo", EffectEventType.Info, "démo de cumul : plusieurs effets sur la même entité");

            foreach (var step in steps)
            {
                float wait = step.delay;
                while (wait > 0f)
                {
                    if (!runner.Paused)
                        wait -= Time.deltaTime * runner.TimeScale;
                    yield return null;
                }

                if (step.effect != null && step.targetIndex >= 0 && step.targetIndex < runner.Entities.Count)
                    runner.Entities[step.targetIndex].Model.Effects.Apply(step.effect, "Démo");
            }

            IsPlaying = false;
        }

        internal void EditorSetup(SimulationRunner simulation, IEnumerable<Step> demoSteps)
        {
            runner = simulation;
            steps = new List<Step>(demoSteps);
        }
    }
}
