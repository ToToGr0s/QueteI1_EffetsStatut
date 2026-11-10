using System.Collections.Generic;

namespace QueteI1.StatusEffects
{
    /// <summary>
    /// Horloge de simulation commune à toutes les entités. Le temps reçu est découpé en pas fixes :
    /// à chaque pas, toutes les entités avancent de la même durée, dans le même ordre. Le résultat ne
    /// dépend donc pas de la fréquence d'image, y compris quand plusieurs effets ou plusieurs entités
    /// interagissent. Le reste inférieur à un pas est gardé pour l'appel suivant.
    /// </summary>
    public sealed class SimulationClock
    {
        public const float FixedStep = 0.02f;

        const float StepTolerance = 1e-5f;

        readonly List<StatusEntity> entities = new List<StatusEntity>();
        float accumulator;
        long steps;

        /// <summary>Temps de simulation écoulé, en secondes (toujours un nombre entier de pas).</summary>
        public float Time => (float)(steps * (double)FixedStep);

        public IReadOnlyList<StatusEntity> Entities => entities;

        public void Add(StatusEntity entity)
        {
            if (entity != null && !entities.Contains(entity))
                entities.Add(entity);
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            accumulator += deltaTime;
            while (accumulator >= FixedStep - StepTolerance)
            {
                accumulator = System.Math.Max(0f, accumulator - FixedStep);
                steps++;
                foreach (var entity in entities)
                    entity.Tick(FixedStep);
            }
        }
    }
}
