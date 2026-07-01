using UnityEngine;
using System.Collections.Generic;
using System;

public static class BallSkinStore
{
    private const string SELECTED_SKIN_KEY = "SelectedBallSkin";
    private const string UNLOCKED_SKIN_PREFIX = "UnlockedBallSkin_";

    // ===== Simple pricing config (tanpa isi harga satu-satu) =====
    // Skin index 0 selalu gratis (default).
    // 1..basicFlatLastIndex memakai harga sama.
    // Di atas itu harga naik bertahap.
    private const int basicFlatLastIndex = 5;
    private const int basicFlatPrice = 15;
    private const int progressiveStartPrice = 30;
    private const int progressiveStep = 10;
    private const int progressiveStepEverySkins = 2;

    // Optional override per index jika ada skin spesial tertentu.
    // Contoh: { 12, 120 } artinya skin index 12 harganya 120.
    private static readonly Dictionary<int, int> PriceOverrides = new Dictionary<int, int>();
    private static int[] runtimeCustomPriceList = Array.Empty<int>();
    private static bool hasRuntimeCustomPriceList;

    public static int GetPrice(int skinIndex)
    {
        if (skinIndex <= 0)
            return 0;

        if (hasRuntimeCustomPriceList &&
            runtimeCustomPriceList != null &&
            skinIndex >= 0 &&
            skinIndex < runtimeCustomPriceList.Length)
        {
            return Mathf.Max(0, runtimeCustomPriceList[skinIndex]);
        }

        if (PriceOverrides.TryGetValue(skinIndex, out int forcedPrice))
            return Mathf.Max(0, forcedPrice);

        if (skinIndex <= basicFlatLastIndex)
            return Mathf.Max(0, basicFlatPrice);

        int relative = skinIndex - (basicFlatLastIndex + 1);
        int stepGroup = relative / Mathf.Max(1, progressiveStepEverySkins);
        int price = progressiveStartPrice + (stepGroup * progressiveStep);

        return Mathf.Max(0, price);
    }

    public static void SetCustomPriceList(int[] prices, bool forceSkinZeroFree = true)
    {
        if (prices == null || prices.Length == 0)
        {
            hasRuntimeCustomPriceList = false;
            runtimeCustomPriceList = Array.Empty<int>();
            return;
        }

        runtimeCustomPriceList = (int[])prices.Clone();
        hasRuntimeCustomPriceList = true;

        if (forceSkinZeroFree && runtimeCustomPriceList.Length > 0)
            runtimeCustomPriceList[0] = 0;
    }

    public static void ClearCustomPriceList()
    {
        hasRuntimeCustomPriceList = false;
        runtimeCustomPriceList = Array.Empty<int>();
    }

    public static bool IsUnlocked(int skinIndex)
    {
        if (skinIndex <= 0)
            return true;

        return PlayerPrefs.GetInt(GetUnlockKey(skinIndex), 0) == 1;
    }

    public static void Unlock(int skinIndex)
    {
        if (skinIndex <= 0)
            return;

        PlayerPrefs.SetInt(GetUnlockKey(skinIndex), 1);
        PlayerPrefs.Save();
    }

    public static int GetSelectedSkinIndex()
    {
        int selected = Mathf.Max(0, PlayerPrefs.GetInt(SELECTED_SKIN_KEY, 0));
        if (!IsUnlocked(selected))
            return 0;

        return selected;
    }

    public static bool TrySelectUnlocked(int skinIndex)
    {
        if (!IsUnlocked(skinIndex))
            return false;

        PlayerPrefs.SetInt(SELECTED_SKIN_KEY, Mathf.Max(0, skinIndex));
        PlayerPrefs.Save();
        return true;
    }

    public static bool TryBuyAndSelectSkin(int skinIndex, out string message)
    {
        message = string.Empty;
        int selectedBefore = GetSelectedSkinIndex();

        if (skinIndex < 0)
        {
            message = "Skin index tidak valid.";
            return false;
        }

        if (IsUnlocked(skinIndex))
        {
            TrySelectUnlocked(skinIndex);
            message = $"Skin {skinIndex} sudah unlocked.";
            return true;
        }

        if (!TryBuySkin(skinIndex, out message))
        {
            RestoreSelectedSkin(selectedBefore);
            return false;
        }

        TrySelectUnlocked(skinIndex);
        message = $"Skin {skinIndex} dipakai.";
        return true;
    }

    public static bool TryBuySkin(int skinIndex, out string message)
    {
        message = string.Empty;
        int selectedBefore = GetSelectedSkinIndex();

        if (skinIndex < 0)
        {
            message = "Skin index tidak valid.";
            return false;
        }

        if (IsUnlocked(skinIndex))
        {
            message = $"Skin {skinIndex} sudah dibeli.";
            return true;
        }

        int price = GetPrice(skinIndex);
        if (!CoinWallet.TrySpendCoins(price))
        {
            int current = CoinWallet.GetCoins();
            int missing = Mathf.Max(0, price - current);
            message = $"Coin kurang. Butuh {missing} coin lagi.";
            RestoreSelectedSkin(selectedBefore);
            return false;
        }

        Unlock(skinIndex);
        message = $"Berhasil beli skin {skinIndex} ({price} coin).";
        return true;
    }

    public static void RestoreSelectedSkin(int skinIndex)
    {
        int safeIndex = Mathf.Max(0, skinIndex);
        if (!IsUnlocked(safeIndex))
            safeIndex = 0;

        PlayerPrefs.SetInt(SELECTED_SKIN_KEY, safeIndex);
        PlayerPrefs.Save();
    }

    public static void ResetUnlockedSkins(int maxSkinIndex, bool resetSelectedSkin = true)
    {
        int safeMax = Mathf.Max(0, maxSkinIndex);
        for (int i = 1; i <= safeMax; i++)
            PlayerPrefs.DeleteKey(GetUnlockKey(i));

        if (resetSelectedSkin)
            PlayerPrefs.DeleteKey(SELECTED_SKIN_KEY);

        PlayerPrefs.Save();
    }

    private static string GetUnlockKey(int skinIndex)
    {
        return $"{UNLOCKED_SKIN_PREFIX}{Mathf.Max(0, skinIndex)}";
    }
}
