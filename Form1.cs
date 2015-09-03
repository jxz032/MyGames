using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace MultiThreading
{
    public partial class Form1 : Form
    {
        private int numToWin = 5;
        private int initPositionOffset = 200;
        private int pace = 50;
        private int canvasSize = 1000;
        private int chestSize = 50;
        private bool redTurn;
        private static bool gameOver;
        private static bool playWithComputer;
        private static List<Chest> blueChestStore;
        private static List<Chest> redChestStore;
        private enum Direction
        {
            Xy00, //(1,  1)
            Xy01, //(1, -1)
            Xy10, //(-1, 1)
            Xy11  //(-1, -1)

        };

        #region Events
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            InitGame();
        }

        private void btnRegret_Click(object sender, EventArgs e)
        {
            Chest newAddedChest;
            List<Chest> store = redTurn ? blueChestStore : redChestStore;

            if (store.Count > 0)
            {
                newAddedChest = store[store.Count - 1];
                store.RemoveAt(store.Count - 1);
                int chestX = newAddedChest.Location_X * pace - chestSize / 2;
                int chestY = newAddedChest.Location_Y * pace - chestSize / 2;
                EraseChest(chestX, chestY);
                SwitchTurn();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            chbOnePlayer.Checked = true;
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (gameOver == false)
            {
                chbOnePlayer.Enabled = false;
                playWithComputer = chbOnePlayer.Checked;

                int x = e.Location.X - initPositionOffset;
                int y = e.Location.Y - initPositionOffset;

                int xLoc = x / pace;
                int xOffset = x % pace;
                int yLoc = y / pace;
                int yOffset = y % pace;

                xLoc = (xOffset > pace / 2) ? xLoc + 1 : xLoc;
                yLoc = (yOffset > pace / 2) ? yLoc + 1 : yLoc;

                if (xLoc >= 0 && xLoc <= 10 && yLoc >= 0 && yLoc <= 10)
                {
                    PlayGame(xLoc, yLoc);

                    //Chest test = FindDangerTriangleLevel2(redChestStore, new Chest { Location_X = xLoc, Location_Y = yLoc });

                    //if (test != null)
                    //    PlayGame(test.Location_X, test.Location_Y);

                    #region check around

                    if (playWithComputer && !redTurn && !gameOver)
                    {
                        AutoPlay(3, xLoc, yLoc);
                    }

                    #endregion
                }
            }
            else
            {
                btnRegret.Enabled = false;
                DialogResult userAnswer = MessageBox.Show("Game Over. Do you want to start a new game?", "Game Over",
                    MessageBoxButtons.OKCancel);

                if (userAnswer == System.Windows.Forms.DialogResult.OK)
                {
                    InitGame();
                }
            }

        }

        #endregion Events

        #region Methods

        private bool PlayGame(int xLoc, int yLoc)
        {
            bool isChestAvail = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), xLoc, yLoc);

            if (isChestAvail)
            {
                Chest newChest = new Chest { Location_X = xLoc, Location_Y = yLoc };

                if (redTurn)
                {
                    redChestStore.Add(newChest);
                }
                else
                {
                    blueChestStore.Add(newChest);
                }

                int chestX = xLoc * pace - chestSize / 2;
                int chestY = yLoc * pace - chestSize / 2;

                PaintChest(chestX, chestY, redTurn);

                if (redChestStore.Count >= numToWin && redTurn)
                {
                    bool isRedWin = false;

                    Judge(redChestStore, 5, ref isRedWin);

                    if (isRedWin)
                    {
                        MessageBox.Show("You Win!");
                        gameOver = true;
                        btnRegret.Enabled = false;
                    }
                }

                if (blueChestStore.Count >= numToWin && !redTurn)
                {
                    bool isBlueWin = false;
                    Judge(blueChestStore, 5, ref isBlueWin);

                    if (isBlueWin)
                    {
                        MessageBox.Show("Sorry you lose.");
                        gameOver = true;
                        btnRegret.Enabled = false;
                    }
                }

                SwitchTurn();

                return true;
            }
            return false;
        }

        private CompareResult Judge(List<Chest> chestsStore, int numToCheck, ref bool isWin)
        {
            Stopwatch watch = Stopwatch.StartNew();

            CompareResult finalResult = new CompareResult();

            int targetHorizon, targetVertical;

            IEnumerable<Chest> horizonSort = chestsStore.OrderBy(x => x.Location_X);
            IEnumerable<Chest> verticalSort = chestsStore.OrderBy(x => x.Location_Y);

            int mainLoop = 0;

            while (mainLoop < chestsStore.Count)
            {
                targetHorizon = horizonSort.ToArray()[mainLoop].Location_X;
                targetVertical = horizonSort.ToArray()[mainLoop].Location_Y;

                finalResult = JudgeSingleNode(chestsStore, horizonSort, verticalSort, numToCheck, targetHorizon, targetVertical, ref isWin);
                Chest dangerTriangeLevel1 = FindDangerTriangleLevel1(chestsStore, horizonSort.ToArray()[mainLoop]);
                Chest dangerTriangeLevel2 = FindDangerTriangleLevel2(chestsStore, horizonSort.ToArray()[mainLoop]);

                if (dangerTriangeLevel1 != null)
                {
                    finalResult.SuggestedChests.Add(dangerTriangeLevel1);
                }

                if (dangerTriangeLevel2 != null)
                {
                    finalResult.SuggestedChests.Add(dangerTriangeLevel2);
                }

                if (isWin && (finalResult.SuggestedChests.Count != 0 || numToCheck == 5))
                {
                    return finalResult;
                }
                mainLoop++;
            }//while

            watch.Stop();

            Debug.WriteLine("TIME - " + watch.GetTimeString());

            return finalResult;
        }

        private CompareResult JudgeSingleNode(List<Chest> chestsStore, IEnumerable<Chest> horizonSort, IEnumerable<Chest> verticalSort, int numToAlert, int targetHorizon, int targetVertical, ref bool isWin)
        {
            #region local variables
            bool xCheck = false;
            bool yCheck = false;
            bool xyCheck = false;
            List<Chest> chestOnEdge = new List<Chest>();

            List<Chest> chestOnEdgeX = new List<Chest>();
            List<Chest> chestOnEdgeY = new List<Chest>();
            List<Chest> chestOnEdgeXy = new List<Chest>();

            Chest tempChestLeft = new Chest();
            Chest tempChestRight = new Chest();

            CompareResult compareResultX = new CompareResult();
            CompareResult compareResultY = new CompareResult();
            CompareResult compareResultXy = new CompareResult();
            CompareResult finalResult = new CompareResult();

            bool isPosAvail, isPosAvailLeft, isPosAvailRight;

            Chest[] x_arr = horizonSort.Where(c => c.Location_Y == targetVertical).ToArray();
            Chest[] y_arr = verticalSort.Where(c => c.Location_X == targetHorizon).ToArray();

            Chest[] xy_arr00, xy_arr01, xy_arr10, xy_arr11;
            xy_arr00 =
                chestsStore.Where(
                    c => (c.Location_X == targetHorizon && c.Location_Y == targetVertical) ||
                        (c.Location_X - targetHorizon < 0 && c.Location_Y - targetVertical < 0 &&
                        Math.Abs(c.Location_X - targetHorizon) == Math.Abs(c.Location_Y - targetVertical))).ToArray();

            xy_arr01 =
                chestsStore.Where(
                    c => (c.Location_X == targetHorizon && c.Location_Y == targetVertical) ||
                        c.Location_X - targetHorizon < 0 && c.Location_Y - targetVertical > 0 &&
                        Math.Abs(c.Location_X - targetHorizon) == Math.Abs(c.Location_Y - targetVertical)).ToArray();

            xy_arr10 =
                chestsStore.Where(
                    c => (c.Location_X == targetHorizon && c.Location_Y == targetVertical) ||
                        c.Location_X - targetHorizon > 0 && c.Location_Y - targetVertical < 0 &&
                        Math.Abs(c.Location_X - targetHorizon) == Math.Abs(c.Location_Y - targetVertical)).ToArray();

            xy_arr11 =
                chestsStore.Where(
                    c => (c.Location_X == targetHorizon && c.Location_Y == targetVertical) ||
                        c.Location_X - targetHorizon > 0 && c.Location_Y - targetVertical > 0 &&
                        Math.Abs(c.Location_X - targetHorizon) == Math.Abs(c.Location_Y - targetVertical)).ToArray();
            #endregion

            Thread thX, thY, thXy;

            #region X
            if (x_arr.Count() >= numToAlert)
            {
                //thX = new Thread(unused => Compare(x_arr, ref xCheck));
                //thX.Name = "thread_CompareX";
                //thX.Start();

                compareResultX = Compare(x_arr, numToAlert, ref xCheck);

                #region

                if (compareResultX.SuggestedChests.Count != 0) // if xx0x or xxxx
                {
                    chestOnEdgeX = compareResultX.SuggestedChests;
                }
                else if (xCheck && compareResultX.NumInRowStartIndex + numToAlert <= x_arr.Count()) // if xxx
                {
                    tempChestLeft = new Chest
                    {
                        Location_X = x_arr[compareResultX.NumInRowStartIndex].Location_X - 1,
                        Location_Y = targetVertical
                    };
                    tempChestRight = new Chest
                    {
                        Location_X = x_arr[compareResultX.NumInRowStartIndex + numToAlert - 1].Location_X + 1,
                        Location_Y = targetVertical
                    };
                    isPosAvailLeft = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                       tempChestLeft.Location_X, tempChestLeft.Location_Y);

                    isPosAvailRight = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                        tempChestRight.Location_X, tempChestRight.Location_Y);

                    if (isPosAvailLeft && isPosAvailRight && numToAlert >= 3)
                    {
                        tempChestLeft.HighRecommand = true;
                        tempChestRight.HighRecommand = true;
                    }

                    if (isPosAvailLeft)
                    {
                        chestOnEdgeX.Add(tempChestLeft);
                    }

                    if (isPosAvailRight)
                    {
                        chestOnEdgeX.Add(tempChestRight);
                    }
                }
                #endregion
            }
            #endregion X

            #region Y
            if (y_arr.Count() >= numToAlert)
            {
                //thY = new Thread(unused => Compare(y_arr, ref yCheck));
                //thY.Name = "thread_CompareY";
                //thY.Start();

                compareResultY = Compare(y_arr, numToAlert, ref yCheck);

                #region if xx0x

                if (compareResultY.SuggestedChests.Count != 0)
                {
                    chestOnEdgeY = compareResultY.SuggestedChests;
                }

                #endregion

                if (yCheck && compareResultY.NumInRowStartIndex + numToAlert <= y_arr.Count())
                {
                    tempChestLeft = new Chest { Location_X = targetHorizon, Location_Y = y_arr[compareResultY.NumInRowStartIndex].Location_Y - 1 };
                    tempChestRight = new Chest { Location_X = targetHorizon, Location_Y = y_arr[compareResultY.NumInRowStartIndex + numToAlert - 1].Location_Y + 1 };
                    isPosAvailLeft = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), tempChestLeft.Location_X, tempChestLeft.Location_Y);
                    isPosAvailRight = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), tempChestRight.Location_X, tempChestRight.Location_Y);

                    if (isPosAvailLeft && isPosAvailRight && numToAlert >= 3)
                    {
                        tempChestLeft.HighRecommand = true;
                        tempChestRight.HighRecommand = true;
                    }

                    if (isPosAvailLeft)
                    {
                        chestOnEdgeY.Add(tempChestLeft);
                    }

                    if (isPosAvailRight)
                    {
                        chestOnEdgeY.Add(tempChestRight);
                    }
                }
            }
            #endregion Y

            #region XY

            Chest[] xy_arrCheck;
            string direction;

            if (xy_arr00.Count() + xy_arr11.Count() - 1 >= numToAlert)
            {
                xy_arrCheck = xy_arr00.Concat(xy_arr11).Distinct().OrderBy(c => c.Location_X).ToArray();
                direction = xy_arr00.Count() >= numToAlert ? Direction.Xy00.ToString() : Direction.Xy11.ToString();
            }
            else
            {
                xy_arrCheck = xy_arr01.Concat(xy_arr10).Distinct().OrderBy(c => c.Location_X).ToArray();
                direction = xy_arr01.Count() >= numToAlert ? Direction.Xy01.ToString() : Direction.Xy10.ToString();
            }

            //thXy = new Thread(unused => CompareXY(xy_arrUp, ref xyCheck));
            //thXy.Name = "thread_CompareXY";
            //thXy.Start();

            compareResultXy = CompareXY(xy_arrCheck, numToAlert, ref xyCheck);

            #region if xx0x

            if (compareResultXy.SuggestedChests.Count != 0)
                chestOnEdgeXy = compareResultXy.SuggestedChests;

            #endregion

            if (xyCheck && compareResultXy.NumInRowStartIndex + numToAlert <= xy_arrCheck.Count())
            {
                switch (direction)
                {
                    case "Xy00":
                    case "Xy11":
                        tempChestLeft = new Chest
                        {
                            Location_X = xy_arrCheck[compareResultXy.NumInRowStartIndex].Location_X - 1,
                            Location_Y = xy_arrCheck[compareResultXy.NumInRowStartIndex].Location_Y - 1
                        };
                        tempChestRight = new Chest
                        {
                            Location_X = xy_arrCheck[compareResultXy.NumInRowStartIndex + numToAlert - 1].Location_X + 1,
                            Location_Y = xy_arrCheck[compareResultXy.NumInRowStartIndex + numToAlert - 1].Location_Y + 1
                        };
                        break;
                    default:
                        tempChestLeft = new Chest
                        {
                            Location_X = xy_arrCheck[compareResultXy.NumInRowStartIndex].Location_X - 1,
                            Location_Y = xy_arrCheck[compareResultXy.NumInRowStartIndex].Location_Y + 1
                        };
                        tempChestRight = new Chest
                        {
                            Location_X = xy_arrCheck[compareResultXy.NumInRowStartIndex + numToAlert - 1].Location_X + 1,
                            Location_Y = xy_arrCheck[compareResultXy.NumInRowStartIndex + numToAlert - 1].Location_Y - 1
                        };
                        break;
                }

                isPosAvailLeft = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), tempChestRight.Location_X, tempChestRight.Location_Y);
                isPosAvailRight = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), tempChestLeft.Location_X, tempChestLeft.Location_Y);

                if (isPosAvailLeft && isPosAvailRight && numToAlert >= 3)
                {
                    tempChestLeft.HighRecommand = true;
                    tempChestRight.HighRecommand = true;
                }

                if (isPosAvailLeft && isPosAvailRight)
                {
                    chestOnEdgeXy.Add(tempChestRight);
                    chestOnEdgeXy.Add(tempChestLeft);
                }
            }

            #endregion XY

            if (chestOnEdgeX.Count == 1 && compareResultX.NumInRow > 4 && compareResultX.SuggestedChests.Count == 0) chestOnEdgeX = new List<Chest>();

            if (chestOnEdgeY.Count == 1 && compareResultY.NumInRow > 4 && compareResultY.SuggestedChests.Count == 0) chestOnEdgeY = new List<Chest>();

            if (chestOnEdgeXy.Count == 1 && compareResultXy.NumInRow > 4 && compareResultXy.SuggestedChests.Count == 0) chestOnEdgeXy = new List<Chest>();

            chestOnEdge = chestOnEdgeX.Concat(chestOnEdgeY).Concat(chestOnEdgeXy).ToList();

            finalResult.NumInRow = Math.Max(Math.Max(compareResultX.NumInRow, compareResultY.NumInRow), compareResultXy.NumInRow);
            finalResult.SuggestedChests = chestOnEdge;
            isWin = xCheck || yCheck || xyCheck;

            return finalResult;
        }

        private ChestTriange GetChestTriange(List<Chest> chestsStore, Chest centerChest)
        {
            // '3 1
            //  0   2

            Chest chest1, chest2;

            IEnumerable<Chest> chest1Opts =
                chestsStore.Where(
                    c =>
                        Math.Abs(c.Location_X - centerChest.Location_X) == 1 &&
                        Math.Abs(c.Location_Y - centerChest.Location_Y) == 1);

            IEnumerable<Chest> chest2Opts =
                chestsStore.Where(
                    c =>
                        Math.Pow(c.Location_X - centerChest.Location_X, 2) +
                        Math.Pow(c.Location_Y - centerChest.Location_Y, 2) == 4);

            foreach (Chest chest1Opt in chest1Opts)
            {
                Chest tempChest = chest2Opts.FirstOrDefault(c => Math.Abs(c.Location_X - chest1Opt.Location_X) == 1 &&
                                                                 Math.Abs(c.Location_Y - chest1Opt.Location_Y) == 1);

                if (tempChest != null)
                {
                    chest1 = chest1Opt;
                    chest2 = tempChest;
                    return new ChestTriange { Chest0 = centerChest, Chest1 = chest1, Chest2 = chest2 };
                }
            }
            return null;
        }

        private Chest FindDangerTriangleLevel1(List<Chest> chestsStore, Chest centerChest)
        {
            // '3 1
            //  0   2
            ChestTriange chestTri = GetChestTriange(chestsStore, centerChest);

            if (chestTri != null)
            {
                Chest chest3;
                Chest dangerChest = new Chest { HighRecommand = true };

                bool isHorizon = centerChest.Location_Y == chestTri.Chest2.Location_Y;

                IEnumerable<Chest> chest3Opts = isHorizon
                    ? chestsStore.Where(
                        c =>
                            (c.Location_X == centerChest.Location_X &&
                             Math.Abs(c.Location_Y - centerChest.Location_Y) == 1) ||
                            (c.Location_X == chestTri.Chest2.Location_X && Math.Abs(c.Location_Y - chestTri.Chest2.Location_Y) == 1))
                            :
                            chestsStore.Where(c => (c.Location_Y == centerChest.Location_Y &&
                             Math.Abs(c.Location_X - centerChest.Location_X) == 1) ||
                            (c.Location_Y == chestTri.Chest2.Location_Y && Math.Abs(c.Location_X - chestTri.Chest2.Location_X) == 1))
                            ;


                int direction21X = chestTri.Chest1.Location_X - chestTri.Chest2.Location_X;
                int direction21Y = chestTri.Chest1.Location_Y - chestTri.Chest2.Location_Y;

                dangerChest.Location_X = chestTri.Chest1.Location_X + direction21X;
                dangerChest.Location_Y = chestTri.Chest1.Location_Y + direction21Y;

                chest3 = chest3Opts.FirstOrDefault(
                        c =>
                            c.Location_X == centerChest.Location_X &&
                            Math.Abs(c.Location_Y - centerChest.Location_Y) == 1);

                if (chest3 != null && CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), dangerChest.Location_X, dangerChest.Location_Y))
                {
                    return dangerChest;
                }

                int direction01X = chestTri.Chest1.Location_X - centerChest.Location_X;
                int direction01Y = chestTri.Chest1.Location_Y - centerChest.Location_Y;

                dangerChest.Location_X = chestTri.Chest1.Location_X + direction01X;
                dangerChest.Location_Y = chestTri.Chest1.Location_Y + direction01Y;

                chest3 = chest3Opts.FirstOrDefault(
                    c =>
                        c.Location_X == chestTri.Chest2.Location_X &&
                        Math.Abs(c.Location_Y - chestTri.Chest2.Location_Y) == 1);

                if (chest3 != null && CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), dangerChest.Location_X, dangerChest.Location_Y))
                    return dangerChest;

            }
            return null;
        }

        private Chest FindDangerTriangleLevel2(List<Chest> chestsStore, Chest centerChest)
        {
            //    1
            //  0   2
            //    3

            ChestTriange chestTri = GetChestTriange(chestsStore, centerChest);

            if (chestTri != null)
            {
                Chest chest3;
                Chest dangerChest = new Chest { HighRecommand = true };

                bool isHorizon = centerChest.Location_Y == chestTri.Chest2.Location_Y;
                
                dangerChest.Location_X = isHorizon ? chestTri.Chest1.Location_X : chestTri.Chest0.Location_X;
                dangerChest.Location_Y = isHorizon ? chestTri.Chest0.Location_Y : chestTri.Chest1.Location_Y;

                List<Chest> temp = new List<Chest>();
                temp.Add(chestTri.Chest1);

                chest3 = chestsStore.Where(
                    c =>
                        Math.Abs(c.Location_X - centerChest.Location_X) == 1 &&
                        Math.Abs(c.Location_Y - centerChest.Location_Y) == 1).Except(temp).FirstOrDefault();

                if (chest3 != null && CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), dangerChest.Location_X, dangerChest.Location_Y))
                {
                    return dangerChest;
                }
            }
            return null;
        }
        private Chest FindAvailablePosition(Chest chest)
        {
            Random rdm = new Random();

            int offSetX = chest.Location_X == 0 ? rdm.Next(0, 2) : rdm.Next(-1, 2);
            int offSetY;

            if (chest.Location_Y == 0 && offSetX == 0) offSetY = 1;
            else if (chest.Location_Y == 0 && offSetX != 0) offSetY = rdm.Next(0, 2);
            else offSetY = rdm.Next(-1, 2);

            bool isPosAvail = CheckPositionAvailable(blueChestStore.Concat(redChestStore).ToList(), chest.Location_X + offSetX, chest.Location_Y + offSetY);

            while (!isPosAvail)
            {
                offSetX = chest.Location_X == 0 ? rdm.Next(0, 2) : rdm.Next(-1, 2);
                offSetY = chest.Location_Y == 0 ? rdm.Next(0, 2) : rdm.Next(-1, 2);

                isPosAvail = CheckPositionAvailable(blueChestStore.Concat(redChestStore).ToList(), chest.Location_X + offSetX, chest.Location_Y + offSetY);
            }

            return new Chest { Location_X = chest.Location_X + offSetX, Location_Y = chest.Location_Y + offSetY };
        }
        private void AutoPlay(int numToAlert, int targetHorizon, int targetVertical)
        {
            IEnumerable<Chest> horizonSort = redChestStore.OrderBy(x => x.Location_X);
            IEnumerable<Chest> verticalSort = redChestStore.OrderBy(x => x.Location_Y);

            bool isWin = false;

            bool isBlue2InRow = false;
            bool isBlue3InRow = false;

            CompareResult redResult = JudgeSingleNode(redChestStore, horizonSort, verticalSort, numToAlert, targetHorizon,
                targetVertical, ref isWin);
            CompareResult red3InRowResult = Judge(redChestStore, 3, ref isWin);
            CompareResult blue3InRowResult = Judge(blueChestStore, 3, ref isBlue3InRow);
            CompareResult blue2InRowResult = Judge(blueChestStore, 2, ref isBlue2InRow);

            if (redResult.SuggestedChests.Count == 0 && red3InRowResult.SuggestedChests.Count == 0 && !red3InRowResult.SuggestedChests.Any(c => c.HighRecommand)) //If red is still ok, put BLUE in an random position close to previos RED
            {
                #region no suggestion
                int blueCount = blueChestStore.Count;
                Chest lastBlue = blueCount == 0 ? redChestStore[redChestStore.Count - 1] : blueChestStore[blueCount - 1];

                if (blueCount > 2)
                {
                    List<Chest> suggestedResult;

                    if (isBlue3InRow && blue3InRowResult.SuggestedChests.Count != 0)
                    {
                        suggestedResult = blue3InRowResult.SuggestedChests;
                    }
                    else if (isBlue2InRow && blue2InRowResult.SuggestedChests.Count != 0)
                    {
                        suggestedResult = blue2InRowResult.SuggestedChests;
                    }
                    else
                    {
                        suggestedResult = new List<Chest>();
                    }

                    if (suggestedResult.Count != 0) // if blue has two in row
                    {
                        Random rdm = new Random();

                        int rdmIndex = rdm.Next(0, suggestedResult.Count - 1);

                        PlayGame(suggestedResult[rdmIndex].Location_X, suggestedResult[rdmIndex].Location_Y);
                    }
                    else
                    {
                        int index = blueCount - 1;
                        Chest rdmPosition = FindAvailablePosition(blueChestStore[index]);

                        while (rdmPosition == null)
                        {
                            index--;
                            rdmPosition = FindAvailablePosition(blueChestStore[index]);
                            if (index < 0)
                                break;
                        }
                        PlayGame(rdmPosition.Location_X, rdmPosition.Location_Y);
                    }
                }
                else//Blue first two steps
                {
                    Chest rdmChest = FindAvailablePosition(lastBlue);
                    PlayGame(rdmChest.Location_X, rdmChest.Location_Y);
                }
                #endregion
            }
            else //if red is already 3 in a row or dangerous triange appears
            {
                Random rdm = new Random();
                int rdmIndex;

                if (blue3InRowResult.SuggestedChests.Count == 0 && blue2InRowResult.SuggestedChests.Count != 0 && blue2InRowResult.SuggestedChests.Any(c => c.HighRecommand))
                {
                    blue2InRowResult.SuggestedChests = blue2InRowResult.SuggestedChests.Where(c => c.HighRecommand).ToList();
                    rdmIndex = rdm.Next(0, blue2InRowResult.SuggestedChests.Count());
                    PlayGame(blue2InRowResult.SuggestedChests[rdmIndex].Location_X, blue2InRowResult.SuggestedChests[rdmIndex].Location_Y);
                }
                else if (blue3InRowResult.SuggestedChests.Count != 0 && (redResult.NumInRow < 4))
                {
                    List<Chest> temp = redResult.SuggestedChests.Intersect(blue3InRowResult.SuggestedChests).ToList();
                    if (temp.Count != 0)
                    {
                        rdmIndex = rdm.Next(0, temp.Count);
                        PlayGame(temp[rdmIndex].Location_X, temp[rdmIndex].Location_Y);
                    }
                    else
                    {
                        rdmIndex = rdm.Next(0, blue3InRowResult.SuggestedChests.Count());
                        PlayGame(blue3InRowResult.SuggestedChests[rdmIndex].Location_X, blue3InRowResult.SuggestedChests[rdmIndex].Location_Y);
                    }
                }
                else if (redResult.SuggestedChests.Count != 0 && blue3InRowResult.NumInRow < 4)
                {
                    rdmIndex = rdm.Next(0, redResult.SuggestedChests.Count);
                    PlayGame(redResult.SuggestedChests[rdmIndex].Location_X,
                        redResult.SuggestedChests[rdmIndex].Location_Y);
                }
                else
                {
                    rdmIndex = rdm.Next(0, red3InRowResult.SuggestedChests.Count);
                    PlayGame(red3InRowResult.SuggestedChests[rdmIndex].Location_X,
                       red3InRowResult.SuggestedChests[rdmIndex].Location_Y);
                }
            }
        }

        private bool CheckPositionAvailable(List<Chest> chestStore, int x, int y)
        {
            bool isWithinCanvas = x >= 0 && x <= 10 && y >= 0 && y <= 10;
            return isWithinCanvas && !chestStore.Any(c => c.Location_X == x && c.Location_Y == y);
        }

        #region Single Direction Check
        private CompareResult Compare(Chest[] chests, int numToAlert, ref bool isWin)
        {
            int[] arrayInts;

            bool isX = false;
            CompareResult result = new CompareResult();

            List<Chest> breakPoints = new List<Chest>();

            if (chests.Where(c => c.Location_Y == chests[0].Location_Y).Count() == chests.Count())
            {
                isX = true;
                arrayInts = chests.Select(c => c.Location_X).ToArray();
            }
            else
            {
                arrayInts = chests.Select(c => c.Location_Y).ToArray();
            }

            int processedCount = 1;
            int numInRow = 1;
            int numInRowStartIndex = 0;

            int count = numToAlert - 1;

            if (chests.Count() >= numToAlert)
            {
                for (int index = 1; index < arrayInts.Count(); index++)
                {
                    if (arrayInts[index] - arrayInts[index - 1] == 1) // constinous
                    {
                        processedCount++;
                        numInRow++;
                        Chest suggestChestLeft, suggestChestRight;

                        if (numInRow == 4)
                        {
                            suggestChestLeft = isX ? new Chest { Location_X = arrayInts[numInRowStartIndex] - 1, Location_Y = chests[numInRowStartIndex].Location_Y } : new Chest { Location_X = chests[numInRowStartIndex].Location_X, Location_Y = arrayInts[numInRowStartIndex] - 1 };
                            suggestChestRight = isX ? new Chest { Location_X = arrayInts[numInRowStartIndex + 3] + 1, Location_Y = chests[numInRowStartIndex].Location_Y } : new Chest { Location_X = chests[numInRowStartIndex + 3].Location_X, Location_Y = arrayInts[numInRowStartIndex + 3] + 1 };

                            bool isPosAvailLeft, isPosAvailRight;
                            isPosAvailLeft = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), suggestChestLeft.Location_X, suggestChestLeft.Location_Y);
                            isPosAvailRight = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), suggestChestRight.Location_X, suggestChestRight.Location_Y);

                            if (isPosAvailLeft)
                            {
                                breakPoints.Add(suggestChestLeft);
                            }

                            if (isPosAvailRight)
                            {
                                breakPoints.Add(suggestChestRight);
                            }
                        }

                        count--;
                        if (count <= 0)
                        {
                            isWin = true;
                        }
                    }
                    else if (arrayInts[index] - arrayInts[index - 1] == 2) //xx0x
                    {
                        processedCount++;
                        numInRowStartIndex = index;

                        if ((processedCount > 2 && arrayInts[index - 1] - arrayInts[index - 2] == 1) || (processedCount == 2 && index < arrayInts.Count() - 1 && arrayInts[index + 1] - arrayInts[index] == 1)) //xx0x
                        {
                            bool tempHighRecommand = false;

                            tempHighRecommand = isX
                                ? CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), arrayInts[0] - 1,
                                    chests[0].Location_Y)
                                : CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), chests[0].Location_X, arrayInts[0] - 1);
                            Chest suggestChest = isX
                                ? new Chest
                                {
                                    Location_X = arrayInts[index] - 1,
                                    Location_Y = chests[0].Location_Y,
                                    HighRecommand = tempHighRecommand
                                }
                                : new Chest
                                {
                                    Location_X = chests[0].Location_X,
                                    Location_Y = arrayInts[index] - 1,
                                    HighRecommand = tempHighRecommand
                                };

                            bool breakNextPointAvail =
                                CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                                    suggestChest.Location_X, suggestChest.Location_Y);

                            if (breakNextPointAvail)
                            {
                                breakPoints.Add(suggestChest);
                            }
                        }
                        count = numToAlert - 1;
                    }
                    else if (arrayInts[index] - arrayInts[index - 1] == 3) //xx00x
                    {
                        processedCount++;
                        numInRowStartIndex = index;

                        bool tempHighRecommand;

                        tempHighRecommand = isX
                            ? CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), arrayInts[0] - 1,
                                chests[0].Location_Y)
                            : CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), chests[0].Location_X, arrayInts[0] - 1);

                        if ((processedCount > 2 && arrayInts[index - 1] - arrayInts[index - 2] == 1)
                            ||
                            (processedCount == 2 && index < arrayInts.Count() - 1 &&
                             arrayInts[index + 1] - arrayInts[index] == 1)) //xx00x || x00xx
                        {
                            Chest suggestChest1 = isX
                                ? new Chest
                                {
                                    Location_X = arrayInts[index] - 1,
                                    Location_Y = chests[0].Location_Y,
                                    HighRecommand = tempHighRecommand
                                }
                                : new Chest
                                {
                                    Location_X = chests[0].Location_X,
                                    Location_Y = arrayInts[index] - 1,
                                    HighRecommand = tempHighRecommand
                                };
                            Chest suggestChest2 = isX
                                ? new Chest
                                {
                                    Location_X = arrayInts[index] - 2,
                                    Location_Y = chests[0].Location_Y,
                                    HighRecommand = tempHighRecommand
                                }
                                : new Chest
                                {
                                    Location_X = chests[0].Location_X,
                                    Location_Y = arrayInts[index] - 2,
                                    HighRecommand = tempHighRecommand
                                };

                            bool breakNextPointAvail1 =
                                CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                                    suggestChest1.Location_X, suggestChest1.Location_Y);

                            bool breakNextPointAvail2 = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                                suggestChest2.Location_X, suggestChest2.Location_Y);

                            if (breakNextPointAvail1 && breakNextPointAvail2)
                            {
                                breakPoints.Add(suggestChest1);
                                breakPoints.Add(suggestChest2);
                            }
                        }
                        count = numToAlert - 1;
                    }
                }
            }

            result.NumInRow = processedCount;
            result.SuggestedChests = breakPoints;
            result.NumInRowStartIndex = numInRowStartIndex;
            return result;
        }

        private CompareResult CompareXY(Chest[] chests, int numToAlert, ref bool isWin)
        {
            int count = numToAlert - 1;
            int processedCount = 1;
            int numInRow = 1;
            int numInRowStartIndex = 0;

            List<Chest> breakPoints = new List<Chest>();
            CompareResult result = new CompareResult();

            string lastStepDirection;

            int routePoint = 0;
            FixSizeQueue<int> routeQueue = new FixSizeQueue<int>(2);
            routeQueue.Enqueue(routePoint);

            for (int index = 1; index < chests.Count(); index++)
            {
                int stepYDiff = 0;
                int stepXDiff = 0;

                routePoint = (chests[index].Location_X != chests[routePoint].Location_X
                    && (chests[index].Location_X - chests[routePoint].Location_X >= 1)
                      && chests[index].Location_Y != chests[routePoint].Location_Y)
                    ? index
                    : routePoint;

                if (routePoint != routeQueue.LastOrDefault())
                {
                    routeQueue.Enqueue(routePoint);

                    stepYDiff = chests[routeQueue.LastOrDefault()].Location_Y -
                                chests[routeQueue.Peek()].Location_Y;

                    stepXDiff = chests[routeQueue.LastOrDefault()].Location_X -
                                chests[routeQueue.Peek()].Location_X;

                    lastStepDirection = Direction.Xy00.ToString();
                    if (stepXDiff > 0 && stepYDiff > 0) lastStepDirection = Direction.Xy00.ToString();
                    else if (stepXDiff > 0 && stepYDiff < 0) lastStepDirection = Direction.Xy01.ToString();
                    else if (stepXDiff < 0 && stepYDiff < 0) lastStepDirection = Direction.Xy11.ToString();
                    else if (stepXDiff < 0 && stepYDiff > 0) lastStepDirection = Direction.Xy10.ToString();

                    if (Math.Abs(stepYDiff) == 1 && Math.Abs(stepXDiff) == 1) // continuous
                    {
                        count--;
                        processedCount++;
                        numInRow++;

                        if (numInRow == 4)
                        {
                            Chest suggestChestLeft = new Chest();
                            Chest suggestChestRight = new Chest();
                            bool isPosAvail;
                            switch (lastStepDirection)
                            {
                                case "Xy00":
                                case "Xy11":
                                    suggestChestLeft = new Chest
                                    {
                                        Location_X = chests[numInRowStartIndex].Location_X - 1,
                                        Location_Y = chests[numInRowStartIndex].Location_Y - 1,
                                        HighRecommand = true
                                    };

                                    suggestChestRight = new Chest
                                    {
                                        Location_X = chests[numInRowStartIndex + 3].Location_X + 1,
                                        Location_Y = chests[numInRowStartIndex + 3].Location_Y + 1,
                                        HighRecommand = true
                                    };
                                    break;
                                default:
                                    suggestChestLeft = new Chest
                                    {
                                        Location_X = chests[numInRowStartIndex].Location_X - 1,
                                        Location_Y = chests[numInRowStartIndex].Location_Y + 1,
                                        HighRecommand = true
                                    };
                                    suggestChestRight = new Chest
                                    {
                                        Location_X = chests[numInRowStartIndex + 3].Location_X + 1,
                                        Location_Y = chests[numInRowStartIndex + 3].Location_Y - 1,
                                        HighRecommand = true
                                    };
                                    break;
                            }

                            bool isPosAvailLeft = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                                       suggestChestLeft.Location_X, suggestChestLeft.Location_Y);

                            bool isPosAvailRight = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                                       suggestChestRight.Location_X, suggestChestRight.Location_Y);

                            if (isPosAvailLeft)
                            {
                                breakPoints.Add(suggestChestLeft);
                            }

                            if (isPosAvailRight)
                            {
                                breakPoints.Add(suggestChestRight);
                            }
                        }

                        if (count <= 0)
                        {
                            isWin = true;
                        }
                    }
                    else if (Math.Abs(stepYDiff) == 2 && Math.Abs(stepXDiff) == 2) //xx0x
                    {
                        processedCount++;
                        numInRowStartIndex = index;

                        int stepXDiffTemp, stepYDiffTemp;
                        bool index1Avail = false;

                        if (index < chests.Count() - 1)
                        {
                            stepXDiffTemp = chests[index + 1].Location_X - chests[index].Location_X;
                            stepYDiffTemp = chests[index + 1].Location_Y - chests[index].Location_Y;
                            index1Avail = Math.Abs(stepXDiffTemp) == 1 && Math.Abs(stepYDiffTemp) == 1;
                        }

                        if (processedCount > 2 || (processedCount == 2 && index1Avail)) //xx0x or x0xx
                        {
                            bool breakNextPointAvail = false;
                            Chest suggestChest = new Chest();

                            switch (lastStepDirection)
                            {
                                case "Xy00":
                                    suggestChest.Location_X = chests[routeQueue.Peek()].Location_X + 1;
                                    suggestChest.Location_Y = chests[routeQueue.Peek()].Location_Y + 1;
                                    suggestChest.HighRecommand = true;
                                    break;
                                case "Xy01":
                                    suggestChest.Location_X = chests[routeQueue.Peek()].Location_X + 1;
                                    suggestChest.Location_Y = chests[routeQueue.Peek()].Location_Y - 1;
                                    suggestChest.HighRecommand = true;
                                    break;
                                case "Xy10":
                                    suggestChest.Location_X = chests[routeQueue.Peek()].Location_X - 1;
                                    suggestChest.Location_Y = chests[routeQueue.Peek()].Location_Y + 1;
                                    suggestChest.HighRecommand = true;
                                    break;
                                case "Xy11":
                                    suggestChest.Location_X = chests[routeQueue.Peek()].Location_X - 1;
                                    suggestChest.Location_Y = chests[routeQueue.Peek()].Location_Y - 1;
                                    suggestChest.HighRecommand = true;
                                    break;
                            }

                            breakNextPointAvail = CheckPositionAvailable(blueChestStore.Concat(redChestStore).ToList(),
                                suggestChest.Location_X, suggestChest.Location_Y);

                            if (breakNextPointAvail)
                            {
                                breakPoints.Add(suggestChest);
                            }
                        }
                        count = numToAlert - 1;
                    }
                    else if (Math.Abs(stepYDiff) == 3 && Math.Abs(stepXDiff) == 3) //xx00x
                    {
                        processedCount++;
                        numInRowStartIndex = index;

                        int stepXDiffTemp, stepYDiffTemp;
                        bool index1Avail = false;

                        if (index < chests.Count() - 1)
                        {
                            stepXDiffTemp = Math.Abs(chests[index + 1].Location_X - chests[index].Location_X);
                            stepYDiffTemp = Math.Abs(chests[index + 1].Location_Y - chests[index].Location_Y);
                            index1Avail = stepXDiffTemp == 1 && stepYDiffTemp == 1;
                        }

                        if (processedCount > 2 || (processedCount == 2 && index1Avail)) //xx00x or x00xx
                        {
                            bool breakNextPointAvail = false;
                            Chest suggestChest = new Chest();

                            switch (lastStepDirection)
                            {
                                case "Xy00":
                                    suggestChest.Location_X = chests[routeQueue.Peek()].Location_X + 2;
                                    suggestChest.Location_Y = chests[routeQueue.Peek()].Location_Y + 2;
                                    suggestChest.HighRecommand = true;
                                    break;
                                case "Xy01":
                                    suggestChest.Location_X = chests[routeQueue.Peek()].Location_X + 2;
                                    suggestChest.Location_Y = chests[routeQueue.Peek()].Location_Y - 2;
                                    suggestChest.HighRecommand = true;
                                    break;
                                case "Xy10":
                                    suggestChest.Location_X = chests[routeQueue.Peek()].Location_X - 2;
                                    suggestChest.Location_Y = chests[routeQueue.Peek()].Location_Y + 2;
                                    suggestChest.HighRecommand = true;
                                    break;
                                case "Xy11":
                                    suggestChest.Location_X = chests[routeQueue.Peek()].Location_X - 2;
                                    suggestChest.Location_Y = chests[routeQueue.Peek()].Location_Y - 2;
                                    suggestChest.HighRecommand = true;
                                    break;
                            }

                            breakNextPointAvail = CheckPositionAvailable(blueChestStore.Concat(redChestStore).ToList(),
                                suggestChest.Location_X, suggestChest.Location_Y);

                            if (breakNextPointAvail)
                            {
                                breakPoints.Add(suggestChest);
                            }
                        }
                        count = numToAlert - 1;
                    }
                    else
                    {
                        count = numToAlert - 1;
                    }
                }
            }

            result.NumInRow = processedCount;
            result.NumInRowStartIndex = numInRowStartIndex;
            result.SuggestedChests = breakPoints;
            return result;
        }
        #endregion

        private void InitGame()
        {
            chbOnePlayer.Enabled = true;

            btnRegret.Enabled = true;
            this.CreateGraphics().Clear(this.BackColor);
            redTurn = true;
            blueChestStore = new List<Chest>();
            redChestStore = new List<Chest>();
            gameOver = false;

            #region Draw canvas
            this.Size = new Size(canvasSize, canvasSize);

            for (int i = 0; i <= 10; i++)
            {
                Point startPoint = new Point(initPositionOffset, initPositionOffset + i * pace);
                Point endPoint = new Point(initPositionOffset + pace * 10, initPositionOffset + i * pace);
                this.CreateGraphics().DrawLine(new Pen(Brushes.Black, 2), startPoint, endPoint);
            }

            for (int j = 0; j <= 10; j++)
            {
                Point startPoint = new Point(initPositionOffset + j * pace, initPositionOffset);
                Point endPoint = new Point(initPositionOffset + j * pace, initPositionOffset + pace * 10);
                this.CreateGraphics().DrawLine(new Pen(Brushes.Black, 2), startPoint, endPoint);
            }

            #endregion

        }

        private void SwitchTurn()
        {
            redTurn = !redTurn;

            SolidBrush solidBrush = new SolidBrush(this.BackColor);
            this.CreateGraphics().FillRectangle(solidBrush, new Rectangle(400 + initPositionOffset, -180 + initPositionOffset, chestSize, chestSize));
            PaintChest(400, -180, redTurn);
        }

        private void PaintChest(int x, int y, bool isRedTurn)
        {
            // SolidBrush solidBrush = isRedTurn ? new SolidBrush(Color.Red) : new SolidBrush(Color.Blue);
            //   this.CreateGraphics().FillEllipse(solidBrush, rect);

            Rectangle rect = new Rectangle(x + initPositionOffset, y + initPositionOffset, chestSize, chestSize);
            Graphics gfx = this.CreateGraphics();
            Icon newIcon = isRedTurn ? new Icon(Directory.GetCurrentDirectory() + @"..\..\..\img\furRed.ico") : new Icon(Directory.GetCurrentDirectory() + @"..\..\..\img\catTied.ico");
            gfx.DrawIcon(newIcon, rect);
        }

        private void EraseChest(int x, int y)
        {
            SolidBrush solidBrush = new SolidBrush(this.BackColor);

            this.CreateGraphics().FillRectangle(solidBrush, new Rectangle(x + initPositionOffset, y + initPositionOffset, chestSize, chestSize));

            Point startPointX = new Point(x + initPositionOffset, y + initPositionOffset + chestSize / 2);
            Point endPointX = new Point(x + initPositionOffset + chestSize, y + initPositionOffset + chestSize / 2);
            this.CreateGraphics().DrawLine(new Pen(Brushes.Black, 2), startPointX, endPointX);

            Point startPointY = new Point(x + initPositionOffset + chestSize / 2, y + initPositionOffset);
            Point endPointY = new Point(x + initPositionOffset + chestSize / 2, y + initPositionOffset + chestSize);
            this.CreateGraphics().DrawLine(new Pen(Brushes.Black, 2), startPointY, endPointY);
        }

        #endregion
    }


}
