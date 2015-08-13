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
    public partial class Form1 : Form
    {
        private int numToWin = 5;
        private int initPositionOffset = 200;
        private int pace = 50;
        private int canvasSize = 1000;
        private int chestSize = 30;
        private bool redTurn;
        private static bool gameOver;
        private static List<Chest> blueChestStore;
        private static List<Chest> redChestStore;
        private enum Direction
        {
            X,
            MinusX,
            Y,
            MinusY,

            Xy00, //(1,  1)
            Xy01, //(1, -1)
            Xy10, //(-1, 1)
            Xy11  //(1, 1)

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
                EraseSolidCircle(chestX, chestY);
                SwitchTurn();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            lblColor.BackColor = Color.Red;
            lblColor.Text = "RED";
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (gameOver == false)
            {
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

                    if (!redTurn && !gameOver)
                    {
                        string initDirection = Direction.X.ToString();

                        AutoPlay(3, xLoc, yLoc, ref initDirection);
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

        private void PlayGame(int xLoc, int yLoc)
        {
            bool isChestExist = redChestStore.Any(c => c.Location_X == xLoc && c.Location_Y == yLoc) ||
                                blueChestStore.Any(c => c.Location_X == xLoc && c.Location_Y == yLoc);

            if (!isChestExist)
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

                PaintSolidCircle(chestX, chestY, redTurn);

                if (redChestStore.Count >= numToWin && redTurn)
                {
                    bool isRedWin = Judge(redChestStore, 5);

                    if (isRedWin)
                    {
                        MessageBox.Show("You Win!");
                        gameOver = true;
                        btnRegret.Enabled = false;
                    }
                }

                if (blueChestStore.Count >= numToWin && !redTurn)
                {
                    bool isBlueWin = Judge(blueChestStore, 5);

                    if (isBlueWin)
                    {
                        MessageBox.Show("Sorry you lose.");
                        gameOver = true;
                        btnRegret.Enabled = false;
                    }
                }

                SwitchTurn();
            }
        }

        private bool Judge(List<Chest> chestsStore, int numToCheck)
        {
            Stopwatch watch = Stopwatch.StartNew();

            List<Chest> judgeResult = new List<Chest>();
            int targetHorizon, targetVertical;

            IEnumerable<Chest> horizonSort = chestsStore.OrderBy(x => x.Location_X);
            IEnumerable<Chest> verticalSort = chestsStore.OrderBy(x => x.Location_Y);

            int mainLoop = 0;

            bool isWin = false;
            while (mainLoop < chestsStore.Count)
            {
                targetHorizon = horizonSort.ToArray()[mainLoop].Location_X;
                targetVertical = horizonSort.ToArray()[mainLoop].Location_Y;

                JudgeSingleNode(chestsStore, horizonSort, verticalSort, numToCheck, targetHorizon, targetVertical, ref isWin);

                if (isWin)
                {
                    break;
                }
                mainLoop++;
            }//while

            watch.Stop();

            Debug.WriteLine("TIME - " + watch.GetTimeString());

            return isWin;
        }

        private List<Chest> JudgeSingleNode(List<Chest> chestsStore, IEnumerable<Chest> horizonSort, IEnumerable<Chest> verticalSort, int numToAlert, int targetHorizon, int targetVertical, ref bool isWin)
        {
            bool xCheck = false;
            bool yCheck = false;
            bool xyCheck = false;
            List<Chest> chestOnEdge = new List<Chest>();

            List<Chest> chestOnEdgeX = new List<Chest>();
            List<Chest> chestOnEdgeY = new List<Chest>();
            List<Chest> chestOnEdgeXy = new List<Chest>();

            Chest tempChest = new Chest();

            bool isPosAvailRed, isPosAvailBlue;

            int[] x_arr = horizonSort.Where(c => c.Location_Y == targetVertical).Select(x => x.Location_X).ToArray();
            int[] y_arr = verticalSort.Where(c => c.Location_X == targetHorizon).Select(x => x.Location_Y).ToArray();

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

            Thread thX, thY, thXy;

            #region X
            if (x_arr.Count() >= numToAlert)
            {
                //thX = new Thread(unused => Compare(x_arr, ref xCheck));
                //thX.Name = "thread_CompareX";
                //thX.Start();

                Compare(x_arr, numToAlert, ref xCheck);

                if (xCheck)
                {
                    tempChest = new Chest { Location_X = x_arr[0] - 1, Location_Y = targetVertical };

                    isPosAvailRed = CheckPositionAvailability(redChestStore, tempChest.Location_X, tempChest.Location_Y);
                    isPosAvailBlue = CheckPositionAvailability(blueChestStore, tempChest.Location_X, tempChest.Location_Y);
                    if (isPosAvailBlue && isPosAvailRed)
                    {
                        chestOnEdgeX.Add(tempChest);
                    }

                    tempChest = new Chest { Location_X = x_arr[x_arr.Count() - 1] + 1, Location_Y = targetVertical };
                    isPosAvailRed = CheckPositionAvailability(redChestStore, tempChest.Location_X, tempChest.Location_Y);
                    isPosAvailBlue = CheckPositionAvailability(blueChestStore, tempChest.Location_X, tempChest.Location_Y);
                    if (isPosAvailBlue && isPosAvailRed)
                    {
                        chestOnEdgeX.Add(tempChest);
                    }
                }
            }
            #endregion X

            #region Y
            if (y_arr.Count() >= numToAlert)
            {
                //thY = new Thread(unused => Compare(y_arr, ref yCheck));
                //thY.Name = "thread_CompareY";
                //thY.Start();

                Compare(y_arr, numToAlert, ref yCheck);
                if (yCheck)
                {
                    tempChest = new Chest { Location_X = targetHorizon, Location_Y = y_arr[0] - 1 };
                    isPosAvailRed = CheckPositionAvailability(redChestStore, tempChest.Location_X, tempChest.Location_Y);
                    isPosAvailBlue = CheckPositionAvailability(blueChestStore, tempChest.Location_X, tempChest.Location_Y);
                    if (isPosAvailBlue && isPosAvailRed)
                    {
                        chestOnEdgeY.Add(tempChest);
                    }

                    tempChest = new Chest { Location_X = targetHorizon, Location_Y = y_arr[y_arr.Count() - 1] + 1 };
                    isPosAvailRed = CheckPositionAvailability(redChestStore, tempChest.Location_X, tempChest.Location_Y);
                    isPosAvailBlue = CheckPositionAvailability(blueChestStore, tempChest.Location_X, tempChest.Location_Y);
                    if (isPosAvailBlue && isPosAvailRed)
                    {
                        chestOnEdgeY.Add(tempChest);
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

            CompareXY(xy_arrCheck, numToAlert, ref xyCheck);

            if (xyCheck)
            {
                switch (direction)
                {
                    case "Xy00":
                        tempChest = new Chest
                        {
                            Location_X = xy_arrCheck[xy_arrCheck.Count() - 1].Location_X + 1,
                            Location_Y = xy_arrCheck[xy_arrCheck.Count() - 1].Location_Y + 1
                        };
                        isPosAvailRed = CheckPositionAvailability(redChestStore, tempChest.Location_X, tempChest.Location_Y);
                        isPosAvailBlue = CheckPositionAvailability(blueChestStore, tempChest.Location_X, tempChest.Location_Y);
                        if (isPosAvailBlue && isPosAvailRed)
                        {
                            chestOnEdgeXy.Add(tempChest);
                        }
                        break;
                    case "Xy11":

                        tempChest = new Chest
                        {
                            Location_X = xy_arrCheck[0].Location_X - 1,
                            Location_Y = xy_arrCheck[0].Location_Y - 1
                        };
                        isPosAvailRed = CheckPositionAvailability(redChestStore, tempChest.Location_X, tempChest.Location_Y);
                        isPosAvailBlue = CheckPositionAvailability(blueChestStore, tempChest.Location_X, tempChest.Location_Y);
                        if (isPosAvailBlue && isPosAvailRed)
                        {
                            chestOnEdgeXy.Add(tempChest);
                        }
                        break;
                    case "Xy01":
                        tempChest = new Chest
                        {
                            Location_X = xy_arrCheck[xy_arrCheck.Count() - 1].Location_X + 1,
                            Location_Y = xy_arrCheck[xy_arrCheck.Count() - 1].Location_Y - 1
                        };
                        isPosAvailRed = CheckPositionAvailability(redChestStore, tempChest.Location_X, tempChest.Location_Y);
                        isPosAvailBlue = CheckPositionAvailability(blueChestStore, tempChest.Location_X, tempChest.Location_Y);
                        if (isPosAvailBlue && isPosAvailRed)
                        {
                            chestOnEdgeXy.Add(tempChest);
                        }
                        break;
                    default: // 10
                        tempChest = new Chest
                        {
                            Location_X = xy_arrCheck[0].Location_X - 1,
                            Location_Y = xy_arrCheck[0].Location_Y + 1
                        };
                        isPosAvailRed = CheckPositionAvailability(redChestStore, tempChest.Location_X, tempChest.Location_Y);
                        isPosAvailBlue = CheckPositionAvailability(blueChestStore, tempChest.Location_X, tempChest.Location_Y);
                        if (isPosAvailBlue && isPosAvailRed)
                        {
                            chestOnEdgeXy.Add(tempChest);
                        }
                        break;
                }
            }

            #endregion XY

            if (chestOnEdgeX.Count == 2)
            {
                chestOnEdge = chestOnEdgeX;
            }
            else if (chestOnEdgeY.Count == 2)
            {
                chestOnEdge = chestOnEdgeY;
            }
            else if (chestOnEdgeXy.Count == 2)
            {
                chestOnEdge = chestOnEdgeXy;
            }

            chestOnEdge = chestOnEdgeX.Concat(chestOnEdgeY).Concat(chestOnEdgeXy).ToList();

            isWin = xCheck || yCheck || xyCheck;
            return chestOnEdge;
        }

        private void AutoPlay(int numToAlert, int targetHorizon, int targetVertical, ref string direction)
        {
            IEnumerable<Chest> horizonSort = redChestStore.OrderBy(x => x.Location_X);
            IEnumerable<Chest> verticalSort = redChestStore.OrderBy(x => x.Location_Y);

            bool isWin = false;
            List<Chest> redResult = JudgeSingleNode(redChestStore, horizonSort, verticalSort, numToAlert, targetHorizon, targetVertical, ref isWin);
            
            if (redResult.Count == 0) //If red is still ok, put BLUE in an random position close to previos RED
            {
                int blueCount = blueChestStore.Count;
                Chest lastBlue = blueChestStore[blueCount - 1];
                bool isBlue2InRow = false;

                List<Chest> blueResult = JudgeSingleNode(blueChestStore, blueChestStore.OrderBy(x => x.Location_X),
                    blueChestStore.OrderBy(x => x.Location_Y), 2, lastBlue.Location_X, lastBlue.Location_Y, ref isBlue2InRow);

                Random rdm = new Random();

                int rdmIndex = rdm.Next();

                PlayGame(blueResult[rdmIndex].Location_X, blueResult[rdmIndex].Location_Y);
            }
            else //if red is already 3 in a row
            {
                int count = redResult.Count;

                Random rdm = new Random();
                int rdmIndex = rdm.Next(0, count - 1);

                PlayGame(redResult[rdmIndex].Location_X, redResult[rdmIndex].Location_Y);
            }
        }

        private bool CheckPositionAvailability(List<Chest> chestStore, int x, int y)
        {
            return !chestStore.Any(
                       c => c.Location_X == x && c.Location_Y == y);
        }
        #region Single Direction Check
        private int Compare(int[] arrayInts, int numToAlert, ref bool isWin)
        {
            int count = numToAlert - 1;
            if (arrayInts.Count() >= numToAlert)
            {
                for (int index = 1; index < arrayInts.Count(); index++)
                {
                    if (arrayInts[index] - arrayInts[index - 1] == 1)
                    {
                        count--;
                        if (count == 0)
                        {
                            isWin = true;
                            return index;
                        }
                    }
                    else
                    {
                        count = numToAlert - 1;
                    }
                }
            }
            return 0;
        }

        private int CompareXY(Chest[] chests, int numToAlert, ref bool isWin)
        {
            int count = numToAlert - 1;

            int lastStepDirection = 0; // 0 init, -1 up, 1 down

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

                    if (Math.Abs(stepYDiff) == 1 && Math.Abs(stepXDiff) == 1)
                    {
                        lastStepDirection = stepYDiff;

                        count = (lastStepDirection == 0 || lastStepDirection == stepYDiff) ? count - 1 : numToAlert - 2;

                        if (count == 0)
                        {
                            isWin = true;
                            return index;
                        }
                    }
                    else
                    {
                        count = numToAlert - 1;
                    }
                }

            }
            return 0;
        }
        #endregion

        private void InitGame()
        {
            chbOnePlayer.Enabled = false;
            btnRegret.Enabled = true;
            lblColor.Visible = true;
            this.CreateGraphics().Clear(this.BackColor);
            redTurn = true;
            blueChestStore = new List<Chest>();
            redChestStore = new List<Chest>();
            gameOver = false;
            lblColor.BackColor = Color.Red;
            lblColor.Text = "RED";
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
            lblColor.BackColor = redTurn ? Color.Red : Color.Blue;
            lblColor.Text = redTurn ? "RED" : "BLUE";
        }

        private void PaintSolidCircle(int x, int y, bool isRedTurn)
        {
            SolidBrush solidBrush = isRedTurn ? new SolidBrush(Color.Red) : new SolidBrush(Color.Blue);

            this.CreateGraphics().FillEllipse(solidBrush, new Rectangle(x + initPositionOffset, y + initPositionOffset, 30, 30));
        }

        private void EraseSolidCircle(int x, int y)
        {
            SolidBrush solidBrush = new SolidBrush(this.BackColor);

            this.CreateGraphics().FillEllipse(solidBrush, new Rectangle(x + initPositionOffset, y + initPositionOffset, 30, 30));

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
