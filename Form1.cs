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
        private int initPositionOffset = 200;
        private int pace = 50;
        private int canvasSize = 1000;
        private int chestSize = 30;
        private bool redTurn;
        private static bool gameOver;
        private static List<Chest> blueChestStore;
        private static List<Chest> redChestStore;

        #region Events
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            InitGame();
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
                #region In game

                int x = e.Location.X - initPositionOffset;
                int y = e.Location.Y - initPositionOffset;

                int xLoc = x / pace;
                int xOffset = x % pace;
                int yLoc = y / pace;
                int yOffset = y % pace;

                xLoc = (xOffset > pace / 2) ? xLoc + 1 : xLoc;
                yLoc = (yOffset > pace / 2) ? yLoc + 1 : yLoc;

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

                    if (redChestStore.Count >= 5 && redTurn)
                    {
                        bool isRedWin = Judge(redChestStore);

                        if (isRedWin)
                        {
                            MessageBox.Show("You Win!");
                            gameOver = true;
                        }
                    }

                    if (blueChestStore.Count >= 5 && !redTurn)
                    {
                        bool isBlueWin = Judge(blueChestStore);

                        if (isBlueWin)
                        {
                            MessageBox.Show("Sorry you lose.");
                            gameOver = true;
                        }
                    }

                    SwitchTurn();
                }

                #endregion
            }
            else
            {
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

        private bool Judge(List<Chest> chests)
        {
            Stopwatch watch = Stopwatch.StartNew();

            bool xCheck = false;
            bool yCheck = false;
            bool xyCheck = false;

            IEnumerable<Chest> horizonSort = chests.OrderBy(x => x.Location_X);
            IEnumerable<Chest> verticalSort = chests.OrderBy(x => x.Location_Y);

            int targetVertical;
            int targetHorizon;
            int mainLoop = 0;

            while (mainLoop < chests.Count)
            {
                targetVertical = horizonSort.ToArray()[mainLoop].Location_Y;
                targetHorizon = horizonSort.ToArray()[mainLoop].Location_X;

                int[] x_arr = horizonSort.Where(c => c.Location_Y == targetVertical).Select(x => x.Location_X).ToArray();
                int[] y_arr = verticalSort.Where(c => c.Location_X == targetHorizon).Select(x => x.Location_Y).ToArray();

                Chest[] xy_arr = chests.Where(c => (c.Location_X == targetHorizon && c.Location_Y == targetVertical) || (c.Location_X != targetHorizon && c.Location_Y != targetVertical)).OrderBy(c => c.Location_X).ToArray();
                Chest[] xy_arrDown = xy_arr.Where(c => (c.Location_X == targetHorizon && c.Location_Y == targetVertical) || c.Location_Y > targetVertical).OrderBy(c => c.Location_Y).ToArray();
                Chest[] xy_arrUp = xy_arr.Where(c => (c.Location_X == targetHorizon && c.Location_Y == targetVertical) || c.Location_Y < targetVertical).OrderByDescending(c => c.Location_Y).ToArray();

                Thread thX, thY, thXy;

                if (x_arr.Count() >= 5)
                {
                    thX = new Thread(unused => Compare(x_arr, ref xCheck));
                    thX.Name = "thread_CompareX";
                    thX.Start();

                    //   Compare(x_arr, ref xCheck);
                    if (xCheck)
                    {
                        break;
                    }
                }

                if (y_arr.Count() >= 5)
                {
                    thY = new Thread(unused => Compare(y_arr, ref yCheck));
                    thY.Name = "thread_CompareY";
                    thY.Start();

                    // Compare(y_arr, ref yCheck);

                    if (yCheck)
                    {
                        break;
                    }
                }

                if (xy_arr.Count() >= 5)
                {
                    if (xy_arrUp.Count() >= 5)
                    {
                        thXy = new Thread(unused => CompareXY(xy_arrUp, ref xyCheck));
                        thXy.Name = "thread_CompareXY";
                        thXy.Start();

                        // CompareXY(xy_arrUp, ref xyCheck);
                        if (xyCheck)
                        {
                            break;
                        }
                    }
                    else if (xy_arrDown.Count() >= 5)
                    {
                        thXy = new Thread(unused => CompareXY(xy_arrDown, ref xyCheck));
                        thXy.Start();
                        // CompareXY(xy_arrDown, ref xyCheck);
                        if (xyCheck)
                        {
                            break;
                        }
                    }
                }

                mainLoop++;
            }

            watch.Stop();

            Debug.WriteLine("TIME - " + watch.GetTimeString());
            return xCheck || yCheck || xyCheck;
        }

        #region Single Direction Check
        private bool Compare(int[] arrayInts, ref bool isWin)
        {
            if (arrayInts.Count() >= 5)
            {
                int count = 4;

                for (int index = 1; index < arrayInts.Count(); index++)
                {
                    if (arrayInts[index] - 1 == arrayInts[index - 1])
                    {
                        count--;
                        if (count == 0)
                        {
                            isWin = true;
                            return isWin;
                        }
                    }
                    else
                    {
                        count = 4;
                    }
                }
            }
            return isWin;
        }

        private bool CompareXY(Chest[] chests, ref bool isWin)
        {
            if (chests.Count() >= 5)
            {
                int count = 4;
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

                            count = (lastStepDirection == 0 || lastStepDirection == stepYDiff) ? count - 1 : 3;

                            if (count == 0)
                            {
                                isWin = true;
                                return isWin;
                            }
                        }
                        else
                        {
                            count = 4;
                        }
                    }
                }
            }
            return isWin;
        }
        #endregion

        private void InitGame()
        {
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

        private void eraseSolidCircle(int x, int y)
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
                eraseSolidCircle(chestX, chestY);
                SwitchTurn();
            }
        }

    }
}
