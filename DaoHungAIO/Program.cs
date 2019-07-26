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

namespace DaoHungAIO
{
    internal class Program
    {
        public static AIHeroClient player;
        public static string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        //public static IncomingDamage IncDamages;
        public static Menu SPredictionMenu;

        public static bool IsSPrediction
        {
            get { return SPredictionMenu["SPREDX"].GetValue<MenuList>("PREDICTONLIST").SelectedValue == "SPrediction"; }
        }

        private static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad()
        {
            try
            {
                player = ObjectManager.Player;
                SPredictionMenu = SPrediction.Prediction.Initialize(); //new Menu("SPREDX", "SPrediction");
                SPredictionMenu.Add(new MenuList("PREDICTONLIST2", "PREDICTONLIST2", new[] { "SPrediction", "Common Predicion" }, 1));
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
                switch (player.CharacterName)
                {
                    case "Jax":
                        new Jax();
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed To load: " + e);
            }
        }
    }

}