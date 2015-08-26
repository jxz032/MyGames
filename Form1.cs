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

        private List<Chest> Judge(List<Chest> chestsStore, int numToCheck, ref bool isWin)
        {
            Stopwatch watch = Stopwatch.StartNew();

            List<Chest> suggestedChest = new List<Chest>();

            int targetHorizon, targetVertical;

            IEnumerable<Chest> horizonSort = chestsStore.OrderBy(x => x.Location_X);
            IEnumerable<Chest> verticalSort = chestsStore.OrderBy(x => x.Location_Y);

            int mainLoop = 0;

            while (mainLoop < chestsStore.Count)
            {
                targetHorizon = horizonSort.ToArray()[mainLoop].Location_X;
                targetVertical = horizonSort.ToArray()[mainLoop].Location_Y;

                suggestedChest = JudgeSingleNode(chestsStore, horizonSort, verticalSort, numToCheck, targetHorizon, targetVertical, ref isWin);

                if (isWin)
                {
                    return suggestedChest;
                }
                mainLoop++;
            }//while

            watch.Stop();

            Debug.WriteLine("TIME - " + watch.GetTimeString());

            return suggestedChest;
        }

        private List<Chest> JudgeSingleNode(List<Chest> chestsStore, IEnumerable<Chest> horizonSort, IEnumerable<Chest> verticalSort, int numToAlert, int targetHorizon, int targetVertical, ref bool isWin)
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

            BreakPointInfo breakPointX = new BreakPointInfo();
            BreakPointInfo breakPointY = new BreakPointInfo();
            BreakPointInfo breakPointXy = new BreakPointInfo();
            bool isPosAvail;

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

                breakPointX = Compare(x_arr, numToAlert, ref xCheck);

                #region if xx0x

                if (breakPointX.ProcessedCount > 2 && breakPointX.SuggestChest != null)
                {
                    chestOnEdgeX.Add(breakPointX.SuggestChest);
                }

                #endregion

                #region if 3 in row
                if (xCheck)
                {
                    tempChestLeft = new Chest
                    {
                        Location_X = x_arr[0].Location_X - 1,
                        Location_Y = targetVertical
                    };
                    tempChestRight = new Chest
                    {
                        Location_X = x_arr[x_arr.Count() - 1].Location_X + 1,
                        Location_Y = targetVertical
                    };
                    isPosAvail = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                       tempChestLeft.Location_X, tempChestLeft.Location_Y);

                    if (isPosAvail)
                    {
                        chestOnEdgeX.Add(tempChestLeft);
                    }

                    isPosAvail = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                        tempChestRight.Location_X, tempChestRight.Location_Y);

                    if (isPosAvail)
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

                breakPointY = Compare(y_arr, numToAlert, ref yCheck);

                #region if xx0x

                if (breakPointY.ProcessedCount > 2 && breakPointY.SuggestChest != null)
                {
                    chestOnEdgeY.Add(breakPointY.SuggestChest);
                }

                #endregion

                if (yCheck)
                {
                    tempChestLeft = new Chest { Location_X = targetHorizon, Location_Y = y_arr[0].Location_Y - 1 };
                    tempChestRight = new Chest { Location_X = targetHorizon, Location_Y = y_arr[y_arr.Count() - 1].Location_Y + 1 };
                    isPosAvail = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), tempChestLeft.Location_X, tempChestLeft.Location_Y);

                    if (isPosAvail)
                    {
                        chestOnEdgeY.Add(tempChestLeft);
                    }

                    isPosAvail = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), tempChestRight.Location_X, tempChestRight.Location_Y);

                    if (isPosAvail)
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

            breakPointXy = CompareXY(xy_arrCheck, numToAlert, ref xyCheck);

            #region if xx0x

            if (breakPointXy.ProcessedCount > 2 && breakPointXy.SuggestChest != null)
            {
                chestOnEdgeXy.Add(breakPointXy.SuggestChest);
            }

            #endregion

            if (xyCheck)
            {
                switch (direction)
                {
                    case "Xy00":
                    case "Xy11":
                        tempChestRight = new Chest
                        {
                            Location_X = xy_arrCheck[xy_arrCheck.Count() - 1].Location_X + 1,
                            Location_Y = xy_arrCheck[xy_arrCheck.Count() - 1].Location_Y + 1
                        };

                        tempChestLeft = new Chest
                        {
                            Location_X = xy_arrCheck[0].Location_X - 1,
                            Location_Y = xy_arrCheck[0].Location_Y - 1
                        };
                        isPosAvail = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), tempChestRight.Location_X, tempChestRight.Location_Y);

                        if (isPosAvail)
                        {
                            chestOnEdgeXy.Add(tempChestRight);
                        }

                        isPosAvail = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), tempChestLeft.Location_X, tempChestLeft.Location_Y);

                        if (isPosAvail)
                        {
                            chestOnEdgeXy.Add(tempChestLeft);
                        }
                        break;

                    default:
                        tempChestRight = new Chest
                        {
                            Location_X = xy_arrCheck[xy_arrCheck.Count() - 1].Location_X + 1,
                            Location_Y = xy_arrCheck[xy_arrCheck.Count() - 1].Location_Y - 1
                        };
                        tempChestLeft = new Chest
                        {
                            Location_X = xy_arrCheck[0].Location_X - 1,
                            Location_Y = xy_arrCheck[0].Location_Y + 1
                        };
                        isPosAvail = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), tempChestRight.Location_X, tempChestRight.Location_Y);

                        if (isPosAvail)
                        {
                            chestOnEdgeXy.Add(tempChestRight);
                        }

                        isPosAvail = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(), tempChestLeft.Location_X, tempChestLeft.Location_Y);

                        if (isPosAvail)
                        {
                            chestOnEdgeXy.Add(tempChestLeft);
                        }
                        break;
                }
            }

            #endregion XY

            if (chestOnEdgeX.Count == 1 && breakPointX.SuggestChest == null && breakPointX.ProcessedCount != 4) chestOnEdgeX = new List<Chest>();

            if (chestOnEdgeY.Count == 1 && breakPointY.SuggestChest == null && breakPointY.ProcessedCount != 4) chestOnEdgeY = new List<Chest>();

            if (chestOnEdgeXy.Count == 1 && breakPointXy.SuggestChest == null && breakPointXy.ProcessedCount != 4) chestOnEdgeXy = new List<Chest>();

            chestOnEdge = chestOnEdgeX.Concat(chestOnEdgeY).Concat(chestOnEdgeXy).ToList();

            isWin = xCheck || yCheck || xyCheck;
            return chestOnEdge;
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

            List<Chest> redResult = JudgeSingleNode(redChestStore, horizonSort, verticalSort, numToAlert, targetHorizon,
                targetVertical, ref isWin);

            List<Chest> blue3InRowResult = Judge(blueChestStore, 3, ref isBlue3InRow);
            List<Chest> blue2InRowResult = Judge(blueChestStore, 2, ref isBlue2InRow);

            if (redResult.Count == 0) //If red is still ok, put BLUE in an random position close to previos RED
            {
                #region no suggestion
                int blueCount = blueChestStore.Count;
                Chest lastBlue = blueCount == 0 ? redChestStore[redChestStore.Count - 1] : blueChestStore[blueCount - 1];

                if (blueCount > 2)
                {
                    List<Chest> suggestedResult;

                    if (isBlue3InRow && blue3InRowResult.Count != 0)
                    {
                        suggestedResult = blue3InRowResult;
                    }
                    else if (isBlue2InRow && blue2InRowResult.Count != 0)
                    {
                        suggestedResult = blue2InRowResult;
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
            else //if red is already 3 in a row
            {
                Random rdm = new Random();
                int rdmIndex;

                if (blue3InRowResult.Count == 0)
                {
                    rdmIndex = rdm.Next(0, redResult.Count);
                    PlayGame(redResult[rdmIndex].Location_X, redResult[rdmIndex].Location_Y);
                }
                else
                {
                    rdmIndex = rdm.Next(0, blue3InRowResult.Count);
                    PlayGame(blue3InRowResult[rdmIndex].Location_X, blue3InRowResult[rdmIndex].Location_Y);
                }
            }
        }

        private bool CheckPositionAvailable(List<Chest> chestStore, int x, int y)
        {
            bool isWithinCanvas = x >= 0 && x <= 10 && y >= 0 && y <= 10;
            return isWithinCanvas && !chestStore.Any(c => c.Location_X == x && c.Location_Y == y);
        }

        #region Single Direction Check
        private BreakPointInfo Compare(Chest[] chests, int numToAlert, ref bool isWin)
        {
            int[] arrayInts;
            bool isX = false;

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
            int count = numToAlert - 1;
            BreakPointInfo indexWhereBreaks = new BreakPointInfo();

            if (chests.Count() >= numToAlert)
            {
                for (int index = 1; index < arrayInts.Count(); index++)
                {
                    if (arrayInts[index] - arrayInts[index - 1] == 1)
                    {
                        processedCount++;
                        count--;
                        if (count == 0)
                        {
                            isWin = true;
                        }
                    }
                    else if (arrayInts[index] - arrayInts[index - 1] == 2)
                    {
                        processedCount++;

                        Chest suggestChest = isX ? new Chest { Location_X = arrayInts[index] - 1, Location_Y = chests[0].Location_Y } : new Chest { Location_X = chests[0].Location_X, Location_Y = arrayInts[index] - 1 };

                        bool breakNextPointAvail = CheckPositionAvailable(redChestStore.Concat(blueChestStore).ToList(),
                            suggestChest.Location_X, suggestChest.Location_Y);

                        indexWhereBreaks = breakNextPointAvail ? new BreakPointInfo { Index = index, SuggestChest = suggestChest } : new BreakPointInfo();
                        count = numToAlert - 1;
                    }
                }
            }

            indexWhereBreaks.ProcessedCount = processedCount;
            return indexWhereBreaks;
        }

        private BreakPointInfo CompareXY(Chest[] chests, int numToAlert, ref bool isWin)
        {
            int count = numToAlert - 1;
            int processedCount = 1;
            BreakPointInfo indexWhereBreaks = new BreakPointInfo();

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

                    if (Math.Abs(stepYDiff) == 1 && Math.Abs(stepXDiff) == 1)
                    {
                        count--;
                        processedCount++;

                        if (count == 0)
                        {
                            isWin = true;
                        }
                    }
                    else if (Math.Abs(stepYDiff) == 2 && Math.Abs(stepXDiff) == 2)
                    {
                        processedCount++;

                        bool breakNextPointAvail = false;
                        Chest suggestChest = new Chest();

                        switch (lastStepDirection)
                        {
                            case "Xy00":
                                suggestChest.Location_X = chests[routeQueue.Peek()].Location_X + 1;
                                suggestChest.Location_Y = chests[routeQueue.Peek()].Location_Y + 1;
                                break;
                            case "Xy01":
                                suggestChest.Location_X = chests[routeQueue.Peek()].Location_X + 1;
                                suggestChest.Location_Y = chests[routeQueue.Peek()].Location_Y - 1;
                                break;
                            case "Xy10":
                                suggestChest.Location_X = chests[routeQueue.Peek()].Location_X - 1;
                                suggestChest.Location_Y = chests[routeQueue.Peek()].Location_Y + 1;
                                break;
                            case "Xy11":
                                suggestChest.Location_X = chests[routeQueue.Peek()].Location_X - 1;
                                suggestChest.Location_Y = chests[routeQueue.Peek()].Location_Y - 1;
                                break;
                        }

                        breakNextPointAvail = CheckPositionAvailable(blueChestStore.Concat(redChestStore).ToList(),
                            suggestChest.Location_X, suggestChest.Location_Y);
                        indexWhereBreaks = breakNextPointAvail ? new BreakPointInfo { Index = index, SuggestChest = suggestChest } : new BreakPointInfo();
                        count = numToAlert - 1;
                    }
                    else
                    {
                        count = numToAlert - 1;
                    }
                }

            }

            indexWhereBreaks.ProcessedCount = processedCount;
            return indexWhereBreaks;
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
            Icon newIcon = isRedTurn ? new Icon(Directory.GetCurrentDirectory() + @"..\..\..\img\furRed.ico") : new Icon(Directory.GetCurrentDirectory() + @"..\..\..\img\catBlue.ico");
            gfx.DrawIcon(newIcon, rect);
        }

        private void EraseChest(int x, int y)
        {
            SolidBrush solidBrush = new SolidBrush(this.BackColor);

            this.CreateGraphics().FillEllipse(solidBrush, new Rectangle(x + initPositionOffset, y + initPositionOffset, chestSize, chestSize));

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
