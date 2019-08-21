using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using SPrediction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility = EnsoulSharp.SDK.Utility;
using DaoHungAIO.Helpers;

namespace DaoHungAIO.Champions
{
    internal class Azir
    {
        private const int _soldierAARange = 250;
        public static AIHeroClient Player { get; set; }
        public static Menu Menu { get; set; }

        public static Spell Q { get; set; }
        public static Spell Qline { get; set; }
        public static Spell W { get; set; }
        public static Spell E { get; set; }
        public static Spell R { get; set; }

        public static SpellSlot IgniteSlot;

        private static int _allinT = 0;

        public Azir()
        {
            Player = ObjectManager.Player;

            #region Spells
            Q = new Spell(SpellSlot.Q, 825);
            Qline = new Spell(SpellSlot.Q, 825);

            W = new Spell(SpellSlot.W, 450);
            E = new Spell(SpellSlot.E, 1250);
            R = new Spell(SpellSlot.R, 450);

            Q.SetSkillshot(0, 70, 1600, false, false, SkillshotType.Circle);
            Qline.SetSkillshot(0, 70, 1600, false, false, SkillshotType.Line);
            E.SetSkillshot(0, 100, 1700, false, false, SkillshotType.Line);
            R.SetSkillshot(0.5f, 0, 1400, false, false, SkillshotType.Line);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");
            #endregion

            #region Menu
            Menu = new Menu("Azir", "Azir", true);

            Menu Combo = new Menu("Combo", "Combo");
            Combo.Add(new MenuBool("UseQC", "Use Q"));
            Combo.Add(new MenuBool("UseWC", "Use W"));
            Combo.Add(new MenuBool("UseEC", "Use E"));
            Combo.Add(new MenuBool("UseRC", "Use R"));
            Combo.Add(new MenuBool("UseIgnite", "Use Ignite"));
            Combo.Add(new MenuKeyBind("AllInKEK", "All-in (tap)!", System.Windows.Forms.Keys.A, KeyBindType.Press)).Permashow();
            Combo.Add(new MenuKeyBind("ComboActive", "Combo!", System.Windows.Forms.Keys.Space, KeyBindType.Press)).Permashow();
            Menu.Add(Combo);

            Menu Harass = new Menu("Harass", "Harass");
            Harass.Add(new MenuSlider("HarassMinMana", "Min mana %", 20, 0, 100));
            Harass.Add(new MenuKeyBind("HarassActive", "Harass!", System.Windows.Forms.Keys.C, KeyBindType.Press)).Permashow();
            Menu.Add(Harass);

            Menu LaneClear = new Menu("LaneClear", "LaneClear");
            LaneClear.Add(new MenuBool("UseQLC", "Use Q"));
            LaneClear.Add(new MenuBool("UseWLC", "Use W"));
            LaneClear.Add(new MenuKeyBind("LaneClearActive", "LaneClear!", System.Windows.Forms.Keys.S, KeyBindType.Press)).Permashow();
            Menu.Add(LaneClear);

            Menu Misc = new Menu("Misc", "Misc");
            Misc.Add(new MenuKeyBind("Jump", "Jump towards cursor", System.Windows.Forms.Keys.Z, KeyBindType.Press)).Permashow();
            Misc.Add(new MenuBool("AutoEInterrupt", "Interrupt targets with E", false));
            Menu.Add(Misc);

            Menu AzirR = new Menu("AzirR", "AzirR");
            AzirR.Add(new MenuSlider("AutoRN", "Auto R if it will hit >=", 3, 1, 6));
            AzirR.Add(new MenuBool("AutoRInterrupt", "Interrupt targets with R"));
            Menu.Add(AzirR);

            Menu Credit = new Menu("Credit", "Credit iMed as Esk0r");
            Menu.Add(Credit);
            //var dmgAfterComboItem = new MenuBool("DamageAfterR", "Draw damage after combo");
            //EnsoulSharp.SDK.Utility.HpBarDamageIndicator.DamageToUnit += hero => GetComboDamage(hero);
            //Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem;
            //dmgAfterComboItem.ValueChanged += delegate (object sender, OnValueChangeEventArgs eventArgs)
            //{
            //    Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            //};

            Menu Drawings = new Menu("Drawings", "Drawings");
            Drawings.Add(new MenuBool("QRange", "Q range"));
            Drawings.Add(new MenuBool("WRange", "W range"));
            Drawings.Add(new MenuBool("RRange", "R range"));
            //Drawings.Add(dmgAfterComboItem);
            Menu.Add(Drawings);

            Menu.Attach();
            #endregion
            Interrupter.OnInterrupterSpell += Interrupter_OnInterruptableTarget;
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        //static void Program_ValueChanged(object sender, OnValueChangeEventArgs e)
        //{
        //    if (e.GetNewValue<KeyBind>().Active)
        //    {
        //        Jumper.Jump();
        //    }
        //}
        //public AttackableUnit GetTarget()
        //{

        //    AttackableUnit result;
        //    if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear || Orbwalker.ActiveMode == OrbwalkerMode.Harass ||
        //        Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
        //    {
        //        foreach (var minion in
        //            ObjectManager.Get<AIMinionClient>()
        //                .Where(
        //                    minion =>
        //                        minion.IsValidTarget() &&
        //                        minion.Health <
        //                        3 *
        //                        (ObjectManager.Player.BaseAttackDamage + ObjectManager.Player.FlatPhysicalDamageMod))
        //            )
        //        {
        //            var r = CustomInAutoattackRange(minion);
        //            if (r != 0)
        //            {
        //                var t = (int)(Player.AttackCastDelay * 1000) - 100 + Game.Ping / 2;
        //                var predHealth = HealthPrediction.GetPrediction(minion, t, 0);

        //                var damage = (r == 1) ? Player.GetAutoAttackDamage(minion) : Player.GetSpellDamage(minion, SpellSlot.W);
        //                if (minion.Team != GameObjectTeam.Neutral && minion.IsMinion)
        //                {
        //                    if (predHealth > 0 && predHealth <= damage)
        //                    {
        //                        return minion;
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    if (Orbwalker.ActiveMode != OrbwalkerMode.LastHit)
        //    {
        //        var posibleTargets = new Dictionary<AIBaseClient, float>();
        //        var autoAttackTarget = TargetSelector.GetTarget(-1);
        //        if (autoAttackTarget.IsValidTarget())
        //        {
        //            posibleTargets.Add(autoAttackTarget, GetDamageValue(autoAttackTarget, false));
        //        }

        //        foreach (var soldier in SoldiersManager.ActiveSoldiers)
        //        {
        //            var soldierTarget = TargetSelector.GetTarget(_soldierAARange + 65 + 65, true, null, soldier.Position);
        //            if (soldierTarget.IsValidTarget())
        //            {
        //                if (posibleTargets.ContainsKey(soldierTarget))
        //                {
        //                    posibleTargets[soldierTarget] *= 1.25f;
        //                }
        //                else
        //                {
        //                    posibleTargets.Add(soldierTarget, GetDamageValue(soldierTarget, true));
        //                }
        //            }
        //        }

        //        if (posibleTargets.Count > 0)
        //        {
        //            return posibleTargets.MinOrDefault(p => p.Value).Key;
        //        }
        //        var soldiers = SoldiersManager.ActiveSoldiers;
        //        if (soldiers.Count > 0)
        //        {
        //            var minions = MinionManager.GetMinions(1100, MinionTypes.All, MinionTeam.NotAlly);
        //            var validEnemiesPosition = HeroManager.Enemies.Where(e => e.IsValidTarget(1100)).Select(e => e.Position.ToVector2()).ToList();
        //            const int AAWidthSqr = 100 * 100;
        //            //Try to harass using minions
        //            foreach (var soldier in soldiers)
        //            {
        //                foreach (var minion in minions)
        //                {
        //                    var soldierAArange = _soldierAARange + 65 + minion.BoundingRadius;
        //                    soldierAArange *= soldierAArange;
        //                    if (soldier.Distance(minion, true) < soldierAArange)
        //                    {
        //                        var p1 = minion.Position.ToVector2();
        //                        var p2 = soldier.Position.ToVector2().Extend(minion.Position.ToVector2(), 375);
        //                        foreach (var enemyPosition in validEnemiesPosition)
        //                        {
        //                            if (enemyPosition.Distance(p1, p2, true, true) < AAWidthSqr)
        //                            {
        //                                return minion;
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    /* turrets / inhibitors / nexus */
        //    if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
        //    {
        //        /* turrets */
        //        foreach (var turret in
        //            ObjectManager.Get<AITurretClient>().Where(t => t.IsValidTarget() && t.InAutoAttackRange()))
        //        {
        //            return turret;
        //        }

        //        /* inhibitor */
        //        foreach (var turret in
        //            ObjectManager.Get<BarracksDampenerClient>().Where(t => t.IsValidTarget() && t.InAutoAttackRange()))
        //        {
        //            return turret;
        //        }

        //        /* nexus */
        //        foreach (var nexus in
        //            ObjectManager.Get<HQClient>().Where(t => t.IsValidTarget() && t.InAutoAttackRange()))
        //        {
        //            return nexus;
        //        }
        //    }

        //    /*Jungle minions*/
        //    if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear || Orbwalker.ActiveMode == OrbwalkerMode.Harass)
        //    {
        //        result =
        //            ObjectManager.Get<AIMinionClient>()
        //                .Where(
        //                    mob =>
        //                        mob.IsValidTarget() && mob.InAutoAttackRange() && mob.Team == GameObjectTeam.Neutral)
        //                .MaxOrDefault(mob => mob.MaxHealth);
        //        if (result != null)
        //        {
        //            return result;
        //        }
        //    }

        //    if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
        //    {
        //        return (ObjectManager.Get<AIMinionClient>().Where(minion => minion.IsValidTarget() && InAutoAttackRange(minion))).MaxOrDefault(m => CustomInAutoattackRange(m) * m.Health);
        //    }

        //    return null;
        //}

         static void Interrupter_OnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (args.DangerLevel != Interrupter.DangerLevel.High)
            {
                return;
            }

            if (Menu["Misc"].GetValue<MenuBool>("AutoEInterrupt") && E.IsReady())
            {
                foreach (var soldier in SoldiersManager.AllSoldiers.Where(s => Player.Distance(s) < E.Range))
                {
                    if (E.WillHit(sender, soldier.Position))
                    {
                        E.Cast(soldier.Position);
                        return;
                    }
                }
                return;
            }

            if (Menu["AzirR"].GetValue<MenuBool>("AutoRInterrupt") && R.IsReady())
            {
                var dist = Player.Distance(sender);

                if (dist < R.Range)
                {
                    R.Cast(sender, false, true);
                    return;
                }

                if (dist < Math.Pow(Math.Sqrt(R.Range + Math.Pow(R.Width + sender.BoundingRadius, 2)), 2))
                {
                    var angle = (float)Math.Atan(R.Width + sender.BoundingRadius / R.Range);
                    var p = (sender.Position.ToVector2() - Player.Position.ToVector2()).Rotated(angle);
                    R.Cast(p);
                }
            }
        }

        static float GetComboDamage(AIBaseClient target)
        {
            var damage = 0d;
            if (Q.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.Q);
            }

            if (E.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.E);
            }

            if (R.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.R);
            }

            if (IgniteSlot != SpellSlot.Unknown && Player.Spellbook.GetSpell(IgniteSlot).State == SpellState.Ready)
            {
                damage += Player.GetSummonerSpellDamage(target, SummonerSpell.Ignite);
            }

            damage += SoldiersManager.ActiveSoldiers.Count * Player.GetSpellDamage(target, SpellSlot.W);

            return (float)damage;
        }

        static void LaneClear()
        {
            var useQ = Menu["LaneClear"].GetValue<MenuBool>("UseQLC");
            var useW = Menu["LaneClear"].GetValue<MenuBool>("UseWLC");
            var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range)).ToList();
            if (minions.Count() == 0)
            {
                minions = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth).ToList();
            }

            if (minions.Count() > 0)
            {
                if (useW && W.Instance.Ammo > 0 && (minions.Count() > 2 || minions[0].Team == GameObjectTeam.Neutral))
                {
                    var p = Player.Position.ToVector2().Extend(minions[0].Position.ToVector2(), W.Range);
                    W.Cast(p);
                    return;
                }

                if (useQ && Qline.IsReady() && (minions.Count() >= 2 || minions[0].Team == GameObjectTeam.Neutral))
                {
                    var positions = new Dictionary<Vector3, int>();

                    foreach (var soldier in SoldiersManager.AllSoldiers)
                    {
                        Qline.UpdateSourcePosition(soldier.Position, ObjectManager.Player.Position);
                        foreach (var minion in minions)
                        {
                            var hits = Qline.GetLineFarmLocation(minions).MinionsHit;
                            if (hits >= 2 || minions[0].Team == GameObjectTeam.Neutral)
                            {
                                if (!positions.ContainsKey(minion.Position))
                                {
                                    positions.Add(minion.Position, hits);
                                }
                            }
                        }
                    }

                    if (positions.Count > 0)
                    {
                        Qline.Cast(positions.MaxOrDefault(k => k.Value).Key);
                    }
                }
                return;
            }
        }

        static void Harass()
        {
            var harassTarget = TargetSelector.GetTarget(Q.Range);
            if (harassTarget == null)
            {
                return;
            }

            if (W.Instance.Ammo > 0)
            {
                var p = Player.Position.ToVector2().Extend(harassTarget.Position.ToVector2(), W.Range);
                if (Q.IsReady() || GameObjects.EnemyHeroes.Any(h => h.IsValidTarget(W.Range + 200)))
                {
                    W.Cast(p);
                }
                return;
            }

            if (Q.IsReady())
            {
                var qTarget = TargetSelector.GetTarget(Q.Range);
                if (qTarget != null)
                {
                    foreach (var soldier in SoldiersManager.AllSoldiers)
                    {
                        Q.UpdateSourcePosition(soldier.Position, ObjectManager.Player.Position);
                        Q.Cast(qTarget);
                    }
                }
            }
        }

        static void Combo()
        {
            var useQ = Menu["Combo"].GetValue<MenuBool>("UseQC");
            var useW = Menu["Combo"].GetValue<MenuBool>("UseWC");
            var useE = Menu["Combo"].GetValue<MenuBool>("UseEC");
            var useR = (Variables.TickCount - _allinT < 4000) && Menu["Combo"].GetValue<MenuBool>("UseRC");

            var qTarget = TargetSelector.GetTarget(Q.Range + 200);
            if (qTarget == null)
            {
                return;
            }

            if (useQ && Q.IsReady())
            {
                foreach (var soldier in SoldiersManager.AllSoldiers)
                {
                    Q.UpdateSourcePosition(soldier.Position, ObjectManager.Player.Position);
                    Q.Cast(qTarget);
                    return;
                }
            }

            if (useW && W.Instance.Ammo > 0)
            {
                var p = Player.Distance(qTarget) > W.Range ? Player.Position.ToVector2().Extend(qTarget.Position.ToVector2(), W.Range) : qTarget.Position.ToVector2();
                W.Cast(p);
                return;
            }

            if (useE && ((Variables.TickCount - _allinT) < 4000 || (GameObjects.EnemyHeroes.Count(e => e.IsValidTarget(1000)) <= 2 && GetComboDamage(qTarget) > qTarget.Health)) && E.IsReady())
            {
                foreach (var soldier in SoldiersManager.AllSoldiers2.Where(s => Player.Distance(s) < E.Range))
                {
                    if (E.WillHit(qTarget, soldier.Position))
                    {
                        E.Cast(soldier.Position);
                        return;
                        return;
                    }
                }
            }

            if (GetComboDamage(qTarget) > qTarget.Health)
            {
                if (useR && R.IsReady())
                {
                    R.Cast(qTarget, false, true);
                    return;
                }

                if (Menu["Combo"].GetValue<MenuBool>("UseIgnite") && IgniteSlot != SpellSlot.Unknown && EnsoulSharp.Player.GetSpell(IgniteSlot).State == SpellState.Ready && Player.Distance(qTarget) < 600 * 600)
                {
                    Player.Spellbook.CastSpell(IgniteSlot, qTarget);
                    return;
                }
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            R.Width = 133 * (3 + R.Level);

            var minTargets = Menu["AzirR"].GetValue<MenuSlider>("AutoRN").Value;

            var target = GameObjects.EnemyHeroes.Where(x => x.InAutoAttackRange()).FirstOrDefault();

            if (minTargets != 6)
            {
                R.CastIfWillHit(R.GetTarget(), minTargets);
            }
            //if (Orbwalker.GetTarget() == null)
            //{
            //    var soldiers = SoldiersManager.ActiveSoldiers;
            //    if (soldiers.Count > 0)
            //    {
            //        var minions = MinionManager.GetMinions(1100, MinionTypes.All, MinionTeam.NotAlly);
            //        var validEnemiesPosition = HeroManager.Enemies.Where(e => e.IsValidTarget(1100)).Select(e => e.Position.ToVector2()).ToList();
            //        const int AAWidthSqr = 100 * 100;
            //        //Try to harass using minions
            //        foreach (var soldier in soldiers)
            //        {
            //            foreach (var minion in minions)
            //            {
            //                var soldierAArange = _soldierAARange + 65 + minion.BoundingRadius;
            //                soldierAArange *= soldierAArange;
            //                if (soldier.Distance(minion) < soldierAArange)
            //                {
            //                    var p1 = minion.Position.ToVector2();
            //                    var p2 = soldier.Position.ToVector2().Extend(minion.Position.ToVector2(), 375);
            //                    foreach (var enemyPosition in validEnemiesPosition)
            //                    {
            //                        if (enemyPosition.Distance(p1, p2, true, true) < AAWidthSqr)
            //                        {
            //                            return minion;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}

            if (Menu["Combo"].GetValue<MenuKeyBind>("AllInKEK").Active)
            {
                _allinT = Variables.TickCount;
            }

            if (Menu["Harass"].GetValue<MenuKeyBind>("HarassActive").Active && Player.ManaPercent > Menu["Harass"].GetValue<MenuSlider>("HarassMinMana").Value)
            {
                Harass();
                return;
            }

            if (Menu["Combo"].GetValue<MenuKeyBind>("ComboActive").Active)
            {
                Combo();
                return;
            }

            if (Menu["LaneClear"].GetValue<MenuKeyBind>("LaneClearActive").Active)
            {
                LaneClear();
                return;
            }

            if (Menu["Misc"].GetValue<MenuKeyBind>("Jump").Active)
            {
                Jumper.Jump();
                return;
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var qCircle = Menu["Drawings"].GetValue<MenuBool>("QRange");
            if (qCircle)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.LightGray);
            }

            var wCircle = Menu["Drawings"].GetValue<MenuBool>("WRange");
            if (wCircle)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, System.Drawing.Color.LightGray);
            }

            var rCircle = Menu["Drawings"].GetValue<MenuBool>("RRange");
            if (rCircle)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, System.Drawing.Color.LightGray);
            }
        }
    }

    internal static class Jumper
    {
        private static int CastQT = 0;
        private static Vector2 CastQLocation = new Vector2();

        private static int CastET = 0;
        private static Vector2 CastELocation = new Vector2();

        static Jumper()
        {
            AIBaseClient.OnProcessSpellCast += AIBaseClient_OnProcessSpellCast;
        }

        static void AIBaseClient_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name == "AzirE" && Variables.TickCount - CastQT < 500)
                {
                    Azir.Q.Cast(CastQLocation, true);
                    CastQT = 0;
                }

                if (args.SData.Name == "AzirQ" && Variables.TickCount - CastET < 500)
                {
                    Azir.E.Cast(CastELocation, true);
                    CastET = 0;
                }
            }
        }

        public static void Jump()
        {
            if (Azir.E.IsReady())
            {
                var extended = ObjectManager.Player.Position.ToVector2().Extend(Game.CursorPosRaw.ToVector2(), Azir.Q.Range - 25);

                if (Azir.W.IsReady() && (SoldiersManager.AllSoldiers2.Count == 0 || Azir.Q.Instance.State == SpellState.Cooldown && SoldiersManager.AllSoldiers2.Min(s => s.Distance(extended)) >= Azir.Player.Distance(extended)))
                {
                    Azir.W.Cast(extended);

                    if (Azir.Q.Instance.State != SpellState.Cooldown)
                    {
                        var extended2 = ObjectManager.Player.Position.ToVector2().Extend(Game.CursorPosRaw.ToVector2(), Azir.W.Range);
                        if (extended2.IsWall())
                        {
                            Utility.DelayAction.Add(250, () => Azir.Q.Cast(extended, true));
                            CastET = Variables.TickCount + 250;
                            CastELocation = extended;
                        }
                        else
                        {
                            Utility.DelayAction.Add(250, () => Azir.E.Cast(extended, true));
                            CastQT = Variables.TickCount + 250;
                            CastQLocation = extended;
                        }
                    }
                    else
                    {
                        Utility.DelayAction.Add(100, () => Azir.E.Cast(extended, true));
                    }
                    return;
                }

                if (SoldiersManager.AllSoldiers2.Count > 0 && Azir.Q.IsReady())
                {
                    var closestSoldier = SoldiersManager.AllSoldiers2.MinOrDefault(s => s.Distance(extended));
                    if (closestSoldier.Distance(extended) < ObjectManager.Player.Distance(extended) && ObjectManager.Player.Distance(closestSoldier) > Azir.W.Range)
                    {
                        Utility.DelayAction.Add(250, () => Azir.E.Cast(extended, true));
                        CastQT = Variables.TickCount + 250;
                        CastQLocation = extended;
                    }
                    else
                    {
                        Utility.DelayAction.Add(250, () => Azir.Q.Cast(extended, true));
                        Utility.DelayAction.Add(600, () => Azir.E.Cast(extended, true));
                    }
                }
            }
        }
    }

    internal static class SoldiersManager
    {
        private static List<AIMinionClient> _soldiers = new List<AIMinionClient>();
        private static Dictionary<int, string> Animations = new Dictionary<int, string>();
        private const bool DrawSoldiers = true;

        public static List<AIMinionClient> ActiveSoldiers
        {
            get { return _soldiers.Where(s => s.IsValid && !s.IsDead && !s.IsMoving && (!Animations.ContainsKey((int)s.NetworkId) || Animations[(int)s.NetworkId] != "Inactive")).ToList(); }
        }

        public static List<AIMinionClient> AllSoldiers2
        {
            get { return _soldiers.Where(s => s.IsValid && !s.IsDead).ToList(); }
        }

        public static List<AIMinionClient> AllSoldiers
        {
            get { return _soldiers.Where(s => s.IsValid && !s.IsDead && !s.IsMoving).ToList(); }
        }

        static SoldiersManager()
        {
            AIMinionClient.OnCreate += AIMinionClient_OnCreate;
            AIMinionClient.OnDelete += AIMinionClient_OnDelete;
            AIMinionClient.OnPlayAnimation += AIMinionClient_OnPlayAnimation;

            if (DrawSoldiers)
            {
                Drawing.OnDraw += Drawing_OnDraw;
            }
        }

        static void AIMinionClient_OnPlayAnimation(GameObject sender, AIBaseClientPlayAnimationEventArgs args)
        {
            if (sender is AIMinionClient && ((AIMinionClient)sender).IsSoldier())
            {
                Animations[(int)sender.NetworkId] = args.Animation;
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var soldier in ActiveSoldiers)
            {
                Render.Circle.DrawCircle(soldier.Position, 320, System.Drawing.Color.FromArgb(150, System.Drawing.Color.Yellow));
            }
        }

        private static bool IsSoldier(this AIMinionClient soldier)
        {
            return soldier.IsAlly && String.Equals(soldier.CharacterName, "azirsoldier", StringComparison.InvariantCultureIgnoreCase);
        }

        static void AIMinionClient_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender is AIMinionClient && ((AIMinionClient)sender).IsSoldier())
            {
                _soldiers.Add((AIMinionClient)sender);
            }
        }

        static void AIMinionClient_OnDelete(GameObject sender, EventArgs args)
        {
            _soldiers.RemoveAll(s => s.NetworkId == sender.NetworkId);
            Animations.Remove((int)sender.NetworkId);
        }
    }
}
