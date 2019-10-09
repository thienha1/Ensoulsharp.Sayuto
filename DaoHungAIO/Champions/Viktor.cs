using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.Common;
using Menu = EnsoulSharp.Common.Menu;
using MenuItem = EnsoulSharp.Common.MenuItem;

namespace DaoHungAIO.Champions
{
    public class Viktor
    {
        
        private const string CHAMP_NAME = "Viktor";
        private static readonly AIHeroClient player = ObjectManager.Player;

        private static Orbwalking.Orbwalker Orbwalker;
        // Spells
        private static Spell Q, W, E, R;
        private static readonly int maxRangeE = 1225;
        private static readonly int lengthE = 700;
        private static readonly int speedE = 1050;
        private static readonly int rangeE = 525;
        private static int lasttick = 0;
        private static SharpDX.Vector3 GapCloserPos;
        private static bool AttacksEnabled
        {
            get
            {
                if (keyLinks["comboActive"].GetValue<KeyBind>().Active)
                {
                    return ((!Q.IsReady() || player.Mana < Q.Instance.ManaCost) && (!E.IsReady() || player.Mana < E.Instance.ManaCost) && (!boolLinks["qAuto"].GetValue<bool>() || player.HasBuff("viktorpowertransferreturn")));
                }
                else if (keyLinks["harassActive"].GetValue<KeyBind>().Active)
                {
                    return ((!Q.IsReady() || player.Mana < Q.Instance.ManaCost) && (!E.IsReady() || player.Mana < E.Instance.ManaCost));
                }
                return true;
            }
        }
        // Menu
        public static Menu menu;

        // Menu links
        public static Dictionary<string, MenuItem> boolLinks = new Dictionary<string, MenuItem>();
        public static Dictionary<string, MenuItem> circleLinks = new Dictionary<string, MenuItem>();
        public static Dictionary<string, MenuItem> keyLinks = new Dictionary<string, MenuItem>();
        public static Dictionary<string, MenuItem> sliderLinks = new Dictionary<string, MenuItem>();
        public static Dictionary<string, MenuItem> stringLinks = new Dictionary<string, MenuItem>();


        private static void OrbwalkingOnBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Target.Type == GameObjectType.AIHeroClient)
            {
                args.Process = AttacksEnabled;
            }
            else
                args.Process = true;

        }
        public Viktor()
        {
            // Champ validation
          



            // Define spells
            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, rangeE);
            R = new Spell(SpellSlot.R, 700);

            // Finetune spells
            Q.SetTargetted(0.25f, 2000);
            W.SetSkillshot(0.5f, 300, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0, 80, speedE, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.25f, 300f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            // Create menu
            SetupMenu();

            // Register events
            Game.OnTick += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Gapclosers.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Orbwalking.OnNonKillableMinion += Orbwalking_OnNonKillableMinion;
            Orbwalking.BeforeAttack += OrbwalkingOnBeforeAttack;
            Interrupters.OnInterrupter += Interrupter2_OnInterruptableTarget;
        }
        private static void Orbwalking_OnNonKillableMinion(AttackableUnit minion)
        {
            QLastHit((AIBaseClient)minion);
        }
        private static void QLastHit(AIBaseClient minion)
        {
            bool castQ = ((keyLinks["waveUseQLH"].GetValue<KeyBind>().Active) || boolLinks["waveUseQ"].GetValue<bool>() && keyLinks["waveActive"].GetValue<KeyBind>().Active);
            if (castQ)
            {
                var distance = Geometry.Distance(player, minion);
                var t = 250 + (int)distance / 2;
                var predHealth = HealthPrediction.GetHealthPrediction(minion, t, 0);
                // Console.WriteLine(" Distance: " + distance + " timer : " + t + " health: " + predHealth);
                if (predHealth > 0 && Q.IsKillable(minion))
                {
                    Q.Cast(minion);
                }
            }
        }
        private void Game_OnGameUpdate(EventArgs args)
        {
            // Combo
            if (keyLinks["comboActive"].GetValue<KeyBind>().Active)
                OnCombo();
            // Harass�
            if (keyLinks["harassActive"].GetValue<KeyBind>().Active)
                OnHarass();
            // WaveClear
            if (keyLinks["waveActive"].GetValue<KeyBind>().Active)
                OnWaveClear();

            if (keyLinks["jungleActive"].GetValue<KeyBind>().Active)
                OnJungleClear();

            if (keyLinks["FleeActive"].GetValue<KeyBind>().Active)
                Flee();

            if (keyLinks["forceR"].GetValue<KeyBind>().Active)
            {
                if (R.IsReady())
                {
                    List<AIHeroClient> ignoredchamps = new List<AIHeroClient>();

                    foreach (var hero in HeroManager.Enemies)
                    {
                        if (!boolLinks["RU" + hero.CharacterName].GetValue<bool>())
                        {
                            ignoredchamps.Add(hero);
                        }
                    }
                    AIHeroClient RTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical, true, ignoredchamps);
                    if (RTarget.IsValidTarget())
                    {
                        R.Cast(RTarget);
                    }
                }

            }
            // Ultimate follow
            if (R.Instance.Name != "ViktorChaosStorm" && boolLinks["AutoFollowR"].GetValue<bool>() && Environment.TickCount - lasttick > 0)
            {
                var stormT = TargetSelector.GetTarget(player, 1100, TargetSelector.DamageType.Magical);
                if (stormT != null)
                {
                    R.Cast(stormT.Position);
                    lasttick = Environment.TickCount + 500;
                }
            }
        }

        private void OnCombo()
        {

            try
            {


                bool useQ = boolLinks["comboUseQ"].GetValue<bool>() && Q.IsReady();
                bool useW = boolLinks["comboUseW"].GetValue<bool>() && W.IsReady();
                bool useE = boolLinks["comboUseE"].GetValue<bool>() && E.IsReady();
                bool useR = boolLinks["comboUseR"].GetValue<bool>() && R.IsReady();

                bool killpriority = boolLinks["spPriority"].GetValue<bool>() && R.IsReady();
                bool rKillSteal = boolLinks["rLastHit"].GetValue<bool>();
                AIHeroClient Etarget = TargetSelector.GetTarget(maxRangeE, TargetSelector.DamageType.Magical);
                AIHeroClient Qtarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                AIHeroClient RTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                if (killpriority && Qtarget != null & Etarget != null && Etarget != Qtarget && ((Etarget.Health > TotalDmg(Etarget, false, true, false, false)) || (Etarget.Health > TotalDmg(Etarget, false, true, true, false) && Etarget == RTarget)) && Qtarget.Health < TotalDmg(Qtarget, true, true, false, false))
                {
                    Etarget = Qtarget;
                }

                if (RTarget != null && rKillSteal && useR && boolLinks["RU" + RTarget.CharacterName].GetValue<bool>())
                {
                    if (TotalDmg(RTarget, true, true, false, false) < RTarget.Health && TotalDmg(RTarget, true, true, true, true) > RTarget.Health)
                    {
                        R.Cast(RTarget.Position);
                    }
                }


                if (useE)
                {
                    if (Etarget != null)
                        PredictCastE(Etarget);
                }
                if (useQ)
                {

                    if (Qtarget != null)
                        Q.Cast(Qtarget);
                }
                if (useW)
                {
                    var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);

                    if (t != null)
                    {
                        if (t.Path.Count() < 2)
                        {
                            if (t.HasBuffOfType(BuffType.Slow))
                            {
                                if (W.GetPrediction(t).Hitchance >= HitChance.VeryHigh)
                                    if (W.Cast(t) == Spell.CastStates.SuccessfullyCasted)
                                        return;
                            }
                            if (t.CountEnemiesInRange(250) > 2)
                            {
                                if (W.GetPrediction(t).Hitchance >= HitChance.VeryHigh)
                                    if (W.Cast(t) == Spell.CastStates.SuccessfullyCasted)
                                        return;
                            }
                        }
                    }
                }
                if (useR && R.Instance.Name == "ViktorChaosStorm" && player.CanCast && !player.Spellbook.IsCastingSpell)
                {

                    foreach (var unit in HeroManager.Enemies.Where(h => h.IsValidTarget(R.Range)))
                    {
                        R.CastIfWillHit(unit, stringLinks["HitR"].GetValue<StringList>().SelectedIndex + 1);

                    }
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
            }
        }

        private static void Flee()
        {
            Orbwalking.MoveTo(Game.CursorPosRaw);
            if (!Q.IsReady() || !(player.HasBuff("viktorqaug") || player.HasBuff("viktorqeaug") || player.HasBuff("viktorqwaug") || player.HasBuff("viktorqweaug")))
            {
                return;
            }
            var closestminion = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly).MinOrDefault(m => player.Distance(m));
            var closesthero = HeroManager.Enemies.MinOrDefault(m => player.Distance(m) < Q.Range);
            if (closestminion.IsValidTarget(Q.Range))
            {
                Q.Cast(closestminion);
            }
            else if (closesthero.IsValidTarget(Q.Range))
            {
                Q.Cast(closesthero);

            }
        }


        private static void OnHarass()
        {
            // Mana check
            if ((player.Mana / player.MaxMana) * 100 < sliderLinks["harassMana"].GetValue<Slider>().Value)
                return;
            bool useE = boolLinks["harassUseE"].GetValue<bool>() && E.IsReady();
            bool useQ = boolLinks["harassUseQ"].GetValue<bool>() && Q.IsReady();
            if (useQ)
            {
                var qtarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (qtarget != null)
                    Q.Cast(qtarget);
            }
            if (useE)
            {
                var harassrange = sliderLinks["eDistance"].GetValue<Slider>().Value;
                var target = TargetSelector.GetTarget(harassrange, TargetSelector.DamageType.Magical);

                if (target != null)
                    PredictCastE(target);
            }
        }

        private static void OnWaveClear()
        {
            // Mana check
            if ((player.Mana / player.MaxMana) * 100 < sliderLinks["waveMana"].GetValue<Slider>().Value)
                return;

            bool useQ = boolLinks["waveUseQ"].GetValue<bool>() && Q.IsReady();
            bool useE = boolLinks["waveUseE"].GetValue<bool>() && E.IsReady();

            if (useQ)
            {
                foreach (var minion in MinionManager.GetMinions(player.Position, player.AttackRange))
                {
                    if (Q.IsKillable(minion) && minion.CharacterData.SkinName.Contains("Siege"))
                    {
                        QLastHit(minion);
                        break;
                    }
                }
            }

            if (useE)
                PredictCastMinionE();
        }

        private static void OnJungleClear()
        {
            // Mana check
            if ((player.Mana / player.MaxMana) * 100 < sliderLinks["waveMana"].GetValue<Slider>().Value)
                return;

            bool useQ = boolLinks["waveUseQ"].GetValue<bool>() && Q.IsReady();
            bool useE = boolLinks["waveUseE"].GetValue<bool>() && E.IsReady();

            if (useQ)
            {
                foreach (var minion in MinionManager.GetMinions(player.Position, player.AttackRange, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth))
                {
                    Q.Cast(minion);
                }
            }

            if (useE)
                PredictCastMinionEJungle();
        }

        public static FarmLocation GetBestLaserFarmLocation(bool jungle)
        {
            var bestendpos = new SharpDX.Vector2();
            var beststartpos = new SharpDX.Vector2();
            var minionCount = 0;
            List<AIBaseClient> allminions;
            var minimalhit = sliderLinks["waveNumE"].GetValue<Slider>().Value;
            if (!jungle)
            {
                allminions = MinionManager.GetMinions(maxRangeE);

            }
            else
            {
                allminions = MinionManager.GetMinions(maxRangeE, MinionTypes.All, MinionTeam.Neutral);
            }
            var minionslist = (from mnion in allminions select mnion.Position.To2D()).ToList<SharpDX.Vector2>();
            var posiblePositions = new List<SharpDX.Vector2>();
            posiblePositions.AddRange(minionslist);
            var max = posiblePositions.Count;
            for (var i = 0; i < max; i++)
            {
                for (var j = 0; j < max; j++)
                {
                    if (posiblePositions[j] != posiblePositions[i])
                    {
                        posiblePositions.Add((posiblePositions[j] + posiblePositions[i]) / 2);
                    }
                }
            }

            foreach (var startposminion in allminions.Where(m => player.Distance(m) < rangeE))
            {
                var startPos = startposminion.Position.To2D();

                foreach (var pos in posiblePositions)
                {
                    if (pos.Distance(startPos, true) <= lengthE * lengthE)
                    {
                        var endPos = startPos + lengthE * (pos - startPos).Normalized();

                        var count =
                            minionslist.Count(pos2 => pos2.Distance(startPos, endPos, true, true) <= 140 * 140);

                        if (count >= minionCount)
                        {
                            bestendpos = endPos;
                            minionCount = count;
                            beststartpos = startPos;
                        }

                    }
                }
            }
            if ((!jungle && minimalhit < minionCount) || (jungle && minionCount > 0))
            {
                //Console.WriteLine("MinimalHits: " + minimalhit + "\n Startpos: " + beststartpos + "\n Count : " + minionCount);
                return new FarmLocation(beststartpos, bestendpos, minionCount);
            }
            else
            {
                return new FarmLocation(beststartpos, bestendpos, 0);
            }
        }



        private static bool PredictCastMinionEJungle()
        {
            var farmLocation = GetBestLaserFarmLocation(true);

            if (farmLocation.MinionsHit > 0)
            {
                CastE(farmLocation.Position1, farmLocation.Position2);
                return true;
            }

            return false;
        }

        public struct FarmLocation
        {
            /// <summary>
            /// The minions hit
            /// </summary>
            public int MinionsHit;

            /// <summary>
            /// The start position
            /// </summary>
            public SharpDX.Vector2 Position1;


            /// <summary>
            /// The end position
            /// </summary>
            public SharpDX.Vector2 Position2;

            /// <summary>
            /// Initializes a new instance of the <see cref="FarmLocation"/> struct.
            /// </summary>
            /// <param name="position">The position.</param>
            /// <param name="minionsHit">The minions hit.</param>
            public FarmLocation(SharpDX.Vector2 startpos, SharpDX.Vector2 endpos, int minionsHit)
            {
                Position1 = startpos;
                Position2 = endpos;
                MinionsHit = minionsHit;
            }
        }
        private static bool PredictCastMinionE()
        {
            var farmLoc = GetBestLaserFarmLocation(false);
            if (farmLoc.MinionsHit > 0)
            {
                Console.WriteLine("Minion amount: " + farmLoc.MinionsHit + "\n Startpos: " + farmLoc.Position1 + "\n EndPos: " + farmLoc.Position2);

                CastE(farmLoc.Position1, farmLoc.Position2);
                return true;
            }

            return false;
        }


        private static void PredictCastE(AIHeroClient target)
        {
            // Helpers
            bool inRange = SharpDX.Vector2.DistanceSquared(target.Position.To2D(), player.Position.To2D()) < E.Range * E.Range;
            PredictionOutput prediction;
            bool spellCasted = false;

            // Positions
            SharpDX.Vector3 pos1, pos2;

            // Champs
            var nearChamps = (from champ in ObjectManager.Get<AIHeroClient>() where champ.IsValidTarget(maxRangeE) && target != champ select champ).ToList();
            var innerChamps = new List<AIHeroClient>();
            var outerChamps = new List<AIHeroClient>();
            foreach (var champ in nearChamps)
            {
                if (SharpDX.Vector2.DistanceSquared(champ.Position.To2D(), player.Position.To2D()) < E.Range * E.Range)
                    innerChamps.Add(champ);
                else
                    outerChamps.Add(champ);
            }

            // Minions
            var nearMinions = MinionManager.GetMinions(player.Position, maxRangeE);
            var innerMinions = new List<AIBaseClient>();
            var outerMinions = new List<AIBaseClient>();
            foreach (var minion in nearMinions)
            {
                if (SharpDX.Vector2.DistanceSquared(minion.Position.To2D(), player.Position.To2D()) < E.Range * E.Range)
                    innerMinions.Add(minion);
                else
                    outerMinions.Add(minion);
            }

            // Main target in close range
            if (inRange)
            {
                // Get prediction reduced speed, adjusted sourcePosition
                E.Speed = speedE * 0.9f;
                E.From = target.Position + (SharpDX.Vector3.Normalize(player.Position - target.Position) * (lengthE * 0.1f));
                prediction = E.GetPrediction(target);
                E.From = player.Position;

                // Prediction in range, go on
                if (prediction.CastPosition.Distance(player.Position) < E.Range)
                    pos1 = prediction.CastPosition;
                // Prediction not in range, use exact position
                else
                {
                    pos1 = target.Position;
                    E.Speed = speedE;
                }

                // Set new sourcePosition
                E.From = pos1;
                E.RangeCheckFrom = pos1;

                // Set new range
                E.Range = lengthE;

                // Get next target
                if (nearChamps.Count > 0)
                {
                    // Get best champion around
                    var closeToPrediction = new List<AIHeroClient>();
                    foreach (var enemy in nearChamps)
                    {
                        // Get prediction
                        prediction = E.GetPrediction(enemy);
                        // Validate target
                        if (prediction.Hitchance >= HitChance.High && SharpDX.Vector2.DistanceSquared(pos1.To2D(), prediction.CastPosition.To2D()) < (E.Range * E.Range) * 0.8)
                            closeToPrediction.Add(enemy);
                    }

                    // Champ found
                    if (closeToPrediction.Count > 0)
                    {
                        // Sort table by health DEC
                        if (closeToPrediction.Count > 1)
                            closeToPrediction.Sort((enemy1, enemy2) => enemy2.Health.CompareTo(enemy1.Health));

                        // Set destination
                        prediction = E.GetPrediction(closeToPrediction[0]);
                        pos2 = prediction.CastPosition;

                        // Cast spell
                        CastE(pos1, pos2);
                        spellCasted = true;
                    }
                }

                // Spell not casted
                if (!spellCasted)
                {
                    CastE(pos1, E.GetPrediction(target).CastPosition);
                }

                // Reset spell
                E.Speed = speedE;
                E.Range = rangeE;
                E.From = player.Position;
                E.RangeCheckFrom = player.Position;
            }

            // Main target in extended range
            else
            {
                // Radius of the start point to search enemies in
                float startPointRadius = 150;

                // Get initial start point at the border of cast radius
                SharpDX.Vector3 startPoint = player.Position + SharpDX.Vector3.Normalize(target.Position - player.Position) * rangeE;

                // Potential start from postitions
                var targets = (from champ in nearChamps where SharpDX.Vector2.DistanceSquared(champ.Position.To2D(), startPoint.To2D()) < startPointRadius * startPointRadius && SharpDX.Vector2.DistanceSquared(player.Position.To2D(), champ.Position.To2D()) < rangeE * rangeE select champ).ToList();
                if (targets.Count > 0)
                {
                    // Sort table by health DEC
                    if (targets.Count > 1)
                        targets.Sort((enemy1, enemy2) => enemy2.Health.CompareTo(enemy1.Health));

                    // Set target
                    pos1 = targets[0].Position;
                }
                else
                {
                    var minionTargets = (from minion in nearMinions where SharpDX.Vector2.DistanceSquared(minion.Position.To2D(), startPoint.To2D()) < startPointRadius * startPointRadius && SharpDX.Vector2.DistanceSquared(player.Position.To2D(), minion.Position.To2D()) < rangeE * rangeE select minion).ToList();
                    if (minionTargets.Count > 0)
                    {
                        // Sort table by health DEC
                        if (minionTargets.Count > 1)
                            minionTargets.Sort((enemy1, enemy2) => enemy2.Health.CompareTo(enemy1.Health));

                        // Set target
                        pos1 = minionTargets[0].Position;
                    }
                    else
                        // Just the regular, calculated start pos
                        pos1 = startPoint;
                }

                // Predict target position
                E.From = pos1;
                E.Range = lengthE;
                E.RangeCheckFrom = pos1;
                prediction = E.GetPrediction(target);

                // Cast the E
                if (prediction.Hitchance >= HitChance.High)
                    CastE(pos1, prediction.CastPosition);

                // Reset spell
                E.Range = rangeE;
                E.From = player.Position;
                E.RangeCheckFrom = player.Position;
            }

        }



        private static void CastE(SharpDX.Vector3 source, SharpDX.Vector3 destination)
        {
            E.Cast(source, destination);
        }

        private static void CastE(SharpDX.Vector2 source, SharpDX.Vector2 destination)
        {
            E.Cast(source, destination);
        }

        private static void Interrupter2_OnInterruptableTarget(ActiveInterrupter interrupter)
        {
            var unit = interrupter.Sender;
            if (interrupter.DangerLevel >= InterrupterDangerLevel.High && unit.IsEnemy)
            {
                var useW = boolLinks["wInterrupt"].GetValue<bool>();
                var useR = boolLinks["rInterrupt"].GetValue<bool>();

                if (useW && W.IsReady() && unit.IsValidTarget(W.Range) &&
                    (Game.Time + 1.5 + W.Delay) >= interrupter.EndTime)
                {
                    if (W.Cast(unit) == Spell.CastStates.SuccessfullyCasted)
                        return;
                }
                else if (useR && unit.IsValidTarget(R.Range) && R.Instance.Name == "ViktorChaosStorm")
                {
                    R.Cast(unit);
                }
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (gapcloser.Sender.IsAlly)
            {
                return;
            }
            if (boolLinks["miscGapcloser"].GetValue<bool>() && W.IsInRange(gapcloser.End) && gapcloser.Sender.IsEnemy)
            {
                GapCloserPos = gapcloser.End;
                if (Geometry.Distance(gapcloser.Start, gapcloser.End) > gapcloser.Sender.Spellbook.GetSpell(gapcloser.Slot).SData.CastRangeDisplayOverride && gapcloser.Sender.Spellbook.GetSpell(gapcloser.Slot).SData.CastRangeDisplayOverride > 100)
                {
                    GapCloserPos = Geometry.Extend(gapcloser.Start, gapcloser.End, gapcloser.Sender.Spellbook.GetSpell(gapcloser.Slot).SData.CastRangeDisplayOverride);
                }
                W.Cast(GapCloserPos.To2D(), true);
            }
        }
        private static void AutoW()
        {
            if (!W.IsReady() || !boolLinks["autoW"].GetValue<bool>())
                return;

            var tPanth = HeroManager.Enemies.Find(h => h.IsValidTarget(W.Range) && h.HasBuff("Pantheon_GrandSkyfall_Jump"));
            if (tPanth != null)
            {
                if (W.Cast(tPanth) == Spell.CastStates.SuccessfullyCasted)
                    return;
            }

            foreach (var enemy in HeroManager.Enemies.Where(h => h.IsValidTarget(W.Range)))
            {
                if (enemy.HasBuff("rocketgrab2"))
                {
                    var t = ObjectManager.Get<AIHeroClient>().Where(i => i.IsAlly).ToList().Find(h => h.CharacterName.ToLower() == "blitzcrank" && h.Distance((AttackableUnit)player) < W.Range);
                    if (t != null)
                    {
                        if (W.Cast(t) == Spell.CastStates.SuccessfullyCasted)
                            return;
                    }
                }
                if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                         enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                         enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Suppression) ||
                         enemy.IsStunned || enemy.IsRecalling())
                {
                    if (W.Cast(enemy) == Spell.CastStates.SuccessfullyCasted)
                        return;
                }
                if (W.GetPrediction(enemy).Hitchance == HitChance.Immobile)
                {
                    if (W.Cast(enemy) == Spell.CastStates.SuccessfullyCasted)
                        return;
                }
            }
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            // All circles
            foreach (var circle in circleLinks.Values.Select(link => link.GetValue<Circle>()))
            {
                if (circle.Active)
                    Render.Circle.DrawCircle(player.Position, circle.Radius, circle.Color);
            }
        }

        private static void ProcessLink(string key, object value)
        {
            if (value is MenuItem)
            {
                MenuItem item = (MenuItem)value;
                try
                {
                    if (item.GetValue<StringList>() != null)
                        stringLinks.Add(key, item);
                }
                catch {
                    try
                    {
                        if (item.GetValue<Circle>() != null)
                            circleLinks.Add(key, item);
                    }
                    catch {
                        try
                        {
                            if (item.GetValue<Slider>() != null)
                                sliderLinks.Add(key, item);
                        }
                        catch {
                            try
                            {
                                if (item.GetValue<KeyBind>() != null)
                                    keyLinks.Add(key, item);
                            }
                            catch
                            {
                                boolLinks.Add(key, item);
                            }
                        }
                    }
                }
            }
               
        }
        private float TotalDmg(AIBaseClient enemy, bool useQ, bool useE, bool useR, bool qRange)
        {
            var qaaDmg = new Double[] { 20, 40, 60, 80, 100 };
            var damage = 0d;
            var rTicks = sliderLinks["rTicks"].GetValue<Slider>().Value;
            bool inQRange = ((qRange && Orbwalking.InAutoAttackRange(enemy)) || qRange == false);
            //Base Q damage
            if (useQ && Q.IsReady() && inQRange)
            {
                damage += player.GetSpellDamage(enemy, SpellSlot.Q);
                damage += player.CalcDamage(enemy, Damage.DamageType.Magical, qaaDmg[Q.Level - 1] + 0.5 * player.TotalMagicalDamage + player.TotalAttackDamage);
            }

            // Q damage on AA
            if (useQ && !Q.IsReady() && player.HasBuff("viktorpowertransferreturn") && inQRange)
            {
                damage += player.CalcDamage(enemy, Damage.DamageType.Magical, qaaDmg[Q.Level - 1] + 0.5 * player.TotalMagicalDamage + player.TotalAttackDamage);
            }

            //E damage
            if (useE && E.IsReady())
            {
                if (player.HasBuff("viktoreaug") || player.HasBuff("viktorqeaug") || player.HasBuff("viktorqweaug"))
                    damage += player.GetSpellDamage(enemy, SpellSlot.E, 1);
                else
                    damage += player.GetSpellDamage(enemy, SpellSlot.E);
            }

            //R damage + 2 ticks
            if (useR && R.Level > 0 && R.IsReady() && R.Instance.Name == "ViktorChaosStorm")
            {
                damage += player.GetSpellDamage(enemy, SpellSlot.R, 1) * rTicks;
                damage += player.GetSpellDamage(enemy, SpellSlot.R);
            }

            // Ludens Echo damage
            if (Items.HasItem(3285))
                damage += player.CalcDamage(enemy, Damage.DamageType.Magical, 100 + player.FlatMagicDamageMod * 0.1);

            //sheen damage
            if (Items.HasItem(3057))
                damage += player.CalcDamage(enemy, Damage.DamageType.Physical, 0.5 * player.BaseAttackDamage);

            //lich bane dmg
            if (Items.HasItem(3100))
                damage += player.CalcDamage(enemy, Damage.DamageType.Magical, 0.5 * player.FlatMagicDamageMod + 0.75 * player.BaseAttackDamage);

            return (float)damage;
        }
        private float GetComboDamage(AIBaseClient enemy)
        {

            return TotalDmg(enemy, true, true, true, false);
        }
        private void SetupMenu()
        {

            menu = new Menu("DH.Viktor credit Vasilyi", "Viktor", true);
            // Combo
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            menu.AddSubMenu(targetSelectorMenu);

            menu.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(menu.SubMenu("Orbwalking"));
            var subMenu = menu.AddSubMenu(new Menu("Combo", "Combo"));

            
            ProcessLink("comboUseQ", subMenu.AddItem(new MenuItem("comboUseQ", "Use Q").SetValue(true)));
            ProcessLink("comboUseW", subMenu.AddItem(new MenuItem("comboUseW", "Use W").SetValue(true)));
            ProcessLink("comboUseE", subMenu.AddItem(new MenuItem("comboUseE", "Use E").SetValue(true)));
            ProcessLink("comboUseR", subMenu.AddItem(new MenuItem("comboUseR", "Use R").SetValue(true)));
            ProcessLink("qAuto", subMenu.AddItem(new MenuItem("qAuto", "Dont autoattack without passive").SetValue(true)));
            ProcessLink("comboActive", subMenu.AddItem(new MenuItem("comboActive", "Combo active").SetValue(new KeyBind(32, KeyBindType.Press))));

            subMenu = menu.AddSubMenu(new Menu("R config", "R config"));
            ProcessLink("HitR", subMenu.AddItem(new MenuItem("HitR", "Auto R if: ").SetValue(new StringList(new string[] { "1 target", "2 targets", "3 targets", "4 targets", "5 targets" }, 3))));
            ProcessLink("AutoFollowR", subMenu.AddItem(new MenuItem("AutoFollowR", "Auto Follow R").SetValue(true)));
            ProcessLink("rTicks", subMenu.AddItem(new MenuItem("rTicks", "Ultimate ticks to count").SetValue(new Slider(2, 1, 14))));


            subMenu = subMenu.AddSubMenu(new Menu("R one target", "R one target"));
            ProcessLink("forceR", subMenu.AddItem(new MenuItem("forceR", "Force R on target").SetValue(new KeyBind(84, KeyBindType.Press))));
            ProcessLink("rLastHit", subMenu.AddItem(new MenuItem("rLastHit", "1 target ulti").SetValue(true)));
            foreach (var hero in HeroManager.Enemies)
            {
                ProcessLink("RU" + hero.CharacterName, subMenu.AddItem(new MenuItem("RU" + hero.CharacterName, "Use R on: " + hero.CharacterName).SetValue(true)));
            }


            subMenu = menu.AddSubMenu(new Menu("Test features", "Test features"));
            ProcessLink("spPriority", subMenu.AddItem(new MenuItem("spPriority", "Prioritize kill over dmg").SetValue(true)));


            // Harass
            subMenu = menu.AddSubMenu(new Menu("Harass", "Harass"));
            ProcessLink("harassUseQ", subMenu.AddItem(new MenuItem("harassUseQ", "Use Q").SetValue(true)));
            ProcessLink("harassUseE", subMenu.AddItem(new MenuItem("harassUseE", "Use E").SetValue(true)));
            ProcessLink("harassMana", subMenu.AddItem(new MenuItem("harassMana", "Mana usage in percent (%)").SetValue(new Slider(30))));
            ProcessLink("eDistance", subMenu.AddItem(new MenuItem("eDistance", "Harass range with E").SetValue(new Slider(maxRangeE, rangeE, maxRangeE))));
            ProcessLink("harassActive", subMenu.AddItem(new MenuItem("harassActive", "Harass active").SetValue(new KeyBind('C', KeyBindType.Press))));

            // WaveClear
            subMenu = menu.AddSubMenu(new Menu("WaveClear", "WaveClear"));
            ProcessLink("waveUseQ", subMenu.AddItem(new MenuItem("waveUseQ", "Use Q").SetValue(true)));
            ProcessLink("waveUseE", subMenu.AddItem(new MenuItem("waveUseE", "Use E").SetValue(true)));
            ProcessLink("waveNumE", subMenu.AddItem(new MenuItem("waveNumE", "Minions to hit with E").SetValue(new Slider(2, 1, 10))));
            ProcessLink("waveMana", subMenu.AddItem(new MenuItem("waveMana", "Mana usage in percent (%)").SetValue(new Slider(30))));
            ProcessLink("waveActive", subMenu.AddItem(new MenuItem("waveActive", "WaveClear active").SetValue(new KeyBind('V', KeyBindType.Press))));
            ProcessLink("jungleActive", subMenu.AddItem(new MenuItem("jungleActive", "JungleClear active").SetValue(new KeyBind('G', KeyBindType.Press))));

            subMenu = menu.AddSubMenu(new Menu("LastHit", "LastHit"));
            ProcessLink("waveUseQLH", subMenu.AddItem(new MenuItem("waveUseQLH", "Use Q").SetValue(new KeyBind('A', KeyBindType.Press))));

            // Harass
            subMenu = menu.AddSubMenu(new Menu("Flee", "Flee"));
            ProcessLink("FleeActive", subMenu.AddItem(new MenuItem("FleeActive", "Flee mode").SetValue(new KeyBind('Z', KeyBindType.Press))));

            // Misc
            subMenu = menu.AddSubMenu(new Menu("Misc", "Misc"));
            ProcessLink("rInterrupt", subMenu.AddItem(new MenuItem("rInterrupt", "Use R to interrupt dangerous spells").SetValue(true)));
            ProcessLink("wInterrupt", subMenu.AddItem(new MenuItem("wInterrupt", "Use W to interrupt dangerous spells").SetValue(true)));
            ProcessLink("autoW", subMenu.AddItem(new MenuItem("autoW", "Use W to continue CC").SetValue(true)));
            ProcessLink("miscGapcloser", subMenu.AddItem(new MenuItem("miscGapcloser", "Use W against gapclosers").SetValue(true)));

            // Drawings
            subMenu = menu.AddSubMenu(new Menu("Drawings", "Drawings"));
            ProcessLink("drawRangeQ", subMenu.AddItem(new MenuItem("drawRangeQ", "Q range").SetValue(new Circle(true, Color.FromArgb(150, Color.IndianRed), Q.Range))));
            ProcessLink("drawRangeW", subMenu.AddItem(new MenuItem("drawRangeW", "W range").SetValue(new Circle( true, Color.FromArgb(150, Color.IndianRed), W.Range))));
            ProcessLink("drawRangeE", subMenu.AddItem(new MenuItem("drawRangeE", "E range").SetValue(new Circle(false, Color.FromArgb(150, Color.DarkRed), E.Range))));
            ProcessLink("drawRangeEMax", subMenu.AddItem(new MenuItem("drawRangeEMax", "E max range").SetValue(new Circle(true, Color.FromArgb(150, Color.OrangeRed), maxRangeE))));
            ProcessLink("drawRangeR", subMenu.AddItem(new MenuItem("drawRangeR", "R range").SetValue(new Circle(false, Color.FromArgb(150, Color.Red), R.Range))));
            ProcessLink("dmgdraw", subMenu.AddItem(new MenuItem("dmgdraw", "Draw dmg on healthbar").SetValue(true)));
            var dmgAfterComboItem = menu.SubMenu("Dmg Drawing").AddItem(new MenuItem("dmgdraw", "Draw dmg on healthbar").SetValue(true));
            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = boolLinks["dmgdraw"].GetValue<bool>();
            dmgAfterComboItem.ValueChanged += delegate (object sender, OnValueChangeEventArgs eventArgs)
            {
                Console.WriteLine("menu changed");
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };



        }
    }
}
