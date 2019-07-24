using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DH.Yasuo
{
    class Program
    {

        internal static Yasuo Yasuo;


        #region
        private static readonly MenuBool Qcombo = new MenuBool("qcombo", "[Q] on Combo");
        private static readonly MenuBool Wcombo = new MenuBool("wcombo", "[W] on Combo");
        private static readonly MenuBool Wsave = new MenuBool("wcombo", "^ After Q");
        private static readonly MenuBool Ecombo = new MenuBool("Ecombo", "[E] on Combo");
        private static readonly MenuBool Eminions = new MenuBool("Eminions", "^ on Combo if Out Range");
        private static readonly MenuBool Rcombo = new MenuBool("Rcombo", "[R] on Combo");
        private static readonly MenuSlider Rcount = new MenuSlider("Rcount", "^ when hit X enemies", 1, 1, 5);

        private static readonly MenuBool Qharass = new MenuBool("qharass", "[Q] on Harass");
        private static readonly MenuBool Eharass = new MenuBool("Eharass", "[E] on Harass");

        private static readonly MenuBool Qclear = new MenuBool("qclear", "[Q] on ClearWave");

        #endregion


        static void Main(string[] args)
        {
            Yasuo = new Yasuo();
        }

    }
}
