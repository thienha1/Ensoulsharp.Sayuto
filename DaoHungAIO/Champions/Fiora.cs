using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using SharpDX;
using SPrediction;
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
    }


    internal class EvadeTarget
    {
        #region Static Fields

        private static readonly List<Targets> DetectedTargets = new List<Targets>();

        private static readonly List<SpellData> Spells = new List<SpellData>();
        private static AIHeroClient Player = ObjectManager.Player;

        #endregion

        #region Methods

        internal static void Init()
        {
            LoadSpellData();

            Spells.RemoveAll(i => !HeroManager.Enemies.Any(
            a =>
            string.Equals(
                a.CharacterName,
                i.CharacterName,
                StringComparison.InvariantCultureIgnoreCase)));

            var evadeMenu = new Menu("Evade Targeted SkillShot", "EvadeTarget");
            {
                evadeMenu.Add(new MenuBool("W", "Use W"));
                var aaMenu = new Menu("AA", "Auto Attack");
                {
                    aaMenu.Add(new MenuBool("B", "Basic Attack", false));
                    aaMenu.Add(new MenuSlider("BHpU", "-> If Hp < (%)", 35));
                    aaMenu.Add(new MenuBool("C", "Crit Attack", false));
                    aaMenu.Add(new MenuSlider("CHpU", "-> If Hp < (%)", 40));
                    evadeMenu.Add(aaMenu);
                }
                foreach (var hero in
                    HeroManager.Enemies.Where(
                        i =>
                        Spells.Any(
                            a =>
                            string.Equals(
                                a.CharacterName,
                                i.CharacterName,
                                StringComparison.InvariantCultureIgnoreCase))))
                {
                    evadeMenu.Add(new Menu("-> " + hero.CharacterName, hero.CharacterName.ToLowerInvariant()));
                }
                foreach (var spell in
                    Spells.Where(
                        i =>
                        HeroManager.Enemies.Any(
                            a =>
                            string.Equals(
                                a.CharacterName,
                                i.CharacterName,
                                StringComparison.InvariantCultureIgnoreCase))))
                {
                    ((Menu)evadeMenu[spell.CharacterName.ToLowerInvariant()]).Add(new MenuBool(
                        spell.MissileName,
                        spell.MissileName + " (" + spell.Slot + ")",
                        false));
                }
            }
            Fiora.Config.Add(evadeMenu);
            Game.OnUpdate += OnUpdateTarget;
            GameObject.OnCreate += ObjSpellMissileOnCreate;
            GameObject.OnDelete += ObjSpellMissileOnDelete;
        }

        private static void LoadSpellData()
        {
            Spells.Add(
                new SpellData { CharacterName = "Ahri", SpellNames = new[] { "ahrifoxfiremissiletwo" }, Slot = SpellSlot.W });
            Spells.Add(
                new SpellData { CharacterName = "Ahri", SpellNames = new[] { "ahritumblemissile" }, Slot = SpellSlot.R });
            Spells.Add(
                new SpellData { CharacterName = "Akali", SpellNames = new[] { "akalimota" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData { CharacterName = "Anivia", SpellNames = new[] { "frostbite" }, Slot = SpellSlot.E });
            Spells.Add(
                new SpellData { CharacterName = "Annie", SpellNames = new[] { "disintegrate" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData
                {
                    CharacterName = "Brand",
                    SpellNames = new[] { "brandconflagrationmissile" },
                    Slot = SpellSlot.E
                });
            Spells.Add(
                new SpellData
                {
                    CharacterName = "Brand",
                    SpellNames = new[] { "brandwildfire", "brandwildfiremissile" },
                    Slot = SpellSlot.R
                });
            Spells.Add(
                new SpellData
                {
                    CharacterName = "Caitlyn",
                    SpellNames = new[] { "caitlynaceintheholemissile" },
                    Slot = SpellSlot.R
                });
            Spells.Add(
                new SpellData { CharacterName = "Cassiopeia", SpellNames = new[] { "cassiopeiatwinfang" }, Slot = SpellSlot.E });
            Spells.Add(
                new SpellData { CharacterName = "Elise", SpellNames = new[] { "elisehumanq" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData
                {
                    CharacterName = "Ezreal",
                    SpellNames = new[] { "ezrealarcaneshiftmissile" },
                    Slot = SpellSlot.E
                });
            Spells.Add(
                new SpellData
                {
                    CharacterName = "FiddleSticks",
                    SpellNames = new[] { "fiddlesticksdarkwind", "fiddlesticksdarkwindmissile" },
                    Slot = SpellSlot.E
                });
            Spells.Add(
                new SpellData { CharacterName = "Gangplank", SpellNames = new[] { "parley" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData { CharacterName = "Janna", SpellNames = new[] { "sowthewind" }, Slot = SpellSlot.W });
            Spells.Add(
                new SpellData { CharacterName = "Kassadin", SpellNames = new[] { "nulllance" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData
                {
                    CharacterName = "Katarina",
                    SpellNames = new[] { "katarinaq", "katarinaqmis" },
                    Slot = SpellSlot.Q
                });
            Spells.Add(
                new SpellData { CharacterName = "Kayle", SpellNames = new[] { "judicatorreckoning" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData
                {
                    CharacterName = "Leblanc",
                    SpellNames = new[] { "leblancchaosorb", "leblancchaosorbm" },
                    Slot = SpellSlot.Q
                });
            Spells.Add(new SpellData { CharacterName = "Lulu", SpellNames = new[] { "luluw" }, Slot = SpellSlot.W });
            Spells.Add(
                new SpellData { CharacterName = "Malphite", SpellNames = new[] { "seismicshard" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData
                {
                    CharacterName = "MissFortune",
                    SpellNames = new[] { "missfortunericochetshot", "missFortunershotextra" },
                    Slot = SpellSlot.Q
                });
            Spells.Add(
                new SpellData
                {
                    CharacterName = "Nami",
                    SpellNames = new[] { "namiwenemy", "namiwmissileenemy" },
                    Slot = SpellSlot.W
                });
            Spells.Add(
                new SpellData { CharacterName = "Nunu", SpellNames = new[] { "iceblast" }, Slot = SpellSlot.E });
            Spells.Add(
                new SpellData { CharacterName = "Pantheon", SpellNames = new[] { "pantheonq" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData
                {
                    CharacterName = "Ryze",
                    SpellNames = new[] { "spellflux", "spellfluxmissile" },
                    Slot = SpellSlot.E
                });
            Spells.Add(
                new SpellData { CharacterName = "Shaco", SpellNames = new[] { "twoshivpoison" }, Slot = SpellSlot.E });
            Spells.Add(
                new SpellData { CharacterName = "Shen", SpellNames = new[] { "shenvorpalstar" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData { CharacterName = "Sona", SpellNames = new[] { "sonaqmissile" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData { CharacterName = "Swain", SpellNames = new[] { "swaintorment" }, Slot = SpellSlot.E });
            Spells.Add(
                new SpellData { CharacterName = "Syndra", SpellNames = new[] { "syndrar" }, Slot = SpellSlot.R });
            Spells.Add(
                new SpellData { CharacterName = "Taric", SpellNames = new[] { "dazzle" }, Slot = SpellSlot.E });
            Spells.Add(
                new SpellData { CharacterName = "Teemo", SpellNames = new[] { "blindingdart" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData { CharacterName = "Tristana", SpellNames = new[] { "detonatingshot" }, Slot = SpellSlot.E });
            Spells.Add(
                new SpellData { CharacterName = "Tristana", SpellNames = new[] { "tristanar" }, Slot = SpellSlot.R });
            Spells.Add(
                new SpellData { CharacterName = "TwistedFate", SpellNames = new[] { "bluecardattack" }, Slot = SpellSlot.W });
            Spells.Add(
                new SpellData { CharacterName = "TwistedFate", SpellNames = new[] { "goldcardattack" }, Slot = SpellSlot.W });
            Spells.Add(
                new SpellData { CharacterName = "TwistedFate", SpellNames = new[] { "redcardattack" }, Slot = SpellSlot.W });
            Spells.Add(
                new SpellData
                {
                    CharacterName = "Urgot",
                    SpellNames = new[] { "urgotheatseekinghomemissile" },
                    Slot = SpellSlot.Q
                });
            Spells.Add(
                new SpellData { CharacterName = "Vayne", SpellNames = new[] { "vaynecondemnmissile" }, Slot = SpellSlot.E });
            Spells.Add(
                new SpellData { CharacterName = "Veigar", SpellNames = new[] { "veigarprimordialburst" }, Slot = SpellSlot.R });
            Spells.Add(
                new SpellData { CharacterName = "Viktor", SpellNames = new[] { "viktorpowertransfer" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData
                {
                    CharacterName = "Vladimir",
                    SpellNames = new[] { "vladimirtidesofbloodnuke" },
                    Slot = SpellSlot.E
                });
        }

        private static void ObjSpellMissileOnCreate(GameObject sender, EventArgs args)
        {
            var missile = sender as MissileClient;
            if (missile == null || !missile.IsValid)
            {
                return;
            }
            var caster = missile.SpellCaster as AIHeroClient;
            if (caster == null || !caster.IsValid || caster.Team == ObjectManager.Player.Team || !(missile != null && missile.IsMe))
            {
                return;
            }
            var spellData =
                Spells.FirstOrDefault(
                    i =>
                    i.SpellNames.Contains(missile.SData.Name.ToLower())
                    && Fiora.Config["EvadeTarget"][i.CharacterName.ToLowerInvariant()].GetValue<MenuBool>(i.MissileName));
            if (spellData == null && Orbwalker.IsAutoAttack(missile.SData.Name)
                && (!missile.SData.Name.ToLower().Contains("crit")
                        ? Fiora.Config["EvadeTarget"]["AA"].GetValue<MenuBool>("B")
                          && Player.HealthPercent < Fiora.Config["EvadeTarget"]["AA"].GetValue<MenuSlider>("BHpU").Value
                        : Fiora.Config["EvadeTarget"]["AA"].GetValue<MenuBool>("C")
                          && Player.HealthPercent < Fiora.Config["EvadeTarget"]["AA"].GetValue<MenuSlider>("CHpU").Value))
            {
                spellData = new SpellData { CharacterName = caster.CharacterName, SpellNames = new[] { missile.SData.Name } };
            }
            if (spellData == null)
            {
                return;
            }
            DetectedTargets.Add(new Targets { Start = caster.Position, Obj = missile });
        }

        private static void ObjSpellMissileOnDelete(GameObject sender, EventArgs args)
        {
            var missile = sender as MissileClient;
            if (missile == null || !missile.IsValid)
            {
                return;
            }
            var caster = missile.SpellCaster as AIHeroClient;
            if (caster == null || !caster.IsValid || caster.Team == Player.Team)
            {
                return;
            }
            DetectedTargets.RemoveAll(i => i.Obj.NetworkId == missile.NetworkId);
        }

        private static void OnUpdateTarget(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (Player.HasBuffOfType(BuffType.SpellShield) || Player.HasBuffOfType(BuffType.SpellImmunity))
            {
                return;
            }
            if (!Fiora.Config["EvadeTarget"].GetValue<MenuBool>("W") || !Fiora.W.IsReady())
            {
                return;
            }
            foreach (var target in
                DetectedTargets.Where(i => Fiora.W.IsInRange(i.Obj, 150 + Game.Ping * i.Obj.SData.MissileSpeed / 1000)).OrderBy(i => i.Obj.Position.Distance(Player.Position)))
            {
                var tar = TargetSelector.GetTarget(Fiora.W.Range);
                if (tar.IsValidTarget(Fiora.W.Range))
                    Player.Spellbook.CastSpell(SpellSlot.W, tar.Position);
                else
                {
                    var hero = HeroManager.Enemies.FirstOrDefault(x => x.IsValidTarget(Fiora.W.Range));
                    if (hero != null)
                        Player.Spellbook.CastSpell(SpellSlot.W, hero.Position);
                    else
                        Player.Spellbook.CastSpell(SpellSlot.W, Player.Position.Extend(target.Start, 100));
                }
            }
        }

        #endregion

        private class SpellData
        {
            #region Fields

            public string CharacterName;

            public SpellSlot Slot;

            public string[] SpellNames = { };

            #endregion

            #region Public Properties

            public string MissileName
            {
                get
                {
                    return this.SpellNames.First();
                }
            }

            #endregion
        }

        private class Targets
        {
            #region Fields

            public MissileClient Obj;

            public Vector3 Start;

            #endregion
        }
    }
    public static class FioraPassive
    {
        #region FioraPassive
        public static List<Vector2> GetRadiusPoints(Vector2 targetpredictedpos, Vector2 passivepredictedposition)
        {
            List<Vector2> RadiusPoints = new List<Vector2>();
            for (int i = 50; i <= 300; i = i + 25)
            {
                var x = targetpredictedpos.Extend(passivepredictedposition, i);
                for (int j = -45; j <= 45; j = j + 5)
                {
                    RadiusPoints.Add(x.RotateAroundPoint(targetpredictedpos, j * (float)(Math.PI / 180)));
                }
            }
            return RadiusPoints;
        }
        public static PassiveStatus GetPassiveStatus(this AIHeroClient target, float delay = 0.25f)
        {
            var allobjects = GetPassiveObjects()
                .Where(x => x.Object != null && x.Object.IsValid
                           && x.Object.Position.ToVector2().Distance(target.Position.ToVector2()) <= 50);
            var targetpredictedpos = Prediction.GetFastUnitPosition(target, delay);
            if (!allobjects.Any())
            {
                return new PassiveStatus(false, PassiveType.None, new Vector2(), new List<PassiveDirection>(), new List<Vector2>());
            }
            else
            {
                var x = allobjects.First();
                var listdirections = new List<PassiveDirection>();
                foreach (var a in allobjects)
                {
                    listdirections.Add(a.PassiveDirection);
                }
                var listpositions = new List<Vector2>();
                foreach (var a in listdirections)
                {
                    if (a == PassiveDirection.NE)
                    {
                        var pos = targetpredictedpos;
                        pos.Y = pos.Y + 200;
                        listpositions.Add(pos);
                    }
                    else if (a == PassiveDirection.NW)
                    {
                        var pos = targetpredictedpos;
                        pos.X = pos.X + 200;
                        listpositions.Add(pos);
                    }
                    else if (a == PassiveDirection.SE)
                    {
                        var pos = targetpredictedpos;
                        pos.X = pos.X - 200;
                        listpositions.Add(pos);
                    }
                    else if (a == PassiveDirection.SW)
                    {
                        var pos = targetpredictedpos;
                        pos.Y = pos.Y - 200;
                        listpositions.Add(pos);
                    }
                }
                if (x.PassiveType == PassiveType.PrePassive)
                {
                    return new PassiveStatus(true, PassiveType.PrePassive, targetpredictedpos, listdirections, listpositions);
                }
                if (x.PassiveType == PassiveType.NormalPassive)
                {
                    return new PassiveStatus(true, PassiveType.NormalPassive, targetpredictedpos, listdirections, listpositions);
                }
                if (x.PassiveType == PassiveType.UltiPassive)
                {
                    return new PassiveStatus(true, PassiveType.UltiPassive, targetpredictedpos, listdirections, listpositions);
                }
                return new PassiveStatus(false, PassiveType.None, new Vector2(), new List<PassiveDirection>(), new List<Vector2>());
            }
        }
        public static List<PassiveObject> GetPassiveObjects()
        {
            List<PassiveObject> PassiveObjects = new List<PassiveObject>();
            foreach (var x in FioraPrePassiveObjects.Where(i => i != null && i.IsValid))
            {
                PassiveObjects.Add(new PassiveObject(x.Name, x, PassiveType.PrePassive, GetPassiveDirection(x)));
            }
            foreach (var x in FioraPassiveObjects.Where(i => i != null && i.IsValid))
            {
                PassiveObjects.Add(new PassiveObject(x.Name, x, PassiveType.NormalPassive, GetPassiveDirection(x)));
            }
            foreach (var x in FioraUltiPassiveObjects.Where(i => i != null && i.IsValid))
            {
                PassiveObjects.Add(new PassiveObject(x.Name, x, PassiveType.UltiPassive, GetPassiveDirection(x)));
            }
            return PassiveObjects;
        }
        public static PassiveDirection GetPassiveDirection(EffectEmitter x)
        {
            if (x.Name.Contains("NE"))
            {
                return PassiveDirection.NE;
            }
            else if (x.Name.Contains("SE"))
            {
                return PassiveDirection.SE;
            }
            else if (x.Name.Contains("NW"))
            {
                return PassiveDirection.NW;
            }
            else
            {
                return PassiveDirection.SW;
            }
        }
        public class PassiveStatus
        {
            public bool HasPassive;
            public PassiveType PassiveType;
            public Vector2 TargetPredictedPosition;
            public List<PassiveDirection> PassiveDirections = new List<PassiveDirection>();
            public List<Vector2> PassivePredictedPositions = new List<Vector2>();
            public PassiveStatus(bool hasPassive, PassiveType passiveType, Vector2 targetPredictedPosition
                , List<PassiveDirection> passiveDirections, List<Vector2> passivePredictedPositions)
            {
                HasPassive = hasPassive;
                PassiveType = passiveType;
                TargetPredictedPosition = targetPredictedPosition;
                PassiveDirections = passiveDirections;
                PassivePredictedPositions = passivePredictedPositions;
            }
        }
        public enum PassiveType
        {
            None, PrePassive, NormalPassive, UltiPassive
        }
        public enum PassiveDirection
        {
            NE, SE, NW, SW
        }
        public class PassiveObject
        {
            public string PassiveName;
            public EffectEmitter Object;
            public PassiveType PassiveType;
            public PassiveDirection PassiveDirection;
            public PassiveObject(string passiveName, EffectEmitter obj, PassiveType passiveType, PassiveDirection passiveDirection)
            {
                PassiveName = passiveName;
                Object = obj;
                PassiveType = passiveType;
                PassiveDirection = passiveDirection;
            }
        }
        public static List<EffectEmitter> FioraUltiPassiveObjects = new List<EffectEmitter>();
        //{
        //    get
        //    {
        //        var x = ObjectManager.Get<EffectEmitter>()
        //        .Where(a => a.Name.Contains("Fiora_Base_R_Mark") || (a.Name.Contains("Fiora_Base_R") && a.Name.Contains("Timeout_FioraOnly.troy")))
        //        .ToList();
        //        return x;
        //    }
        //}
        public static List<EffectEmitter> FioraPassiveObjects = new List<EffectEmitter>();
        //{
        //    get
        //    {
        //        var x = ObjectManager.Get<EffectEmitter>().Where(a => FioraPassiveName.Contains(a.Name)).ToList();
        //        return x;
        //    }
        //}
        public static List<EffectEmitter> FioraPrePassiveObjects = new List<EffectEmitter>();
        //{
        //    get
        //    {
        //        var x = ObjectManager.Get<EffectEmitter>().Where(a => FioraPrePassiveName.Contains(a.Name)).ToList();
        //        return x;
        //    }
        //}
        public static List<string> FioraPassiveName = new List<string>()
        {
            "Fiora_Base_Passive_NE.troy",
            "Fiora_Base_Passive_SE.troy",
            "Fiora_Base_Passive_NW.troy",
            "Fiora_Base_Passive_SW.troy",
            "Fiora_Base_Passive_NE_Timeout.troy",
            "Fiora_Base_Passive_SE_Timeout.troy",
            "Fiora_Base_Passive_NW_Timeout.troy",
            "Fiora_Base_Passive_SW_Timeout.troy"
        };
        public static List<string> FioraPrePassiveName = new List<string>()
        {
            "Fiora_Base_Passive_NE_Warning.troy",
            "Fiora_Base_Passive_SE_Warning.troy",
            "Fiora_Base_Passive_NW_Warning.troy",
            "Fiora_Base_Passive_SW_Warning.troy"
        };
        public static void FioraPassiveUpdate()
        {
            FioraPrePassiveObjects = new List<EffectEmitter>();
            FioraPassiveObjects = new List<EffectEmitter>();
            FioraUltiPassiveObjects = new List<EffectEmitter>();
            var ObjectEmitter = ObjectManager.Get<EffectEmitter>()
                                             .Where(a => FioraPassiveName.Contains(a.Name) || FioraPrePassiveName.Contains(a.Name)
                                             || a.Name.Contains("Fiora_Base_R_Mark")
                                             || (a.Name.Contains("Fiora_Base_R") && a.Name.Contains("Timeout_FioraOnly.troy")))
                                             .ToList();
            FioraPrePassiveObjects.AddRange(ObjectEmitter.Where(a => FioraPrePassiveName.Contains(a.Name)));
            FioraPassiveObjects.AddRange(ObjectEmitter.Where(a => FioraPassiveName.Contains(a.Name)));
            FioraUltiPassiveObjects.AddRange(ObjectEmitter
                .Where(a =>
                       a.Name.Contains("Fiora_Base_R_Mark")
                       || (a.Name.Contains("Fiora_Base_R") && a.Name.Contains("Timeout_FioraOnly.troy"))));
        }
        #endregion FioraPassive
    }
    public static class OrbwalkLastClick
    {
        private static Vector2 LastClickPoint = new Vector2();
        public static void Init()
        {
            Game.OnUpdate += Game_OnUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Player.OnIssueOrder += AIBaseClient_OnIssueOrder;
        }

        private static void AIBaseClient_OnIssueOrder(
    AIBaseClient sender,
    PlayerIssueOrderEventArgs args
)
        {
            if (!OrbwalkLastClickActive)
                return;
            if (!sender.IsMe)
                return;
            if (args.Order != GameObjectOrder.MoveTo)
                return;
            if (!Orbwalker.CanMove())// || Player.IsCastingInterruptableSpell())
                args.Process = false;
        }

//        public static void OrbwalkLRCLK_ValueChanged(
//    Object sender,
//    EventArgs e
//)
//        {
//            if (e.GetNewValue<KeyBind>().Active)
//            {
//                LastClickPoint = Game.CursorPosRaw.ToVector2();
//            }
//        }
        private static void Game_OnUpdate(EventArgs args)
        {
            if (!OrbwalkLastClickActive)
                return;
            Combo();
            var target = TargetSelector.GetTarget(500);
            Orbwalker.Orbwalk(
                        target.InAutoAttackRange() ? target : null,
                        LastClickPoint.IsValid() ? LastClickPoint.ToVector3() : Game.CursorPosRaw);
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowsMessages.RBUTTONDOWN)
            {
                LastClickPoint = Game.CursorPosRaw.ToVector2();
            }
        }
    }

    class Fiora
    {
        private static AIHeroClient Player { get { return ObjectManager.Player; } }

        public static Spell Q, W, E, R;

        private const float LaneClearWaitTimeMod = 2f;

        public static Menu Config;

        public Fiora()
        {
            Q = new Spell(SpellSlot.Q, 400);
            W = new Spell(SpellSlot.W, 750);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);
            W.SetSkillshot(0.75f, 80, 2000, false, false, SkillshotType.Line);
            W.MinHitChance = HitChance.High;


            Config = new Menu(Player.CharacterName, "DH.Fiora", true);

            Menu spellMenu = Config.Add(new Menu("Spell", "Spell"));

            Menu Harass = spellMenu.Add(new Menu("Harass", "Harass"));

            Menu Combo = spellMenu.Add(new Menu("Combo", "Combo"));

            Menu Target = Config.Add(new Menu("Targeting Modes", "Targeting Modes"));

            Menu PriorityMode = Target.Add(new Menu("Priority", "Priority Mode"));

            Menu OptionalMode = Target.Add(new Menu("Optional", "Optional Mode"));

            Menu SelectedMode = Target.Add(new Menu("Selected", "Selected Mode"));

            Menu LaneClear = spellMenu.Add(new Menu("Lane Clear", "Lane Clear"));

            spellMenu.Add(new MenuKeyBind("Orbwalk Last Right Click", "Orbwalk Last Right Click", System.Windows.Forms.Keys.A, KeyBindType.Press));
                    //.ValueChanged += OrbwalkLastClick.OrbwalkLRCLK_ValueChanged;

            Menu JungClear = spellMenu.Add(new Menu("Jungle Clear", "Jungle Clear"));

            Menu Misc = Config.Add(new Menu("Misc", "Misc"));

            Menu Draw = Config.Add(new Menu("Draw", "Draw")); ;

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
                SpellBlocking.EvadeManager.Attach();
                Evade.Evade.Init();
                EvadeTarget.Init();
                TargetedNoMissile.Init();
                OtherSkill.Init();
            }
            OrbwalkLastClick.Init();
            Config.Attach();
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
                        if (Player.CanUseItem((int)ItemId.Youmuus_Ghostblade))
                            Player.UseItem((int)ItemId.Youmuus_Ghostblade);
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
                            Render.Circle.DrawCircle(x.ToVector3(), 50, Color.Yellow);
                        }
                    }
                }
            }
            if (activewalljump)
            {
                var Fstwall = GetFirstWallPoint(Player.Position.ToVector2(), Game.CursorPosRaw.ToVector2());
                if (Fstwall != null)
                {
                    var firstwall = ((Vector2)Fstwall);
                    var pos = firstwall.Extend(Game.CursorPosRaw.ToVector2(), 100);
                    var Lstwall = GetLastWallPoint(firstwall, Game.CursorPosRaw.ToVector2());
                    if (Lstwall != null)
                    {
                        var lastwall = ((Vector2)Lstwall);
                        if (InMiddileWall(firstwall, lastwall))
                        {
                            for (int i = 0; i <= 359; i++)
                            {
                                var pos1 = pos.RotateAround(firstwall, i);
                                var pos2 = firstwall.Extend(pos1, 400);
                                if (pos1.InTheCone(firstwall, Game.CursorPosRaw.ToVector2(), 60) && pos1.IsWall() && !pos2.IsWall())
                                {
                                    Render.Circle.DrawCircle(firstwall.ToVector3(), 50, Color.Green);
                                    goto Finish;
                                }
                            }

                            Render.Circle.DrawCircle(firstwall.ToVector3(), 50, Color.Red);
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
                var Fstwall = GetFirstWallPoint(Player.Position.ToVector2(), Game.CursorPosRaw.ToVector2());
                if (Fstwall != null)
                {
                    var firstwall = ((Vector2)Fstwall);
                    var Lstwall = GetLastWallPoint(firstwall, Game.CursorPosRaw.ToVector2());
                    if (Lstwall != null)
                    {
                        var lastwall = ((Vector2)Lstwall);
                        if (InMiddileWall(firstwall, lastwall))
                        {
                            var y = Player.Position.Extend(Game.CursorPosRaw, 30);
                            for (int i = 20; i <= 300; i = i + 20)
                            {
                                if (Utils.GameTimeTickCount - movetick < (70 + Math.Min(60, Game.Ping)))
                                    break;
                                if (Player.Distance(Game.CursorPosRaw) <= 1200 && Player.Position.ToVector2().Extend(Game.CursorPosRaw.ToVector2(), i).IsWall())
                                {
                                    Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.ToVector2().Extend(Game.CursorPosRaw.ToVector2(), i - 20).ToVector3());
                                    movetick = Utils.GameTimeTickCount;
                                    break;
                                }
                                Player.IssueOrder(GameObjectOrder.MoveTo,
                                    Player.Distance(Game.CursorPosRaw) <= 1200 ?
                                    Player.Position.ToVector2().Extend(Game.CursorPosRaw.ToVector2(), 200).ToVector3() :
                                    Game.CursorPosRaw);
                            }
                            if (y.IsWall() && Prediction.GetPrediction(Player, 500).UnitPosition.Distance(Player.Position) <= 10 && Q.IsReady())
                            {
                                var pos = Player.Position.ToVector2().Extend(Game.CursorPosRaw.ToVector2(), 100);
                                for (int i = 0; i <= 359; i++)
                                {
                                    var pos1 = pos.RotateAround(Player.Position.ToVector2(), i);
                                    var pos2 = Player.Position.ToVector2().Extend(pos1, 400);
                                    if (pos1.InTheCone(Player.Position.ToVector2(), Game.CursorPosRaw.ToVector2(), 60) && pos1.IsWall() && !pos2.IsWall())
                                    {
                                        Q.Cast(pos2);
                                    }

                                }
                            }
                        }
                        else if (Utils.GameTimeTickCount - movetick >= (70 + Math.Min(60, Game.Ping)))
                        {
                            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPosRaw);
                            movetick = Utils.GameTimeTickCount;
                        }
                    }
                    else if (Utils.GameTimeTickCount - movetick >= (70 + Math.Min(60, Game.Ping)))
                    {
                        Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPosRaw);
                        movetick = Utils.GameTimeTickCount;
                    }
                }
                else if (Utils.GameTimeTickCount - movetick >= (70 + Math.Min(60, Game.Ping)))
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPosRaw);
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
            var point = midwall.Extend(Game.CursorPosRaw.ToVector2(), 50);
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
                    if (Player.Position.ToVector2().Distance(target.Position.ToVector2()) <= OrbwalkToPassiveRange && status.HasPassive
                        && ((TargetingMode == TargetMode.Selected && OrbwalkToPassiveTargeted && (OrbwalkTargetedUnderTower || !Player.UnderTurret(true)))
                        || (TargetingMode == TargetMode.Optional && OrbwalkToPassiveOptional && (OrbwalkOptionalUnderTower || !Player.UnderTurret(true)))
                        || (TargetingMode == TargetMode.Priority && OrbwalkToPassivePriority && (OrbwalkPriorityUnderTower || !Player.UnderTurret(true)))))
                    {
                        var point = status.PassivePredictedPositions.OrderBy(x => x.Distance(Player.Position.ToVector2())).FirstOrDefault();
                        point = point.IsValid() ? target.Position.ToVector2().Extend(point, 150) : Game.CursorPosRaw.ToVector2();
                        Orbwalker.SetOrbwalkerPosition(point.ToVector3());
                        // humanizer
                        //if (InAutoAttackRange(target)
                        //        && status.PassivePredictedPositions.Any(x => Player.Position.ToVector2()
                        //            .InTheCone(status.TargetPredictedPosition, x, 90)))
                        //{
                        //    Orbwalker.SetMovement(false);
                        //    return;
                        //}
                    }
                    else Orbwalker.SetOrbwalkerPosition(Game.CursorPosRaw);
                }
                else Orbwalker.SetOrbwalkerPosition(Game.CursorPosRaw);
            }
            else Orbwalker.SetOrbwalkerPosition(Game.CursorPosRaw);
            //Orbwalker.SetMovement(true);
        }
        #endregion OrbwalkToPassive
    }
}
