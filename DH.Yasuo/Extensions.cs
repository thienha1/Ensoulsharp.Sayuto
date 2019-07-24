using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DH.Yasuo
{
    static class Extensions
    {
        internal static AIHeroClient Player = Helper.Yasuo;

        internal static bool IsDashable(this AIBaseClient unit, float range = 475)
        {
            if (!SpellSlot.E.IsReady() || unit == null || unit.Team == Player.Team || unit.Distance(Player) > range || !unit.IsValid || unit.IsDead || !unit.IsVisible || !unit.IsTargetable)
            {
                return false;
            }

            if (Helper.GetBool("Misc", "Misc.SafeE"))
            {
                var point = Helper.GetDashPos(unit);
                if (!Evade.Program.IsSafe(point).IsSafe)
                {
                    return false;
                }
            }

            var minion = unit as AIMinionClient;
            return !unit.HasBuff("YasuoDashWrapper") && (unit is AIHeroClient || minion.IsValidMinion());
        }

        internal static bool IsDashableFrom(this AIBaseClient unit, Vector2 fromPos, float range = 475)
        {
            if (unit == null || unit.Team == Player.Team || unit.Distance(fromPos) > range || !unit.IsValid || unit.IsDead || !unit.IsVisible || !unit.IsTargetable)
            {
                return false;
            }

            if (Helper.GetBool("Misc", "Misc.SafeE"))
            {
                var point = Helper.GetDashPos(unit);
                if (!Evade.Program.IsSafe(point).IsSafe)
                {
                    return false;
                }
            }

            var minion = unit as AIMinionClient;
            return !unit.HasBuff("YasuoDashWrapper") && (unit is AIHeroClient || minion.IsValidMinion());
        }


        internal static bool IsValidMinion(this AIMinionClient minion, float range = 2000)
        {
            if (minion == null)
            {
                return false;
            }

            var name = minion.CharacterName.ToLower();
            return (Player.Distance(minion) <= range && minion.IsValid && minion.IsTargetable && !minion.IsInvulnerable && minion.IsVisible && minion.Team != Player.Team && minion.IsHPBarRendered && (minion.IsMinion || minion.Team == GameObjectTeam.Neutral && minion.MaxHealth > 5) && !name.Contains("gangplankbarrel"));
        }

        internal static bool IsValidAlly(this AIBaseClient unit, float range = 2000)
        {
            if (unit == null || unit.Distance(Player) > range || unit.Team != Player.Team || !unit.IsValid || unit.IsDead || !unit.IsVisible || unit.IsTargetable)
            {
                return false;
            }
            return true;
        }

        internal static bool IsValidEnemy(this AIBaseClient unit, float range = 2000)
        {
            if (unit == null || !unit.IsHPBarRendered || unit.IsZombie || unit.Distance(Player) > range || unit.Team == Player.Team || !unit.IsValid || unit.IsDead || !unit.IsVisible || !unit.IsTargetable)
            {
                return false;
            }
            return true;
        }

        internal static bool IsInRange(this AIBaseClient unit, float range)
        {
            if (unit != null)
            {
                return Vector2.Distance(unit.Position.ToVector2(), Helper.Yasuo.Position.ToVector2()) <= range;
            }
            return false;
        }

        internal static bool PointUnderEnemyTurret(this Vector2 Point)
        {
            var EnemyTurrets =
                ObjectManager.Get<AITurretClient>().Find(t => t.IsEnemy && Vector2.Distance(Point, t.Position.ToVector2()) < 910f + Helper.Yasuo.BoundingRadius);
            return EnemyTurrets != null;
        }

        internal static bool PointUnderEnemyTurret(this Vector3 Point)
        {
            var EnemyTurrets =
                ObjectManager.Get<AITurretClient>().Where(t => t.IsEnemy && Vector3.Distance(t.Position, Point) < 910f + Helper.Yasuo.BoundingRadius);
            return EnemyTurrets.Any();
        }

        internal static bool CanKill(this AIBaseClient @base, SpellSlot slot)
        {
            if (slot == SpellSlot.E)
            {
                return Helper.GetProperEDamage(@base) >= @base.Health;
            }
            return Player.GetSpellDamage(@base, slot) >= @base.Health;
        }

        internal static bool IsCloserWP(this Vector2 point, AIBaseClient target)
        {
            var wp = target.GetWaypoints();
            var lastwp = wp.LastOrDefault();
            var wpc = wp.Count();
            var midwpnum = wpc / 2;
            var midwp = wp[midwpnum];
            var plength = wp[0].Distance(lastwp);
            return (point.Distance(target.Position) < 0.95f * Player.Distance(target.Position) - Helper.Yasuo.BoundingRadius) || ((plength < Player.Distance(target.Position) * 1.2f && point.Distance(lastwp.ToVector3()) < Player.Distance(lastwp.ToVector3()) || point.Distance(midwp.ToVector3()) < Player.Distance(midwp)));
        }

        internal static bool IsCloser(this Vector2 point, AIBaseClient target)
        {
            if (Helper.GetBool("Combo", "Combo.EAdvanced"))
            {
                return IsCloserWP(point, target);
            }
            return (point.Distance(target.Position) < 0.95f * Player.Distance(target.Position) - Helper.Yasuo.BoundingRadius);
        }

        internal static bool IsCloser(this AIBaseClient @base, AIBaseClient target)
        {
            return Helper.GetDashPos(@base).Distance(target.Position) < Player.Distance(target.Position);
        }

        internal static Vector3 WTS(this Vector3 vect)
        {
            return Drawing.WorldToScreen(vect).ToVector3();
        }


        //Menu Extensions

        internal static Menu AddSubMenu(this Menu menu, string disp)
        {
            return menu.Add(new Menu(disp, Assembly.GetExecutingAssembly().GetName() + "." + disp));
        }

        internal static MenuItem AddBool(this Menu menu, string name, string displayname, bool @defaultvalue = true)
        {
            return menu.Add(new MenuBool(name, displayname, @defaultvalue));
        }

        internal static MenuItem AddKeyBind(this Menu menu, string name, string displayname, System.Windows.Forms.Keys key, KeyBindType type)
        {
            return menu.Add(new MenuKeyBind(name, displayname, key, type));
        }

        //internal static MenuItem AddCircle(this Menu menu, string name, string dname, float range, System.Drawing.Color col)
        //{
        //    return menu.Add(new MenuItem(name, name).SetValue(new Circle(true, col, range)));
        //}

        internal static MenuItem AddSlider(this Menu menu, string name, string displayname, int initial = 0, int min = 0, int max = 100)
        {
            return menu.Add(new MenuSlider(name, displayname, initial, min, max));
        }

        internal static MenuItem AddSList(this Menu menu, string name, string displayname, string[] stringlist, int @default = 0)
        {
            return menu.Add(new MenuList(name, displayname, stringlist, @default));
        }

        internal static bool IsTargetValid(this AttackableUnit unit,
        float range = float.MaxValue,
        bool checkTeam = true,
        Vector3 from = new Vector3())
        {
            if (unit == null || !unit.IsValid || unit.IsDead || !unit.IsVisible || !unit.IsTargetable ||
                unit.IsInvulnerable)
            {
                return false;
            }

            var @base = unit as AIBaseClient;
            if (@base != null)
            {
                if (@base.HasBuff("kindredrnodeathbuff") && @base.HealthPercent <= 10)
                {
                    return false;
                }
            }

            if (checkTeam && unit.Team == ObjectManager.Player.Team)
            {
                return false;
            }

            var unitPosition = @base != null ? @base.Position : unit.Position;

            return !(range < float.MaxValue) ||
                   !(Vector2.DistanceSquared(
                       (@from.ToVector2().IsValid() ? @from : ObjectManager.Player.Position).ToVector2(),
                       unitPosition.ToVector2()) > range * range);
        }

        internal static bool QCanKill(this AIBaseClient minion, bool isQ2 = false)
        {
            //var hpred =
            //  HealthPrediction.GetPrediction(minion, 0, 500 + Game.Ping / 2);
            // return hpred < 0.95 * Player.GetSpellDamage(minion, SpellSlot.Q) && hpred > 0;
            var qspell = isQ2 ? Helper.Spells[Helper.Q2] : Helper.Spells[Helper.Q];
            var dmg = Player.GetSpellDamage(minion, SpellSlot.Q)
                            >= 1.1 * HealthPrediction.GetPrediction(
                                minion,
                                (int)(Player.Distance(minion) / qspell.Speed) * 1000,
                                (int)qspell.Delay * 1000);
            return dmg;
        }

        internal static bool ECanKill(this AIBaseClient minion)
        {
            var espell = Helper.Spells[Helper.E];
            return Helper.GetProperEDamage(minion)
                            >= 1.1 * HealthPrediction.GetPrediction(
                                minion,
                                (int)(Player.Distance(minion) / espell.Speed) * 1000,
                                (int)espell.Delay * 1000);
        }

        internal static bool isBlackListed(this AIHeroClient unit)
        {
            return !Helper.GetBool("Combo", "ult" + unit.CharacterName);
        }

        internal static int MinionsInRange(this AIBaseClient unit, float range)
        {
            var minions = ObjectManager.Get<AIMinionClient>().Count(x => x.Distance(unit) <= range && x.NetworkId != unit.NetworkId && x.Team == unit.Team);
            return minions;
        }

        internal static int MinionsInRange(this Vector2 pos, float range)
        {
            var minions = ObjectManager.Get<AIMinionClient>().Count(x => x.Distance(pos) <= range && (x.IsEnemy || x.Team == GameObjectTeam.Neutral));
            return minions;
        }

        internal static int MinionsInRange(this Vector3 pos, float range)
        {
            var minions =
                ObjectManager.Get<AIMinionClient>()
                    .Count(x => x.Distance(pos) <= range && (x.IsEnemy || x.Team == GameObjectTeam.Neutral));
            return minions;
        }


        internal static IEnumerable<AIMinionClient> GetMinionsInRange(this Vector2 pos, float range)
        {
            var minions = ObjectManager.Get<AIMinionClient>().Where(x => x.IsValidTarget() && x.Distance(pos) <= range && (x.IsEnemy || x.Team == GameObjectTeam.Neutral));
            return minions;
        }
    }
}
