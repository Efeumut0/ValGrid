using System;
using System.Collections.Generic;
using ValGrid.Properties;

namespace ValGrid.Helpers;

/// <summary>
/// Centralized localization manager providing strong Turkish (tr) and English (en) support across ValGrid.
/// </summary>
public static class L10n
{
    public static bool IsEnglish =>
        string.Equals(Settings.Default.Language, "en", StringComparison.OrdinalIgnoreCase);

    public static bool IsTurkish => !IsEnglish;

    /// <summary>
    /// Returns the exact valorant-api.com language parameter for API queries.
    /// </summary>
    public static string ValApiLanguage => IsEnglish ? "en-US" : "tr-TR";

    public static string CurrentCode => IsEnglish ? "en" : "tr";

    private static readonly Dictionary<string, (string TR, string EN)> Strings = new(StringComparer.OrdinalIgnoreCase)
    {
        // --- Home & Nav View ---
        { "HomeTitle", ("Ana Sayfa", "Home") },
        { "YourParty", ("GRUBUNUZ", "YOUR PARTY") },
        { "Status", ("DURUM", "STATUS") },
        { "ValorantStatus", ("Valorant", "Valorant") },
        { "AccountStatus", ("Hesap", "Account") },
        { "MatchStatus", ("Maç", "Match") },
        { "Refresh", ("Yenile", "Refresh") },
        { "RefreshingIn", ("Yenileniyor:", "Refreshing In:") },
        { "ListButton", ("Liste", "Directory") },
        { "ListToolTip", ("Oyuncu Rehberi & Liste\nKız oyuncular, not eklenenler ve önceden denk geldiklerin", "Player Directory & List\nFemale players, noted players, and past encounters") },
        { "CareerButton", ("Kariyer", "Career") },
        { "CareerToolTip", ("Kariyer & Maç Geçmişi\nOynanan maçlar, takımların envanterleri ve oyuncu notları", "Career & Match History\nMatches played, team loadouts, and player notes") },
        { "NavPlayerList", ("Liste", "Directory") },
        { "NavPlayerListTooltip", ("Oyuncu Rehberi & Liste\nKız oyuncular, not eklenenler ve önceden denk geldiklerin", "Player Directory & List\nFemale players, noted players, and past encounters") },
        { "NavCareer", ("Kariyer", "Career") },
        { "NavCareerTooltip", ("Kariyer & Maç Geçmişi\nOynanan maçlar, takımların envanterleri ve oyuncu notları", "Career & Match History\nMatches played, team loadouts, and player notes") },
        { "NavInfoTooltip", ("Hakkında & Bilgi", "About & Info") },
        { "NavSettingsTooltip", ("Ayarlar", "Settings") },
        { "StoreButton", ("Günlük Mağaza", "Daily Store") },
        { "StoreToolTip", ("Valorant Günlük Mağazanız ve Kalan Süre\nRiot Client açık değilse otomatik başlatılır", "Valorant Daily Store & Remaining Time\nAutomatically launches Riot Client if not running") },
        { "EmptyPartyTitle", ("Grup Bekleniyor", "Waiting for Party") },
        { "EmptyPartyDesc", ("Valorant açıkken lobi veya grup üyelerinizin envanterleri burada canlı olarak listelenir.", "When Valorant is running, lobby or party members' loadouts will be listed live here.") },

        // --- Store View ---
        { "StoreTitle", ("GÜNLÜK MAĞAZA", "DAILY STORE") },
        { "StoreSubtitle", ("Valorant günlük rotasyon skin teklifleriniz ve fiyatları", "Your daily rotation Valorant skin offers and prices") },
        { "StoreRefreshIn", ("Yenilenmeye: ", "Refreshes in: ") },
        { "StoreAllWeapons", ("Tüm Silahlar", "All Weapons") },
        { "StoreAllWeaponsToolTip", ("Oyundaki Tüm Silahları ve Kaplamaları Listele", "List all weapons and skins in the game") },
        { "StoreVpToolTip", ("Mevcut Valorant Points Bakiyeniz", "Current Valorant Points Balance") },
        { "StoreRpToolTip", ("Mevcut Radyanit Puanı Bakiyeniz", "Current Radianite Points Balance") },
        { "StoreKcToolTip", ("Mevcut Kingdom Kredisi Bakiyeniz", "Current Kingdom Credits Balance") },
        { "StoreRefreshToolTip", ("Mağazayı Yenile", "Refresh Store") },
        { "StoreCloseToolTip", ("Kapat (ESC)", "Close (ESC)") },
        { "StoreWaitingTitle", ("Riot Client Bekleniyor...", "Waiting for Riot Client...") },
        { "StoreWaitingMessage", ("Günlük mağazanızı çekebilmek için Riot Client'a bağlanılıyor. Lütfen Riot Client'ın açılmasını bekleyin...", "Connecting to Riot Client to retrieve your daily store. Please wait for Riot Client to open...") },
        { "StoreWaitingConnecting", ("Riot Client'a Bağlanılıyor...", "Connecting to Riot Client...") },
        { "StoreWaitingPreparing", ("Valorant mağaza teklifleriniz hazırlanıyor...", "Preparing your Valorant store offers...") },
        { "StoreWaitingFetching", ("Mağaza Verileri Alınıyor...", "Fetching Store Data...") },
        { "StoreWaitingFetchingSub", ("Günlük rotasyon teklifleri ve fiyatlar çekiliyor...", "Retrieving daily rotation offers and prices...") },
        { "StoreWaitingCancel", ("İptal Et", "Cancel") },
        { "StoreWaitingRelaunch", ("Riot Client'ı Başlat", "Launch Riot Client") },
        { "StoreErrorTitle", ("Mağaza Verisi Alınamadı", "Failed to Load Store") },
        { "StoreErrorMessageLogin", ("Riot Client'a giriş yapılamadı veya zaman aşımına uğradı.\nLütfen Riot Client'ı açıp giriş yaptıktan sonra tekrar deneyin.", "Failed to connect to Riot Client or timed out.\nPlease open Riot Client, log in, and try again.") },
        { "StoreErrorMessageServer", ("Günlük mağaza verileri Valorant sunucularından alınamadı.\nRiot hesabınızın oturumunu kontrol edip tekrar deneyin.", "Failed to retrieve daily store data from Valorant servers.\nPlease verify your Riot session and try again.") },
        { "StoreErrorClose", ("Kapat", "Close") },
        { "StoreErrorRetry", ("Tekrar Dene", "Retry") },
        { "StoreFeaturedPill", ("ÖNE ÇIKAN PAKET", "FEATURED BUNDLE") },
        { "StoreBundleDrawerTitle", ("PAKET İÇERİĞİNDEKİ SİLAHLAR VE EŞYALAR", "WEAPONS AND ITEMS IN BUNDLE") },
        { "StoreBundleDrawerSub", ("• Tıklayarak inceleyebilir ve seviye videolarını izleyebilirsiniz", "• Click to inspect and watch upgrade videos") },
        { "StoreToggleDrawerOpen", ("Paket İçeriğini Gizle", "Hide Bundle Items") },
        { "StoreToggleDrawerClosed", ("Paket İçeriğini Görüntüle ({0} Eşya)", "View Bundle Items ({0} Items)") },
        { "StoreDailyOffersTitle", ("GÜNLÜK TEKLİFLER (4 SİLAH)", "DAILY OFFERS (4 WEAPONS)") },
        { "StoreDailyOffersSub", ("• İncelemek ve videosunu izlemek için bir silaha tıklayın", "• Click a weapon to inspect and watch video") },
        { "StoreInspectAndVideo", ("İncele & Video", "Inspect & Video") },
        { "StoreNightMarketTitle", ("GECE PAZARI (NIGHT MARKET)", "NIGHT MARKET") },
        { "StoreNightMarketSub", ("• İndirimli Özel Teklifler (Kartı çevirmek ve incelemek için tıklayın)", "• Exclusive Discounted Offers (Click to flip card and inspect)") },
        { "StoreRevealAll", ("Tümünü Çevir", "Reveal All") },
        { "StoreFlipCard", ("KARTI ÇEVİR", "REVEAL CARD") },
        { "StoreClickAndOpen", ("Tıkla ve Aç", "Click to Open") },
        { "StoreBackToStore", ("Mağazaya Dön", "Back to Store") },
        { "StoreCatalogSearchPlaceholder", ("Skin adı ara...", "Search skin name...") },
        { "StoreCatalogWeaponLabel", ("Silah:", "Weapon:") },
        { "StoreCatalogTierLabel", ("Seri:", "Edition:") },
        { "StoreAllWeaponsDropdown", ("Tüm Silahlar", "All Weapons") },
        { "StoreAllTiersDropdown", ("Tüm Seriler", "All Editions") },
        { "StoreSkinsCountFormat", ("{0} Kaplama", "{0} Skins") },
        { "StorePlayerCardBadge", ("OYUNCU KARTI", "PLAYER CARD") },
        { "StorePlayerCardSub", ("Oyuncu kartının profilde, lobide ve simge olarak tüm görünüm formatları", "All display formats for player card in profile, lobby, and avatar") },
        { "StoreCardVerticalTitle", ("DİKEY GÖRÜNÜM (PROFİL)", "VERTICAL VIEW (PROFILE)") },
        { "StoreCardVerticalSub", ("Profil & Envanter Görünümü (Tam Boyut)", "Profile & Inventory View (Full Size)") },
        { "StoreCardHorizontalTitle", ("YATAY GÖRÜNÜM (LOBİ & KARŞILAŞMA)", "HORIZONTAL VIEW (LOBBY & MATCH)") },
        { "StoreCardHorizontalSub", ("Lobi Başlığı ve Karşılaşma Yükleme Ekranı Banner'ı", "Lobby Header and Match Loading Screen Banner") },
        { "StoreCardSquareTitle", ("KARE GÖRÜNÜM (AVATAR & SİMGE)", "SQUARE VIEW (AVATAR & ICON)") },
        { "StoreCardSquareSub", ("Arkadaş listesinde, grup lobisinde ve sohbet pencerelerinde kullanılan avatar simgesi.", "Avatar icon used in friend list, party lobby, and chat windows.") },
        { "StoreCardSquarePill", ("Standart Kare En-Boy Oranı", "Standard Square Aspect Ratio") },
        { "StoreCardFooterInfo", ("Bu kart paket içeriğinde veya mağaza rotasyonunda yer almaktadır.", "This card is part of bundle contents or store rotation.") },

        // --- Skin Inspect View ---
        { "InspectCloseToolTip", ("Kapat (ESC)", "Close (ESC)") },
        { "InspectVideoLoading", ("Tanıtım Videosu Yükleniyor...", "Loading Preview Video...") },
        { "InspectLevels", ("SEVİYELER:", "LEVELS:") },
        { "InspectVariants", ("VARYANTLAR:", "VARIANTS:") },
        { "InspectStandardModel", ("Standart Model • Yükseltme ve Tanıtım Videosu Bulunmuyor", "Standard Model • No Upgrades or Preview Video") },
        { "InspectShortcuts", ("← 5s Geri | → 5s İleri | Boşluk: Oynat | F: Tam Ekran | 1-5: Seviyeler", "← 5s Back | → 5s Forward | Space: Play | F: Fullscreen | 1-5: Levels") },
        { "InspectSeekBackwardToolTip", ("5 Saniye Geri (Sol Ok ←)", "5 Seconds Back (Left Arrow ←)") },
        { "InspectSeekForwardToolTip", ("5 Saniye İleri (Sağ Ok →)", "5 Seconds Forward (Right Arrow →)") },
        { "InspectPlayPauseToolTip", ("Durdur / Oynat (Boşluk)", "Play / Pause (Space)") },
        { "InspectReplayToolTip", ("Başa Sar (R)", "Replay (R)") },
        { "InspectMuteToolTip", ("Sesi Aç / Kapat (M)", "Mute / Unmute (M)") },
        { "InspectFullscreenToolTip", ("Tam Ekran (F / F11 / Çift Tıklama)", "Fullscreen (F / F11 / Double Click)") },
        { "InspectExitFullscreenToolTip", ("Tam Ekrandan Çık (F / ESC)", "Exit Fullscreen (F / ESC)") },
        { "InspectInBundle", ("Paket İçi: {0} ({1})", "In Bundle: {0} ({1})") },
        { "InspectLoading", ("Yükleniyor...", "Loading...") },
        { "InspectStaticImage", ("Statik Görsel", "Static Image") },
        { "InspectBaseModel", ("{0} • Temel Model", "{0} • Base Model") },
        { "InspectVariantLabel", ("Varyant: {0}", "Variant: {0}") },

        // --- Player List View ---
        { "PlayerDirectoryTitle", ("OYUNCU REHBERİ & LİSTESİ", "PLAYER DIRECTORY & LIST") },
        { "PlayerDirectorySubtitle", ("Önceden karşılaştığın oyuncular, kız oyuncu etiketleri ve kişisel oyuncu notların", "Past encountered players, female player tags, and personal notes") },
        { "RiotConnected", ("Riot Client Bağlı", "Riot Client Connected") },
        { "RiotDisconnected", ("Riot Client Bekleniyor", "Waiting for Riot Client") },
        { "StatTotal", ("Toplam: ", "Total: ") },
        { "StatFemale", ("Kız Oyuncu: ", "Female: ") },
        { "StatMale", ("Erkek Oyuncu: ", "Male: ") },
        { "StatNoted", ("Notlu: ", "With Notes: ") },
        { "StatEncounter", ("Denk Gelinen: ", "Encountered: ") },
        { "StatBlacklist", ("Engelli: ", "Blocked: ") },
        { "TabFemale", ("Kız Oyuncular", "Female Players") },
        { "TabMale", ("Erkek Oyuncular", "Male Players") },
        { "TabNoted", ("Not Eklediklerin", "Noted Players") },
        { "TabEncounter", ("Önceden Denk Geldiklerin", "Past Encounters") },
        { "TabBlacklist", ("Engellediklerin", "Blocked Players") },
        { "TabAll", ("Tümü", "All") },
        { "SearchPlaceholder", ("Oyuncu adı veya notlarda ara...", "Search by player name or notes...") },
        { "SortLabel", ("Sıralama: ", "Sort by: ") },
        { "SortRecent", ("En Son Görülen", "Most Recent") },
        { "SortCount", ("En Çok Karşılaşılan", "Most Encountered") },
        { "SortName", ("İsim (A-Z)", "Name (A-Z)") },
        { "CloseEsc", ("KAPAT (ESC)", "CLOSE (ESC)") },
        { "EmptyStateTitle", ("Kayıtlı Oyuncu Bulunamadı", "No Players Found") },
        { "EmptyStateDesc", ("Bu kategoride veya arama kriterinde henüz bir oyuncu kaydı yok.", "No player records found in this category or search criteria.") },
        { "EmptyStateTip", ("İpucu: Maç ekranındaki oyuncu kartlarında ♀ butonuna basarak kız oyuncuları, kalem simgesine basarak notları kaydedebilirsin.", "Tip: In match screen, click ♀ to tag female players, or pen icon to save notes.") },
        { "LoadMoreText", ("Daha Fazla Yükle (+30)", "Load More (+30)") },
        { "LoadAllText", ("Tümünü Göster", "Show All") },
        { "PagingStatusFormat", ("{0} / {1} oyuncu gösteriliyor", "Showing {0} / {1} players") },

        // --- Career View ---
        { "CareerTitle", ("KARİYER & MAÇ GEÇMİŞİ", "CAREER & MATCH HISTORY") },
        { "CareerSubtitle", ("Geçmiş maçların detayları, takımların envanterleri, takılı silahlar ve oyuncu notları", "Past match details, team loadouts, equipped weapons, and player notes") },
        { "CareerMatchCount", ("Kayıtlı Maç Sayısı: ", "Recorded Matches: ") },
        { "CareerTotalPlayers", ("Toplam Oyuncu: ", "Total Players: ") },
        { "CareerWinRate", ("Kazanma Oranı: ", "Win Rate: ") },
        { "CareerPlayedMatches", ("Oynanan Maçlar", "Played Matches") },
        { "CareerSearchPlaceholder", ("Harita veya ajan ara...", "Search map or agent...") },
        { "CareerFilterAll", ("Tümü", "All") },
        { "CareerFilterWin", ("🟢 Galibiyet", "🟢 Victory") },
        { "CareerFilterLoss", ("🔴 Mağlubiyet", "🔴 Defeat") },
        { "CareerNoMatchesPrompt", ("Henüz kaydedilmiş bir maç bulunmuyor. Bir maça girdiğinizde tüm takım envanterleri ve oyuncular otomatik buraya kaydedilecektir.", "No recorded matches found yet. When you enter a match, team loadouts and players will automatically be saved here.") },
        { "CareerDeleteContext", ("Bu Maçı Geçmişten Sil", "Delete This Match from History") },

        // --- Settings View ---
        { "SettingsLanguageTitle", ("Dil Seçin", "Select Language") },
        { "SettingsWeaponsHeader", ("Gösterilecek Silahlar (soldan sağa)", "Weapons to Display (left to right)") },
        { "DataManageHeader", ("Veri & Depolama Yönetimi", "Data & Storage Management") },
        { "OpenFolder", ("Klasörü Aç", "Open Folder") },
        { "OpenFolderToolTip", ("Uygulama veri ve maç kayıtları klasörünü aç", "Open application data and match records folder") },
        { "ClearCareer", ("Kariyeri Temizle", "Clear Career") },
        { "ClearCareerToolTip", ("Kayıtlı maç geçmişini ve kariyer verilerini temizle", "Clear recorded match history and career data") },
        { "PlayerData", ("Oyuncu Verileri", "Player Data") },
        { "PlayerDataToolTip", ("Önceden denk gelinenleri, notları, erkek ve kız oyuncu verilerini yönet ve temizle", "Manage and clear past encounters, notes, male and female player data") },
        { "WatcherTitle", ("Valorant İzleyici (Watcher)", "Valorant Watcher") },
        { "WatcherSubtitle", ("Valorant açıldığında ValGrid'i otomatik başlatır", "Automatically launches ValGrid when Valorant starts") },
        { "WatcherEnabled", ("İzleyici Açık", "Watcher Enabled") },
        { "WatcherDisabled", ("İzleyici Kapalı", "Watcher Disabled") },
        { "WatcherWaiting", ("Arka planda Valorant bekleniyor", "Waiting for Valorant in background") },
        { "WatcherOff", ("Otomatik başlatma devre dışı", "Auto-launch is disabled") },
        { "VersionChecking", ("Kontrol ediliyor...", "Checking...") },
        { "DownloadUpdating", ("İçerikler güncelleniyor...", "Updating content...") },
        { "DownloadCheckingVersion", ("Sürüm ve eksik içerikler kontrol ediliyor...", "Checking version and missing content...") },
        { "DownloadUpdatedSuccess", ("Yeni içerikler bulundu ve başarıyla güncellendi!", "New content found and updated successfully!") },
        { "DownloadAlreadyLatest", ("İçerikler kontrol edildi: Zaten en son sürüm yüklü.", "Content checked: Already up to date.") },
        { "DownloadForceUpdating", ("Tüm içerikler zorla güncelleniyor...", "Forcefully updating all content...") },

        // Settings Cards (Info, Auto Login, Content, Auth, Updates)
        { "SettingsInfoTitle", ("Bilgi", "Info") },
        { "SettingsInfoDesc1", ("Bu sayfa manuel olarak giriş yapmanızı, uygulama dilini değiştirmenizi vb. sağlar.", "This page lets you manually sign in, change the app language, etc.") },
        { "SettingsInfoDesc2", ("Giriş bilgilerinizle ilgili tüm iletişim doğrudan Riot sunucularıyla yapılır, parolalarınız yerel olarak veya çevrimiçi asla depolanmaz.", "All communication regarding your credentials is done directly with Riot servers with no storage of passwords being done locally or online.") },
        { "SettingsAutoLoginTitle", ("Otomatik Giriş", "Auto Login") },
        { "SettingsAutoLoginDesc", ("Not: Bunun çalışması için Valorant'ın açık olması gerekir", "Note: Valorant needs to be running for this to work") },
        { "SettingsLoginBtn", ("Giriş Yap", "Login") },
        { "SettingsContentTitle", ("İndirilen İçeriği Kontrol Et", "Check Downloaded Content") },
        { "SettingsForceUpdateBtn", ("Zorla Güncelle", "Force Update") },
        { "SettingsCheckContentBtn", ("Güncellemeleri Kontrol Et", "Check for Updates") },
        { "SettingsAuthStatusTitle", ("Kimlik Doğrulama Durumu", "Authentication Status") },
        { "SettingsAuthStatusDefault", ("Güncellemek için aşağıya tıklayın", "Click below to update") },
        { "SettingsAuthCheckBtn", ("Kimliği Doğrula", "Check Authentication") },
        { "SettingsAppUpdatesTitle", ("Uygulama Güncellemeleri", "Check for Updates") },
        { "SettingsLatestVersionLabel", ("En Son Sürüm: ", "Latest Version: ") },
        { "SettingsCurrentVersionLabel", ("Mevcut Sürüm: ", "Current Version: ") },
        { "SettingsCheckAppUpdatesBtn", ("Güncellemeleri Kontrol Et", "Check for Updates") },

        // Settings Dialogs & Feedback
        { "ClearCareerConfirmMsg", ("Kayıtlı maç geçmişi ve kariyer verilerini temizlemek istediğinizden emin misiniz?\n(Bu işlem geri alınamaz)", "Are you sure you want to clear recorded match history and career data?\n(This action cannot be undone)") },
        { "ClearCareerConfirmTitle", ("Kariyeri Temizle", "Clear Career") },
        { "ClearCareerSuccessMsg", ("✓ Maç geçmişi ve kariyer başarıyla temizlendi!", "✓ Match history and career data cleared successfully!") },
        { "ClearEncountersConfirmMsg", ("Önceden karşılaşılan oyuncu kayıtlarını ve karşılaşma sayaçlarını ({0} kayıt) temizlemek istediğinizden emin misiniz?\n(Özel oyuncu notları ve etiketler korunacaktır)", "Are you sure you want to clear past encounter records and counters ({0} records)?\n(Custom player notes and tags will be preserved)") },
        { "ClearEncountersConfirmTitle", ("Önceden Denk Gelinenleri Temizle", "Clear Past Encounters") },
        { "ClearEncountersSuccessMsg", ("✓ Karşılaşma kayıtları başarıyla temizlendi!", "✓ Encounter records cleared successfully!") },
        { "ClearNotesConfirmMsg", ("Tüm oyuncu notlarını ({0} not) temizlemek istediğinizden emin misiniz?\n(Bu işlem geri alınamaz)", "Are you sure you want to clear all player notes ({0} notes)?\n(This action cannot be undone)") },
        { "ClearNotesConfirmTitle", ("Oyuncu Notlarını Temizle", "Clear Player Notes") },
        { "ClearNotesSuccessMsg", ("✓ Tüm oyuncu notları başarıyla temizlendi!", "✓ Player notes cleared successfully!") },
        { "ClearMaleConfirmMsg", ("Kayıtlı erkek oyuncu etiketlerini ({0} oyuncu) temizlemek istediğinizden emin misiniz?\n(Bu işlem geri alınamaz)", "Are you sure you want to clear male player tags ({0} players)?\n(This action cannot be undone)") },
        { "ClearMaleConfirmTitle", ("Erkek Oyuncuları Temizle", "Clear Male Tags") },
        { "ClearMaleSuccessMsg", ("✓ Erkek oyuncu etiketleri başarıyla temizlendi!", "✓ Male player tags cleared successfully!") },
        { "ClearFemaleConfirmMsg", ("Kayıtlı kız oyuncu etiketlerini ({0} oyuncu) temizlemek istediğinizden emin misiniz?\n(Bu işlem geri alınamaz)", "Are you sure you want to clear female player tags ({0} players)?\n(This action cannot be undone)") },
        { "ClearFemaleConfirmTitle", ("Kız Oyuncuları Temizle", "Clear Female Tags") },
        { "ClearFemaleSuccessMsg", ("✓ Kız oyuncu etiketleri başarıyla temizlendi!", "✓ Female player tags cleared successfully!") },
        { "ClearBlacklistConfirmMsg", ("Kara listedeki tüm oyuncuları ({0} oyuncu) engelli listesinden çıkarmak istediğinizden emin misiniz?\n(Bu işlem geri alınamaz)", "Are you sure you want to remove all blacklisted players ({0} players) from the blocklist?\n(This action cannot be undone)") },
        { "ClearBlacklistConfirmTitle", ("Kara Listeyi Temizle", "Clear Blacklist") },
        { "ClearBlacklistSuccessMsg", ("✓ Kara listedeki oyuncular başarıyla temizlendi!", "✓ Blacklist records cleared successfully!") },
        { "ClearAllConfirmMsg", ("DİKKAT: Önceden denk gelinen tüm oyuncu karşılaşma kayıtları, özel notlar, erkek ve kız etiketleri ve KARA LİSTENİN TAMAMI silinecektir.\nDevam etmek istediğinize emin misiniz?", "WARNING: ALL past encounter records, notes, male and female tags, and the ENTIRE BLACKLIST will be permanently deleted.\nAre you sure you want to proceed?") },
        { "ClearAllConfirmTitle", ("Tüm Oyuncu Verilerini Temizle", "Clear All Player Data") },
        { "ClearAllSuccessMsg", ("✓ Tüm oyuncu verileri başarıyla temizlendi!", "✓ All player data cleared successfully!") },

        // --- Match View ---
        { "MatchValueLabel", ("Maç Değeri: ", "Match Value: ") },
        { "MatchEquippedOnly", (" (SADECE TAKILI OLANLAR)", " (EQUIPPED ONLY)") },
        { "MatchLeaderboardBadge", ("Sırasıyla Liderlik", "Inventory Leaderboard") },
        { "MatchBlacklistBadge", ("KARA LİSTE UYARISI", "BLACKLIST ALERT") },
        { "MatchLeaderboardTooltip", ("Sol Tık: Liderlik Metnini Kopyala (Tek Satır)\nSağ Tık: Liderlik Metnini Kopyala (Alt Alta Liste)", "Left Click: Copy Leaderboard (Single Line)\nRight Click: Copy Leaderboard (Multi-line List)") },
        { "MatchBlacklistTooltip", ("Lobide engellenmiş (kara listeye alınmış) oyuncu tespit edildi!", "Blocked (blacklisted) player detected in this lobby!") },

        // --- Info View ---
        { "InfoAppSubtitle", ("Valorant Canlı Maç & Mağaza Asistanı", "Valorant Live Match & Store Assistant") },

        // --- Modal Player Data Clear Strings ---
        { "ModalClearHeader", ("Oyuncu Verilerini Temizleme", "Clear Player Data") },
        { "ModalClearSub", ("Aşağıdaki veri kategorilerini tek tek veya toplu olarak temizleyebilirsiniz.", "You can clear the data categories below individually or altogether.") },
        { "ModalClearEncountersTitle", ("Önceden Denk Geldiklerimiz", "Past Encounters") },
        { "ModalClearEncountersSub", ("Maçlarda karşılaşılan oyuncuların sayaç ve geçmiş kayıtları", "Encounter counters and history logs of players from matches") },
        { "ModalClearNotesTitle", ("Not Eklediklerim", "My Player Notes") },
        { "ModalClearNotesSub", ("Oyunculara özel olarak eklediğiniz tüm kişisel notlar", "All custom personal notes you added to players") },
        { "ModalClearMaleTitle", ("Erkek Oyuncu Etiketleri", "Male Player Tags") },
        { "ModalClearMaleSub", ("Erkek (♂) olarak etiketlediğiniz oyuncu kayıtları", "Player records tagged as male (♂)") },
        { "ModalClearFemaleTitle", ("Kız Oyuncu Etiketleri", "Female Player Tags") },
        { "ModalClearFemaleSub", ("Kız (♀) olarak etiketlediğiniz oyuncu kayıtları", "Player records tagged as female (♀)") },
        { "ModalClearBlacklistTitle", ("Kara Liste (Engellenenler)", "Blacklist (Blocked)") },
        { "ModalClearBlacklistSub", ("Kara listeye aldığınız ve engellediğiniz tüm oyuncu kayıtları", "All players you added to the blacklist and blocked") },
        { "ModalBtnClear", ("Temizle", "Clear") },
        { "ModalBtnClearAll", ("Tüm Oyuncu Verilerini Temizle", "Clear All Player Data") },
        { "ModalBtnClose", ("Kapat", "Close") },

        // --- ValAPI Content Update Strings ---
        { "ApiUpdateMapsAgents", ("Harita, ajan ve rütbe verileri güncelleniyor...", "Updating maps, agents, and ranks...") },
        { "ApiUpdateSkins", ("Skinler, varyantlar ve seviyeler indiriliyor...", "Downloading skins, variants, and levels...") },
        { "ApiUpdateCardsSprays", ("Kartlar, spreyler ve fiyat verileri güncelleniyor...", "Updating cards, sprays, and pricing...") },
        { "ApiUpdateCheckingLang", ("İçerikler ve güncel sürüm kontrol ediliyor...", "Checking content and latest version...") },

        // --- Chat Templates Strings ---
        { "ChatTemplatesBtnText", ("Chat Şablonları", "Chat Templates") },
        { "ChatTemplatesBtnToolTip", ("Maç başlangıç, maç sonu ve sıralama sohbet şablonlarını özelleştir", "Customize match start, match end, and leaderboard chat templates") },
        { "ChatTemplatesModalTitle", ("Chat Mesaj Şablonları", "Chat Message Templates") },
        { "ChatTemplatesModalSubtitle", ("Maç içinde panoya (Ctrl+V) kopyalanan mesajları özelleştirin. Değişken butonlarına tıklayarak şablona ekleyebilirsiniz.", "Customize messages copied to clipboard (Ctrl+V) during matches. Click variable buttons to insert into template.") },
        { "TemplateTabMatchStart", ("Maç Başlangıcı", "Match Start") },
        { "TemplateTabMatchEnd", ("Maç Sonu", "Match End") },
        { "TemplateTabLeaderboard", ("Liderlik Sıralaması", "Leaderboard") },
        { "TemplateInsertVar", ("Değişken Ekle:", "Insert Variable:") },
        { "TemplateLivePreview", ("Canlı Önizleme (Örnek Maç Çıktısı):", "Live Preview (Sample Match Output):") },
        { "TemplateResetDefault", ("Varsayılana Sıfırla", "Reset to Default") },
        { "TemplateSave", ("Kaydet", "Save") },
        { "TemplateSavedStatus", ("✓ Şablonlar başarıyla kaydedildi!", "✓ Templates saved successfully!") },
        { "TemplateResetStatus", ("✓ Şablonlar varsayılana sıfırlandı!", "✓ Templates reset to defaults!") },
    };

    /// <summary>
    /// Gets a localized string for the specified key.
    /// </summary>
    public static string Get(string key)
    {
        if (Strings.TryGetValue(key, out var pair))
        {
            return IsEnglish ? pair.EN : pair.TR;
        }
        return key;
    }

    /// <summary>
    /// Formats store countdown into localized remaining time text.
    /// </summary>
    public static string FormatCountdown(int totalSeconds)
    {
        if (totalSeconds < 0) totalSeconds = 0;
        var ts = TimeSpan.FromSeconds(totalSeconds);
        if (ts.TotalDays >= 1)
        {
            return IsEnglish
                ? $"{(int)ts.TotalDays}d {ts.Hours}h Left"
                : $"{(int)ts.TotalDays} Gün {ts.Hours} Saat Kaldı";
        }

        return IsEnglish
            ? $"{(int)ts.TotalHours:D2}h {ts.Minutes:D2}m {ts.Seconds:D2}s"
            : $"{(int)ts.TotalHours:D2}s {ts.Minutes:D2}dk {ts.Seconds:D2}sn";
    }

    /// <summary>
    /// Returns localized display name for content tier (Select, Deluxe, Premium, Exclusive, Ultra).
    /// </summary>
    public static string GetTierDisplayName(string devName)
    {
        var dev = devName?.ToLowerInvariant() ?? "";
        return dev switch
        {
            "ultra" => IsEnglish ? "ULTRA EDITION" : "ULTRA SERİ",
            "exclusive" => IsEnglish ? "EXCLUSIVE EDITION" : "SEÇKİN SERİ",
            "premium" => IsEnglish ? "PREMIUM EDITION" : "İHTİŞAMLI SERİ",
            "deluxe" => IsEnglish ? "DELUXE EDITION" : "ÜSTÜN SERİ",
            "select" => IsEnglish ? "SELECT EDITION" : "ÖZEL SERİ",
            _ => IsEnglish ? "STANDARD" : "STANDART"
        };
    }

    /// <summary>
    /// Returns localized level type name for inspect viewer.
    /// </summary>
    public static string GetLevelTypeName(int num, string itemType, bool hasVideo)
    {
        if (IsEnglish)
        {
            return itemType switch
            {
                "EEquippableSkinLevelItem::Finisher" => $"Level {num} (Finisher)",
                "EEquippableSkinLevelItem::Animation" => $"Level {num} (Animation)",
                "EEquippableSkinLevelItem::VFX" => $"Level {num} (VFX)",
                "EEquippableSkinLevelItem::SoundEffects" => $"Level {num} (Sound FX)",
                "EEquippableSkinLevelItem::KillBanner" => $"Level {num} (Kill Banner)",
                "EEquippableSkinLevelItem::InspectAndKill" => $"Level {num} (Inspect & Kill)",
                "EEquippableSkinLevelItem::TopFrag" => $"Level {num} (Top Frag)",
                "EEquippableSkinLevelItem::Heartbeat" => $"Level {num} (Heartbeat)",
                "EEquippableSkinLevelItem::Voiceover" => $"Level {num} (Voiceover)",
                _ => num == 1 ? "Level 1 (Base)" : (hasVideo ? $"Level {num} (Special)" : $"Level {num}")
            };
        }

        return itemType switch
        {
            "EEquippableSkinLevelItem::Finisher" => $"{num}. Seviye (Bitiriş)",
            "EEquippableSkinLevelItem::Animation" => $"{num}. Seviye (Animasyon)",
            "EEquippableSkinLevelItem::VFX" => $"{num}. Seviye (VFX)",
            "EEquippableSkinLevelItem::SoundEffects" => $"{num}. Seviye (Ses Efekti)",
            "EEquippableSkinLevelItem::KillBanner" => $"{num}. Seviye (Öldürme Sancağı)",
            "EEquippableSkinLevelItem::InspectAndKill" => $"{num}. Seviye (İnceleme & Kill)",
            "EEquippableSkinLevelItem::TopFrag" => $"{num}. Seviye (Liderlik)",
            "EEquippableSkinLevelItem::Heartbeat" => $"{num}. Seviye (Kalp Atışı)",
            "EEquippableSkinLevelItem::Voiceover" => $"{num}. Seviye (Seslendirme)",
            _ => num == 1 ? "1. Seviye (Temel)" : (hasVideo ? $"{num}. Seviye (Özel)" : $"{num}. Seviye")
        };
    }

    /// <summary>
    /// Returns localized base style name ("Standard" vs "Standart").
    /// </summary>
    public static string GetDefaultVariantName() => IsEnglish ? "Standard" : "Standart";

    /// <summary>
    /// Returns localized version string with up-to-date tag.
    /// </summary>
    public static string VersionUpToDate(string version)
    {
        return IsEnglish ? $"v{version} (Up to date)" : $"v{version} (Güncel)";
    }

    /// <summary>
    /// Returns localized bundle discount pill text.
    /// </summary>
    public static string FormatBundlePrice(int price, int discountPct)
    {
        return IsEnglish
            ? $"In Bundle: {price:N0} VP (-{discountPct}%)"
            : $"Paket İçi: {price:N0} VP (-%{discountPct})";
    }

    /// <summary>
    /// Returns localized download completion status message.
    /// </summary>
    public static string FormatDownloadSuccess(int count)
    {
        return IsEnglish
            ? $"All content updated! ({count} skins/levels ready)"
            : $"Tüm içerikler güncellendi! ({count} skin/seviye hazır)";
    }
}

