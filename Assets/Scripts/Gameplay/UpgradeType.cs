namespace Gameplay
{
    public enum UpgradeType
    {
        // --- Yağmur & Bulut ---
        DropValue,              // Her damlanın değeri
        RainFrequency,          // Yağmur sıklığı (interval azalır)
        RainingCloudChance,     // Yağmurlu bulut oranı
        CloudCount,             // Ekrandaki bulut sayısı
        GoldenDropChance,       // Altın damla düşme şansı
        StormFrequency,         // Fırtına bulutu sıklığı
        StormDuration,          // Fırtına bulutu süresi

        // --- Kova ---
        BucketSize,             // Kapasite + toplama alanı

        // --- Oyuncu ---
        PlayerSpeed,            // Hareket hızı

        // --- Yıldırım & Mıknatıs ---
        LightningFrequency,     // Yıldırım düşme sıklığı
        MagnetDuration,         // Mıknatıs efektinin süresi
        MagnetRange,            // Mıknatıs çekim yarıçapı

        // --- Depo ---
        DepotCapacity,          // Depodaki max su kapasitesi
        DepotDrainSpeed,        // Kovadan depoya su aktarım hızı
        AutoCollectorCount,     // Yerleştirilebilecek sabit kova sayısı
        AutoCollectorCapacity,  // Sabit kovaların kapasitesi
        AutoCollectorSendSpeed, // Sabit kova → depo gönderim hızı

        // --- Ekonomi ---
        CurrencyMultiplier,     // Tüm gelire uygulanan çarpan
        ComboDecayTime,         // Kombo süre aşımı (sıfırlanmadan önce bekleme)
        ComboMaxMultiplier,     // Kombo sistemi max çarpan
        CriticalChance,         // Çift para şansı
        InterestRate,           // Toplam currency üzerinden saniyede % faiz
        PassiveIncome           // Saniyede otomatik para kazanımı
    }
}
