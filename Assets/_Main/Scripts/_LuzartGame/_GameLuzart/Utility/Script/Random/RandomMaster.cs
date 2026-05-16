using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public static class RandomMaster
{
    public static T RandomInList<T>(List<T> listRandom)
    {
        if (listRandom.Count == 0) return default;
        var indexRandom = Random.Range(0, listRandom.Count);
        return listRandom[indexRandom];
    }
    public static T RandomInList<T>(T[] listRandom)
    {
        var indexRandom = Random.Range(0, listRandom.Length);
        return listRandom[indexRandom];
    }
    public static bool RandomRate(float rate)
    {
        return Random.Range(0f, 100f) < rate;
    }
    public static int RandomRate(List<float> listRate)
    {
        var randomNumb = Random.Range(0f, 100f);
        var milestones = new List<float> { 0 };
        for (int i = 0; i < listRate.Count; i++)
        {
            milestones.Add(milestones[i] + listRate[i]);
            if (randomNumb >= milestones[i] && randomNumb < milestones[i + 1])
                return i;
        }
        return listRate.Count;
    }
    public static int RandomRate(float[] listRate)
    {
        var randomNumb = Random.Range(0f, 100f);
        var milestones = new List<float> { 0 };
        for (int i = 0; i < listRate.Length; i++)
        {
            milestones.Add(milestones[i] + listRate[i]);
            if (randomNumb >= milestones[i] && randomNumb < milestones[i + 1])
                return i;
        }
        return listRate.Length;
    }
    public static int RandomRangeExcept(int start, int end, List<int> excepts)
    {
        var r = Random.Range(start, end);
        if (excepts.Contains(r)) return RandomRangeExcept(start, end, excepts);
        else return r;
    }
}
public class RandomNoRepeat<T>
{
    /// <summary>
    /// Danh sach goc
    /// </summary>
    protected readonly List<T> listRandom;
    /// <summary>
    /// Danh sach cac item duoc Random
    /// </summary>
    public List<T> ListTemp { get; set; }
    public RandomNoRepeat(IEnumerable<T> listR)
    {
        listRandom = new List<T>(listR);
        ListTemp = new List<T>(listRandom);
    }
    public virtual T Random()
    {
        if (ListTemp.Count == 0)
        {
            ListTemp = new List<T>(listRandom);
        }
        var tempObj = RandomMaster.RandomInList(ListTemp);
        _ = ListTemp.Remove(tempObj);
        return tempObj;
    }
}
public class RandomRangeNoRepeat
{
    /// <summary>
    /// Gia tri bat dau cua range
    /// </summary>
    private readonly int startValue;
    /// <summary>
    /// Gia tri ket thuc cua range (exclusive)
    /// </summary>
    private readonly int endValue;
    /// <summary>
    /// Danh sach cac so chua duoc random
    /// </summary>
    public List<int> AvailableNumbers { get; private set; }
    /// <summary>
    /// Tong so luong trong range
    /// </summary>
    public int TotalCount => endValue - startValue;
    /// <summary>
    /// So luong con lai chua duoc random
    /// </summary>
    public int RemainingCount => AvailableNumbers.Count;
    /// <summary>
    /// Kiem tra xem da random het tat ca chua
    /// </summary>
    public bool IsCompleted => AvailableNumbers.Count == 0;
    /// <summary>
    /// Khoi tao RandomRangeNoRepeat
    /// </summary>
    /// <param name="start">Gia tri bat dau (inclusive)</param>
    /// <param name="end">Gia tri ket thuc (exclusive)</param>
    public RandomRangeNoRepeat(int start, int end)
    {
        if (start >= end)
        {
            Debug.LogError($"RandomRangeNoRepeat: start ({start}) phai nho hon end ({end})");
            startValue = 0;
            endValue = 1;
        }
        else
        {
            startValue = start;
            endValue = end;
        }
        Reset();
    }
    /// <summary>
    /// Random mot so trong range ma chua duoc chon
    /// </summary>
    /// <returns>So duoc random, hoac -1 neu da het</returns>
    public int Random()
    {
        if (AvailableNumbers.Count == 0)
        {
            Reset();
        }
        if (AvailableNumbers.Count == 0)
        {
            Debug.LogWarning("RandomRangeNoRepeat: Khong co so nao de random");
            return -1;
        }
        var randomIndex = UnityEngine.Random.Range(0, AvailableNumbers.Count);
        var selectedNumber = AvailableNumbers[randomIndex];
        AvailableNumbers.RemoveAt(randomIndex);
        return selectedNumber;
    }
    /// <summary>
    /// Reset lai danh sach ve trang thai ban dau
    /// </summary>
    public void Reset()
    {
        AvailableNumbers = new List<int>();
        for (int i = startValue; i < endValue; i++)
        {
            AvailableNumbers.Add(i);
        }
    }
    /// <summary>
    /// Xoa mot so khoi danh sach available (neu ton tai)
    /// </summary>
    /// <param name="number">So can xoa</param>
    /// <returns>True neu xoa thanh cong</returns>
    public bool RemoveNumber(int number)
    {
        return AvailableNumbers.Remove(number);
    }
    /// <summary>
    /// Them lai mot so vao danh sach available (neu nam trong range va chua ton tai)
    /// </summary>
    /// <param name="number">So can them</param>
    /// <returns>True neu them thanh cong</returns>
    public bool AddNumber(int number)
    {
        if (number >= startValue && number < endValue && !AvailableNumbers.Contains(number))
        {
            AvailableNumbers.Add(number);
            return true;
        }
        return false;
    }
    /// <summary>
    /// Kiem tra xem mot so co trong danh sach available khong
    /// </summary>
    /// <param name="number">So can kiem tra</param>
    /// <returns>True neu so con available</returns>
    public bool IsNumberAvailable(int number)
    {
        return AvailableNumbers.Contains(number);
    }
}