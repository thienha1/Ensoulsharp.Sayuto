using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.Common;
using SharpDX;
using System.Drawing;
using SharpDX.Direct3D9;

namespace DaoHungAIO.Champions
{
    class Camille
    {
        #region Static Fields

        internal static Menu RootMenu;
        internal static Spell Q, W, E, R;
        internal static Orbwalking.Orbwalker Orbwalker;
        internal static AIHeroClient Player => ObjectManager.Player;
        internal static HpBarIndicatorCamille BarIndicator = new HpBarIndicatorCamille();

        internal static bool IsBrawl;
        internal static int LastECastT;
        internal static bool HasQ2 => Player.HasBuff(Q2BuffName);
        internal static bool HasQ => Player.HasBuff(QBuffName);
        internal static bool OnWall => Player.HasBuff(WallBuffName) || E.Instance.Name != "CamilleE";
        internal static bool IsDashing => Player.HasBuff(EDashBuffName + "1") || Player.HasBuff(EDashBuffName + "2") || Player.IsDashing();
        internal static bool ChargingW => Player.HasBuff(WBuffName);
        internal static bool KnockedBack(AIBaseClient target) => target != null && target.HasBuff(KnockBackBuffName);

        internal static string WBuffName => "camillewconeslashcharge";
        internal static string EDashBuffName => "camilleedash";
        internal static string WallBuffName => "camilleedashtoggle";
        internal static string QBuffName => "camilleqprimingstart";
        internal static string Q2BuffName => "camilleqprimingcomplete";
        internal static string RBuffName => "camillertether";
        internal static string KnockBackBuffName => "camilleeknockback2";

        #endregion

        #region Collections

        internal static Dictionary<float, DangerPos> DangerPoints = new Dictionary<float, DangerPos>();

        #endregion

        #region Properties

        // general
        internal static bool AllowSkinChanger => RootMenu.Item("useskin").GetValue<bool>();
        internal static bool ForceUltTarget => RootMenu.Item("r33").GetValue<bool>();

        // keybinds
        internal static bool FleeModeActive => RootMenu.Item("useflee").GetValue<KeyBind>().Active;

        // sliders
        internal static int HarassMana => RootMenu.Item("harassmana").GetValue<Slider>().Value;
        internal static int WaveClearMana => RootMenu.Item("wcclearmana").GetValue<Slider>().Value;
        internal static int JungleClearMana => RootMenu.Item("jgclearmana").GetValue<Slider>().Value;

        #endregion
        public Camille()
        {
            try
            {
                SetupSpells();
                SetupConfig();

                #region Subscribed Events

                Game.OnTick += Game_OnUpdate;
                Drawing.OnDraw += Drawing_OnDraw;
                Drawing.OnEndScene += Drawing_OnEndScene;
                AIBaseClient.OnDoCast += AIBaseClient_OnDoCast;
                EnsoulSharp.Player.OnIssueOrder += CamilleOnIssueOrder;
                AIBaseClient.OnDoCast += AIBaseClient_OnProcessSpellCast;
                GameObject.OnCreate += EffectEmitter_OnCreate;
                Interrupters.OnInterrupter += Interrupter2_OnInterruptableTarget;

                #endregion

                var color = System.Drawing.Color.FromArgb(200, 0, 220, 144);
                var hexargb = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }



        private static void AIBaseClient_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            var attacker = sender as AIHeroClient;
            if (attacker != null && attacker.IsEnemy && attacker.Distance(Player) <= R.Range + 25)
            {
                var aiTarget = args.Target as AIHeroClient;

                var tsTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                if (tsTarget == null)
                {
                    return;
                }

                if (R.IsReady() && RootMenu.Item("revade").GetValue<bool>())
                {
                    foreach (var spell in Evadeable.DangerList.Select(entry => entry.Value)
                        .Where(spell => spell.SDataName.ToLower() == args.SData.Name.ToLower())
                        .Where(spell => RootMenu.Item("revade" + spell.SDataName.ToLower()).GetValue<bool>()))
                    {
                        switch (spell.EvadeType)
                        {
                            case EvadeType.Target:
                                if (aiTarget != null && aiTarget.IsMe)
                                {
                                    UseR(tsTarget, true);
                                }
                                break;

                            case EvadeType.SelfCast:
                                if (attacker.Distance(Player) <= R.Range)
                                {
                                    UseR(tsTarget, true);
                                }
                                break;
                            case EvadeType.SkillshotLine:
                                var lineStart = args.Start.To2D();
                                var lineEnd = args.End.To2D();

                                if (lineStart.Distance(lineEnd) < R.Range)
                                    lineEnd = lineStart + (lineEnd - lineStart).Normalized() * R.Range + 25;

                                if (lineStart.Distance(lineEnd) > R.Range)
                                    lineEnd = lineStart + (lineEnd - lineStart).Normalized() * R.Range * 2;

                                var spellProj = Player.Position.To2D().ProjectOn(lineStart, lineEnd);
                                if (spellProj.IsOnSegment)
                                {
                                    UseR(tsTarget, true);
                                }
                                break;

                            case EvadeType.SkillshotCirce:
                                var curStart = args.Start.To2D();
                                var curEnd = args.End.To2D();

                                if (curStart.Distance(curEnd) > R.Range)
                                    curEnd = curStart + (curEnd - curStart).Normalized() * R.Range;

                                if (curEnd.Distance(Player) <= R.Range)
                                {
                                    UseR(tsTarget, true);
                                }
                                break;
                        }
                    }
                }
            }
        }

        private static void Interrupter2_OnInterruptableTarget(ActiveInterrupter interrupter)
        {
            if (RootMenu.Item("interrupt2").GetValue<bool>())
            {
                if (interrupter.Sender.IsValidTarget(E.Range) && E.IsReady())
                {
                    UseE(interrupter.Sender.Position);
                }
            }
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (RootMenu.Item("drawhpbarfill").GetValue<bool>())
            {
                foreach (
                    var enemy in
                        ObjectManager.Get<AIHeroClient>()
                            .Where(ene => ene.IsValidTarget() && !ene.IsZombie))
                {
                    var color = R.IsReady() && EasyKill(enemy)
                        ? new ColorBGRA(0, 255, 0, 90)
                        : new ColorBGRA(255, 255, 0, 90);

                    BarIndicator.unit = enemy;
                    BarIndicator.drawDmg((float)Cdmg(enemy), color);
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            var eCircle = RootMenu.Item("drawmyehehe").GetValue<Circle>();
            if (eCircle.Active)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, eCircle.Color);
            }

            var wCircle = RootMenu.Item("drawmywhehe").GetValue<Circle>();
            if (wCircle.Active)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, wCircle.Color);
            }

            var rCircle = RootMenu.Item("drawmyrhehe").GetValue<Circle>();
            if (rCircle.Active)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, rCircle.Color);
            }
        }

        private static void EffectEmitter_OnCreate(GameObject sender, EventArgs args)
        {
            var emitter = sender as EffectEmitter;
            if (emitter != null && emitter.Name.ToLower() == "camille_base_r_indicator_edge.troy")
            {
                DangerPoints[Game.Time] = new DangerPos(emitter, AvoidType.Outside, 450f); // 450f ?
            }

            if (emitter != null && emitter.Name.ToLower() == "veigar_base_e_cage_red.troy")
            {
                DangerPoints[Game.Time] = new DangerPos(emitter, AvoidType.Inside, 400f); // 400f ?
            }
        }

        private static bool UltEnemies()
        {
            return RootMenu.SubMenu("cmenu").SubMenu("abmenu").SubMenu("whemenu").Items.Any(i => i.GetValue<bool>()) &&
                 Player.GetEnemiesInRange(E.Range * 2).Any(ez => RootMenu.Item("whR" + ez.CharacterName).GetValue<bool>());
        }

        private static void CamilleOnIssueOrder(AIBaseClient sender, PlayerIssueOrderEventArgs args)
        {
            if (OnWall && E.IsReady() && RootMenu.Item("usecombo").GetValue<KeyBind>().Active)
            {
                var issueOrderPos = args.TargetPosition;
                if (sender.IsMe && args.Order == GameObjectOrder.MoveTo)
                {
                    var issueOrderDirection = (issueOrderPos - Player.Position).To2D().Normalized();

                    var aiHero = TargetSelector.GetTarget(E.Range + 100, TargetSelector.DamageType.Physical);
                    if (aiHero != null)
                    {
                        var heroDirection = (aiHero.Position - Player.Position).To2D().Normalized();
                        if (heroDirection.AngleBetween(issueOrderDirection) > 10)
                        {
                            var anyDangerousPos = false;
                            var dashEndPos = Player.Position.To2D() + heroDirection * Player.Distance(aiHero.Position);

                            if (Player.Position.To2D().Distance(dashEndPos) > E.Range)
                                dashEndPos = Player.Position.To2D() + heroDirection * E.Range;

                            foreach (var x in DangerPoints)
                            {
                                var obj = x.Value;
                                if (obj.Type == AvoidType.Outside && dashEndPos.Distance(obj.Emitter.Position) > obj.Radius)
                                {
                                    anyDangerousPos = true;
                                    break;
                                }

                                if (obj.Type == AvoidType.Inside)
                                {
                                    var proj = obj.Emitter.Position.To2D().ProjectOn(Player.Position.To2D(), dashEndPos);
                                    if (proj.IsOnSegment && proj.SegmentPoint.Distance(obj.Emitter.Position) <= obj.Radius)
                                    {
                                        anyDangerousPos = true;
                                        break;
                                    }
                                }
                            }

                            if (dashEndPos.To3D().UnderTurret(true) && RootMenu.Item("eturret").GetValue<KeyBind>().Active)
                                anyDangerousPos = true;

                            if (anyDangerousPos)
                            {
                                args.Process = false;
                            }
                            else
                            {
                                args.Process = false;

                                var poutput = E.GetPrediction(aiHero);
                                if (poutput.Hitchance >= HitChance.Medium)
                                {
                                    Player.IssueOrder(GameObjectOrder.MoveTo, poutput.CastPosition, false);
                                }
                            }
                        }
                    }
                }
            }

            if (OnWall && E.IsReady() && RootMenu.Item("usejgclear").GetValue<KeyBind>().Active)
            {
                var issueOrderPos = args.TargetPosition;
                if (sender.IsMe && args.Order == GameObjectOrder.MoveTo)
                {
                    var issueOrderDirection = (issueOrderPos - Player.Position).To2D().Normalized();

                    var aiMob = MinionManager.GetMinions(Player.Position, W.Range + 100,
                        MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();

                    if (aiMob != null)
                    {
                        //var heroDirection = (aiMob.Position - Player.Position).To2D().Normalized();
                        //if (heroDirection.AngleBetween(issueOrderDirection) > 10)
                        //{
                        args.Process = false;
                        Player.IssueOrder(GameObjectOrder.MoveTo, aiMob.Position, false);
                        //}
                    }
                }
            }
        }

        private static void AIBaseClient_OnDoCast(
    AIBaseClient sender,
    AIBaseClientProcessSpellCastEventArgs args
)
        {
            if (sender.IsMe && args.SData.IsAutoAttack())
            {
                var aiHero = args.Target as AIHeroClient;
                if (aiHero.IsValidTarget())
                {
                    if (!Player.UnderTurret(true) || RootMenu.Item("usecombo").GetValue<KeyBind>().Active)
                    {
                        if (!Q.IsReady() || HasQ && !HasQ2)
                        {
                            if (Items.CanUseItem(3077))
                                Items.UseItem(3077);
                            if (Items.CanUseItem(3074))
                                Items.UseItem(3074);
                            if (Items.CanUseItem(3748))
                                Items.UseItem(3748);
                        }
                    }
                }

                if (RootMenu.Item("usecombo").GetValue<KeyBind>().Active)
                {
                    if (aiHero.IsValidTarget() && RootMenu.Item("useqcombo").GetValue<bool>())
                    {
                        UseQ(aiHero);
                    }
                }

                if (RootMenu.Item("useharass").GetValue<KeyBind>().Active)
                {
                    if (aiHero.IsValidTarget())
                    {
                        if (Player.Mana / Player.MaxMana * 100 < RootMenu.Item("harassmana").GetValue<Slider>().Value)
                        {
                            return;
                        }

                        UseQ(aiHero);
                    }
                }

                if (RootMenu.Item("usejgclear").GetValue<KeyBind>().Active)
                {
                    var aiMob = args.Target as AIMinionClient;
                    if (aiMob != null && aiMob.IsValidTarget())
                    {
                        if (!Player.UnderTurret(true) || Player.CountEnemiesInRange(1000) <= 0)
                        {
                            if (!Q.IsReady() || HasQ && !HasQ2)
                            {
                                if (RootMenu.Item("t11").GetValue<bool>())
                                {
                                    if (!aiMob.IsMinion || (Player.CountEnemiesInRange(900) < 1
                                                            || !RootMenu.Item("clearnearenemy").GetValue<bool>() ||
                                                            Player.UnderAllyTurret()))
                                    {
                                        if (Items.CanUseItem(3077))
                                            Items.UseItem(3077);
                                        if (Items.CanUseItem(3074))
                                            Items.UseItem(3074);
                                        if (Items.CanUseItem(3748))
                                            Items.UseItem(3748);
                                    }
                                }
                            }
                        }
                    }

                    #region AA-> Q any attackable
                    var unit = args.Target as AttackableUnit;
                    if (unit != null)
                    {
                        if (Player.CountEnemiesInRange(1000) < 1 || Player.UnderAllyTurret()
                            || !RootMenu.Item("clearnearenemy").GetValue<bool>())
                        {
                            // if jungle minion
                            var m = unit as AIMinionClient;
                            if (m != null)
                            {
                                if (!m.CharacterName.StartsWith("sru_plant") && !m.Name.StartsWith("Minion"))
                                {
                                    #region AA -> Q

                                    if (Q.IsReady() && RootMenu.Item("useqjgclear").GetValue<bool>())
                                    {
                                        if (m.Position.Distance(Player.Position) <= Q.Range + 90)
                                        {
                                            UseQ(m);
                                        }
                                    }

                                    #endregion
                                }
                            }

                            if (Q.IsReady() && !unit.Name.StartsWith("Minion"))
                            {
                                if (RootMenu.Item("useqjgclear").GetValue<bool>())
                                {
                                    UseQ(unit);
                                }
                            }
                        }
                    }

                    #endregion
                }

                if (RootMenu.Item("usewcclear").GetValue<KeyBind>().Active)
                {
                    var aiMob = args.Target as AIMinionClient;
                    if (aiMob != null && aiMob.IsValidTarget())
                    {
                        if (!Player.UnderTurret(true) || Player.CountEnemiesInRange(1000) <= 0)
                        {
                            if (!Q.IsReady() || HasQ && !HasQ2)
                            {
                                if (RootMenu.Item("t11").GetValue<bool>())
                                {
                                    if (!aiMob.IsMinion || (Player.CountEnemiesInRange(900) < 1
                                                            || !RootMenu.Item("clearnearenemy").GetValue<bool>() ||
                                                            Player.UnderAllyTurret()))
                                    {
                                        if (Items.CanUseItem(3077))
                                            Items.UseItem(3077);
                                        if (Items.CanUseItem(3074))
                                            Items.UseItem(3074);
                                        if (Items.CanUseItem(3748))
                                            Items.UseItem(3748);
                                    }
                                }
                            }
                        }
                    }

                    var aiBase = args.Target as AIBaseClient;
                    if (aiBase != null && aiBase.IsValidTarget() && aiBase.Name.StartsWith("Minion"))
                    {
                        #region LaneClear Q

                        if (Player.CountEnemiesInRange(1000) < 1 || Player.UnderAllyTurret()
                            || !RootMenu.Item("clearnearenemy").GetValue<bool>())
                        {
                            if (aiBase.UnderTurret(true) && Player.CountEnemiesInRange(1000) > 0 && !Player.UnderAllyTurret())
                            {
                                return;
                            }

                            if (Player.Mana / Player.MaxMana * 100 < RootMenu.Item("wcclearmana").GetValue<Slider>().Value)
                            {
                                if (Player.CountEnemiesInRange(1000) > 0 && !Player.UnderAllyTurret())
                                {
                                    return;
                                }
                            }

                            #region AA -> Q 

                            if (Q.IsReady() && RootMenu.Item("useqwcclear").GetValue<bool>())
                            {
                                if (aiBase.Distance(Player.Position) <= Q.Range + 90)
                                {
                                    UseQ(aiBase);
                                }
                            }

                            #endregion
                        }

                        #endregion
                    }

                    #region AA-> Q any attackable
                    var unit = args.Target as AttackableUnit;
                    if (unit != null)
                    {
                        if (Player.CountEnemiesInRange(1000) < 1 || Player.UnderAllyTurret()
                            || !RootMenu.Item("clearnearenemy").GetValue<bool>())
                        {
                            // if jungle minion
                            var m = unit as AIMinionClient;
                            if (m != null && !m.CharacterName.StartsWith("sru_plant"))
                            {
                                #region AA -> Q

                                if (Q.IsReady() && RootMenu.Item("useqwcclear").GetValue<bool>())
                                {
                                    if (m.Position.Distance(Player.Position) <= Q.Range + 90)
                                    {
                                        UseQ(m);
                                    }
                                }

                                #endregion
                            }

                            if (Q.IsReady())
                            {
                                if (RootMenu.Item("useqwcclear").GetValue<bool>())
                                {
                                    UseQ(unit);
                                }
                            }
                        }
                    }

                    #endregion
                }
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            // an ok check for teamfighting (sfx style)
            IsBrawl = Player.CountAlliesInRange(1500) >= 2
                && Player.CountEnemiesInRange(1350) > 2
                || Player.CountEnemiesInRange(1200) > 3;

            // turn off orbwalk attack while charging to allow movement
            Orbwalker.SetAttack(!ChargingW);

            // remove danger positions
            foreach (var entry in DangerPoints)
            {
                var ultimatum = entry.Value.Emitter;
                if (ultimatum.IsValid == false || ultimatum.IsVisibleOnScreen == false)
                {
                    DangerPoints.Remove(entry.Key);
                    break;
                }

                var timestamp = entry.Key;
                if (Game.Time - timestamp > 4f)
                {
                    DangerPoints.Remove(timestamp);
                    break;
                }
            }

            if (FleeModeActive)
            {
                Orbwalking.Orbwalk(null, Game.CursorPosRaw);
                UseE(Game.CursorPosRaw, false);
            }

            //if (AllowSkinChanger)
            //{
            //    Player.SetSkin(Player.CharData.BaseSkinName, RootMenu.Item("skinid").GetValue<Slider>().Value);
            //}

            if (ForceUltTarget)
            {
                var rtarget = HeroManager.Enemies.FirstOrDefault(x => x.HasBuff(RBuffName));
                if (rtarget != null && rtarget.IsValidTarget() && !rtarget.IsZombie)
                {
                    if (rtarget.Distance(Player) <= Player.AttackRange + Player.Distance(Player.BBox.Minimum) + 75)
                    {
                        TargetSelector.SetTarget(rtarget);
                        Orbwalker.ForceTarget(rtarget);
                    }
                }
            }

            if (IsDashing || OnWall || Player.IsDead)
            {
                return;
            }

            if (RootMenu.Item("usecombo").GetValue<KeyBind>().Active)
            {
                Combo();
            }

            if (RootMenu.Item("usewcclear").GetValue<KeyBind>().Active)
            {
                if (Player.Mana / Player.MaxMana * 100 > WaveClearMana)
                {
                    Clear();
                }
            }

            if (RootMenu.Item("usejgclear").GetValue<KeyBind>().Active)
            {
                if (Player.Mana / Player.MaxMana * 100 > JungleClearMana)
                {
                    Clear();
                }
            }

            if (RootMenu.Item("useharass").GetValue<KeyBind>().Active)
            {
                if (Player.Mana / Player.MaxMana * 100 > HarassMana)
                {
                    Harass();
                }
            }
        }

        #region Setup

        static void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 325f);
            W = new Spell(SpellSlot.W, 610f);

            E = new Spell(SpellSlot.E, 800f);
            E.SetSkillshot(0.125f, ObjectManager.Player.BoundingRadius, 1750, false, SkillshotType.SkillshotLine);

            R = new Spell(SpellSlot.R, 475f);
        }

        static void SetupConfig()
        {
            RootMenu = new Menu("DH.Camille credit Kurisu", "camille", true);

            var omenu = new Menu("-] Orbwalk", "orbwalk");
            Orbwalker = new Orbwalking.Orbwalker(omenu);
            RootMenu.AddSubMenu(omenu);

            var kemenu = new Menu("-] Keys", "kemenu");
            kemenu.AddItem(new MenuItem("usecombo", "Combo [active]")).SetValue(new KeyBind(32, KeyBindType.Press));
            kemenu.AddItem(new MenuItem("useharass", "Harass [active]")).SetValue(new KeyBind('G', KeyBindType.Press));
            kemenu.AddItem(new MenuItem("usewcclear", "Wave Clear [active]")).SetValue(new KeyBind('V', KeyBindType.Press));
            kemenu.AddItem(new MenuItem("usejgclear", "Jungle Clear [active]")).SetValue(new KeyBind('V', KeyBindType.Press));
            kemenu.AddItem(new MenuItem("useflee", "Flee [active]")).SetValue(new KeyBind('A', KeyBindType.Press));
            RootMenu.AddSubMenu(kemenu);

            var comenu = new Menu("-] Combo", "cmenu");

            var tcmenu = new Menu("-] Extra", "tcmenu");

            var abmenu = new Menu("-] Skills", "abmenu");

            var whemenu = new Menu("R Focus Targets", "whemenu").SetFontStyle(FontStyle.Regular, SharpDX.Color.Cyan);
            foreach (var hero in HeroManager.Enemies)
                whemenu.AddItem(new MenuItem("whR" + hero.CharacterName, hero.CharacterName))
                    .SetValue(false).DontSave();
            abmenu.AddSubMenu(whemenu);

            abmenu.AddItem(new MenuItem("useqcombo", "Use Q")).SetValue(true);
            abmenu.AddItem(new MenuItem("usewcombo", "Use W")).SetValue(true);
            abmenu.AddItem(new MenuItem("useecombo", "Use E")).SetValue(true);
            abmenu.AddItem(new MenuItem("usercombo", "Use R")).SetValue(true);

            var revade = new Menu("-] Evade", "revade");
            revade.AddItem(new MenuItem("revade", "Use R to Evade")).SetValue(true);

            foreach (var spell in from entry in Evadeable.DangerList
                                  select entry.Value
                into spell
                                  from hero in HeroManager.Enemies.Where(x => x.CharacterName.ToLower() == spell.ChampionName.ToLower())
                                  select spell)
            {
                revade.AddItem(new MenuItem("revade" + spell.SDataName.ToLower(), "-> " + spell.ChampionName + " R"))
                    .SetValue(true);
            }

            var mmenu = new Menu("-] Magnet", "mmenu");
            mmenu.AddItem(new MenuItem("lockw", "Magnet W [Beta]")).SetValue(true);
            mmenu.AddItem(new MenuItem("lockwcombo", "-> Combo")).SetValue(true);
            mmenu.AddItem(new MenuItem("lockwharass", "-> Harass")).SetValue(true);
            mmenu.AddItem(new MenuItem("lockwclear", "-> Clear")).SetValue(true);
            mmenu.AddItem(new MenuItem("lockorbwalk", "Magnet Orbwalking"))
                .SetValue(false);

            tcmenu.AddItem(new MenuItem("r55", "Only R Selected Target")).SetValue(false);
            tcmenu.AddItem(new MenuItem("r33", "Orbwalk Focus R Target")).SetValue(true);
            tcmenu.AddItem(new MenuItem("eturret", "Dont E Under Turret")).SetValue(new KeyBind('L', KeyBindType.Toggle, true)).Permashow();
            tcmenu.AddItem(new MenuItem("minerange", "Minimum E Range")).SetValue(new Slider(165, 0, (int)E.Range));
            tcmenu.AddItem(new MenuItem("enhancede", "Enhanced E Precision")).SetValue(false);
            tcmenu.AddItem(new MenuItem("www", "Expirimental Combo(W -> E)")).SetValue(false);
            comenu.AddSubMenu(tcmenu);

            comenu.AddSubMenu(revade);
            comenu.AddSubMenu(mmenu);
            comenu.AddSubMenu(abmenu);

            RootMenu.AddSubMenu(comenu);


            var hamenu = new Menu("-] Harass", "hamenu");
            hamenu.AddItem(new MenuItem("useqharass", "Use Q")).SetValue(true);
            hamenu.AddItem(new MenuItem("usewharass", "Use W")).SetValue(true);
            hamenu.AddItem(new MenuItem("harassmana", "Harass Mana %")).SetValue(new Slider(65));
            RootMenu.AddSubMenu(hamenu);

            var clmenu = new Menu("-] Clear", "clmenu");

            var jgmenu = new Menu("Jungle", "jgmenu");
            jgmenu.AddItem(new MenuItem("jgclearmana", "Minimum Mana %")).SetValue(new Slider(35));
            jgmenu.AddItem(new MenuItem("useqjgclear", "Use Q")).SetValue(true);
            jgmenu.AddItem(new MenuItem("usewjgclear", "Use W")).SetValue(true);
            jgmenu.AddItem(new MenuItem("useejgclear", "Use E")).SetValue(true);
            clmenu.AddSubMenu(jgmenu);

            var wcmenu = new Menu("WaveClear", "wcmenu");
            wcmenu.AddItem(new MenuItem("wcclearmana", "Minimum Mana %")).SetValue(new Slider(55));
            wcmenu.AddItem(new MenuItem("useqwcclear", "Use Q")).SetValue(true);
            wcmenu.AddItem(new MenuItem("usewwcclear", "Use W")).SetValue(true);
            wcmenu.AddItem(new MenuItem("usewwcclearhit", "-> Min Hit >=")).SetValue(new Slider(3, 1, 6));
            clmenu.AddSubMenu(wcmenu);

            clmenu.AddItem(new MenuItem("clearnearenemy", "Dont Clear Near Enemy")).SetValue(true);
            clmenu.AddItem(new MenuItem("t11", "Use Hydra")).SetValue(true);

            RootMenu.AddSubMenu(clmenu);

            var fmenu = new Menu("-] Flee", "fmenu");
            fmenu.AddItem(new MenuItem("useeflee", "Use E")).SetValue(true);
            RootMenu.AddSubMenu(fmenu);

            var exmenu = new Menu("-] Events", "exmenu");
            exmenu.AddItem(new MenuItem("interrupt2", "Interrupt")).SetValue(false);
            exmenu.AddItem(new MenuItem("antigapcloserx", "Anti-Gapcloser")).SetValue(false);
            RootMenu.AddSubMenu(exmenu);

            //var skmenu = new Menu("-] Skins", "skmenu");
            //var skinitem = new MenuItem("useskin", "Enabled");
            //skmenu.AddItem(skinitem).SetValue(false);

            //skinitem.ValueChanged += (sender, eventArgs) =>
            //{
            //    if (!eventArgs.GetNewValue<bool>())
            //    {
            //        ObjectManager.Player.SetSkin(ObjectManager.Player.CharData.BaseSkinName, ObjectManager.Player.BaseSkinId);
            //    }
            //};

            //skmenu.AddItem(new MenuItem("skinid", "Skin Id")).SetValue(new Slider(1, 0, 4));
            //RootMenu.AddSubMenu(skmenu);

            var drmenu = new Menu("-] Draw", "drmenu");
            drmenu.AddItem(new MenuItem("drawhpbarfill", "Draw HPBarFill")).SetValue(true);
            drmenu.AddItem(new MenuItem("drawmyehehe", "Draw E")).SetValue(new Circle(true, System.Drawing.Color.FromArgb(165, 0, 220, 144)));
            drmenu.AddItem(new MenuItem("drawmywhehe", "Draw W")).SetValue(new Circle(true, System.Drawing.Color.FromArgb(165, 37, 230, 255)));
            drmenu.AddItem(new MenuItem("drawmyrhehe", "Draw R")).SetValue(new Circle(true, System.Drawing.Color.FromArgb(165, 0, 220, 144)));
            RootMenu.AddSubMenu(drmenu);

        }

        #endregion

        #region Modes

        static void Combo()
        {
            var target = TargetSelector.GetTarget(E.IsReady() ? E.Range * 2 : W.Range, TargetSelector.DamageType.Physical);
            if (target.IsValidTarget() && !target.IsZombie)
            {
                if (RootMenu.Item("lockwcombo").GetValue<bool>())
                {
                    LockW(target);
                }

                if (RootMenu.Item("usewcombo").GetValue<bool>())
                {
                    if (!E.IsReady() || !RootMenu.Item("useecombo").GetValue<bool>())
                    {
                        UseW(target);
                    }
                }

                if (RootMenu.Item("useecombo").GetValue<bool>())
                {
                    UseE(target.Position);
                }

                if (RootMenu.Item("usercombo").GetValue<bool>())
                {
                    UseR(target);
                }
            }
        }

        static void Harass()
        {
            var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (target.IsValidTarget() && !target.IsZombie)
            {
                if (RootMenu.Item("lockwharass").GetValue<bool>())
                {
                    LockW(target);
                }

                if (RootMenu.Item("usewharass").GetValue<bool>())
                {
                    UseW(target);
                }
            }
        }

        private static void Clear()
        {
            var minions = MinionManager.GetMinions(Player.Position, W.Range,
                MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);

            foreach (var unit in minions)
            {
                if (!unit.Name.Contains("Mini")) // mobs
                {
                    if (RootMenu.Item("lockwclear").GetValue<bool>())
                    {
                        LockW(unit);
                    }

                    if (RootMenu.Item("usewjgclear").GetValue<bool>())
                    {
                        UseW(unit);
                    }

                    if (!W.IsReady() || !RootMenu.Item("usewjgclear").GetValue<bool>())
                    {
                        if (!ChargingW && RootMenu.Item("useejgclear").GetValue<bool>())
                        {
                            if (Player.CountEnemiesInRange(1200) <= 0 || !RootMenu.Item("clearnearenemy").GetValue<bool>())
                            {
                                UseE(unit.Position, false);
                            }
                        }
                    }
                }
                else // minions
                {
                    if (RootMenu.Item("lockwclear").GetValue<bool>())
                    {
                        LockW(unit);
                    }

                    if (Player.CountEnemiesInRange(1000) < 1 || !RootMenu.Item("clearnearenemy").GetValue<bool>())
                    {
                        if (RootMenu.Item("usewwcclear").GetValue<bool>() && W.IsReady())
                        {
                            var farmradius =
                                MinionManager.GetBestCircularFarmLocation(
                                    minions.Where(x => x.IsMinion).Select(x => x.Position.To2D()).ToList(), 165f, W.Range);

                            if (farmradius.MinionsHit >= RootMenu.Item("usewwcclearhit").GetValue<Slider>().Value)
                            {
                                W.Cast(farmradius.Position);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Skills

        static void UseQ(AttackableUnit t)
        {
            if (Q.IsReady())
            {
                if (!HasQ || HasQ2)
                {
                    if (Q.Cast())
                    {
                        return;
                    }
                }
                else
                {
                    var aiHero = t as AIHeroClient;
                    if (aiHero != null && Qdmg(aiHero, false) + Player.GetAutoAttackDamage(aiHero, true) * 1 >= aiHero.Health)
                    {
                        if (Q.Cast())
                        {
                            return;
                        }
                    }
                }
            }
        }

        static void UseW(AIBaseClient target)
        {
            if (ChargingW || IsDashing || OnWall || !CanW(target))
            {
                return;
            }

            if (KnockedBack(target))
            {
                return;
            }

            if (W.IsReady() && target.Distance(Player.Position) <= W.Range)
            {
                W.Cast(target.Position);
            }
        }

        static void UseE(Vector3 p, bool combo = true)
        {
            if (IsDashing || OnWall || ChargingW || !E.IsReady())
            {
                return;
            }

            if (combo)
            {
                if (Player.Spellbook.GetSpell(E.Slot).Name == "CamilleE" && Player.Distance(p) < RootMenu.Item("minerange").GetValue<Slider>().Value)
                {
                    return;
                }

                if (p.UnderTurret(true) && RootMenu.Item("eturret").GetValue<KeyBind>().Active)
                {
                    return;
                }
            }

            var posChecked = 0;
            var maxPosChecked = 40;
            var posRadius = 145;
            var radiusIndex = 0;

            if (RootMenu.Item("enhancede").GetValue<bool>())
            {
                maxPosChecked = 80;
                posRadius = 65;
            }

            var candidatePosList = new List<Vector2>();

            while (posChecked < maxPosChecked)
            {
                radiusIndex++;

                var curRadius = radiusIndex * (0x2 * posRadius);
                var curCurcleChecks = (int)Math.Ceiling((0x2 * Math.PI * curRadius) / (0x2 * (double)posRadius));

                for (var i = 1; i < curCurcleChecks; i++)
                {
                    posChecked++;

                    var cRadians = (0x2 * Math.PI / (curCurcleChecks - 0x1)) * i;
                    var xPos = (float)Math.Floor(p.X + curRadius * Math.Cos(cRadians));
                    var yPos = (float)Math.Floor(p.Y + curRadius * Math.Sin(cRadians));
                    var desiredPos = new Vector2(xPos, yPos);
                    var anyDangerousPos = false;

                    foreach (var x in DangerPoints)
                    {
                        var obj = x.Value;
                        if (obj.Type == AvoidType.Outside && desiredPos.Distance(obj.Emitter.Position) > obj.Radius)
                        {
                            anyDangerousPos = true;
                            break;
                        }

                        if (obj.Type == AvoidType.Inside)
                        {
                            var proj = obj.Emitter.Position.To2D().ProjectOn(desiredPos, p.To2D());
                            if (proj.IsOnSegment && proj.SegmentPoint.Distance(obj.Emitter.Position) <= obj.Radius)
                            {
                                anyDangerousPos = true;
                                break;
                            }
                        }
                    }

                    if (anyDangerousPos)
                    {
                        continue;
                    }

                    var wtarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                    if (wtarget != null && ChargingW)
                    {
                        if (desiredPos.Distance(wtarget.Position) > W.Range - 100)
                        {
                            continue;
                        }
                    }

                    if (desiredPos.IsWall())
                    {
                        candidatePosList.Add(desiredPos);
                    }
                }
            }

            var bestWallPoint =
                candidatePosList.Where(x => Player.Distance(x) <= E.Range && x.Distance(p) <= E.Range)
                    .OrderBy(x => x.Distance(p))
                    .FirstOrDefault();

            if (E.IsReady() && bestWallPoint.IsValid())
            {
                if (W.IsReady() && RootMenu.Item("usewcombo").GetValue<bool>() && combo)
                {
                    W.UpdateSourcePosition(bestWallPoint.To3D(), bestWallPoint.To3D());

                    if (RootMenu.Item("www").GetValue<bool>())
                    {
                        int dashSpeedEst = 1450;
                        int hookSpeedEst = 1250;

                        float e1Time = 1000 * (Player.Distance(bestWallPoint) / hookSpeedEst);
                        float meToWall = e1Time + (1000 * (Player.Distance(bestWallPoint) / dashSpeedEst));
                        float wallToHero = (1000 * (bestWallPoint.Distance(p) / dashSpeedEst));

                        var travelTime = 250 + meToWall + wallToHero;
                        if (travelTime >= 1250 && travelTime <= 1750)
                        {
                            W.Cast(p);
                        }

                        if (travelTime > 1750)
                        {
                            var delay = 100 + (travelTime - 1750);
                            Utility.DelayAction.Add((int)delay, () => W.Cast(p));
                        }
                    }
                }

                if (E.Cast(bestWallPoint))
                {
                    LastECastT = Utils.GameTimeTickCount;
                }
            }
        }

        static void UseR(AIHeroClient target, bool force = false)
        {
            if (R.IsReady() && force)
            {
                R.CastOnUnit(target);
            }

            if (target.Distance(Player) <= R.Range)
            {
                if (RootMenu.Item("r55").GetValue<bool>())
                {
                    var unit = TargetSelector.SelectedTarget;
                    if (unit == null || unit.NetworkId != target.NetworkId)
                    {
                        return;
                    }
                }

                if (Qdmg(target) + Player.GetAutoAttackDamage(target) * 2 >= target.Health)
                {
                    if (Orbwalking.InAutoAttackRange(target))
                    {
                        return;
                    }
                }

                if (R.IsReady() && Cdmg(target) >= target.Health)
                {
                    if (!IsBrawl || IsBrawl && !UltEnemies() || RootMenu.Item("whR" + target.CharacterName).GetValue<bool>())
                    {
                        R.CastOnUnit(target);
                    }
                }
            }
        }

        static bool CanW(AIBaseClient target)
        {
            const float wCastTime = 2000f;

            if (OnWall || IsDashing || target == null)
            {
                return false;
            }

            if (Q.IsReady())
            {
                if (!HasQ || HasQ2)
                {
                    if (target.Distance(Player) <= Player.AttackRange + Player.Distance(Player.BBox.Minimum) + 65)
                    {
                        return false;
                    }
                }
                else
                {
                    if (Qdmg(target, false) + Player.GetAutoAttackDamage(target, true) * 1 >= target.Health)
                    {
                        return false;
                    }
                }
            }

            if (Utils.GameTimeTickCount - LastECastT < 500)
            {
                // to prevent e away from w in the spur of the moment
                return false;
            }

            if (target.Distance(Player) <= Player.AttackRange + Player.Distance(Player.BBox.Minimum) + 65)
            {
                if (Player.GetAutoAttackDamage(target, true) * 2 + Qdmg(target, false) >= target.Health)
                {
                    return false;
                }
            }

            var b = Player.GetBuff(QBuffName);
            if (b != null && (b.EndTime - Game.Time) * 1000 <= wCastTime)
            {
                return false;
            }

            var c = Player.GetBuff(Q2BuffName);
            if (c != null && (c.EndTime - Game.Time) * 1000 <= wCastTime)
            {
                return false;
            }

            return true;
        }

        static void LockW(AIBaseClient target)
        {
            if (!RootMenu.Item("lockw").GetValue<bool>())
            {
                return;
            }

            if (OnWall || IsDashing || target == null || !CanW(target))
            {
                return;
            }

            if (ChargingW && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.None)
            {
                Orbwalker.SetAttack(false);
            }

            if (ChargingW && target.Distance(Player) <= W.Range + 35)
            {
                var pos = Prediction.GetPrediction(target, Game.Ping / 2000f).UnitPosition.Extend(Player.Position, W.Range - 65);
                if (pos.UnderTurret(true) && RootMenu.Item("eturret").GetValue<KeyBind>().Active)
                {
                    return;
                }

                Player.IssueOrder(GameObjectOrder.MoveTo, pos, false);
            }
        }

        #endregion

        #region Damage

        private static bool EasyKill(AIBaseClient unit)
        {
            return Cdmg(unit) / 1.65 >= unit.Health;
        }

        private static double Cdmg(AIBaseClient unit)
        {
            if (unit == null)
                return 0d;

            var extraqq = new[] { 1, 1, 2, 2, 3 };
            var qcount = new[] { 2, 3, 4, 4 }[(Math.Min(Player.Level, 18) / 6)];

            qcount += (int)Math.Abs(Player.PercentCooldownMod) * 100 / 10;

            return Math.Min(qcount * extraqq[(int)(Math.Abs(Player.PercentCooldownMod) * 100 / 10)],
                    Player.Mana / Q.ManaCost) * Qdmg(unit, false) + Wdmg(unit) +
                        (Rdmg(Player.GetAutoAttackDamage(unit, true), unit) * qcount) + Edmg(unit);
        }

        private static double Qdmg(AIBaseClient target, bool includeq2 = true)
        {
            double dmg = 0;

            if (Q.IsReady() && target != null)
            {
                dmg += Player.CalcDamage(target, Damage.DamageType.Physical, Player.GetAutoAttackDamage(target, true) +
                    (new[] { 0.2, 0.25, 0.30, 0.35, 0.40 }[Q.Level - 1] * (Player.BaseAttackDamage + Player.FlatPhysicalDamageMod)));

                var dmgreg = Player.CalcDamage(target, Damage.DamageType.Physical, Player.GetAutoAttackDamage(target, true) +
                    (new[] { 0.4, 0.5, 0.6, 0.7, 0.8 }[Q.Level - 1] * (Player.BaseAttackDamage + Player.FlatPhysicalDamageMod)));

                var pct = 52 + (3 * Math.Min(16, Player.Level));

                var dmgtrue = Player.CalcDamage(target, Damage.DamageType.True, dmgreg * pct / 100);

                if (includeq2)
                {
                    dmg += dmgtrue;
                }
            }

            return dmg;
        }

        private static double Wdmg(AIBaseClient target, bool bonus = false)
        {
            double dmg = 0;

            if (W.IsReady() && target != null)
            {
                dmg += Player.CalcDamage(target, Damage.DamageType.Physical,
                    (new[] { 65, 95, 125, 155, 185 }[W.Level - 1] + (0.6 * Player.FlatPhysicalDamageMod)));

                var wpc = new[] { 6, 6.5, 7, 7.5, 8 };
                var pct = wpc[W.Level - 1];

                if (Player.FlatPhysicalDamageMod >= 100)
                    pct += Math.Min(300, Player.FlatPhysicalDamageMod) * 3 / 100;

                if (bonus && target.Distance(Player.Position) > 400)
                    dmg += Player.CalcDamage(target, Damage.DamageType.Physical, pct * (target.MaxHealth / 100));
            }

            return dmg;
        }

        private static double Edmg(AIBaseClient target)
        {
            double dmg = 0;

            if (E.IsReady() && target != null)
            {
                dmg += Player.CalcDamage(target, Damage.DamageType.Physical,
                    (new[] { 70, 115, 160, 205, 250 }[E.Level - 1] + (0.75 * Player.FlatPhysicalDamageMod)));
            }

            return dmg;
        }

        private static double Rdmg(double dmg, AIBaseClient target)
        {
            if (R.IsReady() || target.HasBuff(RBuffName))
            {
                var xtra = new[] { 5, 10, 15, 15 }[R.Level - 1] + (new[] { 4, 6, 8, 8 }[R.Level - 1] * (target.Health / 100));
                return dmg + xtra;
            }

            return dmg;
        }

        #endregion
    }

    enum EvadeType
    {
        Target,
        SkillshotLine,
        SkillshotCirce,
        SelfCast
    }

    enum AvoidType
    {
        Inside,
        Outside
    }

    class DangerPos
    {
        public AvoidType Type;
        public EffectEmitter Emitter;
        public float Radius;

        public DangerPos(EffectEmitter obj, AvoidType type, float radius)
        {
            this.Type = type;
            this.Emitter = obj;
            this.Radius = radius;
        }
    }

    class Evadeable
    {
        public string SDataName;
        public EvadeType EvadeType;
        public string ChampionName;
        public SpellSlot Slot;

        public Evadeable(string name, EvadeType type, string championName)
        {
            this.SDataName = name;
            this.EvadeType = type;
            this.ChampionName = championName;
        }

        internal static Dictionary<string, Evadeable> DangerList = new Dictionary<string, Evadeable>
        {
            {
                "infernalguardian",
                new Evadeable("infernalguardian", EvadeType.SkillshotCirce, "Annie")
            },
            {
                "curseofthesadmummy",
                new Evadeable("curseofthesadmummy", EvadeType.SelfCast, "Amumu")},
            {
                "enchantedcystalarrow",
                new Evadeable("enchantedcystalarrow", EvadeType.SkillshotLine, "Ashe")
            },
            {
                "aurelionsolr",
                new Evadeable("aurelionsolr", EvadeType.SkillshotLine, "AurelionSol")
            },
            {
                "azirr",
                new Evadeable("azirr", EvadeType.SkillshotLine, "Azir")
            },
            {
                "cassiopeiar",
                new Evadeable("cassiopeiar", EvadeType.SkillshotCirce, "Cassiopeia")
            },
            {
                "feast",
                new Evadeable("feast", EvadeType.Target, "Chogath")
            },
            {
                "dariusexecute",
                new Evadeable("dariusexecute", EvadeType.Target, "Darius")
            },
            {
                "evelynnr",
                new Evadeable("evelynnr", EvadeType.SkillshotCirce, "Evelynn")
            },
            {
                "galioidolofdurand",
                new Evadeable("galioidolofdurand", EvadeType.SelfCast, "Galio")
            },
            {
                "gnarult",
                new Evadeable("gnarult", EvadeType.SelfCast, "Gnar")
            },
            {
                "garenr",
                new Evadeable("garenr", EvadeType.Target, "Garen")
            },
            {
                "gravesr",
                new Evadeable("gravesr", EvadeType.SkillshotLine, "Graves")
            },
            {
                "hecarimult",
                new Evadeable("hecarimult", EvadeType.SkillshotLine, "Hecarim")
            },
            {
                "illaoir",
                new Evadeable("illaoir", EvadeType.SelfCast, "Illaoi")
            },
            {
                "jarvanivcataclysm",
                new Evadeable("jarvanivcataclysm", EvadeType.Target, "JarvanIV")
            },
            {
                "blindmonkrkick",
                new Evadeable("blindmonkrkick", EvadeType.Target, "LeeSin")
            },
            {
                "lissandrar",
                new Evadeable("lissandrar", EvadeType.Target, "Lissandra")
            },
            {
                "ufslash",
                new Evadeable("ufslash", EvadeType.SkillshotCirce, "Malphite")
            },
            {
                "monkeykingspintowin",
                new Evadeable("monkeykingspintowin", EvadeType.SelfCast, "MonkeyKing")
            },
            {
                "rivenizunablade",
                new Evadeable("rivenizunablade", EvadeType.SkillshotLine, "Riven")
            },
            {
                "sejuaniglacialprisoncast",
                new Evadeable("sejuaniglacialprisoncast", EvadeType.SkillshotLine, "Sejuani")
            },
            {
                "shyvanatransformcast",
                new Evadeable("shyvanatrasformcast", EvadeType.SkillshotLine, "Shyvana")
            },
            {
                "sonar",
                new Evadeable("sonar", EvadeType.SkillshotLine, "Sona")
            },
            {
                "syndrar",
                new Evadeable("syndrar", EvadeType.Target, "Syndra")
            },
            {
                "varusr",
                new Evadeable("varusr", EvadeType.SkillshotLine, "Varus")
            },
            {
                "veigarprimordialburst",
                new Evadeable("veigarprimordialburst", EvadeType.Target, "Veigar")
            },
            {
                "viktorchaosstorm",
                new Evadeable("viktorchaosstorm", EvadeType.SkillshotCirce, "Viktor")
            },
        };
    }
    internal class HpBarIndicatorCamille
    {
        // hpbar fills by Detuks
        public static Device dxDevice = Drawing.Direct3DDevice;
        public static Line dxLine;

        public float hight = 9;
        public float width = 104;


        public HpBarIndicatorCamille()
        {
            dxLine = new Line(dxDevice) { Width = 9 };

            Drawing.OnPreReset += DrawingOnOnPreReset;
            Drawing.OnPostReset += DrawingOnOnPostReset;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;
        }

        public AIHeroClient unit { get; set; }

        private Vector2 Offset
        {
            get
            {
                if (unit != null)
                {
                    return unit.IsAlly ? new Vector2(34, 9) : new Vector2(10, 20);
                }

                return new Vector2();
            }
        }

        public Vector2 startPosition
        {
            get { return new Vector2(unit.HPBarPosition.X + Offset.X, unit.HPBarPosition.Y + Offset.Y); }
        }


        private static void CurrentDomainOnDomainUnload(object sender, EventArgs eventArgs)
        {
            dxLine.Dispose();
        }

        private static void DrawingOnOnPostReset(EventArgs args)
        {
            dxLine.OnResetDevice();
        }

        private static void DrawingOnOnPreReset(EventArgs args)
        {
            dxLine.OnLostDevice();
        }


        private float getHpProc(float dmg = 0)
        {
            float health = ((unit.Health - dmg) > 0) ? (unit.Health - dmg) : 0;
            return (health / unit.MaxHealth);
        }

        private Vector2 getHpPosAfterDmg(float dmg)
        {
            float w = getHpProc(dmg) * width;
            return new Vector2(startPosition.X + w, startPosition.Y);
        }

        public void drawDmg(float dmg, ColorBGRA color)
        {
            Vector2 hpPosNow = getHpPosAfterDmg(0);
            Vector2 hpPosAfter = getHpPosAfterDmg(dmg);

            fillHPBar(hpPosNow, hpPosAfter, color);
            //fillHPBar((int)(hpPosNow.X - startPosition.X), (int)(hpPosAfter.X- startPosition.X), color);
        }

        private void fillHPBar(int to, int from, Color color)
        {
            var sPos = startPosition;
            for (var i = from; i < to; i++)
            {
                Drawing.DrawLine(sPos.X + i, sPos.Y, sPos.X + i, sPos.Y + 9, 1, color);
            }
        }

        private void fillHPBar(Vector2 from, Vector2 to, ColorBGRA color)
        {
            dxLine.Begin();

            dxLine.Draw(new[] {
                new Vector2((int) from.X, (int) from.Y + 4f),
                new Vector2((int) to.X, (int) to.Y + 4f) }, color);

            dxLine.End();
        }
    }
}
