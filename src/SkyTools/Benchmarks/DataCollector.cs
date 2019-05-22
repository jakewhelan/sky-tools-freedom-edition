﻿// <copyright file="DataCollector.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace SkyTools.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// An utility class that collects the performance data of the methods.
    /// </summary>
    internal sealed class DataCollector
    {
        private readonly object syncObject = new object();
        private readonly Dictionary<MethodInfo, MethodPerformance> counters = new Dictionary<MethodInfo, MethodPerformance>();
        private readonly List<Snapshot> snapshots = new List<Snapshot>();

        private readonly int averagingWindow;

        /// <summary>Initializes a new instance of the <see cref="DataCollector"/> class.</summary>
        /// <param name="averagingWindow">The averaging window size for statistics.</param>
        public DataCollector(int averagingWindow)
        {
            this.averagingWindow = averagingWindow;
        }

        /// <summary>Records a sample for the specified <paramref name="method"/>.</summary>
        /// <param name="method">The method to record the performance sample for.</param>
        /// <param name="elapsed">The method execution time in ticks.</param>
        public void RecordSample(MethodInfo method, long elapsed)
        {
            if (method == null)
            {
                return;
            }

            lock (syncObject)
            {
                if (!counters.TryGetValue(method, out MethodPerformance stats))
                {
                    stats = new MethodPerformance(averagingWindow);
                    counters.Add(method, stats);
                }

                stats.AddSample(elapsed);
            }
        }

        /// <summary>Clears all data this instance currently holds.</summary>
        public void Clear()
        {
            lock (syncObject)
            {
                counters.Clear();
            }

            snapshots.Clear();
        }

        /// <summary>Makes a snapshot of the data this instance currently holds
        /// and adds this snapshot in the snapshots history.</summary>
        public void MakeSnapshot()
        {
            var snapshotData = new Dictionary<MethodInfo, long[]>();
            lock (syncObject)
            {
                foreach (var item in counters)
                {
                    snapshotData[item.Key] = item.Value.GetSnapshot();
                }
            }

            var snapshot = new Snapshot();
            foreach (var item in snapshotData)
            {
                snapshot.Data[item.Key] = MethodSnapshot.Calculate(item.Value);
            }

            snapshots.Add(snapshot);
        }

        /// <summary>Dumps all snapshots contained in the snapshots history to a semicolon-separated string .</summary>
        /// <param name="methods">The methods to dump the information for.</param>
        /// <returns>A semicolon-separated string containing the recorded performance data.</returns>
        public string Dump(IEnumerable<MethodInfo> methods)
        {
            var sb = new StringBuilder(1024);
            foreach (var snapshot in snapshots)
            {
                foreach (var method in methods)
                {
                    if (snapshot.Data.TryGetValue(method, out MethodSnapshot data))
                    {
                        sb.Append(data.SamplesCount);
                        sb.Append(';');
                        sb.Append(data.Average);
                        sb.Append(';');
                        sb.Append(data.Median);
                        sb.Append(';');
                        sb.Append(data.Max);
                        sb.Append(';');
                    }
                    else
                    {
                        sb.Append(";;;;");
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private readonly struct MethodSnapshot
        {
            private MethodSnapshot(int samplesCount, long average, long median, long max)
            {
                SamplesCount = samplesCount;
                Average = average;
                Median = median;
                Max = max;
            }

            public int SamplesCount { get; }

            public long Average { get; }

            public long Median { get; }

            public long Max { get; }

            public static MethodSnapshot Calculate(long[] data)
            {
                if (data.Length == 0)
                {
                    return default;
                }

                Array.Sort(data);
                long median;
                if (data.Length % 2 == 0)
                {
                    int middle = data.Length / 2;
                    median = (data[middle - 1] + data[middle]) / 2;
                }
                else
                {
                    median = data[data.Length / 2];
                }

                long max = long.MinValue;
                long avg = 0;
                for (int i = 0; i < data.Length; ++i)
                {
                    if (data[i] > max)
                    {
                        max = data[i];
                    }

                    avg += data[i];
                }

                return new MethodSnapshot(data.Length, avg / data.Length, median, max);
            }
        }

        private sealed class Snapshot
        {
            public Dictionary<MethodInfo, MethodSnapshot> Data { get; } = new Dictionary<MethodInfo, MethodSnapshot>();
        }

        private sealed class MethodPerformance
        {
            private readonly long[] samples;
            private int next;

            public MethodPerformance(int averagingWindow)
            {
                samples = new long[averagingWindow];
            }

            public long[] GetSnapshot()
            {
                int count = next;
                long[] result = new long[count];
                if (count > 0)
                {
                    Array.Copy(samples, result, count);
                }

                next = 0;
                return result;
            }

            public void AddSample(long elapsed)
            {
                if (next < samples.Length)
                {
                    samples[next++] = elapsed;
                }
            }
        }
    }
}
