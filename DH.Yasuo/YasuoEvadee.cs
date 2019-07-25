using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Prediction;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DH.Yasuo
{
    static class YasuoEvadee
    {

        private static Random rand = new Random();

        internal static void Evade()
        {
            if (!Helper.GetBool("Evade", "Evade.Enabled"))
            {
                return;
            }

            if (!Helper.Spells[Helper.W].IsReady() && !Helper.Spells[Helper.E].IsReady())
            {
                return;
            }

            var skillshots = Program.DetectedSkillshots.Where(x => !x.Dodged).OrderBy(x => x.SpellData.DangerValue);

            foreach (var skillshot in skillshots)
            {
                if (skillshot.Dodged)
                {
                    continue;
                }

                //Avoid trying to evade while dashing
                if (Helper.Yasuo.IsDashing())
                {
                    return;
                }

                //Avoid dodging the skillshot if it is not set as dangerous
                if (Helper.GetBool("Evade", "Evade.OnlyDangerous") && !skillshot.SpellData.IsDangerous)
                {
                    continue;
                }

                var randDist = Helper.Yasuo.BoundingRadius + rand.Next(0, 20);
                //Avoid dodging the skillshot if there is no room/time to safely block it
                if (skillshot.Start.Distance(Helper.Yasuo.Position) < randDist)
                {
                    continue;
                }


                if (((Program.NoSolutionFound ||
                      !Program.IsSafePath(Helper.Yasuo.GetWaypoints(), 1000).IsSafe &&
                      !Program.IsSafe(Helper.Yasuo.Position.ToVector2()).IsSafe)))
                {
                    Helper.DontDash = true;
                    bool windWallable = true;
                    if (skillshot.IsAboutToHit(1000, Helper.Yasuo))
                    {
                        if (Helper.GetBool("Evade", "Evade.WFilter"))
                        {
                            windWallable =
                                skillshot.SpellData.CollisionObjects.Contains(CollisionObjectTypes.YasuoWall) &&
                                skillshot.SpellData.Type !=
                                SkillshotType.Circle;
                        }

                        if (Helper.GetBool("Evade", "Evade.UseW") && windWallable)
                        {
                            if (skillshot.Evade(SpellSlot.W)
                                && skillshot.SpellData.DangerValue >= Helper.GetSliderInt("Evade", "Evade.MinDangerLevelWW"))
                            {
                                var castpos = Helper.Yasuo.Position.Extend(skillshot.MissilePosition.ToVector3(), randDist);
                                var delay = Helper.GetSliderInt("Evade", "Evade.Delay");
                                if (Helper.TickCount - skillshot.StartTick >=
                                    skillshot.SpellData.setdelay +
                                    rand.Next(delay - 77 > 0 ? delay - 77 : 0, delay + 65))
                                {
                                    bool WCasted = Helper.Spells[Helper.W].Cast(castpos);
                                    Program.DetectedSkillshots.Remove(skillshot);
                                    skillshot.Dodged = WCasted;
                                    if (WCasted)
                                    {
                                        if (Helper.Debug)
                                        {
                                            Chat.PrintChat("Blocked " + skillshot.SpellData.SpellName +
                                                           " with Windwall ");
                                        }
                                    }
                                }
                            }
                        }

                        else if (Helper.GetBool("Evade", "Evade.UseE"))
                        {
                            if (skillshot.Evade(SpellSlot.E) && !skillshot.Dodged &&
                                skillshot.SpellData.DangerValue >= Helper.GetSliderInt("Evade", "Evade.MinDangerLevelE"))
                            {
                                var evadetarget =
                                    ObjectManager
                                        .Get<AIBaseClient>()
                                        .Where(
                                            x =>
                                                x.IsDashable() && !Helper.GetDashPos(x).PointUnderEnemyTurret() &&
                                                Program.IsSafe(x.Position.ToVector2()).IsSafe &&
                                                Program.IsSafePath(x.GeneratePathTo(), 0, 1200, 250).IsSafe)
                                        .FirstOrDefault(x => x.Distance(Helper.shop));

                                if (evadetarget != null)
                                {
                                    Helper.Spells[Helper.E].CastOnUnit(evadetarget);
                                    Program.DetectedSkillshots.Remove(skillshot);
                                    skillshot.Dodged = true;
                                    if (Helper.Debug)
                                    {
                                        Chat.PrintChat("Evading " + skillshot.SpellData.SpellName + " " + "using E to " +
                                                       evadetarget.CharacterName);
                                    }
                                }
                            }
                        }
                    }
                    Helper.DontDash = false;
                }
            }
        }



        static List<Vector2> GeneratePathTo(this AIBaseClient unit)
        {
            List<Vector2> path = new List<Vector2>();
            path.Add(Helper.Yasuo.Position.ToVector2());
            path.Add(Helper.GetDashPos(unit));
            return path;
        }

    }
}
