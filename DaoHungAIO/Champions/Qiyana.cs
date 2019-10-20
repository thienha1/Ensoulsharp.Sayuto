using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EnsoulSharp.SDK.Geometry;
using Utility = EnsoulSharp.SDK.Utility;

namespace DaoHungAIO.Champions
{
    class Qiyana
    {

        private static Spell _q, _w, _e, _r, _q2;
        private static Menu _menu;
        private static int _wRangeCollect = 1250;
        private static AIHeroClient Player = ObjectManager.Player;


        #region
        private static readonly MenuBool Qcombo = new MenuBool("qcombo", "[Q] on Combo");
        private static readonly MenuBool Wcombo = new MenuBool("wcombo", "[W] on Combo");
        private static readonly MenuList WPriority = new MenuList("wpriority", "^ Priority", new[] { "Rock", "Grass", "Water" }, 2);
        private static readonly MenuList WFindType = new MenuList("wfindtype", "^ Find type", new[] { "Around hero", "Around cursor" }, 1);
        private static readonly MenuList WDashType = new MenuList("WDashType", "^ Dash type", new[] { "Safe", "Cursor" }, 1);
        private static readonly MenuBool Wsave = new MenuBool("wsave", "^ After Q");
        private static readonly MenuBool Ecombo = new MenuBool("Ecombo", "[E] on Combo");
        private static readonly MenuBool Eminions = new MenuBool("Eminions", "^ Cast on Minion on Combo if Out Range");
        private static readonly MenuBool Rcombo = new MenuBool("Rcombo", "[R] on Combo");
        private static readonly MenuSlider Rcount = new MenuSlider("Rcount", "^ when hit X enemies", 1, 1, 5);

        private static readonly MenuBool Qharass = new MenuBool("qharass", "[Q] on Harass");
        private static readonly MenuBool Eharass = new MenuBool("Eharass", "[E] on Harass");

        private static readonly MenuBool Qclear = new MenuBool("qclear", "[Q] on ClearWave");

        private static readonly MenuBool MiscWAntiGapcloser = new MenuBool("MiscWAntiGapcloser", "AntiGapcloser with W");
        private static readonly MenuBool MiscEAntiGapcloser = new MenuBool("MiscEAntiGapcloser", "AntiGapcloser with E");


        private static string _qType = "QiyanaQ";
        private static bool IsRock() => Player.HasBuff("QiyanaQ_Rock");
        private static bool IsWater() => Player.HasBuff("QiyanaQ_Water");
        private static bool IsGrass() => Player.HasBuff("QiyanaQ_Grass");
        private static bool HasRock(AIHeroClient target) => target.HasBuff("qiyanapassivecd_rock");
        private static bool HasWater(AIHeroClient target) => target.HasBuff("qiyanapassivecd_water");
        private static bool HasGrass(AIHeroClient target) => target.HasBuff("qiyanapassivecd_grass");
        // QiyanaQ_Rock
        // QiyanaQ_Water
        // QiyanaQ_Grass
        #endregion


        public Qiyana()
        {
            if (ObjectManager.Player.CharacterName != "Qiyana")
            {
                return;
            }

            _q = new Spell(SpellSlot.Q, 470);
            _q2 = new Spell(SpellSlot.Q, 710);
            // of have buff 500f 900f
            _w = new Spell(SpellSlot.W, 1250);
            // 330f for dash and 1100f for range scan target;
            _e = new Spell(SpellSlot.E, 650f);
            // 650f
            _r = new Spell(SpellSlot.R, 875);
            // 950f
            _q.SetSkillshot(0.25f, 140, 1200f, false, SkillshotType.Line);
            _w.SetSkillshot(0.25f, 0f, 1200f, false, SkillshotType.Circle);
            _e.SetTargetted(0.25f, float.MaxValue);
            _r.SetSkillshot(0.25f, 280, 2000, false, SkillshotType.Line);

            CreateMenu();
            Game.OnTick += OnTick;
            AIHeroClient.OnProcessSpellCast += OnProcessSpellCast;
            Gapcloser.OnGapcloser += OnGapcloser;
        }

        private void CastW(AIHeroClient target)
        {
            var pos = GetPosWCast(target);
            if (pos != null && pos != new GameObject())
            {
                try
                {
                    _w.Cast(pos.Position);
                }
                catch
                {
                }
                return;
            }
        }
        private void OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            Chat.Print("a spell cast");
            if(sender.IsEnemy)
            {
                if(MiscWAntiGapcloser.Enabled && _e.IsReady())
                {
                    Chat.Print("try W");
                    CastW(sender);
                }
            }
        }

        private void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if(Orbwalker.ActiveMode == OrbwalkerMode.Combo && Wsave.Enabled && args.Slot == SpellSlot.Q && _w.IsReady())
                {
                    var target = TargetSelector.SelectedTarget;
                    if(target == null || !target.IsValidEnemy(_q2.Range + 200))
                    {
                        target = TargetSelector.GetTarget(_q2.Range + 200);
                    }
                    if(target == null)
                    {
                        return;
                    }

                    CastW(target);

                }

                if(args.Target is AIMinionClient)
                {
                    var currentState = Qcombo.Enabled;
                    Qcombo.Enabled = false;
                    Utility.DelayAction.Add((int)((args.Time - Game.Time) * 1000), () => Qcombo.Enabled = currentState);
                }

                if(args.Slot == SpellSlot.R)
                {
                    Chat.Print(args.Target.Name);
                }
            }
        }

        private static GameObject GetPosWCast(AIHeroClient target)
        {
            GameObject obj = new GameObject();
            switch (WFindType.Index)
            {
                case 0: //"Around 1200 hero"
                    obj = GetByPiority(1200, Player.Position, target);
                    break;
                case 1: // "Around 183 cursor"
                    obj = GetByPiority(183, Game.CursorPosRaw, target);
                    break;
            }
            return obj;
        }

        private static GameObject GetByPiority(int range, Vector3 from, AIHeroClient target)
        {
            GameObject obj = new GameObject();
            switch (WPriority.SelectedValue)
            {
                case "Rock": //"Rock"
                    if (!IsRock()) {
                        obj = GetRockObject(range, from);
                    }
                    break;
                case "Grass": // "Grass"
                    if (!IsGrass())
                    {
                        obj = GetGrassObject(range, from);
                    }
                    break;
                case "Water": // "Water"
                    if (!IsWater()) {
                        obj = GetByDefault(target);
                    }
                    break;
            }
            if(obj == null || obj == new GameObject())
            {
                return GetByDefault(target);
            } else
            {
                return obj;
            }
        }

        private static GameObject OrderByPos(IEnumerable<GameObject> ienum)
        {
            switch (WDashType.Index)
            {
                case 0:
                    return ienum.OrderBy(o => o.CountEnemyHeroesInRange(500)).FirstOrDefault();
                case 1:
                    return ienum.OrderBy(o => o.DistanceToCursor()).FirstOrDefault();
                default:
                    return ienum.OrderBy(o => o.DistanceToCursor()).FirstOrDefault();
            }
        }
        private static GrassObject OrderByPos(IEnumerable<GrassObject> ienum)
        {
            switch (WDashType.Index)
            {
                case 0:
                    return ienum.OrderBy(o => o.CountEnemyHeroesInRange(500)).FirstOrDefault();
                case 1:
                    return ienum.OrderBy(o => o.DistanceToCursor()).FirstOrDefault();
                default:
                    return ienum.OrderBy(o => o.DistanceToCursor()).FirstOrDefault();
            }
        }
        private static GameObject GetByDefault(AIHeroClient target)
        {
            if (HasRock(target))
            {
                return OrderByPos(ObjectManager.Get<GameObject>().Where(o => o.DistanceToPlayer() <= 1200 && !o.Position.IsWall() && !o.Position.IsBuilding()));
            }
            if (HasGrass(target))
            {
                return OrderByPos(ObjectManager.Get<GameObject>().Where(o => o.DistanceToPlayer() <= 1200 && !(o is GrassObject)));
            }
            //  HasWater
            return OrderByPos(ObjectManager.Get<GameObject>().Where(o => o.DistanceToPlayer() <= 1200));
        }
        private static GameObject GetRockObject(int range, Vector3 from)
        {
            return OrderByPos(ObjectManager.Get<GameObject>().Where(o => o.Distance(from) <= range && (o.Position.IsWall() || o.Position.IsBuilding())));
        }
        private static GrassObject GetGrassObject(int range, Vector3 from)
        {
            return OrderByPos(ObjectManager.Get<GrassObject>().Where(o => o.Distance(from) <= range));
        }

        private static bool IsEnchanced()
        {
            return IsGrass() || IsRock() || IsWater();
        }

        private static void CreateMenu()
        {
            _menu = new Menu("dhqiyana", "DH.Qiyana(Isnt Release)", true);
            var _combat = new Menu("dh_qiyana_combat", "[Combo] Settings");
            var _harass = new Menu("dh_qiyana_harrass", "[Harass] Settings");
            var _farm = new Menu("dh_qiyana_farm", "[Farm] Settings");
            var _misc = new Menu("dh_qiyana_misc", "[Misc] Settings");
            _combat.Add(Qcombo);
            _combat.Add(Wcombo);
            _combat.Add(Wsave);
            _combat.Add(Ecombo);
            _combat.Add(Eminions);

            _harass.Add(Qharass);
            _harass.Add(Eharass);

            _farm.Add(Qclear);

            _misc.Add(WPriority);
            _misc.Add(WFindType);
            _misc.Add(WDashType);
            _misc.Add(MiscWAntiGapcloser);
            _misc.Add(MiscEAntiGapcloser);

            _menu.Add(_combat);
            _menu.Add(_harass);
            _menu.Add(_farm);
            _menu.Add(_misc);
            _menu.Attach();
        }

        public void OnTick(EventArgs args)
        {

            //Render.Circle.DrawCircle(Player.Position, 470, System.Drawing.Color.Red);
            //GameObjects.AllGameObjects.Where(o => o.CountEnemyHeroesInRange(500) <= 350 && o is GrassObject ).ForEach(o => {
            //    _w.Cast(o.Position);
            //    Render.Circle.DrawCircle(o.Position, 20, System.Drawing.Color.Red, 10);
            //});
            //ObjectManager.Get<GrassObject>().Where(g => g.DistanceToCursor() <= 350).ForEach(o =>
            //{
            //    //    _w.Cast(o.Position);
            //    Render.Circle.DrawCircle(o.Position, 20, System.Drawing.Color.Red, 10);
            //});
            //ObjectManager.Player.Buffs.ForEach(b => {
            //    Chat.Print(b.Name);
            //});
            //if (IsGrass())
            //{
            //    Chat.Print("Is Grass");
            //}
            //if (IsRock())
            //{
            //    Chat.Print("Is Rock");
            //}
            //if (IsWater())
            //{
            //    Chat.Print("Is Water");
            //}
            //TargetSelector.SelectedTarget.Buffs.ForEach(b => Chat.Print(b.Name));
            //"qiyanapassivecd_base"

            //var abc = Render.Add(new Polygon());

            if (IsEnchanced())
            {
                _q.Range = 710f;
            } else
            {
                _q.Range = 470f;
            }
            switch (Orbwalker.ActiveMode)
            {
                case (OrbwalkerMode.Combo):
                    //ObjectManager.Get<GrassObject>().Where(g => g.DistanceToCursor() <= 350).ForEach(o => {
                    //    _w.CastOnUnit(o);
                    //    Render.Circle.DrawCircle(o.Position, 20, System.Drawing.Color.HotPink, 10);
                    //});
                    //Chat.Print(NavMesh.GetCollisionFlags(Game.CursorPosRaw).ToString());
                    DoCombo();
                    break;
                case OrbwalkerMode.Harass:
                    DoHarass();
                    break;
                case OrbwalkerMode.LaneClear:
                    DoClear();
                    DoJungleClear();
                    break;
                case OrbwalkerMode.LastHit:
                    DoFarm();
                    break;

            }
        }

        //private readonly string[] ignoreMinions = { "jarvanivstandard" };
        //private bool IsValidUnit(AttackableUnit unit, float range = 0f)
        //{
        //    var minion = unit as AIMinionClient;
        //    return unit.IsValidTarget(range > 0 ? range : unit.GetRealAutoAttackRange())
        //           && (minion == null || minion.IsHPBarRendered);
        //}
        //private List<AIMinionClient> GetEnemyMinions(float range = 0)
        //{
        //    return
        //        GameObjects.EnemyMinions.Where(
        //            m => this.IsValidUnit(m, range) && !this.ignoreMinions.Any(b => b.Equals(m.CharacterName.ToLower())))
        //            .ToList();
        //}
        private void DoCombo()
        {
            // buffs: QiyanaQ, QiyanaW, QiyanaPassive
            var player = ObjectManager.Player;
            var target = TargetSelector.GetTarget(_e.Range + _q.Range);
            var etarget = TargetSelector.GetTarget(_e.Range);
            //player.Buffs.ForEach(delegate (BuffInstance buff)
            //    {
            //        Chat.Say(buff.Name, false);
            //    }
            // );
            //Chat.Say(, false);
            if (target == null)
                return;
            if (etarget != null)
            {
                if (_e.CanCast(etarget) && _q.IsReady())
                    _e.Cast(etarget);
            }
            else
            {
                if (Eminions.Enabled)
                {
                    var AttackUnit =
                       GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_e.Range) && x.DistanceToPlayer() > _q.Range)
                           .OrderBy(x => x.Distance(target.Position))
                           .FirstOrDefault();

                    if (AttackUnit != null && !AttackUnit.IsDead && AttackUnit.IsValidTarget(_e.Range))
                    {
                        _e.Cast(AttackUnit);
                        return;
                    }
                }
            }
            if (Qcombo.Enabled && _q.IsReady() && etarget.IsValidTarget(_q.Range))
            {
                _q.Cast(etarget.Position);
            }

            if (((Wsave.Enabled && !_q.IsReady()) || !Wsave.Enabled) && _w.IsReady() && (etarget.IsValidTarget(_q.Range) && _q.CooldownTime > 1.5))
            {

                CastW(etarget);
            }
        }

        private static void DoHarass()
        {
            var t = TargetSelector.GetTarget(_q.Range);
            var etarget = TargetSelector.GetTarget(2000f);
            if (t == null)
                return;
            if (Eharass.Enabled)
            {
                _e.Cast();
            }

            if (_q.IsReady() && t.IsValidTarget(_q.Range) && Qharass.Enabled)
            {
                _q.Cast(t);
            }
            if (_e.IsReady() && t.IsValidTarget(_e.Range) && Eharass.Enabled && !t.HasBuff("AkaliEMis"))
            {
                _e.Cast(t);
            }
        }

        private static float ComboFull(AIHeroClient t)
        {
            var d = 0f;
            if (t != null)
            {
                if (_q.IsReady()) d = d + _q.GetDamage(t);
                if (_e.IsReady()) d = d + _e.GetDamage(t);
                if (_r.IsReady()) d = d + _r.GetDamage(t, DamageStage.Default);
                if (_r.IsReady()) d = d + _r.GetDamage(t, DamageStage.SecondCast);
                d = d + (float)ObjectManager.Player.GetAutoAttackDamage(t);
            }
            return d;
        }

        private static void DoClear()
        {
            if (!Qclear.Enabled)
            {
                return;
            }
            var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_q.Range) && x.IsMinion())
    .Cast<AIBaseClient>().ToList();
            if (minions.Any())
            {
                var qfarm = _q.GetCircularFarmLocation(minions);
                if (qfarm.Position.IsValid() && qfarm.MinionsHit >= 2)
                {
                    _q.Cast(qfarm.Position);
                }
            }
        }
        private static void DoJungleClear()
        {
            var mob = GameObjects.Jungle
                .Where(x => x.IsValidTarget(_q.Range) && x.GetJungleType() != JungleType.Unknown)
                .OrderByDescending(x => x.MaxHealth).FirstOrDefault();

            if (mob != null)
            {
                if (_q.IsReady() && mob.IsValidTarget(_q.Range))
                    _q.Cast(mob);
                if (_e.IsReady() && mob.IsValidTarget(_e.Range))
                    _e.Cast(mob);
            }
        }
        private static void DoFarm()
        {
            //if (!Qfarm.Enabled)
            //{
            //    return;
            //}
            var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_q.Range) && x.IsMinion() && x.Health < _q.GetDamage(x) && x.DistanceToPlayer() > ObjectManager.Player.GetRealAutoAttackRange())
    .Cast<AIBaseClient>().ToList();
            if (minions.Any())
            {
                var qfarm = _q.GetCircularFarmLocation(minions);
                if (qfarm.Position.IsValid() && qfarm.MinionsHit >= 1)
                {
                    _q.Cast(qfarm.Position);
                }
            }
        }
    }

}