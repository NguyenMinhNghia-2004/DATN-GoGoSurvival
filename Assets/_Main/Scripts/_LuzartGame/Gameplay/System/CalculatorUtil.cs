using System;
using System.Collections.Generic;
using UnityEngine.Pool;
namespace Luzart
{
    public static class CalculatorUtil
    {
        /// <summary>
        /// Tính tổng Stat từ các Stat khác
        /// </summary>
        /// <param name="baseStats"></param>
        /// <param name="statsFactor"></param>
        /// <param name="resolvedStats"></param>
        /// <returns></returns>
        public static List<Stat> CalculateStats(List<Stat> baseStats, List<Stat> statsFactor, List<Stat> resolvedStats)
        {
            resolvedStats.Clear();
            return resolvedStats;
        }
        /// <summary>
        /// Lấy ra StatFactor từ Stat gốc
        /// </summary>
        /// <param name="statSubtract"></param>
        /// <param name="baseStats"></param>
        /// <param name="resolvedStat"></param>
        /// <returns></returns>
        public static List<Stat> GetStatFactor(List<Stat> statSubtract, List<Stat> baseStats, List<Stat> resolvedStat)
        {
            resolvedStat.Clear();
            return resolvedStat;
        }
        public static List<Stat> CalculateValueStatAdd(List<Stat> statsBase, List<Stat> statsFactor, List<Stat> solveStats)
        {
            solveStats.Clear();
            return solveStats;
        }
        public static List<Stat> CalculateValueStatAddList(List<Stat> statBase, List<List<Stat>> listlistStat, List<Stat> solveStats)
        {
            solveStats.Clear();
            return solveStats;
        }
    }
}