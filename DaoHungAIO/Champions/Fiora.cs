using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaoHungAIO.Champions
{
    internal class HeroManager
    {
        public static IEnumerable<AIHeroClient> Enemies {
            get { return GameObjects.EnemyHeroes; }
        }
    class Fiora
    {
        private static AIHeroClient Player { get { return ObjectManager.Player; } }

        private static Spell Q, W, E, R;

        private static Menu Menu;

        private const float LaneClearWaitTimeMod = 2f;


        public Fiora()
        {
            Q = new Spell(SpellSlot.Q, 400);
            W = new Spell(SpellSlot.W, 750);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);
            W.SetSkillshot(0.75f, 80, 2000, false, false, SkillshotType.Line);
            W.MinHitChance = HitChance.High;


            Menu = new Menu(Player.CharacterName, "DH.Fiora", true);

            Menu spellMenu = Menu.Add(new Menu("Spell", "Spell"));

            Menu Harass = spellMenu.Add(new Menu("Harass", "Harass"));

            Menu Combo = spellMenu.Add(new Menu("Combo", "Combo"));

            Menu Target = Menu.Add(new Menu("Targeting Modes", "Targeting Modes"));

            Menu PriorityMode = Target.Add(new Menu("Priority", "Priority Mode"));

            Menu OptionalMode = Target.Add(new Menu("Optional", "Optional Mode"));

            Menu SelectedMode = Target.Add(new Menu("Selected", "Selected Mode"));

            Menu LaneClear = spellMenu.Add(new Menu("Lane Clear", "Lane Clear"));

                spellMenu.Add(new MenuKeyBind("Orbwalk Last Right Click", "Orbwalk Last Right Click", System.Windows.Forms.Keys.A, KeyBindType.Press)
                    .ValueChanged += OrbwalkLastClick.OrbwalkLRCLK_ValueChanged;

            Menu JungClear = spellMenu.Add(new Menu("Jungle Clear", "Jungle Clear"));

            Menu Misc = Menu.Add(new Menu("Misc", "Misc"));

            Menu Draw = Menu.Add(new Menu("Draw", "Draw")); ;

            Harass.Add(new MenuBool("UseQHarass","QEnable"));
            Harass.Add(new MenuBool("UseQHarassGap","UseQtogapclose"));
            Harass.Add(new MenuBool("UseQHarassPrePass","UseQtohitpre-passivespot"));
            Harass.Add(new MenuBool("UseQHarassPass","UseQtohitpassive"));
            Harass.Add(new MenuBool("UseEHarass","EEnable"));
            Harass.Add(new MenuSlider("ManaHarass","ManaHarass",40,0,100));

            Combo.Add(new MenuBool("UseQCombo","QEnable"));
            Combo.Add(new MenuBool("UseQComboGap","UseQtogapclose"));
            Combo.Add(new MenuBool("UseQComboPrePass","UseQtohitpre-passivespot"));
            Combo.Add(new MenuBool("UseQComboPass","UseQtohitpassive"));
            Combo.Add(new MenuBool("UseQComboGapMinion","UseQminiontogapclose",false));
            Combo.Add(new MenuSlider("UseQComboGapMinionValue","Qminiongapcloseif%cdr>=",25,0,40));
            Combo.Add(new MenuBool("UseECombo","EEnable"));
            Combo.Add(new MenuBool("UseRCombo","REnable"));
            Combo.Add(new MenuBool("UseRComboLowHP","UseRLowHP"));
            Combo.Add(new MenuSlider("UseRComboLowHPValue","RLowHPifplayerhp<",40,0,100));
            Combo.Add(new MenuBool("UseRComboKillable","UseRKillable"));
            Combo.Add(new MenuBool("UseRComboOnTap","UseRonTap"));
            Combo.Add(new MenuKeyBind("UseRComboOnTapKey","RonTapkey",System.Windows.Forms.Keys.G,KeyBindType.Press));
            Combo.Add(new MenuBool("UseRComboAlways","UseRAlways",false));

            Target.Add(new MenuList("TargetingMode","TargetingMode",new[]{"Optional","Selected","Priority","Normal"}));
            Target.Add(new MenuSlider("OrbwalkToPassiveRange","OrbwalkToPassiveRange",300,250,500));
            Target.Add(new MenuBool("FocusUltedTarget","FocusUltedTarget",false));
            Target.Add(new MenuBool("Note1","GoineachModemenutocustomizewhatyouwant!"));
            Target.Add(new MenuBool("Note2","PleaserememberOrbwalktoPassivespotonlyworks"));
            Target.Add(new MenuBool("Note3","in\"ComboOrbwalktoPassive\"modecanbefound"));
            Target.Add(new MenuBool("Note4","inorbwalkermenu!"));

            PriorityMode.Add(new MenuSlider("PriorityRange","PriorityRange",1000,300,1000));
            PriorityMode.Add(new MenuBool("PriorityOrbwalktoPassive","OrbwalktoPassive"));
            PriorityMode.Add(new MenuBool("PriorityUnderTower","UnderTower"));
            foreach(var hero in HeroManager.Enemies)
            {
                PriorityMode.Add(new MenuSlider("Priority"+ hero.CharacterName, hero.CharacterName,2,1,5));
            }

            OptionalMode.Add(new MenuSlider("OptionalRange","OptionalRange",1000,300,1000));
            OptionalMode.Add(new MenuBool("OptionalOrbwalktoPassive","OrbwalktoPassive"));
            OptionalMode.Add(new MenuBool("OptionalUnderTower","UnderTower",false));
            OptionalMode.Add(new MenuKeyBind("OptionalSwitchTargetKey","SwitchTargetKey",System.Windows.Forms.Keys.T,KeyBindType.Press));
            OptionalMode.Add(new MenuBool("Note5","AlsoCanLeft-clickthetargettoswitch!"));

            SelectedMode.Add(new MenuSlider("SelectedRange","SelectedRange",1000,300,1000));
            SelectedMode.Add(new MenuBool("SelectedOrbwalktoPassive","OrbwalktoPassive"));
            SelectedMode.Add(new MenuBool("SelectedUnderTower","UnderTower",false));
            SelectedMode.Add(new MenuBool("SelectedSwitchIfNoSelected","SwitchtoOptionalifnotarget"));

            LaneClear.Add(new MenuBool("UseELClear","EEnable"));
            LaneClear.Add(new MenuBool("UseTimatLClear","TiamatEnable"));
            LaneClear.Add(new MenuSlider("minimumManaLC","minimumMana",40,0,100));

            JungClear.Add(new MenuBool("UseEJClear","EEnable"));
            JungClear.Add(new MenuBool("UseTimatJClear","TiamatEnable"));
            JungClear.Add(new MenuSlider("minimumManaJC","minimumMana",40,0,100));

            Misc.Add(new MenuKeyBind("WallJump","WallJump",System.Windows.Forms.Keys.H,KeyBindType.Press));

            Draw.Add(new MenuBool("DrawQ","DrawQ",false));
            Draw.Add(new MenuBool("DrawW","DrawW",false));
            Draw.Add(new MenuBool("DrawOptionalRange","DrawOptionalRange"));
            Draw.Add(new MenuBool("DrawSelectedRange","DrawSelectedRange"));
            Draw.Add(new MenuBool("DrawPriorityRange","DrawPriorityRange"));
            Draw.Add(new MenuBool("DrawTarget","DrawTarget"));
            Draw.Add(new MenuBool("DrawVitals","DrawVitals",false));
            Draw.Add(new MenuBool("DrawFastDamage","DrawFastDamage",false)).ValueChanged += DrawHP_ValueChanged;

            if (HeroManager.Enemies.Any())
            {
                Evade.Evade.Init();
                EvadeTarget.Init();
                TargetedNoMissile.Init();
                OtherSkill.Init();
            }
            OrbwalkLastClick.Init();
            Menu.AddToMainMenu();
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;

            GameObject.OnCreate += GameObject_OnCreate;
            Game.OnUpdate += Game_OnGameUpdate;
            Orbwalker.OnAction += OnActionDelegate;
            AfterAttackNoTarget += Orbwalker_AfterAttackNoTarget;
            OnAttack += OnAttack;
            AIBaseClient.OnProcessSpellCast += oncast;
            Game.OnWndProc += Game_OnWndProc;
            //Utility.HpBarDamageIndicator.DamageToUnit = GetFastDamage;
            //Utility.HpBarDamageIndicator.Enabled = DrawHP;
            CustomDamageIndicator.Initialize(GetFastDamage);
            CustomDamageIndicator.Enabled = DrawHP;

            //evade
            FioraProject.Evade.Evade.Evading += EvadeSkillShots.Evading;
        }

        private static void OnActionDelegate(
    Object sender,
    OrbwalkerActionArgs args
)
            {
                if(args.Type == OrbwalkerType.AfterAttack)
                {

                    if (!args.Sender.IsMe)
                        return;
                    if (Orbwalker.ActiveMode == OrbwalkerMode.Combo
                        || OrbwalkLastClickActive)
                    {
                        if (Ecombo && E.IsReady())
                        {
                            E.Cast();
                        }
                        else if (HasItem())
                        {
                            CastItem();
                        }
                    }
                    if (Orbwalker.ActiveMode == OrbwalkerMode.Mixed && (args.Sender is AIHeroClient))
                    {
                        if (Eharass && E.IsReady() && Player.ManaPercent >= Manaharass)
                        {
                            E.Cast();
                        }
                        else if (HasItem())
                        {
                            CastItem();
                        }
                    }
                    if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                    {
                        // jungclear
                        if (EJclear && E.IsReady() && Player.Mana * 100 / Player.MaxMana >= ManaJclear && !ShouldWait()
                            && CountMinionsInRange(Player.Position, Player.GetRealAutoAttackRange() + 200, true) >= 1)
                        {
                            E.Cast();
                        }
                        else if (TimatJClear && HasItem() && !ShouldWait()
                            && CountMinionsInRange(Player.Position, Player.GetRealAutoAttackRange() + 200, true) >= 1)
                        {
                            CastItem();
                        }
                        // laneclear
                        if (ELclear && E.IsReady() && Player.Mana * 100 / Player.MaxMana >= ManaLclear && !ShouldWait()
                            && CountMinionsInRange(Player.Position, Player.GetRealAutoAttackRange() + 200, false) >= 1)
                        {
                            E.Cast();
                        }
                        else if (TimatLClear && HasItem() && !ShouldWait()
                            && CountMinionsInRange(Player.Position, Player.GetRealAutoAttackRange() + 200, false) >= 1)
                        {
                            CastItem();
                        }
                    }

                }
                if (args.Type == OrbwalkerType.OnAttack)
                {
                    if (args.Sender.IsMe
                        && (Orbwalker.ActiveMode == OrbwalkerMode.Combo
                        || OrbwalkLastClickActive))
                    {
                        if (ItemData.Youmuus_Ghostblade.GetItem().IsReady())
                            ItemData.Youmuus_Ghostblade.GetItem().Cast();
                    }
                }
            }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            //if (!sender.Name.ToLower().Contains("fiora"))
            //    return;
            //Game.PrintChat(sender.Name + sender.Type    );
        }

 

        private static int CountMinionsInRange(Vector3 pos, float range, bool dontcare)
            {
                return GameObjects.EnemyMinions.Where(m => pos.Distance(m) < -range).Count();
            }
        private static bool ShouldWait()
        {
            return
                ObjectManager.Get<AIMinionClient>()
                    .Any(
                        minion =>
                            minion.IsValidTarget() && minion.Team != GameObjectTeam.Neutral &&
                            minion.InAutoAttackRange() &&
                            HealthPrediction.GetPrediction(
                                minion, (int)((Player.AttackDelay * 1000) * LaneClearWaitTimeMod)) <=
                            Player.GetAutoAttackDamage(minion));
        }
            private static void Orbwalker_AfterAttackNoTarget(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo 
                || OrbwalkLastClickActive)
            {
                if (Ecombo && E.IsReady() && Player.CountEnemyHeroesInRange(Player.GetRealAutoAttackRange() + 200) >= 1)
                {
                    E.Cast();
                }
                else if (HasItem() && Player.CountEnemyHeroesInRange(Player.GetRealAutoAttackRange() + 200) >= 1)
                {
                    CastItem();
                }
            }
            if (Orbwalker.ActiveMode == OrbwalkerMode.Mixed && (unit is AIHeroClient))
            {
                if (Eharass && E.IsReady() && Player.ManaPercent >= Manaharass
                    && Player.CountEnemyHeroesInRange(Player.GetRealAutoAttackRange() + 200) >= 1)
                {
                    E.Cast();
                }
                else if (HasItem() && Player.CountEnemyHeroesInRange(Player.GetRealAutoAttackRange() + 200) >= 1)
                {
                    CastItem();
                }
            }
            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                // jungclear
                if (EJclear && E.IsReady() && Player.Mana * 100 / Player.MaxMana >= ManaJclear && !ShouldWait()
                    && CountMinionsInRange(Player.Position, Player.GetRealAutoAttackRange() + 200, true) >= 1)
                {
                    E.Cast();
                }
                else if (TimatJClear && HasItem() && !ShouldWait()
                    && CountMinionsInRange(Player.Position, Player.GetRealAutoAttackRange() + 200, true) >= 1)
                {
                    CastItem();
                }
                // laneclear
                if (ELclear && E.IsReady() && Player.Mana * 100 / Player.MaxMana >= ManaLclear && !ShouldWait()
                    && CountMinionsInRange(Player.Position, Player.GetRealAutoAttackRange() + 200, false) >= 1)
                {
                    E.Cast();
                }
                else if (TimatLClear && HasItem() && !ShouldWait()
                    && CountMinionsInRange(Player.Position, Player.GetRealAutoAttackRange() + 200, false) >= 1)
                {
                    CastItem();
                }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;
            FioraPassiveUpdate();
            OrbwalkToPassive();
            WallJump();
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo )
            {
                Combo();
            }
            if (Orbwalker.ActiveMode == OrbwalkerMode.Mixed)
            {
                Harass();
            }
            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {

            }
        }
        private static void oncast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            var spell = args.SData;
            if (!sender.IsMe)
                return;
            if (spell.Name.Contains("ItemTiamatCleave"))
            {

            }
            if (spell.Name.Contains("FioraQ"))
            {

            }
            if (spell.Name == "FioraE")
            {
                        
                Orbwalker.ResetAutoAttackTimer();
            }
            if (spell.Name == "ItemTitanicHydraCleave")
            {
                Orbwalker.ResetAutoAttackTimer();
            }
            if (spell.Name.ToLower().Contains("fiorabasicattack"))
            {
            }

        }
 


        //harass
        private static bool Qharass { get { return Menu.Item("Use Q Harass"); } }
        private static bool Eharass { get { return Menu.Item("Use E Harass"); } }
        private static bool CastQGapCloseHarass { get { return Menu.Item("Use Q Harass Gap"); } }
        private static bool CastQPrePassiveHarass { get { return Menu.Item("Use Q Harass Pre Pass"); } }
        private static bool CastQPassiveHarasss { get { return Menu.Item("Use Q Harass Pass"); } }
        private static int Manaharass { get { return Menu.Item("Mana Harass").Value; } }

        //combo
        private static bool Qcombo { get { return Menu.Item("Use Q Combo"); } }
        private static bool Ecombo { get { return Menu.Item("Use E Combo"); } }
        private static bool CastQGapCloseCombo { get { return Menu.Item("Use Q Combo Gap"); } }
        private static bool CastQPrePassiveCombo { get { return Menu.Item("Use Q Combo Pre Pass"); } }
        private static bool CastQPassiveCombo { get { return Menu.Item("Use Q Combo Pass"); } }
        private static bool CastQMinionGapCloseCombo { get { return Menu.Item("Use Q Combo Gap Minion"); } }
        private static int ValueQMinionGapCloseCombo { get { return Menu.Item("Use Q Combo Gap Minion Value").Value; } }
        private static bool Rcombo { get { return Menu.Item("Use R Combo"); } }
        private static bool UseRComboLowHP { get { return Menu.Item("Use R Combo LowHP"); } }
        private static int ValueRComboLowHP { get { return Menu.Item("Use R Combo LowHP Value").Value; } }
        private static bool UseRComboKillable { get { return Menu.Item("Use R Combo Killable"); } }
        private static bool UseRComboOnTap { get { return Menu.Item("Use R Combo On Tap"); } }
        private static bool RTapKeyActive { get { return Menu.Item("Use R Combo On Tap Key").Active; } }
        private static bool UseRComboAlways { get { return Menu.Item("Use R Combo Always"); } }

        //jclear && lclear
        private static bool ELclear { get { return Menu.Item("Use E LClear"); } }
        private static bool TimatLClear { get { return Menu.Item("Use Timat LClear"); } }
        private static bool EJclear { get { return Menu.Item("Use E JClear"); } }
        private static bool TimatJClear { get { return Menu.Item("Use Timat JClear"); } }
        private static int ManaJclear { get { return Menu.Item("minimum Mana JC").Value; } }
        private static int ManaLclear { get { return Menu.Item("minimum Mana LC").Value; } }

        //orbwalkpassive
        private static float OrbwalkToPassiveRange { get { return Menu.Item("Orbwalk To Passive Range").Value; } }
        private static bool OrbwalkToPassiveTargeted { get { return Menu.Item("Selected Orbwalk to Passive"); } }
        private static bool OrbwalkToPassiveOptional { get { return Menu.Item("Optional Orbwalk to Passive"); } }
        private static bool OrbwalkToPassivePriority { get { return Menu.Item("Priority Orbwalk to Passive"); } }
        private static bool OrbwalkTargetedUnderTower { get { return Menu.Item("Selected Under Tower"); } }
        private static bool OrbwalkOptionalUnderTower { get { return Menu.Item("Optional Under Tower"); } }
        private static bool OrbwalkPriorityUnderTower { get { return Menu.Item("Priority Under Tower"); } }

        // orbwalklastclick
        private static bool OrbwalkLastClickActive { get { return Menu.Item("Orbwalk Last Right Click").Active; } }

        #region Drawing
        private static bool DrawQ { get { return Menu.Item("Draw Q"); } }
        private static bool DrawW { get { return Menu.Item("Draw W"); } }
        private static bool DrawQcast { get { return Menu.Item("Draw Q cast"); } }
        private static bool DrawOptionalRange { get { return Menu.Item("Draw Optional Range"); } }
        private static bool DrawSelectedRange { get { return Menu.Item("Draw Selected Range"); } }
        private static bool DrawPriorityRange { get { return Menu.Item("Draw Priority Range"); } }
        private static bool DrawTarget { get { return Menu.Item("Draw Target"); } }
        private static bool DrawHP { get { return Menu.Item("Draw Fast Damage"); } }
        private static bool DrawVitals { get { return Menu.Item("Draw Vitals"); } }
        private static void DrawHP_ValueChanged(Object sender,
	EventArgs e)
        {
            if (sender != null)
            {
                //Utility.HpBarDamageIndicator.Enabled = e.GetNewValue<bool>();
                CustomDamageIndicator.Enabled = e.GetNewValue<bool>();
            }
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;
            if (DrawQ)
                Render.Circle.DrawCircle(Player.Position, 400, Color.Green);
            if (DrawW)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, Color.Green);
            }
            if (DrawOptionalRange && TargetingMode == TargetMode.Optional)
            {
                Render.Circle.DrawCircle(Player.Position, OptionalRange, Color.DeepPink);
            }
            if (DrawSelectedRange && TargetingMode == TargetMode.Selected)
            {
                Render.Circle.DrawCircle(Player.Position, SelectedRange, Color.DeepPink);
            }
            if (DrawPriorityRange && TargetingMode == TargetMode.Priority)
            {
                Render.Circle.DrawCircle(Player.Position, PriorityRange, Color.DeepPink);
            }
            if (DrawTarget && TargetingMode != TargetMode.Normal)
            {
                var hero = GetTarget();
                if (hero != null)
                    Render.Circle.DrawCircle(hero.Position, 75, Color.Yellow, 5);
            }
            if (DrawVitals && TargetingMode != TargetMode.Normal)
            {
                var hero = GetTarget();
                if (hero != null)
                {
                    var status = hero.GetPassiveStatus(0f);
                    if (status.HasPassive && status.PassivePredictedPositions.Any())
                    {
                        foreach (var x in status.PassivePredictedPositions)
                        {
                            Render.Circle.DrawCircle(x.To3D(), 50, Color.Yellow);
                        }
                    }
                }
            }
            if (activewalljump)
            {
                var Fstwall = GetFirstWallPoint(Player.Position.To2D(), Game.CursorPos.To2D());
                if (Fstwall != null)
                {
                    var firstwall = ((Vector2)Fstwall);
                    var pos = firstwall.Extend(Game.CursorPos.To2D(), 100);
                    var Lstwall = GetLastWallPoint(firstwall, Game.CursorPos.To2D());
                    if (Lstwall != null)
                    {
                        var lastwall = ((Vector2)Lstwall);
                        if (InMiddileWall(firstwall, lastwall))
                        {
                            for (int i = 0; i <= 359; i++)
                            {
                                var pos1 = pos.RotateAround(firstwall, i);
                                var pos2 = firstwall.Extend(pos1, 400);
                                if (pos1.InTheCone(firstwall, Game.CursorPos.To2D(), 60) && pos1.IsWall() && !pos2.IsWall())
                                {
                                    Render.Circle.DrawCircle(firstwall.To3D(), 50, Color.Green);
                                    goto Finish;
                                }
                            }

                            Render.Circle.DrawCircle(firstwall.To3D(), 50, Color.Red);
                        }
                    }
                }
                Finish:;
            }

        }
        private static void Drawing_OnEndScene(EventArgs args)
        {
        }

        #endregion Drawing

        #region WallJump
        private static bool usewalljump = true;
        private static bool activewalljump { get { return Menu.Item("WallJump").Active; } }
        private static int movetick;
        private static void WallJump()
        {
            if (usewalljump && activewalljump)
            {
                var Fstwall = GetFirstWallPoint(Player.Position.To2D(), Game.CursorPos.To2D());
                if (Fstwall != null)
                {
                    var firstwall = ((Vector2)Fstwall);
                    var Lstwall = GetLastWallPoint(firstwall, Game.CursorPos.To2D());
                    if (Lstwall != null)
                    {
                        var lastwall = ((Vector2)Lstwall);
                        if (InMiddileWall(firstwall, lastwall))
                        {
                            var y = Player.Position.Extend(Game.CursorPos, 30);
                            for (int i = 20; i <= 300; i = i + 20)
                            {
                                if (Utils.GameTimeTickCount - movetick < (70 + Math.Min(60, Game.Ping)))
                                    break;
                                if (Player.Distance(Game.CursorPos) <= 1200 && Player.Position.To2D().Extend(Game.CursorPos.To2D(), i).IsWall())
                                {
                                    Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.To2D().Extend(Game.CursorPos.To2D(), i - 20).To3D());
                                    movetick = Utils.GameTimeTickCount;
                                    break;
                                }
                                Player.IssueOrder(GameObjectOrder.MoveTo,
                                    Player.Distance(Game.CursorPos) <= 1200 ?
                                    Player.Position.To2D().Extend(Game.CursorPos.To2D(), 200).To3D() :
                                    Game.CursorPos);
                            }
                            if (y.IsWall() && Prediction.GetPrediction(Player, 500).UnitPosition.Distance(Player.Position) <= 10 && Q.IsReady())
                            {
                                var pos = Player.Position.To2D().Extend(Game.CursorPos.To2D(), 100);
                                for (int i = 0; i <= 359; i++)
                                {
                                    var pos1 = pos.RotateAround(Player.Position.To2D(), i);
                                    var pos2 = Player.Position.To2D().Extend(pos1, 400);
                                    if (pos1.InTheCone(Player.Position.To2D(), Game.CursorPos.To2D(), 60) && pos1.IsWall() && !pos2.IsWall())
                                    {
                                        Q.Cast(pos2);
                                    }

                                }
                            }
                        }
                        else if (Utils.GameTimeTickCount - movetick >= (70 + Math.Min(60, Game.Ping)))
                        {
                            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                            movetick = Utils.GameTimeTickCount;
                        }
                    }
                    else if (Utils.GameTimeTickCount - movetick >= (70 + Math.Min(60, Game.Ping)))
                    {
                        Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                        movetick = Utils.GameTimeTickCount;
                    }
                }
                else if (Utils.GameTimeTickCount - movetick >= (70 + Math.Min(60, Game.Ping)))
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                    movetick = Utils.GameTimeTickCount;
                }
            }
        }
        private static Vector2? GetFirstWallPoint(Vector2 from, Vector2 to, float step = 25)
        {
            var direction = (to - from).Normalized();

            for (float d = 0; d < from.Distance(to); d = d + step)
            {
                var testPoint = from + d * direction;
                var flags = NavMesh.GetCollisionFlags(testPoint.X, testPoint.Y);
                if (flags.HasFlag(CollisionFlags.Wall) || flags.HasFlag(CollisionFlags.Building))
                {
                    return from + (d - step) * direction;
                }
            }

            return null;
        }
        private static Vector2? GetLastWallPoint(Vector2 from, Vector2 to, float step = 25)
        {
            var direction = (to - from).Normalized();
            var Fstwall = GetFirstWallPoint(from, to);
            if (Fstwall != null)
            {
                var firstwall = ((Vector2)Fstwall);
                for (float d = step; d < firstwall.Distance(to) + 1000; d = d + step)
                {
                    var testPoint = firstwall + d * direction;
                    var flags = NavMesh.GetCollisionFlags(testPoint.X, testPoint.Y);
                    if (!flags.HasFlag(CollisionFlags.Wall) && !flags.HasFlag(CollisionFlags.Building))
                    //if (!testPoint.IsWall())
                    {
                        return firstwall + d * direction;
                    }
                }
            }

            return null;
        }
        private static bool InMiddileWall(Vector2 firstwall, Vector2 lastwall)
        {
            var midwall = new Vector2((firstwall.X + lastwall.X) / 2, (firstwall.Y + lastwall.Y) / 2);
            var point = midwall.Extend(Game.CursorPos.To2D(), 50);
            for (int i = 0; i <= 350; i = i + 10)
            {
                var testpoint = point.RotateAround(midwall, i);
                var flags = NavMesh.GetCollisionFlags(testpoint.X, testpoint.Y);
                if (!flags.HasFlag(CollisionFlags.Wall) && !flags.HasFlag(CollisionFlags.Building))
                {
                    return false;
                }
            }
            return true;
        }
        #endregion WallJump

        #region OrbwalkToPassive
        private static void OrbwalkToPassive()
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.OrbwalkPassive)
            {
                var target = GetTarget(OrbwalkToPassiveRange);
                if (target.IsValidTarget(OrbwalkToPassiveRange) && !target.IsZombie)
                {
                    var status = target.GetPassiveStatus(0);
                    if (Player.Position.To2D().Distance(target.Position.To2D()) <= OrbwalkToPassiveRange && status.HasPassive
                        && ((TargetingMode == TargetMode.Selected && OrbwalkToPassiveTargeted && (OrbwalkTargetedUnderTower || !Player.UnderTurret(true)))
                        || (TargetingMode == TargetMode.Optional && OrbwalkToPassiveOptional && (OrbwalkOptionalUnderTower || !Player.UnderTurret(true)))
                        || (TargetingMode == TargetMode.Priority && OrbwalkToPassivePriority && (OrbwalkPriorityUnderTower || !Player.UnderTurret(true)))))
                    {
                        var point = status.PassivePredictedPositions.OrderBy(x => x.Distance(Player.Position.To2D())).FirstOrDefault();
                        point = point.IsValid() ? target.Position.To2D().Extend(point, 150) : Game.CursorPos.To2D();
                        Orbwalker.SetOrbwalkerPoint(point.To3D());
                        // humanizer
                        //if (InAutoAttackRange(target)
                        //        && status.PassivePredictedPositions.Any(x => Player.Position.To2D()
                        //            .InTheCone(status.TargetPredictedPosition, x, 90)))
                        //{
                        //    Orbwalker.SetMovement(false);
                        //    return;
                        //}
                    }
                    else Orbwalker.SetOrbwalkerPoint(Game.CursorPos);
                }
                else Orbwalker.SetOrbwalkerPoint(Game.CursorPos);
            }
            else Orbwalker.SetOrbwalkerPoint(Game.CursorPos);
            //Orbwalker.SetMovement(true);
        }
        #endregion OrbwalkToPassive
    }
}
