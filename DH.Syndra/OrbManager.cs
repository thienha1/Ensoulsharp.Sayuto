#region

using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using SharpDX;

#endregion

namespace DH.Syndra
{
    public static class OrbManager
    {
        private static uint _wobjectnetworkid = 0;

        public static uint WObjectNetworkId
        {
            get
            {
                if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 1)
                    return 0;

                return _wobjectnetworkid;
            }
            set
            {
                _wobjectnetworkid = value;
            }
        }

        public static int tmpQOrbT;
        public static Vector3 tmpQOrbPos = new Vector3();

        public static int tmpWOrbT;
        public static Vector3 tmpWOrbPos = new Vector3();

        static OrbManager()
        {
            AIBaseClient.OnPlayAnimation += AIBaseClientPlayAnimation;
            AIBaseClient.OnProcessSpellCast += AIBaseClientProcessSpellCast;
        }

        static void AIBaseClientPlayAnimation(GameObject sender, AIBaseClientPlayAnimationEventArgs args)
        {
            if (sender is AIMinionClient)
            {
                WObjectNetworkId = sender.NetworkId;
            }
        }

        private static void AIBaseClientProcessSpellCast (AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name.Equals("SyndraQ", StringComparison.InvariantCultureIgnoreCase))
            {
                tmpQOrbT = Variables.TickCount;
                tmpQOrbPos = args.To;
            }

            if (sender.IsMe && WObject(true) != null && (args.SData.Name.Equals("SyndraW", StringComparison.InvariantCultureIgnoreCase) || args.SData.Name.Equals("syndraw2", StringComparison.InvariantCultureIgnoreCase)))
            {
                tmpWOrbT = Variables.TickCount + 250;
                tmpWOrbPos = args.To;
            }
        }

        public static AIMinionClient WObject(bool onlyOrb)
        {
            if (WObjectNetworkId == 0) return null;
            var obj = ObjectManager.GetUnitByNetworkId<AIBaseClient>(WObjectNetworkId);
            if (obj != null && obj is AIMinionClient && (obj.Name == "Seed" && onlyOrb || !onlyOrb)) return (AIMinionClient)obj;
            return null;
        }

        public static List<Vector3> GetOrbs(bool toGrab = false)
        {
            var result = new List<Vector3>();
            foreach (
                var obj in
                    ObjectManager.Get<AIMinionClient>()
                        .Where(obj => obj.IsValid && obj.Team == ObjectManager.Player.Team && !obj.IsDead && obj.Name == "Seed"))
            {

                var valid = false;
                if (obj.NetworkId != WObjectNetworkId)
                    if (
                        ObjectManager.Get<GameObject>()
                            .Any(
                                b =>
                                    b.IsValid && b.Name.Contains("_Q_") && b.Name.Contains("Syndra_") &&
                                    b.Name.Contains("idle") && obj.Position.Distance(b.Position) < 50))
                        valid = true;

                if (valid && (!toGrab || !obj.IsMoving))
                    result.Add(obj.Position);
            }

            if (Variables.TickCount - tmpQOrbT < 400)
            {
                result.Add(tmpQOrbPos);
            }

            if (Variables.TickCount - tmpWOrbT < 400 && Variables.TickCount - tmpWOrbT > 0)
            {
                result.Add(tmpWOrbPos);
            }

            return result;
        }

        public static Vector3 GetOrbToGrab(int range)
        {
            var list = GetOrbs(true).Where(orb => ObjectManager.Player.Distance(orb) < range).ToList();
            return list.Count > 0 ? list[0] : new Vector3();
        }
    }
}