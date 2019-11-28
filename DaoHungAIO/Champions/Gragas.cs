using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using EnsoulSharp;
using SharpDX;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI.Values;
using Keys = System.Windows.Forms.Keys;
using SPrediction;
using DaoHungAIO.Helpers;
using EnsoulSharp.SDK.Events;
using Utility = EnsoulSharp.SDK.Utility;

namespace DaoHungAIO.Champions
{
    class Gragas
    {
        public static HpBarIndicator Hpi = new HpBarIndicator();
        public static Menu Config;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Vector3 insecpos;
        public static Vector3 eqpos;
        public static Vector3 movingawaypos;
        public static GameObject Barrel;
        public static SpellSlot Ignite;
        private static readonly AIHeroClient player = ObjectManager.Player;


        public Gragas()
        {

            Q = new Spell(SpellSlot.Q, 850);
            W = new Spell(SpellSlot.W, 0);
            E = new Spell(SpellSlot.E, 600);
            R = new Spell(SpellSlot.R, 1000);

            Q.SetSkillshot(0.3f, 110f, 1000f, false, SkillshotType.Circle);
            E.SetSkillshot(0.0f, 50, 1000, true, SkillshotType.Line);
            R.SetSkillshot(0.3f, 700, 1000, false, SkillshotType.Circle);


            Config = new Menu("Gragas", "DH.Gragas credit dominiquekleeven Lady Gragas", true);

            //COMBOMENU

            var combo = Config.AddSubMenu(new Menu("ComboSettings", "Gragas: Combo Settings"));
            combo.Add(new MenuBool("UseQ", "Use Q - Barell Roll").SetValue(true));
            combo.Add(new MenuBool("autoQ", "Auto Detonate Q").SetValue(true));
            combo.Add(new MenuBool("UseW", "Use W - Drunken Rage ").SetValue(true));
            combo.Add(new MenuBool("UseE", "Use E - Body Slam").SetValue(true));
            combo.Add(new MenuBool("UseR", "Use R - Explosive Cask | FINISHER").SetValue(true));
            combo.Add(new MenuBool("UseRprotector", "Use R - Explosive Cask | PROTECTOR").SetValue(false));
            combo.Add(new MenuSlider("Rhp", "Own HP%").SetValue(new Slider(35, 100, 0)));
            combo.Add(new MenuBool("UseRdmg", "Use R - Explosive Cask | AOE DMG").SetValue(false));
            combo.Add(new MenuSlider("rdmgslider", "Enemy Count").SetValue(new Slider(3, 5, 1)));
            combo.Add(new MenuKeyBind("InsecMode", "Insec Mode - Leftclick on InsecTarget", Keys.K, KeyBindType.Press));



            //HARASSMENU

            Config.AddSubMenu(new Menu("HarassSettings", "Gragas: Harass Settings"));
            Config.SubMenu("HarassSettings")
                .Add(new MenuBool("harassQ", "Use Q - Barell Roll").SetValue(true));
            Config.SubMenu("HarassSettings")
                .Add(new MenuBool("harassE", "Use E - Bodyslam").SetValue(true));
            Config.SubMenu("HarassSettings")
                .Add(new MenuSlider("harassmana", "Mana Percentage").SetValue(new Slider(30, 100, 0)));

            //LANECLEARMENU
            Config.AddSubMenu(new Menu("LaneClearSettings", "Gragas: Laneclear Settings"));
            Config.SubMenu("LaneClearSettings")
                .Add(new MenuBool("laneQ", "Use Q - Barell Roll").SetValue(true));
            Config.SubMenu("LaneClearSettings")
                .Add(new MenuBool("jungleW", "Use W - Drunken Rage").SetValue(true));
            Config.SubMenu("LaneClearSettings")
                .Add(new MenuBool("laneE", "Use E - Bodyslam").SetValue(true));
            Config.SubMenu("LaneClearSettings")
                .Add(new MenuSlider("laneclearmana", "Mana Percentage").SetValue(new Slider(30, 100, 0)));

            //JUNGLEFARMMENU
            Config.AddSubMenu(new Menu("JungleSettings", "Gragas: Jungle Settings"));
            Config.SubMenu("JungleSettings")
                .Add(new MenuBool("jungleQ", "Use Q - Barell Roll").SetValue(true));
            Config.SubMenu("JungleSettings")
                .Add(new MenuBool("jungleW", "Use W - Drunken Rage").SetValue(true));
            Config.SubMenu("JungleSettings")
                .Add(new MenuBool("jungleE", "Use E - Bodyslam").SetValue(true));
            Config.SubMenu("JungleSettings")
                .Add(new MenuSlider("jungleclearmana", "Mana Percentage").SetValue(new Slider(30, 100, 0)));

            //DRAWINGMENU
            Config.AddSubMenu(new Menu("DrawSettings", "Gragas: Draw Settings"));
            Config.SubMenu("DrawSettings")
                .Add(new MenuBool("DrawInsecPosition", "Draw Insec Position").SetValue(true));
            Config.SubMenu("DrawSettings")
                .Add(new MenuBool("Draw_Disabled", "Disable All Spell Drawings").SetValue(false));
            Config.SubMenu("DrawSettings")
                .Add(new MenuBool("Qdraw", "Draw Q Range"));
            Config.SubMenu("DrawSettings")
                .Add(new MenuBool("Wdraw", "Draw W Range"));
            Config.SubMenu("DrawSettings")
                .Add(new MenuBool("Edraw", "Draw E Range"));
            Config.SubMenu("DrawSettings")
                .Add(new MenuBool("Rdraw", "Draw R Range"));
            Config.SubMenu("DrawSettings").Add(new MenuBool("Rrdy", "Draw R - Status").SetValue(true));

            //MISCMENU
            var killsteal = Config.AddSubMenu(new Menu("KillstealSettings", "Gragas: Killsteal Settings"));
            killsteal.Add(new MenuBool("SmartKS", "Use SmartKS").SetValue(true));
            killsteal.Add(new MenuBool("UseIgnite", "Use Ignite").SetValue(true));
            killsteal.Add(new MenuBool("KSQ", "Use Q").SetValue(true));
            killsteal.Add(new MenuBool("KSE", "Use E").SetValue(true));
            killsteal.Add(new MenuBool("RKR", "Use R").SetValue(true));

            Config.AddSubMenu(new Menu("MiscSettings", "Gragas: Misc Settings"));
            Config.SubMenu("MiscSettings").Add(new MenuBool("DrawD", "Damage Indicator").SetValue(true));
            Config.SubMenu("MiscSettings")
                .Add(new MenuBool("AntiGapE", "Use E on Gapclosers").SetValue(true));
            Config.SubMenu("MiscSettings")
                .Add(new MenuBool("AntiGapR", "Use R on Gapclosers").SetValue(false));
            Config.SubMenu("MiscSettings")
                .Add(new MenuBool("EInterrupt", "Use E to Interrupt Spells").SetValue(true));
            Config.SubMenu("MiscSettings")
                .Add(new MenuBool("RInterrupt", "Use R to Interrupt Spells").SetValue(false));

            Config.Attach();

            //Idk what this is called but it's something <3

            Tick.OnTick += Game_OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += OnEndScene;
            GameObject.OnCreate += GragasObject;
            GameObject.OnDelete += GragasBarrelNull;
            Interrupter.OnInterrupterSpell += Interrupter2_OnInterruptableTarget;
            Gapcloser.OnGapcloser += AntiGapCloser_OnEnemyGapcloser;
            Drawing.OnDraw += etcdraw;
        }


        private static float IgniteDamage(AIHeroClient target)
        {
            if (Ignite == SpellSlot.Unknown || player.Spellbook.CanUseSpell(Ignite) != SpellState.Ready)
                return 0f;
            return (float)player.GetSummonerSpellDamage(target, SummonerSpell.Ignite);
        }


        private static void etcdraw(EventArgs args)
        {
            var target = TargetSelector.SelectedTarget;
            if (target == null || target.IsValidTarget(R.Range))
            {
                target = TargetSelector.GetTarget(R.Range);
            }
            if(target == null)
            {
                return;
            }
            var epos = Drawing.WorldToScreen(target.Position);

            if (Config.Item("DrawInsecPosition").GetValue<MenuBool>() && R.IsReady() && target.IsValidTarget(R.Range) && R.Level > 0)

                Drawing.DrawText(epos.X, epos.Y, Color.DarkSeaGreen, "Insec Target");
            if (Config.Item("DrawInsecPosition").GetValue<MenuBool>() && R.IsReady() && target.IsValidTarget(R.Range) && R.Level > 0)
                Render.Circle.DrawCircle(target.Position, 150, Color.LightSeaGreen);
            if (Config.Item("DrawInsecPosition").GetValue<MenuBool>() && R.IsReady() && target.IsValidTarget(R.Range) && R.Level > 0)
                insecpos = player.Position.Extend(target.Position, player.Distance(target) + 150);
            Render.Circle.DrawCircle(insecpos, 100, Color.GreenYellow);
        }

        private static void AntiGapCloser_OnEnemyGapcloser(
    AIHeroClient sender,
    Gapcloser.GapcloserArgs args
)
        {
            if (!sender.IsEnemy || args.EndPosition.DistanceToPlayer() > 200) {
                return;
            }
            if (E.IsReady() && Config.Item("AntiGapE").GetValue<MenuBool>() && E.GetPrediction(sender).Hitchance >= HitChance.High)
                E.Cast(sender);
            if (R.IsReady() && sender.IsValidTarget(R.Range) && Config.Item("AntiGapR").GetValue<MenuBool>())
                R.Cast(sender);

        }

        private static void Interrupter2_OnInterruptableTarget(
    AIHeroClient sender,
    Interrupter.InterruptSpellArgs args
)
        {
            if (R.IsReady() && sender.IsValidTarget(R.Range) && Config.Item("interrupt").GetValue<MenuBool>())
                R.CastIfHitchanceEquals(sender, HitChance.High);
            if (E.IsReady() && sender.IsValidTarget(E.Range) && Config.Item("interrupt").GetValue<MenuBool>())
                E.CastIfHitchanceEquals(sender, HitChance.High);
        }

        private static void GragasBarrelNull(GameObject sender, EventArgs args) //BARREL LOCATION - GONE
        {
            {
            }

            if (sender.Name.Contains("Gragas") && sender.Name.Contains("Q_Ally"))
            {
                Barrel = null;
            }

        }

        private static void Killsteal()
        {
            {

                foreach (var enemy in
                    ObjectManager.Get<AIHeroClient>()
                        .Where(x => x.IsValidTarget(R.Range))
                        .Where(x => !x.IsDead))
                {
                    Ignite = player.GetSpellSlot("summonerdot");
                    var edmg = E.GetDamage(enemy);
                    var qdmg = Q.GetDamage(enemy);
                    var rdmg = R.GetDamage(enemy);
                    var rpred = R.GetPrediction(enemy);
                    var qpred = Q.GetPrediction(enemy);
                    var epred = E.GetPrediction(enemy);




                    if (enemy.Health < edmg && E.IsReady()&& Config.Item("KSE").GetValue<MenuBool>() && epred.Hitchance >= HitChance.VeryHigh)
                        E.Cast(epred.CastPosition);

                    if (enemy.Health < qdmg && qpred.Hitchance >= HitChance.VeryHigh &&
                        Q.IsReady() &&
                        Config.Item("KSQ").GetValue<MenuBool>())

                        Q.Cast(enemy);

                    if (enemy.Health < rdmg && rpred.Hitchance >= HitChance.VeryHigh &&
                        !Q.IsReady() &&
                        Config.Item("KSR").GetValue<MenuBool>())

                        R.Cast(enemy);

                    if (player.Distance(enemy.Position) <= 600 && IgniteDamage(enemy) >= enemy.Health &&
                        Config.Item("UseIgnite").GetValue<MenuBool>() && R.IsReady() && Ignite.IsReady())
                        player.Spellbook.CastSpell(Ignite, enemy);
                }
            }
        }

        public static bool Exploded { get; set; }

        public static void InsecCombo()
        {
            var target = TargetSelector.SelectedTarget;
            if(target == null || !target.IsValidTarget(R.Range))
            {
                return;
            }
            Orbwalker.Orbwalk(null, Game.CursorPos);

            eqpos = player.Position.Extend(target.Position, player.Distance(target));
            insecpos = player.Position.Extend(target.Position, player.Distance(target) + 200);
            movingawaypos = player.Position.Extend(target.Position, player.Distance(target) + 300);
            eqpos = player.Position.Extend(target.Position, player.Distance(target) + 100);

            if (target.IsFacing(player) == false &&
                target.IsMoving & (R.IsInRange(insecpos) && target.Distance(insecpos) < 300))
                R.Cast(movingawaypos);

            if (R.IsInRange(insecpos) && target.Distance(insecpos) < 300 && target.IsFacing(player) && target.IsMoving)
                R.Cast(eqpos);

            else if (R.IsInRange(insecpos) && target.Distance(insecpos) < 300)
                R.Cast(insecpos);

            if (!Exploded) return;

            var prediction = E.GetPrediction(target);
            if (prediction.Hitchance >= HitChance.High)
            {
                E.Cast(target.Position);
                Q.Cast(target.Position);
            }
        }


        private static
            void GragasObject(GameObject sender, EventArgs args) //BARREL LOCATION
        {
            //if(sender.DistanceToPlayer() <= 500)
            //{
            //    Game.Print(sender.Name);
            //}

            if (sender.Name.Contains("Gragas") && sender.Name.Contains("R_End"))
                {
                Exploded = true;
                Utility.DelayAction.Add(3000, () => { Exploded = false; });
            }

             if (sender.Name.Contains("Gragas") && sender.Name.Contains("Q_Ally"))
                {
                Barrel = sender;



            }
        }



        private static bool IsWall(Vector3 pos)
        {
            CollisionFlags cFlags = NavMesh.GetCollisionFlags(pos);
            return (cFlags == CollisionFlags.Wall);
        }





        private static
        void Game_OnGameUpdate(EventArgs args)
        {
            try
            {
                var target = TargetSelector.GetTarget(1500);
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                    LaneClear();
                if (Orbwalker.ActiveMode == OrbwalkerMode.Harass)
                    Harass();
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                    Combo();
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                    eLogic();

                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                    Overkill();

                if (Config.Item("SmartKS").GetValue<MenuBool>())
                {
                    Killsteal();
                }

                if (Config.Item("InsecMode").GetValue<MenuKeyBind>().Active)
                    InsecCombo();

                if (Barrel != null && Barrel.Position.CountEnemyHeroesInRange(275) >= 1)
                    Q.Cast();
            } catch (Exception e)
            {
                Game.Print(e.Message);
            }

        }

        private static void OnDraw(EventArgs args)
        {
            //Draw Skill Cooldown on Champ
            var pos = Drawing.WorldToScreen(ObjectManager.Player.Position);
            if (R.IsReady() && Config.Item("Rrdy").GetValue<MenuBool>())
            {
                Drawing.DrawText(pos.X, pos.Y, Color.Gold, "R is Ready!");
            }

            if (Config.Item("Draw_Disabled").GetValue<MenuBool>())
                return;

            //foreach (var tar in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.IsValidTarget(2000)))
            //{
            //}

            if (Config.Item("Qdraw").GetValue<MenuBool>().Enabled)
                if (Q.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, Q.IsReady() ? Color.Gray : Color.Red);


            if (Config.Item("Wdraw").GetValue<MenuBool>().Enabled)
                if (W.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, W.IsReady() ? Color.Gray : Color.Red);

            if (Config.Item("Edraw").GetValue<MenuBool>().Enabled)
                if (E.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range - 1,
                        E.IsReady() ? Color.Gray : Color.Red);

            if (Config.Item("Rdraw").GetValue<MenuBool>().Enabled)
                if (R.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range - 2,
                        R.IsReady() ? Color.Gray : Color.Red);
        }

        private static void OnEndScene(EventArgs args)
        {
            {
                //Damage Indicator
                if (Config.SubMenu("MiscSettings").Item("DrawD").GetValue<MenuBool>())
                {
                    foreach (var enemy in
                        ObjectManager.Get<AIHeroClient>().Where(ene => !ene.IsDead && ene.IsEnemy && ene.IsVisible))
                    {
                        Hpi.unit = enemy;
                        Hpi.drawDmg(CalcDamage(enemy), Color.Gold);
                    }
                }
            }
        }

        private static int CalcDamage(AIBaseClient target)
        {
            //Calculate Combo Damage

            var aa = player.GetAutoAttackDamage(target);
            var damage = aa;

            if (Config.Item("UseE").GetValue<MenuBool>()) // edamage
            {
                if (E.IsReady())
                {
                    damage += E.GetDamage(target);
                }

                if (W.IsReady() && Config.Item("UseW").GetValue<MenuBool>())
                {
                    damage += W.GetDamage(target);
                }
            }

            if (R.IsReady() && Config.Item("UseR").GetValue<MenuBool>()) // rdamage
            {
                damage += R.GetDamage(target);
            }

            if (Q.IsReady() && Config.Item("UseQ").GetValue<MenuBool>())
            {
                damage += Q.GetDamage(target);
            }
            return (int)damage;
        }


        private static int overkill(AIBaseClient target)
        {
            //overkill protection m8;
            var aa = player.GetAutoAttackDamage(target);
            var damage = aa;

            if (Ignite != SpellSlot.Unknown &&
                player.Spellbook.CanUseSpell(Ignite) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(target, SummonerSpell.Ignite);

            if (Config.Item("UseE").GetValue<MenuBool>()) // edamage
            {
                if (E.IsReady() && target.IsValidTarget(E.Range))
                {
                    damage += E.GetDamage(target);
                }
                if (W.IsReady() && target.IsValidTarget(E.Range))
                {
                    damage += W.GetDamage(target) + aa;
                }

                if (Config.Item("UseQ").GetValue<MenuBool>() && target.IsValidTarget(E.Range + 100) && Q.IsReady())
                {
                    damage += Q.GetDamage(target) * 1;
                }
                return (int)damage;
            }
            return 0;
        }

        private static void Qcast()
        {

        }
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(R.Range);

            if(target == null)
            {
                return;
            }

            var prediction = Q.GetPrediction(target);

            if (prediction.Hitchance >= HitChance.High
                && Q.IsReady() && Config.Item("UseQ").GetValue<MenuBool>() && target.IsValidTarget(Q.Range) && Barrel == null)
                Q.Cast(target);



            if (R.IsReady() && Config.Item("UseRprotector").GetValue<MenuBool>() && player.HealthPercent <= Config.Item("Rhp").GetValue<MenuSlider>().Value)
                R.Cast(player.Position);

            if (R.IsReady() && Config.Item("UseRdmg").GetValue<MenuBool>() &&
                target.Position.CountEnemyHeroesInRange(250) >= Config.Item("rdmgslider").GetValue<MenuSlider>().Value)

                R.Cast(target);

        }

        private static void eLogic()
        {
            var target = TargetSelector.GetTarget(R.Range);
            if(target == null)
            {
                return;
            }
            var prediction = E.GetPrediction(target);
            if (E.IsReady() && target.IsValidTarget(player.GetRealAutoAttackRange()))
                E.Cast(target);
            else if (W.IsReady() && target.IsValidTarget(E.Range) && Config.Item("UseW").GetValue<MenuBool>())
                W.Cast();
            if (W.IsReady() && Config.Item("UseW").GetValue<MenuBool>())
                return;
            if (player.HasBuff("GragasWAttackBuff"))
                E.Cast(target);
            else if (E.IsReady() && target.IsValidTarget(E.Range))
                E.Cast(target);


            if (player.HasBuff("GragasWAttackBuff") && target.IsValidTarget(player.GetRealAutoAttackRange()))
                player.IssueOrder(GameObjectOrder.AttackUnit, target);
        }

        private static void Overkill()
        {
            var target = TargetSelector.GetTarget(R.Range);
            if(target == null)
            {
                return;
            }
            if (R.IsReady() && Config.Item("UseR").GetValue<MenuBool>() && target.IsValidTarget(R.Range) &&
                     R.GetDamage(target) >= target.Health && overkill(target) <= target.Health)

                R.Cast(target.Position);
        }

        private static void Harass()
        {

            var harassmana = Config.Item("harassmana").GetValue<MenuSlider>().Value;
            var t = TargetSelector.GetTarget(Q.Range);
            if(t == null)
            {
                return;
            }
            var qpred = Q.GetPrediction(t);
            var epred = E.GetPrediction(t);
            if (E.IsReady() && Config.Item("harassE").GetValue<MenuBool>() && t.IsValidTarget(E.Range) &&
                player.ManaPercent >= harassmana && epred.Hitchance >= HitChance.High)
                E.Cast(t);
            {
                if (Q.IsReady() && Config.Item("harassQ").GetValue<MenuBool>() && t.IsValidTarget(Q.Range) &&
                    player.ManaPercent >= harassmana && qpred.Hitchance >= HitChance.High)
                    Q.Cast(t);
            }
        }

        private static void LaneClear()
        {
            var lanemana = Config.Item("laneclearmana").GetValue<MenuSlider>().Value;
            var junglemana = Config.Item("jungleclearmana").GetValue<MenuSlider>().Value;
            var jungleQ = GameObjects.GetJungles(ObjectManager.Player.Position, Q.Range + Q.Width + 30);
            var jungleE = GameObjects.GetJungles(ObjectManager.Player.Position, E.Range + E.Width);
            var laneE = GameObjects.GetMinions(ObjectManager.Player.Position, E.Range + E.Width);
            var laneQ = GameObjects.GetMinions(ObjectManager.Player.Position, Q.Range + Q.Width);
            if(jungleE.Count() == 0 && jungleQ.Count() == 0 && laneE.Count() == 0 && laneQ.Count() == 0)
            {
                return;
            }
            var Qjunglepos = Q.GetCircularFarmLocation(jungleQ, Q.Width);
            var Ejunglepos = E.GetLineFarmLocation(jungleE, E.Width);

            var Qfarmpos = Q.GetCircularFarmLocation(laneQ, Q.Width);
            var Efarmpos = E.GetLineFarmLocation(laneE, E.Width);

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && Qjunglepos.MinionsHit >= 1 &&
                Config.Item("jungleQ").GetValue<MenuBool>()
                && player.ManaPercent >= junglemana)
            {
                Q.Cast(Qjunglepos.Position);
                Utility.DelayAction.Add(500, () => Q.Cast(Qjunglepos.Position));
            }
            foreach (var minion in jungleE)
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && minion.IsValidTarget(E.Range) &&
                    Ejunglepos.MinionsHit >= 1 && jungleE.Count >= 1 && Config.Item("jungleE").GetValue<MenuBool>()
                    && player.ManaPercent >= junglemana)
                {
                    E.Cast(minion.Position);
                }
            foreach (var minion in jungleE)
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && minion.IsValidTarget(E.Range) &&
                    Ejunglepos.MinionsHit >= 1 && jungleE.Count >= 1 && Config.Item("jungleE").GetValue<MenuBool>()
                    && player.ManaPercent >= junglemana)
                {
                    W.Cast(player);
                }
            {
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && Qfarmpos.MinionsHit >= 3 &&
                Config.Item("laneQ").GetValue<MenuBool>()
                && player.ManaPercent >= lanemana)
            {
                Q.Cast(Qfarmpos.Position);
                Utility.DelayAction.Add(500, () => Q.Cast(Qfarmpos.Position));
            }

            foreach (var minion in laneE)
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && minion.IsValidTarget(E.Range) &&
                    Efarmpos.MinionsHit >= 2 && laneE.Count >= 2 && Config.Item("laneE").GetValue<MenuBool>()
                    && player.ManaPercent >= lanemana)
                {
                    E.Cast(minion.Position);
                }
            foreach (var minion in laneE)
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && minion.IsValidTarget(E.Range) &&
                    Efarmpos.MinionsHit >= 1 && laneE.Count >= 1 && Config.Item("laneE").GetValue<MenuBool>()
                    && player.ManaPercent >= lanemana)
                {
                    W.Cast(player);
                }
        }
    }
}
