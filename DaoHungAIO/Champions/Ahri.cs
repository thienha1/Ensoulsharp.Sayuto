using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.Common;
using SharpDX;
using static EnsoulSharp.Common.Map;

namespace DaoHungAIO.Champions
{
    class Ahri
    {
        public static HelperAhri HelperAhri;
        private Menu _menu;

        private Items.Item _itemDFG;

        private Spell _spellQ, _spellW, _spellE, _spellR;

        const float _spellQSpeed = 2500;
        const float _spellQSpeedMin = 400;
        const float _spellQFarmSpeed = 1600;

        private static Orbwalking.Orbwalker _orbwalker;

        public Ahri()
        {

            HelperAhri = new HelperAhri();
            _menu = new Menu("DH.Ahri credit Beaving", "AhriSharp", true);

            var targetSelectorMenu = new Menu("Target Selector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _menu.AddSubMenu(targetSelectorMenu);

            _orbwalker = new Orbwalking.Orbwalker(_menu.AddSubMenu(new Menu("Orbwalking", "Orbwalking")));

            var comboMenu = _menu.AddSubMenu(new Menu("Combo", "Combo"));
            comboMenu.AddItem(new MenuItem("comboQ", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("comboW", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem("comboE", "Use E").SetValue(true));
            comboMenu.AddItem(new MenuItem("comboR", "Use R").SetValue(true));
            comboMenu.AddItem(new MenuItem("comboROnlyUserInitiate", "Use R only if user initiated").SetValue(false));

            var harassMenu = _menu.AddSubMenu(new Menu("Harass", "Harass"));
            harassMenu.AddItem(new MenuItem("harassQ", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem("harassE", "Use E").SetValue(true));
            harassMenu.AddItem(new MenuItem("harassPercent", "Skills until Mana %").SetValue(new Slider(20)));

            var farmMenu = _menu.AddSubMenu(new Menu("Lane Clear", "LaneClear"));
            farmMenu.AddItem(new MenuItem("farmQ", "Use Q").SetValue(true));
            farmMenu.AddItem(new MenuItem("farmW", "Use W").SetValue(false));
            farmMenu.AddItem(new MenuItem("farmPercent", "Skills until Mana %").SetValue(new Slider(20)));
            farmMenu.AddItem(new MenuItem("farmStartAtLevel", "Only AA until Level").SetValue(new Slider(8, 1, 18)));

            var drawMenu = _menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            drawMenu.AddItem(new MenuItem("drawQE", "Draw Q, E range").SetValue(new Circle(true, System.Drawing.Color.FromArgb(125, 0, 255, 0))));
            drawMenu.AddItem(new MenuItem("drawW", "Draw W range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(125, 0, 0, 255))));
            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw Combo Damage").SetValue(true); //copied from esk0r Syndra
            drawMenu.AddItem(dmgAfterComboItem);

            var miscMenu = _menu.AddSubMenu(new Menu("Misc", "Misc"));
            miscMenu.AddItem(new MenuItem("packetCast", "Packet Cast").SetValue(true));

            _itemDFG = GetMap().Type == MapType.TwistedTreeline ? new Items.Item(3188, 750) : new Items.Item(3128, 750);

            _spellQ = new Spell(SpellSlot.Q, 990);
            _spellW = new Spell(SpellSlot.W, 795 - 95);
            _spellE = new Spell(SpellSlot.E, 1000 - 10);
            _spellR = new Spell(SpellSlot.R, 1000 - 100);

            _spellQ.SetSkillshot(.215f, 100, 1600f, false, SkillshotType.SkillshotLine);
            _spellW.SetSkillshot(.71f, _spellW.Range, float.MaxValue, false, SkillshotType.SkillshotLine);
            _spellE.SetSkillshot(.23f, 60, 1500f, true, SkillshotType.SkillshotLine);

            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged += delegate (object sender, OnValueChangeEventArgs eventArgs) { Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>(); };

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;

        }

        void Game_OnUpdate(EventArgs args)
        {
            switch (_orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                    break;
                default:
                    break;
            }
        }

        public float GetManaPercent()
        {
            return (ObjectManager.Player.Mana / ObjectManager.Player.MaxMana) * 100f;
        }

        public bool PacketsNoLel()
        {
            return _menu.Item("packetCast").GetValue<bool>();
        }

        void Harass()
        {
            if (_menu.Item("harassE").GetValue<bool>() && GetManaPercent() >= _menu.Item("harassPercent").GetValue<Slider>().Value)
                CastE();

            if (_menu.Item("harassQ").GetValue<bool>() && GetManaPercent() >= _menu.Item("harassPercent").GetValue<Slider>().Value)
                CastQ();
        }

        void LaneClear()
        {
            _spellQ.Speed = _spellQFarmSpeed;
            var minions = MinionManager.GetMinions(ObjectManager.Player.Position, _spellQ.Range, MinionTypes.All, MinionTeam.NotAlly);

            bool jungleMobs = minions.Any(x => x.Team == GameObjectTeam.Neutral);

            if ((_menu.Item("farmQ").GetValue<bool>() && GetManaPercent() >= _menu.Item("farmPercent").GetValue<Slider>().Value && ObjectManager.Player.Level >= _menu.Item("farmStartAtLevel").GetValue<Slider>().Value) || jungleMobs)
            {
                MinionManager.FarmLocation farmLocation = _spellQ.GetLineFarmLocation(minions);

                if (farmLocation.Position.IsValid())
                    if (farmLocation.MinionsHit >= 2 || jungleMobs)
                        CastQ(farmLocation.Position);
            }

            minions = MinionManager.GetMinions(ObjectManager.Player.Position, _spellW.Range, MinionTypes.All, MinionTeam.NotAlly);

            if (minions.Count() > 0)
            {
                jungleMobs = minions.Any(x => x.Team == GameObjectTeam.Neutral);

                if ((_menu.Item("farmW").GetValue<bool>() && GetManaPercent() >= _menu.Item("farmPercent").GetValue<Slider>().Value && ObjectManager.Player.Level >= _menu.Item("farmStartAtLevel").GetValue<Slider>().Value) || jungleMobs)
                    CastW(true);
            }
        }

        void CastE()
        {
            if (!_spellE.IsReady())
                return;

            var target = TargetSelector.GetTarget(_spellE.Range, TargetSelector.DamageType.Magical);

            if (target != null)
                _spellE.CastIfHitchanceEquals(target, HitChance.High);
        }

        void CastQ()
        {
            if (!_spellQ.IsReady())
                return;

            var target = TargetSelector.GetTarget(_spellQ.Range, TargetSelector.DamageType.Magical);

            if (target != null)
            {
                Vector3 predictedPos = Prediction.GetPrediction(target, _spellQ.Delay).UnitPosition; //correct pos currently not possible with spell acceleration
                _spellQ.Speed = GetDynamicQSpeed(ObjectManager.Player.Distance(predictedPos));
                _spellQ.CastIfHitchanceEquals(target, HitChance.High);
            }
        }

        void CastQ(Vector2 pos)
        {
            if (!_spellQ.IsReady())
                return;

            _spellQ.Cast(pos);
        }

        void CastW(bool ignoreTargetCheck = false)
        {
            if (!_spellW.IsReady())
                return;

            var target = TargetSelector.GetTarget(_spellW.Range, TargetSelector.DamageType.Magical);

            if (target != null || ignoreTargetCheck)
                _spellW.CastOnUnit(ObjectManager.Player);
        }

        void Combo()
        {
            if (_menu.Item("comboE").GetValue<bool>())
                CastE();

            if (_menu.Item("comboQ").GetValue<bool>())
                CastQ();

            if (_menu.Item("comboW").GetValue<bool>())
                CastW();

            if (_menu.Item("comboR").GetValue<bool>() && _spellR.IsReady())
                if (OkToUlt())
                    _spellR.Cast(Game.CursorPosRaw);
        }

        List<SpellSlot> GetSpellCombo()
        {
            var spellCombo = new List<SpellSlot>();

            if (_spellQ.IsReady())
                spellCombo.Add(SpellSlot.Q);
            if (_spellW.IsReady())
                spellCombo.Add(SpellSlot.W);
            if (_spellE.IsReady())
                spellCombo.Add(SpellSlot.E);
            if (_spellR.IsReady())
                spellCombo.Add(SpellSlot.R);
            return spellCombo;
        }

        float GetComboDamage(AIBaseClient target)
        {
            double comboDamage = (float)ObjectManager.Player.GetComboDamage(target, GetSpellCombo());

            return (float)(comboDamage + ObjectManager.Player.GetAutoAttackDamage(target));
        }

        bool OkToUlt()
        {
            if (Ahri.HelperAhri.EnemyTeam.Any(x => x.Distance(ObjectManager.Player) < 500)) //any enemies around me?
                return true;

            Vector3 mousePos = Game.CursorPosRaw;

            var enemiesNearMouse = Ahri.HelperAhri.EnemyTeam.Where(x => x.Distance(ObjectManager.Player) < _spellR.Range && x.Distance(mousePos) < 650);

            if (enemiesNearMouse.Count() > 0)
            {
                if (IsRActive()) //R already active
                    return true;

                bool enoughMana = ObjectManager.Player.Mana > ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost + ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).ManaCost + ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).ManaCost;

                if (_menu.Item("comboROnlyUserInitiate").GetValue<bool>() || !(_spellQ.IsReady() && _spellE.IsReady()) || !enoughMana) //dont initiate if user doesnt want to, also dont initiate if Q and E isnt ready or not enough mana for QER combo
                    return false;

                var friendsNearMouse = Ahri.HelperAhri.OwnTeam.Where(x => x.IsMe || x.Distance(mousePos) < 650); //me and friends near mouse (already in fight)

                if (enemiesNearMouse.Count() == 1) //x vs 1 enemy
                {
                    AIHeroClient enemy = enemiesNearMouse.FirstOrDefault();

                    bool underTower = Utility.UnderTurret(enemy);

                    return GetComboDamage(enemy) / enemy.Health >= (underTower ? 1.25f : 1); //if enemy under tower, only initiate if combo damage is >125% of enemy health
                }
                else //fight if enemies low health or 2 friends vs 3 enemies and 3 friends vs 3 enemies, but not 2vs4
                {
                    int lowHealthEnemies = enemiesNearMouse.Count(x => x.Health / x.MaxHealth <= 0.1); //dont count low health enemies

                    float totalEnemyHealth = enemiesNearMouse.Sum(x => x.Health);

                    return friendsNearMouse.Count() - (enemiesNearMouse.Count() - lowHealthEnemies) >= -1 || ObjectManager.Player.Health / totalEnemyHealth >= 0.8;
                }
            }

            return false;
        }

        void Drawing_OnDraw(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                var drawQE = _menu.Item("drawQE").GetValue<Circle>();
                var drawW = _menu.Item("drawW").GetValue<Circle>();

                if (drawQE.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _spellQ.Range, drawQE.Color);

                if (drawW.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _spellW.Range, drawW.Color);
            }
        }

        float GetDynamicQSpeed(float distance)
        {
            float accelerationrate = _spellQ.Range / (_spellQSpeedMin - _spellQSpeed); // = -0.476...
            return _spellQSpeed + accelerationrate * distance;
        }

        bool IsRActive()
        {
            return ObjectManager.Player.HasBuff("AhriTumble");
        }

        int GetRStacks()
        {
            BuffInstance tumble = ObjectManager.Player.Buffs.FirstOrDefault(x => x.Name == "AhriTumble");
            return tumble != null ? tumble.Count : 0;
        }
    }
    internal class EnemyInfo
    {
        public AIHeroClient Player;
        public int LastSeen;

        public EnemyInfo(AIHeroClient player)
        {
            Player = player;
        }
    }

    internal class HelperAhri
    {
        public IEnumerable<AIHeroClient> EnemyTeam;
        public IEnumerable<AIHeroClient> OwnTeam;
        public List<EnemyInfo> EnemyInfo = new List<EnemyInfo>();

        public HelperAhri()
        {
            var champions = ObjectManager.Get<AIHeroClient>().ToList();

            OwnTeam = champions.Where(x => x.IsAlly);
            EnemyTeam = champions.Where(x => x.IsEnemy);

            EnemyInfo = EnemyTeam.Select(x => new EnemyInfo(x)).ToList();

            Game.OnUpdate += Game_OnUpdate;
        }

        void Game_OnUpdate(EventArgs args)
        {
            var time = Environment.TickCount;

            foreach (EnemyInfo enemyInfo in EnemyInfo.Where(x => x.Player.IsVisible))
                enemyInfo.LastSeen = time;
        }

        public EnemyInfo GetPlayerInfo(AIHeroClient enemy)
        {
            return Ahri.HelperAhri.EnemyInfo.Find(x => x.Player.NetworkId == enemy.NetworkId);
        }

        public float GetTargetHealth(EnemyInfo playerInfo, int additionalTime)
        {
            if (playerInfo.Player.IsVisible)
                return playerInfo.Player.Health;

            var predictedhealth = playerInfo.Player.Health + playerInfo.Player.HPRegenRate * ((Environment.TickCount - playerInfo.LastSeen + additionalTime) / 1000f);

            return predictedhealth > playerInfo.Player.MaxHealth ? playerInfo.Player.MaxHealth : predictedhealth;
        }

        public static T GetSafeMenuItem<T>(MenuItem item)
        {
            if (item != null)
                return item.GetValue<T>();

            return default(T);
        }
    }
}
