using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DaoHungAIO.Champions;
using DaoHungAIO.Helpers;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using Activator = DaoHungAIO.Plugins.Activator;

namespace DaoHungAIO
{
    internal class Program
    {
        public static Menu Config;
        public static AIHeroClient player;
        public static string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        //public static IncomingDamage IncDamages;
        public static Menu SPredictionMenu;

        public static int HitChanceNum = 4, tickNum = 4, tickIndex = 0;

        public static bool LaneClear = false, None = false, Farm = false, Combo = false;
        public static bool IsSPrediction
        {
            get { return SPredictionMenu.GetValue<MenuList>("PREDICTONLIST").SelectedValue == "SPrediction"; }
        }

        public static object Player { get; internal set; }

        private static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnGameLoad;
        }

        private static int timeFuck = 0;

        public static bool LagFree(int offset)
        {
            if (tickIndex == offset)
                return true;
            else
                return false;
        }
        private static void OnGameLoad()
        {
            try
            {
                player = ObjectManager.Player;
                //pred = new Menu("spred", "Prediction settings");
                //SPrediction.Prediction.Initialize(pred);
                SPredictionMenu = SPrediction.Prediction.Initialize(); //new Menu("SPREDX", "SPrediction");
                //SPredictionMenu.Attach();
                //set default to common prediction
                //var type = Type.GetType("DaoHungAIO.Champions." + player.CharacterName);
                //Chat.Print("Loading1");
                //if (type != null)
                //{
                //    Chat.Print("Loading");
                //    Helpers.DynamicInitializer.NewInstance(type);
                //}
                //else
                //{
                //    Chat.Print("Loading2");
                //    var common = Type.GetType("DaoHungAIO.Champions." + "Other");
                //    if (common != null)
                //    {
                //        Chat.Print("Loading3");
                //        Helpers.DynamicInitializer.NewInstance(common);
                //    }
                //}
                //IncDamages = new IncomingDamage();
                new Activator();
                Game.OnUpdate += DelayTime;
                //Game.OnUpdate += TrashTalk;
                switch (player.CharacterName)
                {
                    case "Azir":
                        new Azir();
                        break;
                    case "Fizz":
                        new Fizz();
                        break;
                    case "Garen":
                        new Garen();
                        break;
                    case "Jax":
                        new Jax();
                        break;
                    case "Jayce":
                        new Jayce();
                        break;
                    case "Jhin":
                        new Jhin();
                        break;
                    case "Kennen":
                        new Kennen();
                        break;
                    case "Khazix":
                        new Khazix();
                        break;
                    case "KogMaw":
                        new KogMaw();
                        break;
                    case "Lucian":
                        new Lucian();
                        break;
                    case "Nidalee":
                        new Nidalee();
                        break;
                    case "Orianna":
                        Orianna.initOrianna();
                        break;
                    case "Riven":
                        new Riven();
                        break;
                    case "Renekton":
                        new Renekton();
                        break;
                    case "Ryze":
                        new Ryze();
                        break;
                    case "Syndra":
                        new Syndra();
                        break;
                    case "Tristana":
                        new Tristana();
                        break;
                    case "Varus":
                        new Varus();
                        break;
                    case "Velkoz":
                        new Velkoz();
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed To load: " + e);
            }
        }

        private static void DelayTime(EventArgs args)
        {

            Combo = Orbwalker.ActiveMode == OrbwalkerMode.Combo;
            Farm = (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && Config.GetValue<MenuBool>("harassLaneclear")) || Orbwalker.ActiveMode == OrbwalkerMode.Harass;
            None = Orbwalker.ActiveMode == OrbwalkerMode.None;
            LaneClear = Orbwalker.ActiveMode == OrbwalkerMode.LaneClear;
            tickIndex++;
            if (tickIndex > 4)
                tickIndex = 0;
        }
        private static void TrashTalk(EventArgs args)
        {
            if((int)Game.Time - timeFuck > 10)
            {
                var random = new Random();
                var color = String.Format("#{0:X6}", random.Next(0x1000000));
                Chat.Print("<font color=\"" + color + "\"><b>Dont worry if you not</b>, Fuck you Wwaper, Kastobar, remove my scripts, self-respect</font>");
                timeFuck = (int)Game.Time;
            }
        }
    }

}