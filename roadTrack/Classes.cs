using ConvNetSharp;
using System;
using System.Collections.Generic;

namespace roadTrack
{
    public class Item
    {
        public IVolume Input { get; set; }

        public double Output { get; set; }

        public bool IsValidation { get; set; }
    }

    public class Entry
    {
        public double[] Input { get; set; }

        public double Output { get; set; }

        public override string ToString()
        {
            return "Output: " + Output;
        }
    }

    public class CircularBuffer<T>
    {
        private readonly T[] buffer;
        private int nextFree;

        public CircularBuffer(int capacity)
        {
            Capacity = capacity;
            Count = 0;
            buffer = new T[capacity];
        }

        public int Capacity { get; private set; }

        public int Count { get; private set; }

        public IEnumerable<T> Items
        {
            get { return buffer; }
        }

        public void Add(T o)
        {
            buffer[nextFree] = o;
            nextFree = (nextFree + 1) % buffer.Length;
            Count = Math.Min(Count + 1, Capacity);
        }
    }
}
