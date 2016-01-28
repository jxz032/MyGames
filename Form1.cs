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
        private static bool gameOver, gameStart;
        private static bool playWithComputer;
        private static int gameLevel;
        private static List<Chest> blueChestStore;
        private static List<Chest> redChestStore;
        private static List<Chest> completeChestStore; 
        private enum Direction
        {
            Xy00, //(1,  1)
            Xy01, //(1, -1)
            Xy10, //(-1, 1)
            Xy11  //(-1, -1)

        };

        private enum ChestColor
        {
            Red,
            Blue
        }
        #region Events
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            InitGame();
            gameStart = true;
        }

        private void btnRegret_Click(object sender, EventArgs e)
        {
            Chest newAddedChest;
            List<Chest> store = redTurn ? blueChestStore : redChestStore;

            if (store.Count > 0)
            {
                newAddedChest = store[store.Count - 1];
                store.RemoveAt(store.Count - 1);
                completeChestStore.RemoveAt(completeChestStore.Count - 1);
                int chestX = newAddedChest.LocationX * pace - chestSize / 2;
                int chestY = newAddedChest.LocationY * pace - chestSize / 2;
                EraseChest(chestX, chestY);
                SwitchTurn();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            chbOnePlayer.Checked = true;

            ddlLevel.Items.Add(new ListItem("Easy", 0));
            ddlLevel.Items.Add(new ListItem("Middle", 1));
            ddlLevel.Items.Add(new ListItem("Hard", 2));

            ddlLevel.SelectedIndex = 0;
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (!gameOver && gameStart)
            {
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
                    
                    #region check around

                    if (playWithComputer && !redTurn && !gameOver)
                    {
                        AutoPlay(3, xLoc, yLoc);
                    }

                    #endregion
                }
            }
            else if (gameOver)
            {
                //btnRegret.Enabled = false;
                //DialogResult userAnswer = MessageBox.Show("Game Over. Do you want to start a new game?", "Game Over",
                //    MessageBoxButtons.OKCancel);

                //if (userAnswer == System.Windows.Forms.DialogResult.OK)
                //{
                //    InitGame();
                //}
            }
        }

        #endregion Events

        #region Methods

        private bool PlayGame(int xLoc, int yLoc)
        {
            bool isChestAvail = Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), xLoc, yLoc);

            if (isChestAvail)
            {
                Chest newChest = new Chest { LocationX = xLoc, LocationY = yLoc };

                if (redTurn)
                {
                    newChest.Color = ChestColor.Red.ToString();
                    newChest.IsAvailable = false;
                    redChestStore.Add(newChest);
                    completeChestStore.Add(newChest);
                }
                else
                {
                    newChest.Color = ChestColor.Blue.ToString();
                    newChest.IsAvailable = false;
                    blueChestStore.Add(newChest);
                    completeChestStore.Add(newChest);
                }

                int chestX = xLoc * pace - chestSize / 2;
                int chestY = yLoc * pace - chestSize / 2;

                PaintChest(chestX, chestY, redTurn);
               // if (redChestStore.Count >= numToWin && redTurn)
                if (redChestStore.Count >= 3 && redTurn)
                {
                    bool isRedWin = false;

                    Judge(redChestStore, 5, ref isRedWin);

                    if (isRedWin)
                    {
                        MessageBox.Show("You Win!");
                        gameOver = true;
                        //    btnRegret.Enabled = false;
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
                        //     btnRegret.Enabled = false;
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

            IEnumerable<Chest> horizonSort = chestsStore.OrderBy(x => x.LocationX);
            IEnumerable<Chest> verticalSort = chestsStore.OrderBy(x => x.LocationY);

            int mainLoop = 0;

            while (mainLoop < chestsStore.Count)
            {
                targetHorizon = horizonSort.ToArray()[mainLoop].LocationX;
                targetVertical = horizonSort.ToArray()[mainLoop].LocationY;

                finalResult = JudgeSingleNode(chestsStore, horizonSort, verticalSort, numToCheck, targetHorizon, targetVertical, ref isWin);
                Chest centerChest = horizonSort.ToArray()[mainLoop];
                Chest dangerTriangelLevel1 = Utilities.FindDangerTriangleLevel1(chestsStore, completeChestStore, centerChest);
                Chest dangerTriangelLevel2 = Utilities.FindDangerTriangleLevel2(chestsStore, completeChestStore, centerChest);

                Chest level3CenterChest = new Chest(centerChest.LocationX - 1, centerChest.LocationY); //0
                Chest dangerTriangelLevel3 = Utilities.FindDangerTriangleLevel3(chestsStore, completeChestStore, level3CenterChest);

                if (dangerTriangelLevel3 == null)
                {
                    level3CenterChest = new Chest(centerChest.LocationX, centerChest.LocationY - 1); //1
                    dangerTriangelLevel3 = Utilities.FindDangerTriangleLevel3(chestsStore, completeChestStore, level3CenterChest);

                    if (dangerTriangelLevel3 == null)
                    {
                        level3CenterChest = new Chest(centerChest.LocationX + 1, centerChest.LocationY); //2
                        dangerTriangelLevel3 = Utilities.FindDangerTriangleLevel3(chestsStore, completeChestStore, level3CenterChest);

                        if (dangerTriangelLevel3 == null)
                        {
                            level3CenterChest = new Chest(centerChest.LocationX, centerChest.LocationY - 1); //2
                            dangerTriangelLevel3 = Utilities.FindDangerTriangleLevel3(chestsStore, completeChestStore, level3CenterChest);
                        }
                    }
                }
               
                
                if (dangerTriangelLevel1 != null && gameLevel == 0)
                {
                    finalResult.SuggestedChests.Add(dangerTriangelLevel1);
                }

                if (dangerTriangelLevel2 != null && gameLevel >= 1)
                {
                    finalResult.SuggestedChests.Add(dangerTriangelLevel2);
                }

                if (dangerTriangelLevel3 != null && gameLevel == 2)
                {
                    finalResult.SuggestedChests.Add(dangerTriangelLevel3);
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

            bool isPosAvailLeft, isPosAvailRight;

            Chest[] x_arr = horizonSort.Where(c => c.LocationY == targetVertical).ToArray();
            Chest[] y_arr = verticalSort.Where(c => c.LocationX == targetHorizon).ToArray();

            Chest[] xy_arr00, xy_arr01, xy_arr10, xy_arr11;
            xy_arr00 =
                chestsStore.Where(
                    c => (c.LocationX == targetHorizon && c.LocationY == targetVertical) ||
                        (c.LocationX - targetHorizon < 0 && c.LocationY - targetVertical < 0 &&
                        Math.Abs(c.LocationX - targetHorizon) == Math.Abs(c.LocationY - targetVertical))).ToArray();

            xy_arr01 =
                chestsStore.Where(
                    c => (c.LocationX == targetHorizon && c.LocationY == targetVertical) ||
                        c.LocationX - targetHorizon < 0 && c.LocationY - targetVertical > 0 &&
                        Math.Abs(c.LocationX - targetHorizon) == Math.Abs(c.LocationY - targetVertical)).ToArray();

            xy_arr10 =
                chestsStore.Where(
                    c => (c.LocationX == targetHorizon && c.LocationY == targetVertical) ||
                        c.LocationX - targetHorizon > 0 && c.LocationY - targetVertical < 0 &&
                        Math.Abs(c.LocationX - targetHorizon) == Math.Abs(c.LocationY - targetVertical)).ToArray();

            xy_arr11 =
                chestsStore.Where(
                    c => (c.LocationX == targetHorizon && c.LocationY == targetVertical) ||
                        c.LocationX - targetHorizon > 0 && c.LocationY - targetVertical > 0 &&
                        Math.Abs(c.LocationX - targetHorizon) == Math.Abs(c.LocationY - targetVertical)).ToArray();
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
                        LocationX = x_arr[compareResultX.NumInRowStartIndex].LocationX - 1,
                        LocationY = targetVertical
                    };
                    tempChestRight = new Chest
                    {
                        LocationX = x_arr[compareResultX.NumInRowStartIndex + numToAlert - 1].LocationX + 1,
                        LocationY = targetVertical
                    };
                    isPosAvailLeft = Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                       tempChestLeft.LocationX, tempChestLeft.LocationY);

                    isPosAvailRight = Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                        tempChestRight.LocationX, tempChestRight.LocationY);

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
                    tempChestLeft = new Chest { LocationX = targetHorizon, LocationY = y_arr[compareResultY.NumInRowStartIndex].LocationY - 1 };
                    tempChestRight = new Chest { LocationX = targetHorizon, LocationY = y_arr[compareResultY.NumInRowStartIndex + numToAlert - 1].LocationY + 1 };
                    isPosAvailLeft = Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), tempChestLeft.LocationX, tempChestLeft.LocationY);
                    isPosAvailRight = Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), tempChestRight.LocationX, tempChestRight.LocationY);

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
                xy_arrCheck = xy_arr00.Concat(xy_arr11).Distinct().OrderBy(c => c.LocationX).ToArray();
                direction = xy_arr00.Count() >= numToAlert ? Direction.Xy00.ToString() : Direction.Xy11.ToString();
            }
            else
            {
                xy_arrCheck = xy_arr01.Concat(xy_arr10).Distinct().OrderBy(c => c.LocationX).ToArray();
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
                            LocationX = xy_arrCheck[compareResultXy.NumInRowStartIndex].LocationX - 1,
                            LocationY = xy_arrCheck[compareResultXy.NumInRowStartIndex].LocationY - 1
                        };
                        tempChestRight = new Chest
                        {
                            LocationX = xy_arrCheck[compareResultXy.NumInRowStartIndex + numToAlert - 1].LocationX + 1,
                            LocationY = xy_arrCheck[compareResultXy.NumInRowStartIndex + numToAlert - 1].LocationY + 1
                        };
                        break;
                    default:
                        tempChestLeft = new Chest
                        {
                            LocationX = xy_arrCheck[compareResultXy.NumInRowStartIndex].LocationX - 1,
                            LocationY = xy_arrCheck[compareResultXy.NumInRowStartIndex].LocationY + 1
                        };
                        tempChestRight = new Chest
                        {
                            LocationX = xy_arrCheck[compareResultXy.NumInRowStartIndex + numToAlert - 1].LocationX + 1,
                            LocationY = xy_arrCheck[compareResultXy.NumInRowStartIndex + numToAlert - 1].LocationY - 1
                        };
                        break;
                }

                isPosAvailLeft = Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), tempChestRight.LocationX, tempChestRight.LocationY);
                isPosAvailRight = Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), tempChestLeft.LocationX, tempChestLeft.LocationY);

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
        
        private Chest FindAvailablePosition(Chest chest)
        {
            Random rdm = new Random();

            int offSetX = chest.LocationX == 0 ? rdm.Next(0, 2) : rdm.Next(-1, 2);
            int offSetY;

            if (chest.LocationY == 0 && offSetX == 0) offSetY = 1;
            else if (chest.LocationY == 0 && offSetX != 0) offSetY = rdm.Next(0, 2);
            else offSetY = rdm.Next(-1, 2);

            bool isPosAvail = Utilities.CheckPositionAvailable(blueChestStore.Concat(redChestStore).ToList(), chest.LocationX + offSetX, chest.LocationY + offSetY);

            while (!isPosAvail)
            {
                offSetX = chest.LocationX == 0 ? rdm.Next(0, 2) : rdm.Next(-1, 2);
                offSetY = chest.LocationY == 0 ? rdm.Next(0, 2) : rdm.Next(-1, 2);

                isPosAvail = Utilities.CheckPositionAvailable(blueChestStore.Concat(redChestStore).ToList(), chest.LocationX + offSetX, chest.LocationY + offSetY);
            }

            return new Chest { LocationX = chest.LocationX + offSetX, LocationY = chest.LocationY + offSetY };
        }

        private IEnumerable<Chest> IgnoreChestsOnEdge(IEnumerable<Chest> chests)
        {
            return chests.SkipWhile(c => c.LocationX == 0 || c.LocationX == 10 || c.LocationY == 0 || c.LocationY == 10);
        }

        private void AutoPlay(int numToAlert, int targetHorizon, int targetVertical)
        {
            IEnumerable<Chest> horizonSort = redChestStore.OrderBy(x => x.LocationX);
            IEnumerable<Chest> verticalSort = redChestStore.OrderBy(x => x.LocationY);

            bool isWin = false;

            bool isBlue2InRow = false;
            bool isBlue3InRow = false;
            bool isBlue4InRow = false;

            CompareResult redResult = JudgeSingleNode(redChestStore, horizonSort, verticalSort, numToAlert, targetHorizon,
                targetVertical, ref isWin);


            CompareResult red3InRowResult = Judge(redChestStore, 3, ref isWin);

            CompareResult blue4InRowResult = Judge(blueChestStore, 4, ref isBlue4InRow);
            CompareResult blue3InRowResult = Judge(blueChestStore, 3, ref isBlue3InRow);
            CompareResult blue2InRowResult = Judge(blueChestStore, 2, ref isBlue2InRow);

            if (isBlue4InRow && blue4InRowResult.SuggestedChests.Count != 0)
            {
                PlayGame(blue4InRowResult.SuggestedChests[0].LocationX, blue4InRowResult.SuggestedChests[0].LocationY);
            }
            else if (redResult.SuggestedChests.Count == 0 && red3InRowResult.SuggestedChests.Count == 0 && !red3InRowResult.SuggestedChests.Any(c => c.HighRecommand)) //If red is still ok, put BLUE in an random position close to previos RED
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
                        if (suggestedResult.Count > 1)
                        {
                            suggestedResult = IgnoreChestsOnEdge(suggestedResult).ToList();
                        }

                        PlayGame(suggestedResult[0].LocationX, suggestedResult[0].LocationY);
                    }
                    else
                    {
                        int index = blueCount - 1;
                        Chest rdmPosition = FindAvailablePosition(blueChestStore[index]);

                        if (rdmPosition == null || rdmPosition.LocationX == 0 || rdmPosition.LocationX == 10 || rdmPosition.LocationY == 0 ||
                            rdmPosition.LocationY == 10)
                        {
                            while (true)
                            {
                                index--;
                                rdmPosition = FindAvailablePosition(blueChestStore[index]);
                                if (index < 0 || rdmPosition != null)
                                    break;
                            }
                        }

                        PlayGame(rdmPosition.LocationX, rdmPosition.LocationY);
                    }
                }
                else//Blue first two steps
                {
                    Chest rdmChest = FindAvailablePosition(lastBlue);
                    PlayGame(rdmChest.LocationX, rdmChest.LocationY);
                }
                #endregion
            }
            else //if red is already 3 in a row or dangerous triange appears
            {
                if (redResult.NumInRow == 4 && redResult.IsWinningPositionAvail)
                {
                    PlayGame(redResult.SuggestedChests.FirstOrDefault().LocationX,
                        redResult.SuggestedChests.FirstOrDefault().LocationY);
                }
                else if (blue3InRowResult.SuggestedChests.Count == 0 && blue2InRowResult.SuggestedChests.Count != 0 && blue2InRowResult.SuggestedChests.Any(c => c.HighRecommand))
                {
                    blue2InRowResult.SuggestedChests = blue2InRowResult.SuggestedChests.Where(c => c.HighRecommand).ToList();

                    PlayGame(blue2InRowResult.SuggestedChests[0].LocationX, blue2InRowResult.SuggestedChests[0].LocationY);
                }
                else if (blue3InRowResult.SuggestedChests.Count != 0 && (redResult.NumInRow < 4) && redResult.SuggestedChests.Any(c => c.HighRecommand) == false)
                {
                    List<Chest> temp = redResult.SuggestedChests.Intersect(blue3InRowResult.SuggestedChests).ToList();
                    if (temp.Count != 0)
                    {
                        PlayGame(temp[0].LocationX, temp[0].LocationY);
                    }
                    else
                    {
                        PlayGame(blue3InRowResult.SuggestedChests[0].LocationX, blue3InRowResult.SuggestedChests[0].LocationY);
                    }
                }
                else if (redResult.SuggestedChests.Count != 0 && blue3InRowResult.NumInRow < 4)
                {
                    Chest selectChest = redResult.SuggestedChests.FirstOrDefault(c => c.HighRecommand);

                    PlayGame(selectChest.LocationX, selectChest.LocationY);
                }
                else if (redResult.SuggestedChests.Count != 0 && redResult.SuggestedChests.Exists(c => c.HighRecommand))
                {
                    Chest selectChest = redResult.SuggestedChests.FirstOrDefault(c => c.HighRecommand);
                    PlayGame(selectChest.LocationX, selectChest.LocationY);
                }
                else
                {
                    PlayGame(red3InRowResult.SuggestedChests[0].LocationX,
                       red3InRowResult.SuggestedChests[0].LocationY);
                }
            }
        }
        
        #region Single Direction Check
        private CompareResult Compare(Chest[] chests, int numToAlert, ref bool isWin)
        {
            int[] arrayInts;

            bool isX = false;
            CompareResult result = new CompareResult();

            List<Chest> breakPoints = new List<Chest>();

            if (chests.Where(c => c.LocationY == chests[0].LocationY).Count() == chests.Count())
            {
                isX = true;
                arrayInts = chests.Select(c => c.LocationX).ToArray();
            }
            else
            {
                arrayInts = chests.Select(c => c.LocationY).ToArray();
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
                            #region Num 4 in row
                            suggestChestLeft = isX ? new Chest { LocationX = arrayInts[numInRowStartIndex] - 1, LocationY = chests[numInRowStartIndex].LocationY } : new Chest { LocationX = chests[numInRowStartIndex].LocationX, LocationY = arrayInts[numInRowStartIndex] - 1 };
                            suggestChestRight = isX ? new Chest { LocationX = arrayInts[arrayInts.Count() - 1] + 1, LocationY = chests[numInRowStartIndex].LocationY } : new Chest { LocationX = chests[chests.Count() - 1].LocationX, LocationY = arrayInts[numInRowStartIndex + 3] + 1 };

                            bool isPosAvailLeft, isPosAvailRight;
                            isPosAvailLeft = Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), suggestChestLeft.LocationX, suggestChestLeft.LocationY);
                            isPosAvailRight = Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), suggestChestRight.LocationX, suggestChestRight.LocationY);

                            if (isPosAvailLeft)
                            {
                                suggestChestLeft.HighRecommand = true;
                                breakPoints.Add(suggestChestLeft);
                            }

                            if (isPosAvailRight)
                            {
                                suggestChestRight.HighRecommand = true;
                                breakPoints.Add(suggestChestRight);
                            }

                            if (breakPoints.Count != 0)
                            {
                                result.IsWinningPositionAvail = true;
                            }
                            #endregion
                        }
                        else if (numInRow < 4)
                        {
                            breakPoints =
                                breakPoints.SkipWhile(
                                    c =>
                                        c.LocationX == 0 || c.LocationX == 10 || c.LocationY == 0 ||
                                        c.LocationY == 10).ToList();
                        }
                        count--;
                        if (count <= 0)
                        {
                            isWin = true;
                        }
                    }
                    else if (arrayInts[index] - arrayInts[index - 1] == 2) //xx0x
                    {
                        #region XX0X
                        processedCount++;
                        numInRowStartIndex = index;

                        if ((processedCount > 2 && arrayInts[index - 1] - arrayInts[index - 2] == 1) || (processedCount == 2 && index < arrayInts.Count() - 1 && arrayInts[index + 1] - arrayInts[index] == 1)) //xx0x
                        {
                            bool tempHighRecommand = false;

                            tempHighRecommand = isX
                                ? Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), arrayInts[0] - 1,
                                    chests[0].LocationY)
                                : Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), chests[0].LocationX, arrayInts[0] - 1);
                            Chest suggestChest = isX
                                ? new Chest
                                {
                                    LocationX = arrayInts[index] - 1,
                                    LocationY = chests[0].LocationY,
                                    HighRecommand = tempHighRecommand
                                }
                                : new Chest
                                {
                                    LocationX = chests[0].LocationX,
                                    LocationY = arrayInts[index] - 1,
                                    HighRecommand = tempHighRecommand
                                };

                            bool breakNextPointAvail =
                                Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                                    suggestChest.LocationX, suggestChest.LocationY);

                            if (breakNextPointAvail)
                            {
                                breakPoints.Add(suggestChest);
                            }
                        }
                        count = numToAlert - 1;
                        #endregion
                    }
                    else if (arrayInts[index] - arrayInts[index - 1] == 3) //xx00x
                    {
                        #region xx00x
                        processedCount++;
                        numInRowStartIndex = index;

                        //bool tempHighRecommand = false;

                        //tempHighRecommand = isX
                        //    ? Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), arrayInts[0] - 1,
                        //        chests[0].LocationY)
                        //    : Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), chests[0].LocationX, arrayInts[0] - 1);

                        if ((processedCount > 2 && arrayInts[index - 1] - arrayInts[index - 2] == 1)
                            ||
                            (processedCount == 2 && index < arrayInts.Count() - 1 &&
                             arrayInts[index + 1] - arrayInts[index] == 1)) //xx00x || x00xx
                        {
                            Chest suggestChest1 = isX
                                ? new Chest
                                {
                                    LocationX = arrayInts[index] - 1,
                                    LocationY = chests[0].LocationY
                                    //  HighRecommand = tempHighRecommand
                                }
                                : new Chest
                                {
                                    LocationX = chests[0].LocationX,
                                    LocationY = arrayInts[index] - 1
                                    //   HighRecommand = tempHighRecommand
                                };
                            Chest suggestChest2 = isX
                                ? new Chest
                                {
                                    LocationX = arrayInts[index] - 2,
                                    LocationY = chests[0].LocationY
                                    // HighRecommand = tempHighRecommand
                                }
                                : new Chest
                                {
                                    LocationX = chests[0].LocationX,
                                    LocationY = arrayInts[index] - 2
                                    //  HighRecommand = tempHighRecommand
                                };

                            bool breakNextPointAvail1 =
                                Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                                    suggestChest1.LocationX, suggestChest1.LocationY);

                            bool breakNextPointAvail2 = Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                                suggestChest2.LocationX, suggestChest2.LocationY);

                            if (breakNextPointAvail1 && breakNextPointAvail2)
                            {
                                breakPoints.Add(suggestChest1);
                                breakPoints.Add(suggestChest2);
                            }
                        }
                        count = numToAlert - 1;
                        #endregion
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

                routePoint = (chests[index].LocationX != chests[routePoint].LocationX
                    && (chests[index].LocationX - chests[routePoint].LocationX >= 1)
                      && chests[index].LocationY != chests[routePoint].LocationY)
                    ? index
                    : routePoint;

                if (routePoint != routeQueue.LastOrDefault())
                {
                    routeQueue.Enqueue(routePoint);

                    stepYDiff = chests[routeQueue.LastOrDefault()].LocationY -
                                chests[routeQueue.Peek()].LocationY;

                    stepXDiff = chests[routeQueue.LastOrDefault()].LocationX -
                                chests[routeQueue.Peek()].LocationX;

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
                            #region Num 4 in row
                            Chest suggestChestLeft = new Chest();
                            Chest suggestChestRight = new Chest();

                            switch (lastStepDirection)
                            {
                                case "Xy00":
                                case "Xy11":
                                    suggestChestLeft = new Chest
                                    {
                                        LocationX = chests[numInRowStartIndex].LocationX - 1,
                                        LocationY = chests[numInRowStartIndex].LocationY - 1,
                                        HighRecommand = true
                                    };

                                    suggestChestRight = new Chest
                                    {
                                        LocationX = chests[numInRowStartIndex + 3].LocationX + 1,
                                        LocationY = chests[numInRowStartIndex + 3].LocationY + 1,
                                        HighRecommand = true
                                    };
                                    break;
                                default:
                                    suggestChestLeft = new Chest
                                    {
                                        LocationX = chests[numInRowStartIndex].LocationX - 1,
                                        LocationY = chests[numInRowStartIndex].LocationY + 1,
                                        HighRecommand = true
                                    };
                                    suggestChestRight = new Chest
                                    {
                                        LocationX = chests[numInRowStartIndex + 3].LocationX + 1,
                                        LocationY = chests[numInRowStartIndex + 3].LocationY - 1,
                                        HighRecommand = true
                                    };
                                    break;
                            }

                            bool isPosAvailLeft = Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                                       suggestChestLeft.LocationX, suggestChestLeft.LocationY);

                            bool isPosAvailRight = Utilities.CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                                       suggestChestRight.LocationX, suggestChestRight.LocationY);

                            if (isPosAvailLeft)
                            {
                                suggestChestLeft.HighRecommand = true;
                                breakPoints.Add(suggestChestLeft);
                            }

                            if (isPosAvailRight)
                            {
                                suggestChestRight.HighRecommand = true;
                                breakPoints.Add(suggestChestRight);
                            }
                            #endregion
                        }
                        else if (numInRow < 4)
                        {
                            breakPoints =
                                   breakPoints.SkipWhile(
                                       c =>
                                           c.LocationX == 0 || c.LocationX == 10 || c.LocationY == 0 ||
                                           c.LocationY == 10).ToList();

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
                            stepXDiffTemp = chests[index + 1].LocationX - chests[index].LocationX;
                            stepYDiffTemp = chests[index + 1].LocationY - chests[index].LocationY;
                            index1Avail = Math.Abs(stepXDiffTemp) == 1 && Math.Abs(stepYDiffTemp) == 1;
                        }

                        if (processedCount > 2 || (processedCount == 2 && index1Avail)) //xx0x or x0xx
                        {
                            bool breakNextPointAvail = false;
                            Chest suggestChest = new Chest();

                            switch (lastStepDirection)
                            {
                                case "Xy00":
                                    suggestChest.LocationX = chests[routeQueue.Peek()].LocationX + 1;
                                    suggestChest.LocationY = chests[routeQueue.Peek()].LocationY + 1;
                                    suggestChest.HighRecommand = true;
                                    break;
                                case "Xy01":
                                    suggestChest.LocationX = chests[routeQueue.Peek()].LocationX + 1;
                                    suggestChest.LocationY = chests[routeQueue.Peek()].LocationY - 1;
                                    suggestChest.HighRecommand = true;
                                    break;
                                case "Xy10":
                                    suggestChest.LocationX = chests[routeQueue.Peek()].LocationX - 1;
                                    suggestChest.LocationY = chests[routeQueue.Peek()].LocationY + 1;
                                    suggestChest.HighRecommand = true;
                                    break;
                                case "Xy11":
                                    suggestChest.LocationX = chests[routeQueue.Peek()].LocationX - 1;
                                    suggestChest.LocationY = chests[routeQueue.Peek()].LocationY - 1;
                                    suggestChest.HighRecommand = true;
                                    break;
                            }

                            breakNextPointAvail = Utilities.CheckPositionAvailable(blueChestStore.Concat(redChestStore).ToList(),
                                suggestChest.LocationX, suggestChest.LocationY);

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
                            stepXDiffTemp = Math.Abs(chests[index + 1].LocationX - chests[index].LocationX);
                            stepYDiffTemp = Math.Abs(chests[index + 1].LocationY - chests[index].LocationY);
                            index1Avail = stepXDiffTemp == 1 && stepYDiffTemp == 1;
                        }

                        if (processedCount > 2 || (processedCount == 2 && index1Avail)) //xx00x or x00xx
                        {
                            bool breakNextPointAvail = false;
                            Chest suggestChest = new Chest();

                            switch (lastStepDirection)
                            {
                                case "Xy00":
                                    suggestChest.LocationX = chests[routeQueue.Peek()].LocationX + 2;
                                    suggestChest.LocationY = chests[routeQueue.Peek()].LocationY + 2;
                                    //     suggestChest.HighRecommand = true;
                                    break;
                                case "Xy01":
                                    suggestChest.LocationX = chests[routeQueue.Peek()].LocationX + 2;
                                    suggestChest.LocationY = chests[routeQueue.Peek()].LocationY - 2;
                                    //    suggestChest.HighRecommand = true;
                                    break;
                                case "Xy10":
                                    suggestChest.LocationX = chests[routeQueue.Peek()].LocationX - 2;
                                    suggestChest.LocationY = chests[routeQueue.Peek()].LocationY + 2;
                                    //  suggestChest.HighRecommand = true;
                                    break;
                                case "Xy11":
                                    suggestChest.LocationX = chests[routeQueue.Peek()].LocationX - 2;
                                    suggestChest.LocationY = chests[routeQueue.Peek()].LocationY - 2;
                                    //    suggestChest.HighRecommand = true;
                                    break;
                            }

                            breakNextPointAvail = Utilities.CheckPositionAvailable(blueChestStore.Concat(redChestStore).ToList(),
                                suggestChest.LocationX, suggestChest.LocationY);

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
            ddlLevel.Enabled = true;
            btnRegret.Enabled = true;
            this.CreateGraphics().Clear(this.BackColor);
            redTurn = true;
            blueChestStore = new List<Chest>();
            redChestStore = new List<Chest>();
            completeChestStore = new List<Chest>();
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

            chbOnePlayer.Enabled = false;
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

        private void ddlLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            gameLevel = ddlLevel.SelectedIndex;
        }
    }


}
