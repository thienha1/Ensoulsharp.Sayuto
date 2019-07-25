using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EnsoulSharp.SDK.Prediction.SpellPrediction;

namespace DH.Yasuo
{
    class Helper
    {
        internal static AIHeroClient Yasuo;

        internal float QRadius = 150;

        internal static ShopClient shop;

        internal static bool DontDash = false;

        internal static int Q = 1, Q2 = 2, W = 3, E = 4, R = 5, Ignite = 6, Flash = 7;

        internal const float LaneClearWaitTimeMod = 2f;

        internal static float WCLastE = 0f;

        internal static Items.Item Hydra, Titanic, Tiamat, Blade, Bilgewater, Youmu;

        internal float LastTornadoClearTick;

        internal float LastDashTick;

        internal float FlashRange = 425f;

        public static int TickCount
        {
            get { return (int)(Game.Time * 1000f); }
        }

       
        internal bool InDash
        {
            get { return TickCount - LastDashTick < 500; }
        }

        /* Credits to Brian for Q Skillshot values */
        internal static Dictionary<int, Spell> Spells;


        internal void InitSpells()
        {
            Spells = new Dictionary<int, Spell> {
            { 1, new Spell(SpellSlot.Q, 450f) },
            { 2, new Spell(SpellSlot.Q, 1100f) },
            { 3, new Spell(SpellSlot.W, 450f) },
            { 4, new Spell(SpellSlot.E, 475f) },
            { 5, new Spell(SpellSlot.R, 1250f) },
            { 6, new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 600) },
            { 7, new Spell(ObjectManager.Player.GetSpellSlot("summonerflash"), 425) }
            };

            Spells[Q].SetSkillshot(GetQ1Delay, 20f, float.MaxValue, false, SkillshotType.Line);

            Spells[Q2].SetSkillshot(GetQ2Delay, 90, 1500, false, SkillshotType.Line);
            Spells[E].SetTargetted(0.075f, 1025);
        }

        private static float GetQDelay { get { return 1 - Math.Min((Yasuo.AttackSpeedMod - 1) * 0.0058552631578947f, 0.6675f); } }

        private static float GetQ1Delay { get { return 0.4f * GetQDelay; } }

        private static float GetQ2Delay { get { return 0.5f * GetQDelay; } }


        internal float Qrange
        {
            get { return TornadoReady ? Spells[Q2].Range : Spells[Q].Range; }
        }

        internal float Qdelay
        {
            get
            {
                return 0.250f - (Math.Min(BonusAttackSpeed, 0.66f) * 0.250f);
            }
        }


        internal float BonusAttackSpeed
        {
            get
            {
                return (1 / Yasuo.AttackDelay) - 0.658f;
            }
        }

        internal float Erange
        {
            get { return Spells[E].Range; }
        }

        internal float Rrange
        {
            get { return Spells[R].Range; }
        }

        internal bool TornadoReady
        {
            get { return Yasuo.HasBuff("yasuoq3w"); }
        }

        internal static int DashCount
        {
            get
            {
                var bc = Yasuo.GetBuffCount("yasuodashscalar");
                return bc;
            }
        }

        internal float QLeftPCT
        {
            get
            {
                var buff = Yasuo.GetBuff("yasuoq3w");
                if (buff != null)
                {
                    var timeLeft = buff.EndTime - Game.Time;
                    var totalDuration = buff.EndTime - buff.StartTime;
                    var pctRemain = timeLeft / totalDuration;
                    return pctRemain;

                }
                return 0;
            }
        }



        internal bool ShouldNormalQ(AIHeroClient target)
        {
            var pos = GetDashPos(target);
            return QLeftPCT <= 30 || !TowerCheck(pos, true) || !target.IsDashable(Spells[E].Range * 3) || !targInKnockupRadius(pos.ToVector3());
        }


        internal bool UseQ(AIHeroClient target, HitChance minhc = HitChance.Medium, bool UseQ1 = true, bool UseQ2 = true)
        {
            if (target == null)
            {
                return false;
            }

            if (Yasuo.IsDashing() || InDash)
            {
                return false;
            }

            var tready = TornadoReady;

            if ((tready && !UseQ2) || !tready && !UseQ1)
            {
                return false;
            }

            if (GetMode() == Modes.Beta && !ShouldNormalQ(target))
            {
                if (tready && GetBool("Combo", "Combo.UseEQ"))
                {
                    if (Spells[E].IsReady() && target.IsDashable(500))
                    {
                        var dashPos = GetDashPos(target);
                        if (dashPos.ToVector3().CountEnemyHeroesInRange(QRadius) >= 1)
                        {
                            //Cast E to trigger EQ 
                            if (GetBool("Misc", "Misc.saveQ4QE") && isHealthy && GetBool("Combo", "Combo.UseE") &&
                                (GetBool("Combo", "Combo.ETower") || GetKeyBind("Misc", "Misc.TowerDive") ||
                                 !GetDashPos(target).IsUnderEnemyTurret()))
                            {
                                return Spells[E].CastOnUnit(target);
                            }
                        }
                    }
                }
            }

            else
            {

                Spell sp = tready ? Spells[Q2] : Spells[Q];
                PredictionOutput pred = sp.GetPrediction(target);

                if (pred.Hitchance >= minhc)
                {
                    return sp.Cast(pred.CastPosition);
                }
            }

            return false;
        }

        internal IEnumerable<AIHeroClient> KnockedUp
        {
            get
            {
                List<AIHeroClient> KnockedUpEnemies = new List<AIHeroClient>();
                foreach (var hero in GameObjects.EnemyHeroes)
                {
                    if (hero.IsValidEnemy(Spells[R].Range))
                    {
                        var knockup = hero.Buffs.Find(x => (x.Type == BuffType.Knockup && (x.EndTime - Game.Time) <= (GetSliderFloat("Combo", "Combo.knockupremainingpct") / 100) * (x.EndTime - x.StartTime)) || x.Type == BuffType.Knockback);
                        if (knockup != null)
                        {
                            KnockedUpEnemies.Add(hero);
                        }
                    }
                }
                return KnockedUpEnemies;
            }
        }

        internal static bool isHealthy
        {
            get { return Yasuo.IsInvulnerable || Yasuo.HasBuffOfType(BuffType.Invulnerability) || Yasuo.HasBuffOfType(BuffType.SpellShield) || Yasuo.HasBuffOfType(BuffType.SpellImmunity) || Yasuo.HealthPercent > GetSliderFloat("Misc", "Misc.Healthy") || Yasuo.HasBuff("yasuopassivemovementshield") && Yasuo.HealthPercent > 30; }
        }

        internal static bool GetBool(string parent, string name)
        {
            return YasuoMenu.Config[parent].GetValue<MenuBool>(name);
        }

        internal static bool GetKeyBind(string parent, string name)
        {
            return YasuoMenu.Config[parent].GetValue<MenuKeyBind>(name).Active;
        }

        internal static int GetSliderInt(string parent, string name)
        {
            return YasuoMenu.Config[parent].GetValue<MenuSlider>(name).Value;
        }

        internal static float GetSliderFloat(string parent, string name)
        {
            return YasuoMenu.Config[parent].GetValue<MenuSlider>(name).Value;
        }


        internal static string GetSL(string parent, string name)
        {
            return YasuoMenu.Config[parent].GetValue<MenuList>(name).SelectedValue;
        }

        //internal static Circle GetCircle(string name)
        //{
        //    return YasuoMenu.Config.Item(name).GetValue<Circle>();
        //}

        internal static Vector2 DashPosition;

        internal static Vector2 GetDashPos(AIBaseClient @base)
        {
            var predictedposition = Yasuo.Position.Extend(@base.Position, Yasuo.Distance(@base) + 475 - Yasuo.Distance(@base)).ToVector2();
            DashPosition = predictedposition;
            return predictedposition;
        }

        internal static Vector2 GetDashPosFrom(Vector2 startPos, AIBaseClient @base)
        {
            var startPos3D = startPos.ToVector3();
            var predictedposition = startPos3D.Extend(@base.Position, startPos.Distance(@base) + 475 - startPos.Distance(@base)).ToVector2();
            DashPosition = predictedposition;
            return predictedposition;
        }

        internal static double GetProperEDamage(AIBaseClient target)
        {
            double dmg = Yasuo.GetSpellDamage(target, SpellSlot.E);
            float amplifier = 0;
            if (DashCount == 0)
            {
                amplifier = 0;
            }
            else if (DashCount == 1)
            {
                amplifier = 0.25f;
            }
            else if (DashCount == 2)
            {
                amplifier = 0.50f;
            }
            dmg += dmg * amplifier;
            return dmg;
        }

        internal static bool Debug
        {
            get { return GetBool("Misc", "Misc.Debug"); }
        }

        internal static HitChance GetHitChance(String search)
        {
            var hitchance = YasuoMenu.Config["Misc"].GetValue<MenuList>(search).SelectedValue; // "Low", "Medium", "High", "VeryHigh"
            switch (hitchance)
            {
                case "Low":
                    return HitChance.Low;
                case "Medium":
                    return HitChance.Medium;
                case "High":
                    return HitChance.High;
                case "VeryHigh":
                    return HitChance.VeryHigh;
            }
            return HitChance.Medium;
        }


        internal FleeType FleeMode
        {
            get
            {
                var GetFM = GetSL("Flee", "Flee.Mode"); //"To Nexus", "To Allies", "To Cursor"
                if (GetFM == "To Nexus")
                {
                    return FleeType.ToNexus;
                }
                if (GetFM == "To Allies")
                {
                    return FleeType.ToAllies;
                }
                return FleeType.ToCursor;
            }
        }

        internal enum FleeType
        {
            ToNexus,
            ToAllies,
            ToCursor,
        }

        internal enum UltMode
        {
            Health,
            Priority,
            EnemiesHit
        }

        internal UltMode GetUltMode()
        {
            switch (GetSL("Combo", "Combo.UltMode")) //"Lowest Health", "TS Priority", "Most enemies"
            {
                case "Lowest Health":
                    return UltMode.Health;
                case "TS Priority":
                    return UltMode.Priority;
                case "Most enemies":
                    return UltMode.EnemiesHit;
            }
            return UltMode.Priority;
        }

        internal void InitItems()
        {
            Hydra = new Items.Item(ItemId.Ravenous_Hydra, 350f);
            Tiamat = new Items.Item(ItemId.Tiamat, 350f);
            Titanic = new Items.Item(ItemId.Titanic_Hydra, 700f);
            Blade = new Items.Item(ItemId.Blade_of_the_Ruined_King, 550f);
            Bilgewater = new Items.Item(ItemId.Bilgewater_Cutlass, 550f);
            Youmu = new Items.Item(ItemId.Youmuus_Ghostblade, 0f);
        }

        internal bool SafetyCheck(AIBaseClient unit, bool isCombo = false)
        {
            var pos = GetDashPos(unit);
            return ((isCombo && Helper.GetBool("Combo", "Combo.ETower") && (!GetBool("Combo", "Combo.AvoidDanger") || SpellBlocking.EvadeManager.IsSafe(pos).IsSafe)) || Helper.GetKeyBind("Misc", "Misc.TowerDive") ||
                   !pos.IsUnderEnemyTurret());
        }

        internal bool ShouldDive(AIBaseClient unit)
        {
            return Helper.GetKeyBind("Misc", "Misc.TowerDive") || !Helper.GetDashPos(unit).IsUnderEnemyTurret();
        }

        internal bool TowerCheck(Vector2 pos, bool isCombo = false)
        {
            return (isCombo && Helper.GetBool("Combo", "Combo.ETower") || Helper.GetKeyBind("Misc", "Misc.TowerDive") ||
                    pos.IsUnderEnemyTurret());
        }

        internal bool targInKnockupRadius(AIHeroClient targ)
        {
            var dpos = GetDashPos(targ).ToVector3();
            return dpos.CountEnemyHeroesInRange(QRadius) > 0;
        }

        internal bool targInKnockupRadius(Vector3 dpos)
        {
            return dpos.CountEnemyHeroesInRange(QRadius) > 0;
        }

        internal Modes GetMode()
        {
            var mode = Helper.GetSL("Combo", "Combo.Mode"); //"Old", "Beta"
            if (mode == "Old")
            {
                return Modes.Old;
            }
            else
            {
                return Modes.Beta;
            }
        }


        internal enum Modes
        {
            Old,
            Beta
        }

    }
}
