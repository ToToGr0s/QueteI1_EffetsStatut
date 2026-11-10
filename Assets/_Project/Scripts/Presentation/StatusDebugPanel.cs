using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QueteI1.StatusEffects.Presentation
{
    /// <summary>
    /// Outil de test du système d'effets (IMGUI) : choisir une cible, appliquer ou retirer des effets,
    /// lire les effets actifs (stacks, chrono), les stats (base, finale, calcul) et le journal.
    /// Mise en page en pixels de référence 1080p, mise à l'échelle selon la fenêtre.
    /// </summary>
    public sealed class StatusDebugPanel : MonoBehaviour
    {
        const float RefHeight = 1080f;
        const float TopBarHeight = 44f;
        const float LeftWidth = 360f;
        const float Margin = 10f;
        const float WorldHeight = 260f;
        const float CardHeight = 430f;
        const float EffectButtonHeight = 30f;
        const float ActiveRowHeight = 25f;
        const float LogLineHeight = 20f;
        const float StepDuration = 0.1f;

        static readonly KeyCode[] EffectKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
        };
        static readonly KeyCode[] KeypadKeys =
        {
            KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5,
            KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9, KeyCode.Keypad0
        };
        static readonly float[] SpeedPresets = { 0.25f, 0.5f, 1f, 2f };

        static readonly Color PanelColor = new Color(0.115f, 0.125f, 0.155f, 0.97f);
        static readonly Color RowColor = new Color(0.165f, 0.175f, 0.215f, 1f);
        static readonly Color RowAltColor = new Color(0.145f, 0.155f, 0.19f, 1f);
        static readonly Color BorderColor = new Color(0.27f, 0.29f, 0.35f, 1f);
        static readonly Color TextColor = new Color(0.94f, 0.95f, 0.97f, 1f);
        static readonly Color MutedColor = new Color(0.62f, 0.65f, 0.72f, 1f);
        static readonly Color AccentColor = new Color(0.36f, 0.66f, 1f, 1f);
        static readonly Color GoodColor = new Color(0.45f, 0.9f, 0.5f, 1f);
        static readonly Color BadColor = new Color(1f, 0.45f, 0.4f, 1f);
        static readonly Color WarnColor = new Color(1f, 0.8f, 0.3f, 1f);

        [SerializeField] SimulationRunner runner;
        [SerializeField] EffectLibrary library;
        [SerializeField] DemoScenario demo;
        [SerializeField] Camera worldCamera;

        readonly List<int> visibleLog = new List<int>();
        readonly Dictionary<string, Color> entityColors = new Dictionary<string, Color>();
        readonly Dictionary<EffectDefinition, string> descriptions = new Dictionary<EffectDefinition, string>();

        Vector2[] cardScrolls = Array.Empty<Vector2>();
        int targetIndex;
        Vector2 effectScroll;
        Vector2 logScroll;
        bool showHealthEvents = true;
        bool showStatEvents = true;
        bool followLog = true;
        bool scrollLogToEnd = true;

        float scale = 1f;
        float virtualWidth = 1920f;
        bool stylesReady;
        GUIStyle textStyle, smallStyle, rightSmallStyle, boldStyle, titleStyle, headerStyle, centeredStyle,
            centeredSmallStyle, wrapSmallStyle;

        IReadOnlyList<EffectDefinition> Effects => library != null ? library.Effects : Array.Empty<EffectDefinition>();
        EntityBehaviour Target => runner.Entities[Mathf.Clamp(targetIndex, 0, runner.Entities.Count - 1)];

        void Start()
        {
            if (runner == null || runner.Log == null)
            {
                Debug.LogError("StatusDebugPanel : SimulationRunner manquant.", this);
                enabled = false;
                return;
            }

            cardScrolls = new Vector2[runner.Entities.Count];
            foreach (var entity in runner.Entities)
                entityColors[entity.DisplayName] = Color.Lerp(entity.Color, Color.white, 0.35f);
            runner.Log.Added += _ => scrollLogToEnd = true;
        }

        void Update()
        {
            if (runner == null || runner.Entities.Count == 0)
                return;

            if (Input.GetKeyDown(KeyCode.Tab))
                targetIndex = (targetIndex + 1) % runner.Entities.Count;

            for (int i = 0; i < Mathf.Min(Effects.Count, EffectKeys.Length); i++)
            {
                if (Input.GetKeyDown(EffectKeys[i]) || Input.GetKeyDown(KeypadKeys[i]))
                    ApplyEffect(Effects[i]);
            }

            if (Input.GetKeyDown(KeyCode.Space)) Attack(0, 1);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Attack(1, 0);
            if (Input.GetKeyDown(KeyCode.P)) runner.Paused = !runner.Paused;
            if (Input.GetKeyDown(KeyCode.N)) runner.Step(StepDuration);
            if (Input.GetKeyDown(KeyCode.R)) ResetAll();
        }

        // La caméra de la scène ne dessine que dans le cadre « vue de la scène » du panneau.
        void LateUpdate()
        {
            if (worldCamera == null)
                return;

            UpdateScale();
            var world = WorldRect();
            worldCamera.rect = new Rect(
                world.x * scale / Screen.width,
                1f - world.yMax * scale / Screen.height,
                world.width * scale / Screen.width,
                world.height * scale / Screen.height);
        }

        void ApplyEffect(EffectDefinition definition)
        {
            if (definition != null)
                Target.Model.Effects.Apply(definition, "Outil de test");
        }

        void Attack(int attacker, int defender)
        {
            if (attacker < runner.Entities.Count && defender < runner.Entities.Count)
                runner.Entities[attacker].Model.TryAttack(runner.Entities[defender].Model);
        }

        // La démo s'arrête avec la réinitialisation : sinon elle continuerait d'appliquer ses effets.
        void ResetAll()
        {
            if (demo != null)
                demo.Stop();
            runner.ResetAll();
        }

        void UpdateScale()
        {
            scale = Mathf.Max(0.1f, Screen.height / RefHeight);
            virtualWidth = Screen.width / scale;
        }

        Rect LeftRect() => new Rect(Margin, TopBarHeight + Margin, LeftWidth, RefHeight - TopBarHeight - 2f * Margin);

        Rect WorldRect()
        {
            float x = 2f * Margin + LeftWidth;
            return new Rect(x, TopBarHeight + Margin, virtualWidth - x - Margin, WorldHeight);
        }

        Rect CardRect(int index)
        {
            var world = WorldRect();
            int count = runner.Entities.Count;
            float width = (world.width - (count - 1) * Margin) / count;
            return new Rect(world.x + index * (width + Margin), world.yMax + Margin, width, CardHeight);
        }

        Rect LogRect()
        {
            var world = WorldRect();
            float y = world.yMax + Margin + CardHeight + Margin;
            return new Rect(world.x, y, world.width, RefHeight - y - Margin);
        }

        void OnGUI()
        {
            if (runner == null || runner.Log == null || runner.Entities.Count == 0)
                return;

            EnsureStyles();
            UpdateScale();
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            DrawTopBar(new Rect(0f, 0f, virtualWidth, TopBarHeight));
            DrawLeftPanel(LeftRect());
            DrawWorldOverlay(WorldRect());
            for (int i = 0; i < runner.Entities.Count; i++)
                DrawEntityCard(CardRect(i), i);
            DrawLog(LogRect());
        }

        // ---------- Barre du haut : temps ----------

        void DrawTopBar(Rect rect)
        {
            Fill(rect, PanelColor);
            Fill(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), BorderColor);
            Label(new Rect(14f, 0f, 250f, rect.height), "Effets de statut · test", titleStyle, TextColor);

            float x = 270f;
            float y = 7f;
            float h = rect.height - 14f;
            Label(new Rect(x, 0f, 120f, rect.height), "t = " + TextFormat.Seconds(runner.SimulationTime), boldStyle,
                runner.Paused ? WarnColor : TextColor);
            x += 120f;

            if (FlatButton(new Rect(x, y, 110f, h), runner.Paused ? "Reprendre" : "Pause", RowColor, runner.Paused))
                runner.Paused = !runner.Paused;
            x += 116f;
            if (FlatButton(new Rect(x, y, 80f, h), "+0.1 s", RowColor))
                runner.Step(StepDuration);
            x += 96f;

            Label(new Rect(x, 0f, 64f, rect.height), "Vitesse", smallStyle, MutedColor);
            x += 60f;
            foreach (float preset in SpeedPresets)
            {
                if (FlatButton(new Rect(x, y, 50f, h), "×" + TextFormat.Num(preset), RowColor,
                        Mathf.Approximately(runner.TimeScale, preset)))
                    runner.TimeScale = preset;
                x += 54f;
            }

            x += 12f;
            if (demo != null)
            {
                bool playing = demo.IsPlaying;
                if (FlatButton(new Rect(x, y, 160f, h), playing ? "Arrêter la démo" : "Démo de cumul", RowColor, playing))
                {
                    if (playing) demo.Stop();
                    else demo.Play();
                }
            }

            if (FlatButton(new Rect(rect.width - 142f, y, 130f, h), "Réinitialiser", RowColor))
                ResetAll();
        }

        // ---------- Colonne de gauche : cible, effets, actions ----------

        void DrawLeftPanel(Rect rect)
        {
            Fill(rect, PanelColor);
            float x = rect.x + 12f;
            float width = rect.width - 24f;
            float y = rect.y + 8f;
            int count = runner.Entities.Count;

            Label(new Rect(x, y, width, 24f), "Cible", headerStyle, TextColor);
            y += 28f;
            float targetWidth = (width - (count - 1) * 6f) / count;
            for (int i = 0; i < count; i++)
            {
                var entity = runner.Entities[i];
                if (FlatButton(new Rect(x + i * (targetWidth + 6f), y, targetWidth, 34f), entity.DisplayName,
                        Color.Lerp(RowColor, entity.Color, 0.3f), targetIndex == i))
                    targetIndex = i;
            }
            y += 44f;

            Label(new Rect(x, y, width, 24f), "Appliquer à la cible", headerStyle, TextColor);
            y += 28f;

            const float bottomHeight = 200f;
            float listHeight = Effects.Count * (EffectButtonHeight + 2f);
            float viewHeight = Mathf.Min(listHeight, rect.yMax - y - bottomHeight);
            float innerWidth = listHeight > viewHeight ? width - 18f : width;
            effectScroll = GUI.BeginScrollView(new Rect(x, y, width, viewHeight), effectScroll,
                new Rect(0f, 0f, innerWidth, listHeight));
            for (int i = 0; i < Effects.Count; i++)
            {
                if (Effects[i] != null && DrawEffectButton(
                        new Rect(0f, i * (EffectButtonHeight + 2f), innerWidth, EffectButtonHeight), Effects[i], i))
                    ApplyEffect(Effects[i]);
            }
            GUI.EndScrollView();
            y += viewHeight + 10f;

            Label(new Rect(x, y, width, 24f), "Actions", headerStyle, TextColor);
            y += 28f;
            if (count >= 2)
            {
                float half = (width - 6f) / 2f;
                if (FlatButton(new Rect(x, y, half, 32f), runner.Entities[0].DisplayName + " frappe", RowColor)) Attack(0, 1);
                if (FlatButton(new Rect(x + half + 6f, y, half, 32f), runner.Entities[1].DisplayName + " frappe", RowColor)) Attack(1, 0);
                y += 36f;
            }
            if (FlatButton(new Rect(x, y, width, 32f), "Retirer tous les effets de la cible", RowColor))
                Target.Model.Effects.RemoveAll(RemovalReason.Dispelled);
            y += 40f;

            Label(new Rect(x, y, width, rect.yMax - y - 6f),
                "Tab : cible · 1 à 0 : effets · ← → : déplacer · Espace / Entrée : frapper · P : pause · N : +0.1 s · R : réinitialiser",
                wrapSmallStyle, MutedColor);
        }

        bool DrawEffectButton(Rect rect, EffectDefinition definition, int index)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            Fill(rect, hover ? Color.Lerp(RowColor, Color.white, 0.1f) : RowColor);
            Fill(new Rect(rect.x, rect.y, 4f, rect.height), definition.Color);
            if (index < EffectKeys.Length)
                Label(new Rect(rect.x + 10f, rect.y, 16f, rect.height), ((index + 1) % 10).ToString(), smallStyle, MutedColor);
            Label(new Rect(rect.x + 28f, rect.y, rect.width - 204f, rect.height), definition.DisplayName, boldStyle, TextColor);
            Label(new Rect(rect.xMax - 176f, rect.y, 170f, rect.height), definition.RulesLabel, rightSmallStyle, MutedColor);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        // ---------- Vue de la scène ----------

        void DrawWorldOverlay(Rect rect)
        {
            Frame(rect, BorderColor);
            if (worldCamera == null)
                return;

            for (int i = 0; i < runner.Entities.Count; i++)
            {
                var entity = runner.Entities[i];
                if (entity.BodyRenderer == null)
                    continue;

                var position = entity.transform.position;
                var top = worldCamera.WorldToScreenPoint(new Vector3(position.x, entity.BodyRenderer.bounds.max.y + 0.15f, position.z));
                var feet = worldCamera.WorldToScreenPoint(position);
                if (top.z < 0f)
                    continue;

                float centerX = top.x / scale;
                float labelBottom = (Screen.height - top.y) / scale;
                float feetY = (Screen.height - feet.y) / scale;

                var clickArea = new Rect(centerX - 75f, labelBottom - 44f, 150f, feetY - labelBottom + 50f);
                if (i == targetIndex)
                    Frame(clickArea, AccentColor, 2f);
                if (GUI.Button(clickArea, GUIContent.none, GUIStyle.none))
                    targetIndex = i;

                var model = entity.Model;
                var bar = new Rect(centerX - 60f, labelBottom - 12f, 120f, 8f);
                Fill(bar, new Color(0f, 0f, 0f, 0.6f));
                float ratio = model.Health.Max > 0f ? model.Health.Current / model.Health.Max : 0f;
                Fill(new Rect(bar.x, bar.y, bar.width * ratio, bar.height), Color.Lerp(BadColor, GoodColor, ratio));

                string status = model.IsDead ? " · K.O." : model.IsBlocked(ActionType.Move) ? " · bloqué" : "";
                Label(new Rect(centerX - 130f, bar.y - 26f, 260f, 24f), entity.DisplayName + status, centeredStyle,
                    model.IsDead ? BadColor : status.Length > 0 ? WarnColor : TextColor);
            }
        }

        // ---------- Fiche d'une entité ----------

        void DrawEntityCard(Rect rect, int index)
        {
            var entity = runner.Entities[index];
            var model = entity.Model;
            bool isTarget = index == targetIndex;

            Fill(rect, PanelColor);
            Fill(new Rect(rect.x, rect.y, 6f, rect.height), entity.Color);
            if (isTarget)
                Frame(rect, AccentColor, 2f);

            float x = rect.x + 18f;
            float width = rect.width - 34f;
            float y = rect.y + 8f;

            Label(new Rect(x, y, width, 28f), entity.DisplayName + (isTarget ? "  ·  cible" : ""), titleStyle,
                isTarget ? AccentColor : TextColor);
            if (GUI.Button(new Rect(rect.x, rect.y, rect.width, 38f), GUIContent.none, GUIStyle.none))
                targetIndex = index;
            y += 36f;

            var bar = new Rect(x, y, width, 22f);
            Fill(bar, RowAltColor);
            float ratio = model.Health.Max > 0f ? model.Health.Current / model.Health.Max : 0f;
            Fill(new Rect(bar.x, bar.y, bar.width * ratio, bar.height), Color.Lerp(BadColor, GoodColor, ratio) * 0.8f);
            Label(bar, $"PV : {TextFormat.Num(model.Health.Current)} / {TextFormat.Num(model.Health.Max)}", centeredStyle, TextColor);
            y += 26f;

            Label(new Rect(x, y, width, 22f), ActionState(model, out var stateColor), boldStyle, stateColor);
            y += 28f;

            Label(new Rect(x, y, 100f, 20f), "Stat", smallStyle, MutedColor);
            Label(new Rect(x + 100f, y, 66f, 20f), "Base", smallStyle, MutedColor);
            Label(new Rect(x + 166f, y, 90f, 20f), "Finale", smallStyle, MutedColor);
            Label(new Rect(x + 262f, y, width - 262f, 20f), "Calcul : (base + additifs) × multiplicateurs", smallStyle, MutedColor);
            y += 22f;
            foreach (var stat in StatTypeInfo.All)
            {
                var breakdown = model.Stats.GetBreakdown(stat);
                Fill(new Rect(x - 4f, y, width + 8f, 23f), (int)stat % 2 == 0 ? RowColor : RowAltColor);
                Label(new Rect(x, y, 100f, 23f), StatTypeInfo.Label(stat), textStyle, TextColor);
                Label(new Rect(x + 100f, y, 66f, 23f), TextFormat.Num(breakdown.BaseValue), textStyle, MutedColor);

                bool up = breakdown.Final > breakdown.BaseValue + 0.001f;
                bool down = breakdown.Final < breakdown.BaseValue - 0.001f;
                Label(new Rect(x + 166f, y, 90f, 23f), TextFormat.Num(breakdown.Final), boldStyle,
                    up ? GoodColor : down ? BadColor : TextColor);
                Label(new Rect(x + 262f, y, width - 262f, 23f), breakdown.HasModifiers ? breakdown.ToFormula() : "—",
                    smallStyle, breakdown.HasModifiers ? TextColor : MutedColor);
                y += 24f;
            }
            y += 10f;

            var effects = model.Effects.Active;
            Label(new Rect(x, y, width, 22f), $"Effets actifs ({effects.Count})", headerStyle, TextColor);
            y += 26f;
            var view = new Rect(x - 4f, y, width + 8f, rect.yMax - y - 8f);
            if (effects.Count == 0)
                return;

            float contentHeight = effects.Count * ActiveRowHeight;
            float rowWidth = contentHeight > view.height ? view.width - 18f : view.width;
            cardScrolls[index] = GUI.BeginScrollView(view, cardScrolls[index], new Rect(0f, 0f, rowWidth, contentHeight));
            for (int i = 0; i < effects.Count; i++)
            {
                if (DrawActiveEffectRow(new Rect(0f, i * ActiveRowHeight, rowWidth, ActiveRowHeight - 1f), effects[i], i))
                {
                    model.Effects.Remove(effects[i], RemovalReason.Dispelled);
                    break;
                }
            }
            GUI.EndScrollView();
        }

        static string ActionState(StatusEntity model, out Color color)
        {
            if (model.IsDead)
            {
                color = BadColor;
                return "K.O. (réinitialiser pour le relever)";
            }

            var blocked = (model.IsBlocked(ActionType.Move) ? ActionType.Move : ActionType.None)
                          | (model.IsBlocked(ActionType.Attack) ? ActionType.Attack : ActionType.None);
            if (blocked == ActionType.None)
            {
                color = MutedColor;
                return "Actions : libres";
            }

            color = WarnColor;
            return $"Bloqué : {ActionTypeInfo.Describe(blocked)}  ←  {model.DescribeBlockers(blocked)}";
        }

        bool DrawActiveEffectRow(Rect rect, ActiveEffect effect, int index)
        {
            var definition = effect.Definition;
            Fill(rect, index % 2 == 0 ? RowColor : RowAltColor);
            Fill(new Rect(rect.x, rect.y, 4f, rect.height), definition.Color);

            float x = rect.x + 12f;
            string stacks = definition.MaxStacks > 1 ? $"  ×{effect.Stacks}/{definition.MaxStacks}" : "";
            // Quand chaque source a sa propre instance, on montre laquelle.
            string source = definition.SeparatePerSource ? $"  ({effect.Source.Label})" : "";
            Label(new Rect(x, rect.y, 175f, rect.height), definition.DisplayName + stacks + source, boldStyle, TextColor);
            x += 178f;
            Label(new Rect(x, rect.y, 110f, rect.height), definition.StackingLabel, smallStyle, MutedColor);
            x += 112f;

            var bar = new Rect(x, rect.y + 4f, 110f, rect.height - 8f);
            Fill(bar, new Color(0f, 0f, 0f, 0.35f));
            Fill(new Rect(bar.x, bar.y, bar.width * effect.RemainingNormalized, bar.height), definition.Color * 0.7f);
            Label(bar, effect.RemainingLabel, centeredSmallStyle, TextColor);
            x += 118f;

            Label(new Rect(x, rect.y, rect.xMax - x - 30f, rect.height), Describe(definition), smallStyle, MutedColor);
            return FlatButton(new Rect(rect.xMax - 26f, rect.y + 2f, 22f, rect.height - 4f), "×", RowAltColor);
        }

        // Résumé des composants d'un effet, construit une seule fois par définition.
        string Describe(EffectDefinition definition)
        {
            if (descriptions.TryGetValue(definition, out string description))
                return description;

            var text = new StringBuilder();
            foreach (var component in definition.Components)
            {
                if (component == null) continue;
                if (text.Length > 0) text.Append(" · ");
                text.Append(component.Describe());
            }
            return descriptions[definition] = text.ToString();
        }

        // ---------- Journal ----------

        void DrawLog(Rect rect)
        {
            Fill(rect, PanelColor);
            Label(new Rect(rect.x + 12f, rect.y + 4f, 300f, 24f), "Journal", headerStyle, TextColor);

            float bx = rect.xMax - 12f - 90f;
            if (FlatButton(new Rect(bx, rect.y + 5f, 90f, 22f), "Effacer", RowColor))
                runner.Log.Clear();
            bx -= 128f;
            if (FlatButton(new Rect(bx, rect.y + 5f, 120f, 22f), "Suivre la fin", RowColor, followLog))
                followLog = !followLog;
            bx -= 88f;
            if (FlatButton(new Rect(bx, rect.y + 5f, 80f, 22f), "Stats", RowColor, showStatEvents))
                showStatEvents = !showStatEvents;
            bx -= 138f;
            if (FlatButton(new Rect(bx, rect.y + 5f, 130f, 22f), "Dégâts / soins", RowColor, showHealthEvents))
                showHealthEvents = !showHealthEvents;

            var entries = runner.Log.Entries;
            visibleLog.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                var type = entries[i].Type;
                if (!showHealthEvents && (type == EffectEventType.Damage || type == EffectEventType.Heal)) continue;
                if (!showStatEvents && type == EffectEventType.Stats) continue;
                visibleLog.Add(i);
            }

            var view = new Rect(rect.x + 6f, rect.y + 32f, rect.width - 12f, rect.height - 38f);
            float contentHeight = visibleLog.Count * LogLineHeight;
            if (followLog && scrollLogToEnd && Event.current.type == EventType.Layout)
            {
                logScroll.y = Mathf.Max(0f, contentHeight - view.height);
                scrollLogToEnd = false;
            }

            float lineWidth = view.width - 18f;
            logScroll = GUI.BeginScrollView(view, logScroll, new Rect(0f, 0f, lineWidth, contentHeight));
            int first = Mathf.Max(0, (int)(logScroll.y / LogLineHeight));
            int last = Mathf.Min(visibleLog.Count, first + (int)(view.height / LogLineHeight) + 2);
            for (int i = first; i < last; i++)
            {
                var entry = entries[visibleLog[i]];
                float y = i * LogLineHeight;
                if (i % 2 == 0)
                    Fill(new Rect(0f, y, lineWidth, LogLineHeight), RowAltColor);

                Label(new Rect(6f, y, 80f, LogLineHeight), TextFormat.Seconds(entry.Time), smallStyle, MutedColor);
                Label(new Rect(86f, y, 90f, LogLineHeight), entry.Entity, smallStyle,
                    entityColors.TryGetValue(entry.Entity, out var entityColor) ? entityColor : MutedColor);
                var (typeLabel, typeColor) = EventTypeInfo(entry.Type);
                Label(new Rect(176f, y, 96f, LogLineHeight), typeLabel, smallStyle, typeColor);
                Label(new Rect(272f, y, lineWidth - 276f, LogLineHeight), entry.Message, smallStyle, TextColor);
            }
            GUI.EndScrollView();
        }

        static (string, Color) EventTypeInfo(EffectEventType type) => type switch
        {
            EffectEventType.Applied => ("APPLIQUÉ", new Color(0.45f, 0.78f, 1f)),
            EffectEventType.Stacked => ("STACK +1", new Color(0.62f, 0.62f, 1f)),
            EffectEventType.Refreshed => ("RAFRAÎCHI", new Color(0.45f, 0.9f, 0.9f)),
            EffectEventType.Rejected => ("REFUSÉ", new Color(1f, 0.6f, 0.2f)),
            EffectEventType.Expired => ("EXPIRÉ", new Color(0.68f, 0.68f, 0.72f)),
            EffectEventType.Removed => ("RETIRÉ", new Color(0.78f, 0.66f, 0.95f)),
            EffectEventType.Damage => ("DÉGÂTS", new Color(1f, 0.45f, 0.45f)),
            EffectEventType.Heal => ("SOIN", new Color(0.5f, 1f, 0.55f)),
            EffectEventType.Stats => ("STAT", new Color(0.95f, 0.85f, 0.45f)),
            EffectEventType.Blocked => ("BLOQUÉ", new Color(1f, 0.8f, 0.2f)),
            EffectEventType.Death => ("MORT", new Color(1f, 0.25f, 0.25f)),
            _ => ("INFO", new Color(0.85f, 0.85f, 0.9f))
        };

        // ---------- Dessin ----------

        bool FlatButton(Rect rect, string label, Color baseColor, bool active = false)
        {
            var background = active ? Color.Lerp(baseColor, AccentColor, 0.55f) : baseColor;
            if (rect.Contains(Event.current.mousePosition))
                background = Color.Lerp(background, Color.white, 0.14f);
            Fill(rect, background);
            Label(rect, label, centeredStyle, TextColor);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        static void Fill(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        static void Frame(Rect rect, Color color, float thickness = 1f)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        static void Label(Rect rect, string text, GUIStyle style, Color color)
        {
            var previous = style.normal.textColor;
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
            style.normal.textColor = previous;
        }

        void EnsureStyles()
        {
            if (stylesReady)
                return;

            textStyle = MakeStyle(15, FontStyle.Normal, TextAnchor.MiddleLeft);
            smallStyle = MakeStyle(13, FontStyle.Normal, TextAnchor.MiddleLeft);
            rightSmallStyle = MakeStyle(13, FontStyle.Normal, TextAnchor.MiddleRight);
            boldStyle = MakeStyle(15, FontStyle.Bold, TextAnchor.MiddleLeft);
            titleStyle = MakeStyle(20, FontStyle.Bold, TextAnchor.MiddleLeft);
            headerStyle = MakeStyle(16, FontStyle.Bold, TextAnchor.MiddleLeft);
            centeredStyle = MakeStyle(15, FontStyle.Bold, TextAnchor.MiddleCenter);
            centeredSmallStyle = MakeStyle(12, FontStyle.Normal, TextAnchor.MiddleCenter);
            wrapSmallStyle = MakeStyle(13, FontStyle.Normal, TextAnchor.UpperLeft);
            wrapSmallStyle.wordWrap = true;
            stylesReady = true;
        }

        static GUIStyle MakeStyle(int size, FontStyle fontStyle, TextAnchor alignment)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = fontStyle,
                alignment = alignment,
                clipping = TextClipping.Clip,
                wordWrap = false,
                richText = false,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
        }

        internal void EditorSetup(SimulationRunner simulation, EffectLibrary effectLibrary, DemoScenario demoScenario, Camera sceneCamera)
        {
            runner = simulation;
            library = effectLibrary;
            demo = demoScenario;
            worldCamera = sceneCamera;
        }
    }
}
