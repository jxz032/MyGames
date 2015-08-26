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
    }

    public class Chest
    {
        public int Location_X { get; set; }
        public int Location_Y { get; set; }

    }

    public class BreakPointInfo
    {
        public int Index { get; set; }
        public int ProcessedCount { get; set; }
        public Chest SuggestChest { get; set; }
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
