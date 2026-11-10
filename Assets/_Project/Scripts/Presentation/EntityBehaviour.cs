using UnityEngine;

namespace QueteI1.StatusEffects.Presentation
{
    public enum EntityControl
    {
        Idle,
        Keyboard,
        Patrol
    }

    /// <summary>
    /// Relie une <see cref="StatusEntity"/> à un GameObject : déplacement selon la stat Vitesse,
    /// retour visuel (dégâts, soin, étourdissement, mort).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EntityBehaviour : MonoBehaviour
    {
        const float FlashDuration = 0.2f;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly Color StunColor = new Color(1f, 0.9f, 0.25f);

        [SerializeField] string displayName = "Entité";
        [SerializeField] BaseStats baseStats = new BaseStats();
        [SerializeField] Color color = Color.white;
        [SerializeField] EntityControl control = EntityControl.Idle;
        [SerializeField] float minX = -8f;
        [SerializeField] float maxX = 8f;
        [SerializeField] Renderer bodyRenderer;

        MaterialPropertyBlock propertyBlock;
        Vector3 startPosition;
        int patrolDirection = 1;
        bool moveWasBlocked;
        float flashTimer;
        Color flashColor;

        public StatusEntity Model { get; private set; }

        public string DisplayName => displayName;
        public Color Color => color;
        public Renderer BodyRenderer => bodyRenderer;

        internal void Initialize(EffectEventLog log)
        {
            startPosition = transform.position;
            Model = new StatusEntity(displayName, baseStats, log);
            Model.Health.Changed += change =>
            {
                flashColor = change.Kind == HealthChangeKind.Damage ? new Color(1f, 0.25f, 0.2f) : new Color(0.3f, 1f, 0.4f);
                flashTimer = FlashDuration;
            };
        }

        public void ResetEntity()
        {
            Model.Revive();
            transform.position = startPosition;
            patrolDirection = 1;
        }

        /// <summary>Déplacement de l'image courante, à la vitesse finale de l'entité.</summary>
        internal void Move(float deltaTime)
        {
            float input = control switch
            {
                EntityControl.Keyboard => Input.GetAxisRaw("Horizontal"),
                EntityControl.Patrol => patrolDirection,
                _ => 0f
            };

            if (Mathf.Abs(input) < 0.01f)
            {
                moveWasBlocked = false;
                return;
            }

            if (!Model.CanPerform(ActionType.Move))
            {
                // Le joueur qui essaie de bouger voit pourquoi il ne peut pas (une seule ligne par tentative).
                if (!moveWasBlocked && control == EntityControl.Keyboard)
                    Model.TryPerform(ActionType.Move, "Déplacement");
                moveWasBlocked = true;
                return;
            }

            moveWasBlocked = false;
            var position = transform.position;
            position.x += Mathf.Sign(input) * Model.Stats.Get(StatType.Speed) * deltaTime;
            if (position.x >= maxX)
            {
                position.x = maxX;
                patrolDirection = -1;
            }
            else if (position.x <= minX)
            {
                position.x = minX;
                patrolDirection = 1;
            }
            transform.position = position;
        }

        void Update()
        {
            if (Model == null || bodyRenderer == null)
                return;

            var tint = color;
            if (Model.IsDead)
                tint = Color.Lerp(color, Color.gray, 0.75f);
            else if (Model.IsBlocked(ActionType.Move))
                tint = Color.Lerp(color, StunColor, 0.45f + 0.35f * Mathf.Sin(Time.unscaledTime * 14f));

            if (flashTimer > 0f)
            {
                flashTimer -= Time.unscaledDeltaTime;
                tint = Color.Lerp(tint, flashColor, Mathf.Clamp01(flashTimer / FlashDuration));
            }

            propertyBlock ??= new MaterialPropertyBlock();
            bodyRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(ColorId, tint);
            bodyRenderer.SetPropertyBlock(propertyBlock);

            transform.rotation = Model.IsDead ? Quaternion.Euler(0f, 0f, 80f) : Quaternion.identity;
        }

        // Modifier les stats de base dans l'inspecteur en jeu met à jour les stats finales.
        void OnValidate() => Model?.Stats.Recompute();

        internal void EditorSetup(string name, BaseStats stats, Color bodyColor, EntityControl controlMode, Renderer body,
            float rangeMin, float rangeMax)
        {
            displayName = name;
            baseStats = stats;
            color = bodyColor;
            control = controlMode;
            bodyRenderer = body;
            minX = rangeMin;
            maxX = rangeMax;
        }
    }
}
