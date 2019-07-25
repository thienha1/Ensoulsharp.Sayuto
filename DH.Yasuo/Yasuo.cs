using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DH.Yasuo
{
    internal class Yasuo : Helper
    {
        public AIHeroClient CurrentTarget;
        public bool Fleeing;
        public Yasuo()
        {
            GameEvent.OnGameLoad += OnLoad;
        }

        void OnLoad()
        {
            Yasuo = ObjectManager.Player;

            if (Yasuo.CharacterName != "Yasuo")
            {
                return;
            }

            Chat.PrintChat("<font color='#1d87f2'>YasuoPro by Seph Loaded. Good Luck!</font>");
            Chat.PrintChat("<font color='#12FA54'>::::Latest Update: Reworked Waveclear due to complaints & Combo improvements</font>");
            Chat.PrintChat("<font color='#1d87f2'>::::Any Issues/Recommendations - Post On Topic</font>");
            InitItems();
            InitSpells();
            YasuoMenu.Init(this);
            if (GetBool("Misc", "Misc.Walljump") && Game.MapId == GameMapId.SummonersRift)
            {
                WallJump.Initialize();
            }
            shop = ObjectManager.Get<ShopClient>().FirstOrDefault(x => x.IsAlly);
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Gapcloser.OnGapcloser += OnGapClose;
            Interrupter.OnInterrupterSpell += OnInterruptable;
            AIBaseClient.OnProcessSpellCast += TargettedDanger.SpellCast;
            Dash.OnDash += UnitOnOnDash;
        }


        void OnUpdate(EventArgs args)
        {
            if (Yasuo.IsDead || Yasuo.IsRecalling())
            {
                return;
            }

            CastUlt();

            if (GetBool("Evade", "Evade.WTS"))
            {
                TargettedDanger.OnUpdate();
            }

            var omode = Orbwalker.ActiveMode;

            if (GetBool("Misc", "Misc.AutoStackQ") && omode != OrbwalkerMode.Combo && GetKeyBind("Flee", "Flee.KB") && !TornadoReady && !CurrentTarget.IsValidEnemy(Spells[Q].Range) && !Yasuo.IsDashing() && !InDash)
            {
                var closest =
                    ObjectManager.Get<AIMinionClient>()
                        .Where(x => x.IsValidMinion(Spells[Q].Range) && (x.IsMinion || x.CharacterName.Equals("Sru_Crab"))).OrderBy(x => x.Distance(Yasuo))
                        .FirstOrDefault();
                if (closest != null)
                {
                    var pred = Spells[Q].GetPrediction(closest);
                    if (pred.Hitchance >= HitChance.Low)
                    {
                        Spells[Q].Cast(pred.CastPosition);
                    }
                }
            }

            if (GetBool("Misc", "Misc.Walljump") && Game.MapId == GameMapId.SummonersRift)
            {
                WallJump.OnUpdate();
            }


            if (GetKeyBind("Misc", "Misc.DashMode"))
            {
                MoveToMouse();
                return;
            }

            Fleeing = GetKeyBind("Flee", "Flee.KB");

            if (GetBool("Killsteal", "Killsteal.Enabled") && !Fleeing)
            {
                Killsteal();
            }

            if (GetKeyBind("Harass", "Harass.KB") && !Fleeing)
            {
                Harass();
            }

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Orbwalker.Move(Game.CursorPosRaw);
                    Orbwalker.AttackState  = true;
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    Orbwalker.Move(Game.CursorPosRaw);
                    Orbwalker.AttackState  = true;
                    Mixed();
                    break;
                case OrbwalkerMode.LastHit:
                    Orbwalker.Move(Game.CursorPosRaw);
                    Orbwalker.AttackState  = true;
                    LHSkills();
                    break;
                case OrbwalkerMode.LaneClear:
                    Orbwalker.Move(Game.CursorPosRaw);
                    Orbwalker.AttackState  = true;
                    NewWaveClear();
                    break;
                case OrbwalkerMode.None:
                    Orbwalker.Move(Game.CursorPosRaw);
                    break;
            }
            if(GetKeyBind("Flee", "Flee.KB")) Flee();
        }

        void CastUlt()
        {
            if (!SpellSlot.R.IsReady())
            {
                return;
            }
            if (GetBool("Combo", "Combo.UseR") && Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                CastR(GetSliderInt("Combo", "Combo.RMinHit"));
            }

            if (GetBool("Misc", "Misc.AutoR") && !Fleeing)
            {
                CastR(GetSliderInt("Misc", "Misc.RMinHit"));
            }
        }

        void OnDraw(EventArgs args)
        {
            if (Debug)
            {
                Drawing.DrawCircle(DashPosition.ToVector3(), Yasuo.BoundingRadius, System.Drawing
                    .Color.Chartreuse);
            }


            if (Yasuo.IsDead || GetBool("Drawing", "Drawing.Disable"))
            {
                return;
            }

            TargettedDanger.OnDraw(args);

            if (GetBool("Misc", "Misc.Walljump") && Game.MapId == GameMapId.SummonersRift)
            {
                WallJump.OnDraw();
            }

            var pos = Yasuo.Position.WTS();

            Drawing.DrawText(pos.X, pos.Y + 50, isHealthy ? System.Drawing.Color.Green : System.Drawing.Color.Red,
                "Healthy: " + isHealthy);

            var drawq = GetBool("Drawing", "Drawing.DrawQ");
            var drawe = GetBool("Drawing", "Drawing.DrawE");
            var drawr = GetBool("Drawing", "Drawing.DrawR");

            if (drawq)
            {
                Render.Circle.DrawCircle(Yasuo.Position, Qrange, System.Drawing.Color.Gray);
            }
            if (drawe)
            {
                Render.Circle.DrawCircle(Yasuo.Position, Spells[E].Range, System.Drawing.Color.Gray);
            }
            if (drawr)
            {
                Render.Circle.DrawCircle(Yasuo.Position, Spells[R].Range, System.Drawing.Color.Gray);
            }
        }



        void Combo()
        {
            float range = 0;
            if (SpellSlot.R.IsReady())
            {
                range = Spells[R].Range;
            }

            else if (Spells[Q2].IsReady())
            {
                range = Spells[Q2].Range;
            }

            else if (Spells[E].IsReady())
            {
                range = Spells[E].Range;
            }

            CurrentTarget = TargetSelector.GetTarget(range);

            SmartCombo(CurrentTarget);

            if (GetBool("Combo", "Combo.UseIgnite"))
            {
                CastIgnite();
            }

            if (GetBool("Items", "Items.Enabled"))
            {
                if (GetBool("Items", "Items.UseTIA"))
                {
                    Tiamat.Cast(null);
                }
                if (GetBool("Items", "Items.UseHDR"))
                {
                    Hydra.Cast(null);
                }
                if (GetBool("Items", "Items.UseTitanic"))
                {
                    Titanic.Cast(null);
                }
                if (GetBool("Items", "Items.UseBRK") && CurrentTarget != null)
                {
                    Blade.Cast(CurrentTarget);
                }
                if (GetBool("Items", "Items.UseBLG") && CurrentTarget != null)
                {
                    Bilgewater.Cast(CurrentTarget);
                }
                if (GetBool("Items", "Items.UseYMU"))
                {
                    Youmu.Cast(null);
                }
            }
        }

        float BeyBladeAttemptStick = 0;
        bool InBeyBlade;
        AIBaseClient beyBladeTarget = null;
        AIBaseClient beyBlademidTarget = null;


        void SmartCombo(AIHeroClient target)
        {
            if (target != null)
            {
                //EQ
                if (TornadoReady && Yasuo.IsDashing())
                {
                    var bestTarget = ObjectManager.Get<AIBaseClient>().Where(x => x.IsValidTarget(Qrange) && x.Distance(target) <= QRadius).FirstOrDefault();
                    if (bestTarget != null && GetBool("Combo", "Combo.UseEQ"))
                    {
                        var pred = Spells[Q].GetPrediction(bestTarget);
                        if (pred.Hitchance >= HitChance.Medium)
                        {
                            Spells[Q].Cast(pred.CastPosition);
                        }
                    }
                }


                if (GetBool("Combo", "Combo.UseBeyBlade") && TornadoReady && Spells[Q].IsReady())
                {
                    var bbtarget = TargetSelector.GetTarget(1000);
                    if (bbtarget == null)
                    {
                        return;
                    }

                    if (InBeyBlade && TickCount - BeyBladeAttemptStick > 800)
                    {
                        InBeyBlade = false;
                        DontDash = false;
                        return;
                    }

                    if (InBeyBlade && Spells[Flash].IsReady() && Spells[R].IsReady())
                    {
                        if (Yasuo.IsDashing() && InDash)
                        {
                            var dashinfo = Dash.GetDashInfo(Yasuo);

                            if (beyBladeTarget != null && beyBlademidTarget != null && dashinfo.EndPos.Distance(beyBladeTarget) <= FlashRange && (!beyBladeTarget.Position.PointUnderEnemyTurret() || Helper.GetKeyBind("Misc", "Misc.TowerDive")))
                            {
                                if (Yasuo.Distance(beyBlademidTarget) <= Spells[Q].Range)
                                {
                                    Spells[Q].Cast(beyBlademidTarget.Position);
                                    Spells[Flash].Cast(beyBladeTarget.Position);
                                    InBeyBlade = false;
                                    DontDash = false;
                                }
                            }

                            else
                            {
                                InBeyBlade = false;
                                DontDash = false;
                            }
                        }

                        if (Spells[E].IsReady() && Spells[R].IsReady() && Spells[Flash].IsReady() && !target.Position.PointUnderEnemyTurret())
                        {
                            var minion = ObjectManager.Get<AIBaseClient>().Where(x => x.IsValidTarget(Spells[E].Range) && x.Distance(bbtarget) <= Yasuo.Distance(bbtarget) && GetDashPos(x).Distance(bbtarget) <= Yasuo.Distance(bbtarget) && GetDashPos(x).Distance(bbtarget) <= FlashRange && (SafetyCheck(x, true))).FirstOrDefault();
                            if (minion != null)
                            {
                                Spells[E].Cast(minion);
                                DontDash = true;
                                InBeyBlade = true;
                                beyBlademidTarget = minion;
                                beyBladeTarget = bbtarget;
                                BeyBladeAttemptStick = TickCount;
                            }
                        }
                    }
                }

                if (Spells[Q].IsReady() && !Yasuo.IsDashing())
                {
                    var targ = target.IsInRange(Qrange) ? target : TargetSelector.GetTarget(Qrange);
                    if (targ != null)
                    {
                        CastQ(target);
                    }
                }

                var eqready = Spells[E].IsReady() && Spells[Q].IsReady() && TornadoReady;

                if (eqready && GetBool("Combo", "Combo.UseEQ"))
                {
                    var bestTarget = ObjectManager.Get<AIBaseClient>().Where(x => x.IsDashable() && GetDashPos(x).Distance(target) <= QRadius && (ShouldDive(x) || GetBool("Combo", "Combo.ETower"))).FirstOrDefault();
                    if (bestTarget != null)
                    {
                        Spells[E].Cast(bestTarget);
                    }


                    else if (GetBool("Combo", "Combo.UseQ2") && Spells[Q].IsReady() && TornadoReady && target.IsInRange(Qrange) && !InDash && !Yasuo.IsDashing())
                    {
                        CastQ(target);
                    }
                }


                else if (GetBool("Combo", "Combo.UseQ2") && Spells[Q].IsReady() && TornadoReady && target.IsInRange(Qrange) && !InDash && !Yasuo.IsDashing())
                {
                    CastQ(target);
                }
            }


            if (GetBool("Combo", "Combo.UseE") && !Helper.DontDash)
            {
                var mode = GetMode();
                if (mode == Modes.Old)
                {
                    CastEOld(CurrentTarget);
                }
                else
                {
                    CastENew(CurrentTarget);
                }
            }

            if (GetBool("Combo", "Combo.StackQ") && !target.IsValidEnemy(Qrange) && !TornadoReady && !Yasuo.IsDashing() && !InDash)
            {
                var bestmin =
                    ObjectManager.Get<AIMinionClient>()
                        .Where(x => x.IsValidMinion(Qrange) && x.IsMinion)
                        .MinOrDefault(x => x.Distance(Yasuo));
                if (bestmin != null)
                {
                    var pred = Spells[Q].GetPrediction(bestmin);

                    if (pred.Hitchance >= HitChance.Medium)
                    {
                        Spells[Q].Cast(pred.CastPosition);
                    }
                }
            }

        }

        internal void CastQ(AIHeroClient target)
        {
            if (target != null && !target.IsInRange(Qrange))
            {
                target = TargetSelector.GetTarget(Qrange);
            }

            if (target != null)
            {
                if (Spells[Q].IsReady() && target.IsValidEnemy(Qrange))
                {
                    UseQ(target, GetHitChance("Hitchance.Q"), GetBool("Combo", "Combo.UseQ"), GetBool("Combo", "Combo.UseQ2"));
                    return;
                }

                if (GetBool("Combo", "Combo.StackQ") && !target.IsValidEnemy(Qrange) && !TornadoReady && !Yasuo.IsDashing() && !InDash)
                {
                    var bestmin =
                        ObjectManager.Get<AIMinionClient>()
                            .Where(x => x.IsValidMinion(Qrange) && x.IsMinion)
                            .MinOrDefault(x => x.Distance(Yasuo));
                    if (bestmin != null)
                    {
                        var pred = Spells[Q].GetPrediction(bestmin);

                        if (pred.Hitchance >= HitChance.Medium)
                        {
                            Spells[Q].Cast(bestmin.Position);
                        }
                    }
                }
            }
        }

        internal void CastEOld(AIHeroClient target, bool force = false)
        {
            if (!SpellSlot.E.IsReady() || Helper.DontDash)
            {
                return;
            }

            var minionsinrange = ObjectManager.Get<AIMinionClient>().Any(x => x.IsDashable());
            if (target == null || !target.IsInRange(minionsinrange ? Spells[E].Range * 2 : Spells[E].Range))
            {
                target = TargetSelector.GetTarget(minionsinrange ? Spells[E].Range * 2 : Spells[E].Range);
            }

            if (target != null)
            {
                if (isHealthy && target.Distance(Yasuo) >= 0.30 * Yasuo.AttackRange)
                {
                    if (TornadoReady)
                    {
                        if (SafetyCheck(target))
                        {
                            Spells[E].CastOnUnit(target);
                            return;
                        }
                    }

                    if (DashCount >= 1 && GetDashPos(target).IsCloser(target) && target.IsDashable())
                    {
                        if (SafetyCheck(target))
                        {
                            Spells[E].CastOnUnit(target);
                            return;
                        }
                    }

                    if (DashCount == 0)
                    {
                        var bestminion =
                            ObjectManager.Get<AIBaseClient>()
                                .Where(
                                    x =>
                                         x.IsDashable()
                                         && GetDashPos(x).IsCloser(target) && (SafetyCheck(x, true)))
                                .OrderBy(x => Vector2.Distance(GetDashPos(x), target.Position.ToVector2()))
                                .FirstOrDefault();
                        if (bestminion != null)
                        {
                            Spells[E].CastOnUnit(bestminion);
                        }

                        else if (target.IsDashable() && GetDashPos(target).IsCloser(target) && (SafetyCheck(target, true)))
                        {
                            Spells[E].CastOnUnit(target);
                        }
                    }


                    else
                    {
                        var minion =
                            ObjectManager.Get<AIBaseClient>()
                                .Where(x => x.IsDashable() && GetDashPos(x).IsCloser(target) && SafetyCheck(x, true))
                                .OrderBy(x => GetDashPos(x).Distance(target.Position)).FirstOrDefault();

                        if (minion != null && GetDashPos(minion).IsCloser(target))
                        {
                            Spells[E].CastOnUnit(minion);
                        }
                    }
                }
            }
        }


        internal void CastENew(AIHeroClient target)
        {
            if (!SpellSlot.E.IsReady() || Helper.DontDash || Yasuo.IsDashing() || InDash)
            {
                return;
            }


            var minionsinrange = ObjectManager.Get<AIMinionClient>().Any(x => x.IsDashable());
            if (target == null || !target.IsInRange(minionsinrange ? Spells[E].Range * 2 : Spells[E].Range))
            {
                target = TargetSelector.GetTarget(minionsinrange ? Spells[E].Range * 2 : Spells[E].Range);
            }

            if (target != null && SafetyCheck(target, true))
            {
                var dist = Yasuo.Distance(target);
                var pctOutOfRange = dist / Yasuo.AttackRange * 100;

                if (pctOutOfRange > 0.8f)
                {
                    if (target.IsDashable())
                    {
                        if (target.ECanKill())
                        {
                            return;
                        }

                        if (TornadoReady && target.IsInRange(Spells[E].Range) && targInKnockupRadius(target))
                        {
                            Spells[E].CastOnUnit(target);
                        }

                        //Stay in range
                        else if (pctOutOfRange > 0.8f)
                        {
                            var bestminion = ObjectManager.Get<AIBaseClient>()
                                .Where(x =>
                                    x.IsDashable()
                                    && GetDashPos(x).IsCloser(target) && SafetyCheck(x, true))
                                .MinOrDefault(x => GetDashPos(x).Distance(target));

                            var shouldETarget = bestminion == null || GetDashPos(target).Distance(target) <
                                                GetDashPos(bestminion).Distance(target);
                            if (shouldETarget && GetDashPos(target).IsCloser(target))
                            {
                                Spells[E].CastOnUnit(target);
                            }

                            else if (bestminion != null)
                            {
                                Spells[E].CastOnUnit(bestminion);
                            }
                        }
                    }

                    else
                    {
                        var minion = ObjectManager.Get<AIMinionClient>()
                            .Where(x => x.IsDashable() && x.IsCloser(target) && SafetyCheck(x, true))
                            .MinOrDefault(x => GetDashPos(x).Distance(target));
                        if (minion != null)
                        {
                            Spells[E].CastOnUnit(minion);
                        }
                    }
                }
            }
        }




        private void UnitOnOnDash(AIBaseClient sender, Dash.DashArgs args)
        {
            if (sender.IsMe && !args.IsBlink)
            {
                DashPosition = args.EndPos;
                LastDashTick = Helper.TickCount;
                var endpos = args.EndPos;

                if (GetBool("Combo", "Combo.UseEQ"))
                {
                    if (SpellSlot.Q.IsReady())
                    {
                        var mode = Orbwalker.ActiveMode;
                        if (mode == OrbwalkerMode.Combo || mode == OrbwalkerMode.Harass)
                        {
                            if (TornadoReady)
                            {
                                var goodTarget = GameObjects.Get<AIBaseClient>().Where(x => x.IsValidTarget(QRadius, true, endpos.ToVector3()) && !x.ECanKill()) != null;
                                if (goodTarget)
                                {
                                    Spells[Q].Cast(endpos);
                                }


                                else if (GetBool("Combo", "Combo.UseBeyBlade") && Spells[Flash].IsReady() && Spells[R].IsReady())
                                {
                                    var targ = TargetSelector.GetTarget(Yasuo.Distance(endpos) + Spells[Flash].Range);
                                    if (targ != null && (SafetyCheck(targ, true)) && endpos.Distance(targ.Position) <= 0.85f * Spells[Flash].Range)
                                    {
                                        Chat.PrintChat("Beyblade 2");
                                        Spells[Q].Cast(endpos);
                                        Spells[Flash].Cast(targ.Position);
                                    }
                                }


                                else if (!TornadoReady)
                                {
                                    var targ = GameObjects.Get<AIBaseClient>().Where(x => x.IsValidTarget(QRadius, true, endpos.ToVector3()) && !x.ECanKill()) != null;
                                    if (targ)
                                    {
                                        Spells[Q].Cast(endpos);
                                    }

                                    if (GetBool("Combo", "Combo.StackQ"))
                                    {
                                        var nonkillableMin = endpos.GetMinionsInRange(QRadius).Any(x => !x.ECanKill());
                                        if (nonkillableMin)
                                        {
                                            Spells[Q].Cast(endpos);
                                            return;
                                        }
                                    }
                                }
                            }

                            else if (mode != OrbwalkerMode.None && !TornadoReady)
                            {
                                if (endpos.ToVector3().MinionsInRange(QRadius) > 1 ||
                                    endpos.ToVector3().CountEnemyHeroesInRange(QRadius) >= 1)
                                {
                                    Spells[Q].Cast(endpos);
                                }
                            }
                        }
                    }
                }
            }
        }


        void CastR(int minhit = 1)
        {
            UltMode ultmode = GetUltMode();

            List<AIHeroClient> ordered = new List<AIHeroClient>();

            if (ultmode == UltMode.Health)
            {
                ordered = KnockedUp.OrderBy(x => x.Health).ThenByDescending(x => TargetSelector.GetPriority(x)).ThenByDescending(x => x.CountEnemyHeroesInRange(350)).ToList();
            }

            if (ultmode == UltMode.Priority)
            {
                ordered = KnockedUp.OrderByDescending(x => TargetSelector.GetPriority(x)).ThenBy(x => x.Health).ThenByDescending(x => x.CountEnemyHeroesInRange(350)).ToList();
            }

            if (ultmode == UltMode.EnemiesHit)
            {
                ordered = KnockedUp.OrderByDescending(x => x.CountEnemyHeroesInRange(350)).ThenByDescending(x => TargetSelector.GetPriority(x)).ThenBy(x => x.Health).ToList();
            }

            if (GetBool("Combo", "Combo.UltOnlyKillable"))
            {
                var killable = ordered.FirstOrDefault(x => !x.isBlackListed() && x.Health <= Yasuo.GetSpellDamage(x, SpellSlot.R) && x.HealthPercent >= GetSliderInt("Combo", "Combo.MinHealthUlt") && (GetBool("Combo", "Combo.UltTower") || GetKeyBind("Misc", "Misc.TowerDive") || ShouldDive(x)));
                if (killable != null && (!killable.IsInRange(Spells[Q].Range) || !isHealthy))
                {
                    Spells[R].CastOnUnit(killable);
                }
                return;
            }

            if ((GetBool("Combo", "Combo.OnlyifMin") && ordered.Count() < minhit) || (ordered.Count() == 1 && ordered.FirstOrDefault().HealthPercent < GetSliderInt("Combo", "Combo.MinHealthUlt")))
            {
                return;
            }

            if (GetBool("Combo", "Combo.RPriority"))
            {
                var best = ordered.Find(x => !x.isBlackListed() && TargetSelector.GetPriority(x).Equals(2.5f) && (GetBool("Combo", "Combo.UltTower") || GetKeyBind("Misc", "Misc.TowerDive") || !x.Position.ToVector2().PointUnderEnemyTurret()));
                if (best != null && Yasuo.HealthPercent / best.HealthPercent <= 1)
                {
                    Spells[R].CastOnUnit(best);
                    return;
                }
            }

            if (ordered.Count() >= minhit)
            {
                var best2 = ordered.FirstOrDefault(x => !x.isBlackListed() && (GetBool("Combo", "Combo.UltTower") || GetKeyBind("Misc", "Misc.TowerDive") || !x.Position.ToVector2().PointUnderEnemyTurret()));
                if (best2 != null)
                {
                    Spells[R].CastOnUnit(best2);
                }
                return;
            }
        }

        void Flee()
        {
            Orbwalker.AttackState  = false;
            if (GetBool("Flee", "Flee.UseQ2") && !Yasuo.IsDashing() && SpellSlot.Q.IsReady() && TornadoReady)
            {
                var qtarg = TargetSelector.GetTarget(Spells[Q2].Range);
                if (qtarg != null)
                {
                    var pred = Spells[Q].GetPrediction(qtarg);
                    if (pred.Hitchance >= HitChance.Medium)
                    {
                        Spells[Q2].Cast(pred.CastPosition);
                    }
                }
            }

            if (FleeMode == FleeType.ToCursor)
            {
                Orbwalker.Move(Game.CursorPosRaw);

                var smart = GetBool("Flee", "Flee.Smart");

                if (Spells[E].IsReady())
                {
                    if (smart)
                    {
                        AIBaseClient dashTarg;

                        if (Yasuo.Position.PointUnderEnemyTurret())
                        {
                            var closestturret =
                                ObjectManager.Get<AITurretClient>()
                                    .Where(x => x.IsEnemy)
                                    .MinOrDefault(y => y.Distance(Yasuo));

                            var potential =
                                ObjectManager.Get<AIBaseClient>()
                                    .Where(x => x.IsDashable())
                                    .MaxOrDefault(x => GetDashPos(x).Distance(closestturret));

                            if (potential != null)
                            {
                                var gdpos = GetDashPos(potential);
                                if (gdpos.Distance(Game.CursorPosRaw) < Yasuo.Distance(Game.CursorPosRaw) &&
                                    gdpos.Distance(closestturret.Position) - closestturret.BoundingRadius >
                                    Yasuo.Distance(closestturret.Position) - Yasuo.BoundingRadius)
                                {
                                    Spells[E].Cast(potential);
                                    return;
                                }
                            }
                        }

                        dashTarg = ObjectManager.Get<AIBaseClient>()
                           .Where(x => x.IsDashable())
                           .MinOrDefault(x => GetDashPos(x).Distance(Game.CursorPosRaw));

                        if (dashTarg != null)
                        {
                            var posafdash = GetDashPos(dashTarg);

                            if (posafdash.Distance(Game.CursorPosRaw) < Yasuo.Distance(Game.CursorPosRaw) &&
                                !posafdash.PointUnderEnemyTurret())
                            {
                                Spells[E].CastOnUnit(dashTarg);
                            }
                        }
                    }

                    else
                    {
                        var dashtarg =
                            ObjectManager.Get<AIMinionClient>()
                                .Where(x => x.IsDashable())
                                .MinOrDefault(x => GetDashPos(x).Distance(Game.CursorPosRaw));

                        if (dashtarg != null)
                        {
                            var posafdash = GetDashPos(dashtarg);
                            if (posafdash.Distance(Game.CursorPosRaw) < Yasuo.Distance(Game.CursorPosRaw) && !posafdash.PointUnderEnemyTurret())
                            {
                                Spells[E].CastOnUnit(dashtarg);
                            }
                        }
                    }
                }

                if (GetBool("Flee", "Flee.StackQ") && SpellSlot.Q.IsReady() && !TornadoReady && !Yasuo.IsDashing())
                {
                    AIMinionClient qtarg = null;
                    if (!Spells[E].IsReady())
                    {
                        qtarg =
                            ObjectManager.Get<AIMinionClient>()
                                .Find(x => x.IsValidTarget(Spells[Q].Range) && x.IsMinion);

                    }
                    else
                    {
                        var etargs =
                            ObjectManager.Get<AIMinionClient>()
                                .Where(
                                    x => x.IsValidTarget(Spells[E].Range) && x.IsMinion && x.IsDashable());
                        if (!etargs.Any())
                        {
                            qtarg =
                           ObjectManager.Get<AIMinionClient>()
                               .Find(x => x.IsValidTarget(Spells[Q].Range) && x.IsMinion);
                        }
                    }

                    if (qtarg != null)
                    {
                        Spells[Q].Cast(qtarg.Position);
                    }
                }
            }

            if (FleeMode == FleeType.ToNexus)
            {
                var nexus = shop;
                if (nexus != null)
                {
                    Orbwalker.Move(nexus.Position);
                    var bestminion = ObjectManager.Get<AIBaseClient>().Where(x => x.IsDashable()).MinOrDefault(x => GetDashPos(x).Distance(nexus.Position));
                    if (bestminion != null && (!GetBool("Flee", "Flee.Smart") || GetDashPos(bestminion).Distance(nexus.Position) < Yasuo.Distance(nexus.Position)))
                    {
                        Spells[E].CastOnUnit(bestminion);
                        if (GetBool("Flee", "Flee.StackQ") && SpellSlot.Q.IsReady() && !TornadoReady)
                        {
                            Spells[Q].Cast(bestminion.Position);
                        }
                    }
                }
            }

            if (FleeMode == FleeType.ToAllies)
            {
                AIBaseClient bestally = GameObjects.AllyHeroes.Where(x => !x.IsMe && x.CountEnemyHeroesInRange(300) == 0).MinOrDefault(x => x.Distance(Yasuo));
                if (bestally == null)
                {
                    bestally =
                        ObjectManager.Get<AIMinionClient>()
                            .Where(x => x.IsValidAlly(3000))
                            .MinOrDefault(x => x.Distance(Yasuo));
                }

                if (bestally != null)
                {
                    Orbwalker.Move(bestally.Position);
                    if (Spells[E].IsReady())
                    {
                        var besttarget =
                            ObjectManager.Get<AIBaseClient>()
                                .Where(x => x.IsDashable())
                                .MinOrDefault(x => GetDashPos(x).Distance(bestally.Position));
                        if (besttarget != null)
                        {
                            Spells[E].CastOnUnit(besttarget);
                            if (GetBool("Flee", "Flee.StackQ") && SpellSlot.Q.IsReady() && !TornadoReady)
                            {
                                Spells[Q].Cast(besttarget.Position);
                            }
                        }
                    }
                }

                else
                {
                    var nexus = shop;
                    if (nexus != null)
                    {
                        Orbwalker.Move(nexus.Position);
                        var bestminion = ObjectManager.Get<AIBaseClient>().Where(x => x.IsDashable()).MinOrDefault(x => GetDashPos(x).Distance(nexus.Position));
                        if (bestminion != null && GetDashPos(bestminion).Distance(nexus.Position) < Yasuo.Distance(nexus.Position))
                        {
                            Spells[E].CastOnUnit(bestminion);
                        }
                    }
                }
            }
        }


        void MoveToMouse()
        {
            Yasuo.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPosRaw);
            if (Spells[E].IsReady())
            {
                var bestminion =
                    ObjectManager.Get<AIBaseClient>()
                        .Where(x => x.IsDashable())
                        .MinOrDefault(x => GetDashPos(x).Distance(Game.CursorPosRaw));

                if (bestminion != null)
                {
                    Spells[E].CastOnUnit(bestminion);
                }
            }
        }



        void CastIgnite()
        {
            var target =
                GameObjects.EnemyHeroes.Find(
                    x =>
                        x.IsValidEnemy(Spells[Ignite].Range) &&
                        Yasuo.GetSummonerSpellDamage(x, SummonerSpell.Ignite) >= x.Health);

            if (Spells[Ignite].IsReady() && target != null)
            {
                Spells[Ignite].Cast(target);
            }
        }


        void Waveclear()
        {
            if (SpellSlot.Q.IsReady() && !Yasuo.IsDashing() && !InDash)
            {
                if (!TornadoReady && GetBool("Waveclear", "Waveclear.UseQ") && Yasuo.IsWindingUp)
                {
                    var minion =
                        ObjectManager.Get<AIMinionClient>()
                            .Where(x => x.IsValidMinion(Spells[Q].Range) && ((x.IsDashable() && (x.Health - Yasuo.GetSpellDamage(x, SpellSlot.Q) >= GetProperEDamage(x))) || (x.Health - Yasuo.GetSpellDamage(x, SpellSlot.Q) >= 0.15 * x.MaxHealth || x.QCanKill()))).MaxOrDefault(x => x.MaxHealth);
                    if (minion != null)
                    {
                        Spells[Q].Cast(minion.Position);
                    }
                }

                else if (TornadoReady && GetBool("Waveclear", "Waveclear.UseQ2"))
                {
                    var minions = ObjectManager.Get<AIMinionClient>().Where(x => x.Distance(Yasuo) > Yasuo.AttackRange && x.IsValidMinion(Spells[Q2].Range) && ((x.IsDashable() && x.Health - Yasuo.GetSpellDamage(x, SpellSlot.Q) >= 0.75 * GetProperEDamage(x)) || (x.Health - Yasuo.GetSpellDamage(x, SpellSlot.Q) >= 0.10 * x.MaxHealth) || x.CanKill(SpellSlot.Q)));
                    var pred =
                        FarmPrediction.GetBestLineFarmLocation(minions.Select(m => m.Position.ToVector2()).ToList(),
                            Spells[Q2].Width, Spells[Q2].Range);
                    if (pred.MinionsHit >= GetSliderInt("Waveclear", "Waveclear.Qcount"))
                    {
                        Spells[Q2].Cast(pred.Position);
                        LastTornadoClearTick = Helper.TickCount;
                    }
                }
            }

            if (Helper.TickCount - LastTornadoClearTick < 500)
            {
                return;
            }


            if (SpellSlot.E.IsReady() && GetBool("Waveclear", "Waveclear.UseE") && (!GetBool("Waveclear", "Waveclear.Smart") || isHealthy) && (Helper.TickCount - WCLastE) >= GetSliderInt("Waveclear", "Waveclear.Edelay"))
            {
                var minions = ObjectManager.Get<AIMinionClient>().Where(x => x.IsDashable() && ((GetBool("Waveclear", "Waveclear.UseENK") && (!GetBool("Waveclear", "Waveclear.Smart") || x.Health - GetProperEDamage(x) > GetProperEDamage(x) * 3)) || x.ECanKill()) && (GetBool("Waveclear", "Waveclear.ETower") || ShouldDive(x)));
                AIMinionClient minion = null;
                minion = minions.OrderBy(x => x.ECanKill()).ThenBy(x => GetDashPos(x).MinionsInRange(200)).FirstOrDefault();
                if (minion != null)
                {
                    Spells[E].Cast(minion);
                    WCLastE = Helper.TickCount;
                }
            }

            if (GetBool("Waveclear", "Waveclear.UseItems"))
            {
                if (GetBool("Waveclear", "Waveclear.UseTIA") && GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Tiamat.Range)).Count() >= GetSliderInt("Waveclear", "Waveclear.MinCountHDR"))
                {
                    Tiamat.Cast();
                }
                if (GetBool("Waveclear", "Waveclear.UseTitanic") && GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Titanic.Range)).Count() >= GetSliderInt("Waveclear", "Waveclear.MinCountHDR"))
                {
                    Titanic.Cast();
                }
                if (GetBool("Waveclear", "Waveclear.UseHDR") && GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Hydra.Range)).Count() >= GetSliderInt("Waveclear", "Waveclear.MinCountHDR"))
                {
                    Hydra.Cast();
                }
                if (GetBool("Waveclear", "Waveclear.UseYMU") && GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Youmu.Range)).Count() >= GetSliderInt("Waveclear", "Waveclear.MinCountYOU"))
                {
                    Youmu.Cast();
                }
                //if (GetBool("Waveclear", "Waveclear.UseYMU"))
                //{
                //    Youmu.minioncount = GetSliderInt("Waveclear", "Waveclear.MinCountYOU");
                //    Youmu.Cast();
                //}
            }
        }



        void NewWaveClear()
        {
            var minions = ObjectManager.Get<AIMinionClient>().Where(x => x.IsValidMinion(Spells[Q2].Range));

            Func<bool> wcQ = delegate
            {
                if (Spells[Q].IsReady() && GetBool("Waveclear", "Waveclear.UseQ") && !TornadoReady && !Yasuo.IsDashing())
                {
                    var minion =
                                  ObjectManager.Get<AIMinionClient>()
                                      .Where(x => x.IsValidMinion(Spells[Q].Range) && (x.QCanKill() || x.HealthPercent > 75)).MinOrDefault(x => x.Health);
                    if (minion != null)
                    {
                        var pred = Spells[Q].GetPrediction(minion);
                        if (pred.Hitchance >= HitChance.Medium)
                        {
                            Spells[Q].Cast(pred.CastPosition);
                            return true;
                        }
                    }
                }
                return false;
            };

            Func<bool> wcQDash = delegate
            {
                if (Spells[Q].IsReady() && GetBool("Waveclear", "Waveclear.UseQ") && !TornadoReady && Yasuo.IsDashing())
                {
                    var dashinfo = Dash.GetDashInfo(Yasuo);
                    if (dashinfo.EndPos.ToVector3().MinionsInRange(QRadius) >= GetSliderInt("Waveclear", "Waveclear.Qcount"))
                    {
                        Spells[Q].Cast();
                        return true;
                    }
                }
                return false;
            };

            Func<bool> wcQ2 = delegate
            {
                if (Spells[Q].IsReady() && GetBool("Waveclear", "Waveclear.UseQ2") && TornadoReady)
                {
                    var minionsQ2 =
                      ObjectManager.Get<AIMinionClient>()
                          .Where(x => x.IsValidMinion(Spells[Q2].Range) && (x.Health > Yasuo.GetAutoAttackDamage(x, false) || !x.IsInRange(Yasuo.GetRealAutoAttackRange(x))) && (x.QCanKill(true) || x.HealthPercent > 60));

                    var pred = FarmPrediction.GetBestLineFarmLocation(minionsQ2.Select(m => m.Position.ToVector2()).ToList(),
                        Spells[Q2].Width, Spells[Q2].Range);

                    if (pred.MinionsHit >= GetSliderInt("Waveclear", "Waveclear.Qcount"))
                    {
                        Spells[Q2].Cast(pred.Position);
                        LastTornadoClearTick = Helper.TickCount;
                        return true;
                    }
                }

                return false;
            };

            Func<bool> wcE = delegate
            {
                if (Spells[E].IsReady() && GetBool("Waveclear", "Waveclear.UseE") && isHealthy && TickCount - LastTornadoClearTick > 500 && (Helper.TickCount - WCLastE) >= GetSliderInt("Waveclear", "Waveclear.Edelay"))
                {
                    var minion =
                      ObjectManager.Get<AIMinionClient>()
                          .Where(x => x.IsValidMinion(Spells[E].Range) && x.IsDashable() && ((GetBool("Waveclear", "Waveclear.UseENK") && x.HealthPercent > 60 || x.ECanKill()) && (GetBool("Waveclear", "Waveclear.ETower") || ShouldDive(x)))).MinOrDefault(x => x.Health);

                    if (minion != null)
                    {
                        Spells[E].Cast(minion);
                        WCLastE = Helper.TickCount;
                        return true;
                    }
                }
                return false;
            };

            if (!wcQ() && !wcQDash() && !wcQ2())
            {
                wcE();
            }

            if (GetBool("Waveclear", "Waveclear.UseItems"))
            {
                if (GetBool("Waveclear", "Waveclear.UseTIA") && GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Tiamat.Range)).Count() >= GetSliderInt("Waveclear", "Waveclear.MinCountHDR"))
                {
                    Tiamat.Cast();
                }
                if (GetBool("Waveclear", "Waveclear.UseTitanic") && GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Titanic.Range)).Count() >= GetSliderInt("Waveclear", "Waveclear.MinCountHDR"))
                {
                    Titanic.Cast();
                }
                if (GetBool("Waveclear", "Waveclear.UseHDR") && GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Hydra.Range)).Count() >= GetSliderInt("Waveclear", "Waveclear.MinCountHDR"))
                {
                    Hydra.Cast();
                }
                if (GetBool("Waveclear", "Waveclear.UseYMU") && GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Youmu.Range)).Count() >= GetSliderInt("Waveclear", "Waveclear.MinCountYOU"))
                {
                    Youmu.Cast();
                }
            }
        }


        void Killsteal()
        {
            if (SpellSlot.Q.IsReady() && GetBool("Killsteal", "Killsteal.UseQ"))
            {
                var targ = GameObjects.EnemyHeroes.Find(x => x.CanKill(SpellSlot.Q) && x.IsInRange(Qrange));
                if (targ != null)
                {
                    UseQ(targ, GetHitChance("Hitchance.Q"));
                    return;
                }
            }

            if (SpellSlot.E.IsReady() && GetBool("Killsteal", "Killsteal.UseE"))
            {
                var targ = GameObjects.EnemyHeroes.Find(x => x.CanKill(SpellSlot.E) && x.IsInRange(Spells[E].Range));
                if (targ != null)
                {
                    Spells[E].Cast(targ);
                    return;
                }
            }

            if (SpellSlot.R.IsReady() && GetBool("Killsteal", "Killsteal.UseR"))
            {
                var targ = KnockedUp.Find(x => x.CanKill(SpellSlot.R) && x.IsValidEnemy(Spells[R].Range) && !x.isBlackListed());
                if (targ != null)
                {
                    Spells[R].Cast(targ);
                    return;
                }
            }

            if (GetBool("Killsteal", "Killsteal.UseIgnite"))
            {
                CastIgnite();
                return;
            }

            if (GetBool("Killsteal", "Killsteal.UseItems"))
            {
                if (Tiamat.IsReady)
                {
                    var targ =
                        GameObjects.EnemyHeroes.Find(
                            x =>
                                x.IsValidEnemy(Tiamat.Range));// && x.Health <= Yasuo.GetItemDamage(x, Damage.DamageItems.Tiamat));
                    if (targ != null)
                    {
                        Tiamat.Cast(null);
                    }
                }

                if (Titanic.IsReady)
                {
                    var targ =
                        GameObjects.EnemyHeroes.Find(
                            x =>
                                x.IsValidEnemy(Titanic.Range));// && x.Health <= Yasuo.GetItemDamage(x, Damage.DamageItems.Tiamat)
                    if (targ != null)
                    {
                        Titanic.Cast(null);
                    }
                }
                if (Hydra.IsReady)
                {
                    var targ =
                      GameObjects.EnemyHeroes.Find(
                      x =>
                          x.IsValidEnemy(Hydra.Range));// && x.Health <= Yasuo.GetItemDamage(x, Damage.DamageItems.Tiamat));
                    if (targ != null)
                    {
                        Hydra.Cast(null);
                    }
                }
                if (Blade.IsReady)
                {
                    var targ = GameObjects.EnemyHeroes.Find(
                     x =>
                         x.IsValidEnemy(Blade.Range));// && x.Health <= Yasuo.GetItemDamage(x, Damage.DamageItems.Botrk));
                    if (targ != null)
                    {
                        Blade.Cast(targ);
                    }
                }
                if (Bilgewater.IsReady)
                {
                    var targ = GameObjects.EnemyHeroes.Find(
                                   x =>
                                       x.IsValidEnemy(Bilgewater.Range));// && x.Health <= Yasuo.GetItemDamage(x, Damage.DamageItems.Bilgewater));
                    if (targ != null)
                    {
                        Bilgewater.Cast(targ);
                    }
                }
            }
        }

        void Harass()
        {
            //No harass under enemy turret to avoid aggro
            if (Yasuo.Position.PointUnderEnemyTurret())
            {
                return;
            }

            var target = TargetSelector.GetTarget(Qrange);
            if (target != null)
            {
                if (SpellSlot.Q.IsReady() && target.IsInRange(Qrange))
                {
                    UseQ(target, GetHitChance("Hitchance.Q"), GetBool("Harass", "Harass.UseQ"), GetBool("Harass", "Harass.UseQ2"));
                }

                if (isHealthy && GetBool("Harass", "Harass.UseE") && Spells[E].IsReady() &&
                    target.IsInRange(Spells[E].Range * 3) && !target.Position.ToVector2().PointUnderEnemyTurret())
                {
                    if (target.IsInRange(Spells[E].Range))
                    {
                        Spells[E].CastOnUnit(target);
                        return;
                    }

                    var minion =
                        ObjectManager.Get<AIMinionClient>()
                            .Where(x => x.IsDashable() && !x.Position.ToVector2().PointUnderEnemyTurret())
                            .OrderBy(x => GetDashPos(x).Distance(target.Position))
                            .FirstOrDefault();

                    if (minion != null && GetBool("Harass", "Harass.UseEMinion") && GetDashPos(minion).IsCloser(target))
                    {
                        Spells[E].Cast(minion);
                    }
                }
            }
        }

        void Mixed()
        {
            if (GetBool("Harass", "Harass.InMixed"))
            {
                Harass();
            }
            LHSkills();
        }


        void LHSkills()
        {
            if (SpellSlot.Q.IsReady() && !Yasuo.IsDashing())
            {
                if (!TornadoReady && GetBool("Farm", "Farm.UseQ"))
                {
                    var minion =
                         ObjectManager.Get<AIMinionClient>()
                             .FirstOrDefault(x => x.IsValidMinion(Spells[Q].Range) && x.QCanKill());
                    if (minion != null)
                    {
                        Spells[Q].Cast(minion.Position);
                    }
                }

                else if (TornadoReady && GetBool("Farm", "Farm.UseQ2"))
                {
                    var minions = ObjectManager.Get<AIMinionClient>().Where(x => x.Distance(Yasuo) > Yasuo.AttackRange && x.IsValidMinion(Spells[Q2].Range) && (x.QCanKill()));
                    var pred =
                        FarmPrediction.GetBestLineFarmLocation(minions.Select(m => m.Position.ToVector2()).ToList(),
                            Spells[Q2].Width, Spells[Q2].Range);
                    if (pred.MinionsHit >= GetSliderInt("Farm", "Farm.Qcount"))
                    {
                        Spells[Q2].Cast(pred.Position);
                    }
                }
            }

            if (Spells[E].IsReady() && GetBool("Farm", "Farm.UseE"))
            {
                var minion = ObjectManager.Get<AIMinionClient>().FirstOrDefault(x => x.IsDashable() && x.ECanKill() && (GetBool("Waveclear", "Waveclear.ETower") || ShouldDive(x)));
                if (minion != null)
                {
                    Spells[E].Cast(minion);
                }
            }
        }



        void OnGapClose(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (Yasuo.Position.PointUnderEnemyTurret())
            {
                return;
            }
            if (GetBool("Misc", "Misc.AG") && TornadoReady && Yasuo.Distance(args.EndPosition) <= 500)
            {
                var pred = Spells[Q2].GetPrediction(sender);
                if (pred.Hitchance >= GetHitChance("Hitchance.Q"))
                {
                    Spells[Q2].Cast(pred.CastPosition);
                }
            }
        }

        void OnInterruptable(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (Yasuo.Position.PointUnderEnemyTurret())
            {
                return;
            }
            if (GetBool("Misc", "Misc.Interrupter") && TornadoReady && Yasuo.Distance(sender.Position) <= 500)
            {
                if (args.EndTime >= Spells[Q2].Delay)
                {
                    Spells[Q2].Cast(sender.Position);
                }
            }
        }
    }
}
