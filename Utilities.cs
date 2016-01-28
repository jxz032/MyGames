using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace MultiThreading
{
    public static class Utilities
    {
        public enum ChestColor
        {
            Red,
            Blue
        }

        public static string GetChestColor(List<Chest> completeStore, Chest chest)
        {
            var testChest = completeStore.FirstOrDefault(c => c.LocationX == chest.LocationX && c.LocationY == chest.LocationY);

            if (testChest != null)
            {
                return testChest.Color;
            }

            return null;
        }

        public static bool BranchHasLevel3Chest(List<Chest> completeStore, ChestBranch b1, ChestBranch b2)
        {
            string b1Color = b1.ValidBranch(completeStore);
            string b2Color = b2.ValidBranch(completeStore);

            if (b1Color == b2Color && b1Color != "false")
            {
                return true;
            }
            return false;
        }

        public static string GetTimeString(this Stopwatch stopwatch, int numberofDigits = 1)
        {
            double time = stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;
            if (time > 1)
                return Math.Round(time, numberofDigits) + " s";
            if (time > 1e-3)
                return Math.Round(1e3 * time, numberofDigits) + " ms";
            if (time > 1e-6)
                return Math.Round(1e6 * time, numberofDigits) + " µs";
            if (time > 1e-9)
                return Math.Round(1e9 * time, numberofDigits) + " ns";
            return stopwatch.ElapsedTicks + " ticks";
        }

        public static bool CheckPositionAvailable(List<Chest> chestStore, int x, int y)
        {
            bool isWithinCanvas = x >= 0 && x <= 10 && y >= 0 && y <= 10;
            return isWithinCanvas && !chestStore.Any(c => c.LocationX == x && c.LocationY == y);
        }

        public static ChestBranch GetChestTriange(List<Chest> chestsStore, Chest centerChest)
        {
            // '3 1
            //  0   2

            Chest chest1, chest2;

            IEnumerable<Chest> chest1Opts =
                chestsStore.Where(
                    c =>
                        Math.Abs(c.LocationX - centerChest.LocationX) == 1 &&
                        Math.Abs(c.LocationY - centerChest.LocationY) == 1);

            IEnumerable<Chest> chest2Opts =
                chestsStore.Where(
                    c =>
                        Math.Pow(c.LocationX - centerChest.LocationX, 2) +
                        Math.Pow(c.LocationY - centerChest.LocationY, 2) == 4);

            foreach (Chest chest1Opt in chest1Opts)
            {
                Chest tempChest = chest2Opts.FirstOrDefault(c => Math.Abs(c.LocationX - chest1Opt.LocationX) == 1 &&
                                                                 Math.Abs(c.LocationY - chest1Opt.LocationY) == 1);

                if (tempChest != null)
                {
                    chest1 = chest1Opt;
                    chest2 = tempChest;
                    return new ChestBranch { Chest0 = centerChest, Chest1 = chest1, Chest2 = chest2 };
                }
            }
            return null;
        }

        public static Chest FindDangerTriangleLevel1(List<Chest> chestsStore, List<Chest> completeStore, Chest centerChest)
        {
            // '3 1
            //  0   2
            ChestBranch chestTri = GetChestTriange(chestsStore, centerChest);

            if (chestTri != null)
            {
                Chest chest3;
                Chest dangerChest = new Chest { HighRecommand = true };

                bool isHorizon = centerChest.LocationY == chestTri.Chest2.LocationY;

                IEnumerable<Chest> chest3Opts = isHorizon
                    ? chestsStore.Where(
                        c =>
                            (c.LocationX == centerChest.LocationX &&
                             Math.Abs(c.LocationY - centerChest.LocationY) == 1) ||
                            (c.LocationX == chestTri.Chest2.LocationX && Math.Abs(c.LocationY - chestTri.Chest2.LocationY) == 1))
                            :
                            chestsStore.Where(c => (c.LocationY == centerChest.LocationY &&
                             Math.Abs(c.LocationX - centerChest.LocationX) == 1) ||
                            (c.LocationY == chestTri.Chest2.LocationY && Math.Abs(c.LocationX - chestTri.Chest2.LocationX) == 1))
                            ;


                int direction21X = chestTri.Chest1.LocationX - chestTri.Chest2.LocationX;
                int direction21Y = chestTri.Chest1.LocationY - chestTri.Chest2.LocationY;

                dangerChest.LocationX = chestTri.Chest1.LocationX + direction21X;
                dangerChest.LocationY = chestTri.Chest1.LocationY + direction21Y;

                chest3 = chest3Opts.FirstOrDefault(
                        c =>
                            c.LocationX == centerChest.LocationX &&
                            Math.Abs(c.LocationY - centerChest.LocationY) == 1);

                if (chest3 != null && Utilities.CheckPositionAvailable(completeStore, dangerChest.LocationX, dangerChest.LocationY))
                {
                    return dangerChest;
                }

                int direction01X = chestTri.Chest1.LocationX - centerChest.LocationX;
                int direction01Y = chestTri.Chest1.LocationY - centerChest.LocationY;

                dangerChest.LocationX = chestTri.Chest1.LocationX + direction01X;
                dangerChest.LocationY = chestTri.Chest1.LocationY + direction01Y;

                chest3 = chest3Opts.FirstOrDefault(
                    c =>
                        c.LocationX == chestTri.Chest2.LocationX &&
                        Math.Abs(c.LocationY - chestTri.Chest2.LocationY) == 1);

                if (chest3 != null && CheckPositionAvailable(completeStore, dangerChest.LocationX, dangerChest.LocationY))
                    return dangerChest;

            }
            return null;
        }

        public static Chest FindDangerTriangleLevel2(List<Chest> chestsStore, List<Chest> completeStore, Chest centerChest)
        {
            //    1
            //  0   2
            //    3

            ChestBranch chestTri = GetChestTriange(chestsStore, centerChest);

            if (chestTri != null)
            {
                Chest chest3;
                Chest dangerChest = new Chest { HighRecommand = true };

                bool isHorizon = centerChest.LocationY == chestTri.Chest2.LocationY;

                dangerChest.LocationX = isHorizon ? chestTri.Chest1.LocationX : chestTri.Chest0.LocationX;
                dangerChest.LocationY = isHorizon ? chestTri.Chest0.LocationY : chestTri.Chest1.LocationY;

                List<Chest> temp = new List<Chest>();
                temp.Add(chestTri.Chest1);

                chest3 = chestsStore.Where(
                    c =>
                        Math.Abs(c.LocationX - centerChest.LocationX) == 1 &&
                        Math.Abs(c.LocationY - centerChest.LocationY) == 1).Except(temp).FirstOrDefault();

                if (chest3 != null && Utilities.CheckPositionAvailable(completeStore, dangerChest.LocationX, dangerChest.LocationY))
                {
                    return dangerChest;
                }
            }
            return null;
        }

        /// <summary>
        /// Chest if the chest is considered as dangerous chest
        /// </summary>
        /// <param name="chestStore"></param>
        /// <param name="completeStore"></param>
        /// <param name="centerChest"></param>
        /// <returns>If the input chest is dangerous, return it. Otherwise return null</returns>
        public static Chest FindDangerTriangleLevel3(List<Chest> chestStore, List<Chest> completeStore, Chest centerChest)
        {
            //        
            //        
            //        o
            //        o
            //2 o o x x x o o 0
            //        o
            //        o
            //        3
            ChestBranch branch0 = new ChestBranch(completeStore, new Chest(centerChest.LocationX + 1, centerChest.LocationY), new Chest(centerChest.LocationX + 2, centerChest.LocationY), new Chest(centerChest.LocationX + 3, centerChest.LocationY));
            ChestBranch branch1 = new ChestBranch(completeStore, new Chest(centerChest.LocationX, centerChest.LocationY - 1), new Chest(centerChest.LocationX, centerChest.LocationY - 2), new Chest(centerChest.LocationX, centerChest.LocationY - 3));
            ChestBranch branch2 = new ChestBranch(completeStore, new Chest(centerChest.LocationX - 1, centerChest.LocationY), new Chest(centerChest.LocationX - 2, centerChest.LocationY), new Chest(centerChest.LocationX - 3, centerChest.LocationY));
            ChestBranch branch3 = new ChestBranch(completeStore, new Chest(centerChest.LocationX, centerChest.LocationY + 1), new Chest(centerChest.LocationX, centerChest.LocationY + 2), new Chest(centerChest.LocationX, centerChest.LocationY + 3));

            branch0.ValidBranch(completeStore);
            branch1.ValidBranch(completeStore);
            branch2.ValidBranch(completeStore);
            branch3.ValidBranch(completeStore);

            List<ChestBranch> branches = new List<ChestBranch> { branch0, branch1, branch2, branch3 };
            branches = branches.Where(b => b.BranchValid).ToList();

            for (int i = 0; i < branches.Count; i++)
            {
                for (int j = i + 1; j < branches.Count; j++)
                {
                    bool gotIt = BranchHasLevel3Chest(completeStore, branches[i], branches[j]);

                    if (gotIt)
                    {
                        return centerChest;
                    }
                }
            }

            return null;
        }

    }

    public class ListItem
    {
        public string Text { get; set; }
        public int Value { get; set; }

        public ListItem(string text, int value)
        {
            Text = text;
            Value = value;
        }

        public override string ToString()
        {
            // Generates the text shown in the combo box
            return Text;
        }
    }

    public class Chest
    {
        public Chest()
        {
            HighRecommand = false;
            IsAvailable = true;
        }

        public Chest(int x, int y)
        {
            LocationX = x;
            LocationY = y;
        }

        public string Color { get; set; }
        public bool IsAvailable { get; set; }
        public int LocationX { get; set; }
        public int LocationY { get; set; }
        public bool HighRecommand { get; set; }

    }

    public class ChestBranch
    {
        public ChestBranch(List<Chest> completeStore, Chest chest0, Chest chest1, Chest chest2)
        {
            chest0.Color = Utilities.GetChestColor(completeStore, chest0);
            chest1.Color = Utilities.GetChestColor(completeStore, chest1);
            chest2.Color = Utilities.GetChestColor(completeStore, chest2);
            Chest0 = chest0;
            Chest1 = chest1;
            Chest2 = chest2;
        }

        public ChestBranch()
        {
            BranchValid = false;
        }
        public Chest Chest0 { get; set; }
        public Chest Chest1 { get; set; }
        public Chest Chest2 { get; set; }
        public bool BranchValid { get; set; }
        public string DetectBranchColor()
        {
            if ((Chest0.Color == Utilities.ChestColor.Blue.ToString() || Chest0.Color == null) &&
                (Chest1.Color == Utilities.ChestColor.Blue.ToString() || Chest1.Color == null) &&
                (Chest2.Color == Utilities.ChestColor.Blue.ToString() || Chest2.Color == null) &&
                !(Chest0.Color == null && Chest1.Color == null && Chest2.Color == null)
                )
            {
                return Utilities.ChestColor.Blue.ToString();
            }
            else if
               ((Chest0.Color == Utilities.ChestColor.Red.ToString() || Chest0.Color == null) &&
              (Chest1.Color == Utilities.ChestColor.Red.ToString() || Chest1.Color == null) &&
              (Chest2.Color == Utilities.ChestColor.Red.ToString() || Chest2.Color == null) &&
              !(Chest0.Color == null && Chest1.Color == null && Chest2.Color == null)
                )
            {
                return Utilities.ChestColor.Red.ToString();
            }
            return "false";
        }

        public string ValidBranch(List<Chest> completeStore)
        {
            bool chest1Avail = Chest1.IsAvailable;
            string branchColor = DetectBranchColor();
            bool isSameColor = branchColor != "false";

            List<Chest> chests = new List<Chest> { Chest0, Chest1, Chest2 };
            int count = 0;

            foreach (var chest in chests)
            {
                if (chest.Color != null)
                {
                    count++;
                }
            }

            if (!chest1Avail && isSameColor && count >= 2)
            {
                BranchValid = true;
                return branchColor;
            }

            return "false";
        }
    }

    public class ChestAngle
    {
        public ChestAngle()
        {

        }

        public ChestAngle(Chest chest0, Chest chest1, Chest chest2, Chest chest3, Chest chestOpt1, Chest chestOpt2)
        {
            Chest0 = chest0;
            Chest1 = chest1;
            Chest2 = chest2;
            Chest3 = chest3;
            ChestOpt1 = chestOpt1;
            ChestOpt2 = chestOpt2;
        }
        public Chest Chest0 { get; set; }
        public Chest Chest1 { get; set; }
        public Chest Chest2 { get; set; }
        public Chest Chest3 { get; set; }
        public Chest ChestOpt1 { get; set; }
        public Chest ChestOpt2 { get; set; }
    }

    public class CompareResult
    {
        public CompareResult()
        {
            IsWinningPositionAvail = false;
        }

        public List<Chest> SuggestedChests { get; set; }
        public int NumInRow { get; set; }
        public int NumInRowStartIndex { get; set; }
        public bool IsWinningPositionAvail { get; set; }
    }
    public class FixSizeQueue<T> : Queue<T>
    {
        public int Limit { get; set; }

        public FixSizeQueue(int size)
        {
            this.Limit = size;
        }

        public new void Enqueue(T item)
        {
            if (this.Count >= this.Limit)
            {
                this.Dequeue();
            }

            base.Enqueue(item);
        }
    }
}
