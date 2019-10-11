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
                Chat.Print("<font color=\"#05FAAC\"><b>XDreamms is just a kid stealing, disrespecting the source owner</b></font>");
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
                
                Game.OnTick += DelayTime;

                //AIBaseClient.OnDoCast += OnProcessSpell;
                //Game.OnUpdate += TrashTalk;
                Chat.Print(player.CharacterName);
                switch (player.CharacterName)
                {
                    case "Ahri":
                        new Ahri();
                        break;
                    case "Camille":
                        new Camille();
                        break;
                    //case "Diana":
                    //    new Diana();
                    //    break;
                    case "Draven":
                        new Draven();
                        break;
                    case "Ekko":
                        new Ekko();
                        break;
                    case "Fiora":
                        new Fiora();
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
                    case "Malphite":
                        new Malphite();
                        break;
                    case "Nidalee":
                        new Nidalee();
                        break;
                    case "Olaf":
                        new Olaf();
                        break;
                    case "Orianna":
                        Orianna.initOrianna();
                        break;
                    case "Riven":
                        new RivenReborn();
                        break;
                    //case "Rengar":
                    //    new Rengar();
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
                    case "Viktor":
                        new Viktor();
                        break;
                    case "Yasuo":
                        new Yasuo();
                        break;
                    case "Zed":
                        new Zed();
                        break;
                    case "Ziggs":
                        new Ziggs();
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed To load: " + e);
            }
        }

        private static void OnProcessSpell(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if(sender.IsMe)
            Chat.Print(args.SData.Name);
        }

        private static void DelayTime(EventArgs args)
        {

            Combo = Orbwalker.ActiveMode == OrbwalkerMode.Combo;
            Farm = (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear) || Orbwalker.ActiveMode == OrbwalkerMode.Harass;
            None = Orbwalker.ActiveMode == OrbwalkerMode.None;
            LaneClear = Orbwalker.ActiveMode == OrbwalkerMode.LaneClear;
            tickIndex++;
            if (tickIndex > 4)
                tickIndex = 0;
        }
        private static void TrashTalk(EventArgs args)
        {
            GameObjects.Player.Buffs.ForEach(buff =>
            {
                Chat.Print(buff.Name);
            });
        }
    }

}