using UnityEngine;
using System;

public static class CoinWallet
{
    private const string COIN_KEY = "PlayerCoins";

    public static event Action<int> CoinsChanged;

    public static int GetCoins()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(COIN_KEY, 0));
    }

    public static bool HasSavedCoins()
    {
        return PlayerPrefs.HasKey(COIN_KEY);
    }

    public static void EnsureInitialized(int defaultCoins)
    {
        if (HasSavedCoins())
            return;

        SetCoins(defaultCoins);
    }

    public static void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        int next = GetCoins() + amount;
        PlayerPrefs.SetInt(COIN_KEY, next);
        PlayerPrefs.Save();
        CoinsChanged?.Invoke(next);
    }

    public static bool TrySpendCoins(int amount)
    {
        if (amount <= 0)
            return true;

        int current = GetCoins();
        if (current < amount)
            return false;

        int next = current - amount;
        PlayerPrefs.SetInt(COIN_KEY, next);
        PlayerPrefs.Save();
        CoinsChanged?.Invoke(next);
        return true;
    }

    public static void SetCoins(int amount)
    {
        int clamped = Mathf.Max(0, amount);
        PlayerPrefs.SetInt(COIN_KEY, clamped);
        PlayerPrefs.Save();
        CoinsChanged?.Invoke(clamped);
    }
}
