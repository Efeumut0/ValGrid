<p align="center">
  <img src="ValGrid/Assets/logo.png" alt="ValGrid Logo" width="100" height="100" />
</p>

<h1 align="center">ValGrid</h1>

<p align="center">
  <strong>Valorant match info, skin prices and daily store on Windows</strong>
</p>

<p align="center">
  <a href="https://github.com/Efeumut0/ValGrid/releases/latest"><img src="https://img.shields.io/badge/release-v1.4.0-38bdf8.svg?style=flat-square&logo=github" alt="Release" /></a>
  <img src="https://img.shields.io/badge/platform-Windows_10_|_11-0078d6.svg?style=flat-square&logo=windows" alt="Platform" />
  <img src="https://img.shields.io/badge/.NET-6.0--windows-512bd4.svg?style=flat-square&logo=dotnet" alt=".NET 6.0" />
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-22c55e.svg?style=flat-square" alt="License" /></a>
  <img src="https://img.shields.io/badge/language-TR_%7C_EN-f59e0b.svg?style=flat-square" alt="Localization" />
</p>

<p align="center">
  <a href="#-overview">Overview</a> •
  <a href="#-key-features">Features</a> •
  <a href="#-download--installation">Download</a> •
  <a href="#-automatic-updates">Updates</a> •
  <a href="#-chat-message-templates">Chat Templates</a> •
  <a href="#-privacy--safety">Privacy</a> •
  <a href="#-disclaimer">Disclaimer</a> •
  <a href="#-türkçe-açıklama">🇹🇷 Türkçe</a>
</p>

---

## 📖 Overview

**ValGrid** is an open-source Windows app for Valorant. It reads data from the local Riot Client and public Valorant services to show player ranks and estimate the value of equipped skins. You can also browse the daily store and Night Market, watch skin previews, save player notes and copy chat messages from templates.

The installer includes the app and an optional watcher that opens it when Valorant starts.

---

## ✨ Key Features

- **🎯 Live Match & Pre-Game Intelligence**
  - View real-time ranks, peak act ranks, ratings, and account levels for all players during Agent Select and in-game.
  - Automatically preserves match player records even if the match is dodged.

- **💎 Equipped Skin Inventory Value Calculator**
  - Scans and evaluates currently equipped weapon skins in the match.
  - Calculates total lobby inventory worth in both Valorant Points (VP) and estimated real currency (TL / USD).

- **👥 Encounter Tracker & Player Directory**
  - Shows how often you played with or against each player.
  - Tag players (♀ Female / ♂ Male), save private player notes, or blacklist toxic players.
  - Filter and sort your encounter history.

- **🛍️ Daily Store & Night Market Viewer**
  - View your 24h daily store offers, featured skin bundles, and Night Market cards.
  - Inspect weapon skins with all upgrade levels, color chromas, sound effects, and official gameplay videos.

- **💬 Dynamic Chat Message Templates**
  - One-click copy match start, match end, and inventory leaderboard messages to clipboard.
  - Edit and preview messages in Settings using these tags:
    `{totalVp}`, `{totalTl}`, `{richest}`, `{richest1}`, `{richest2}`, `{blacklist}`, `{leaderboard}`.

- **⚡ Silent Background Watcher (ValGridWatcher)**
  - Opens ValGrid automatically when you start Valorant.
  - Turn it on or off in Settings.

- **🌐 Full Bilingual Support**
  - The interface is available in English and Turkish.

---

## 📥 Download & Installation

1. Download the latest installer from [**Releases**](https://github.com/Efeumut0/ValGrid/releases/latest).
2. Run the setup wizard (installing the silent background watcher is optional during setup).
3. Start Valorant, then open ValGrid to see lobby data.

> **System Requirements:** Windows 10 (version 1903+) or Windows 11 (64-bit). The installer automatically configures the required .NET 6 Desktop Runtime if needed.

---

## 🔄 Automatic Updates

ValGrid includes built-in update detection connected directly to GitHub Releases:
- Whenever a new version is published on GitHub, ValGrid detects it upon launch or via **Settings > Güncellemeleri Denetle / Check for Updates**.
- The app notifies you with release details and downloads the updated installer package automatically.

---

## 💬 Chat Message Templates

ValGrid includes customizable clipboard message templates for team chat:

| Variable | Description | Example Output |
| :--- | :--- | :--- |
| `{totalVp}` | Total lobby equipped skins value in VP | `48,500` |
| `{totalTl}` | Estimated currency value | `~5.820 TL` |
| `{richest}` | Summary of highest inventory players | `Our Jett (12500 VP), Enemy Reyna (8700 VP)` |
| `{richest1}` | #1 richest player in the match | `Jett (12500 VP)` |
| `{blacklist}` | Blacklisted/toxic players in the lobby | `Player1, Player2` |
| `{leaderboard}` | Single-line top inventory leaderboard | `1. Jett (12500), 2. Reyna (8700)...` |

Templates can be personalized under **Settings > Chat Templates** with real-time test preview.

---

## 🔒 Privacy & Safety

- **Zero credential storage:** ValGrid only reads local lockfile data from the running Riot Client (identical to official trackers).
- **Offline local database:** Player notes, encounters, and custom settings remain 100% on your machine (`%LocalAppData%\ValGrid`).
- **Riot TOS compliance:** ValGrid does not inject DLLs, read game memory, or modify game files.

---

## ⚖️ Disclaimer

ValGrid isn't endorsed by Riot Games and doesn't reflect the views or opinions of Riot Games or anyone officially involved in producing or managing Riot Games properties. Riot Games, and all associated properties are trademarks or registered trademarks of Riot Games, Inc.

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

<br/>

<a id="-türkçe-açıklama"></a>

# 🇹🇷 ValGrid (Türkçe Dokümantasyon)

**ValGrid**, Valorant için bir Windows uygulaması. Maçtaki oyuncuların ranklarını ve takılı skinlerin tahmini değerini gösterir. Günlük mağazaya ve Gece Pazarı’na bakabilir, skin videolarını izleyebilir, oyunculara not ekleyebilir ve sohbet mesajı şablonları hazırlayabilirsiniz.

Kurulum sırasında Valorant açıldığında ValGrid’i başlatan arka plan izleyicisini de ekleyebilirsiniz.

---

### 🚀 Öne Çıkan Özellikler

1. **Canlı Maç & Lobi Radarı:**
   - Ajan seçim ekranında (pre-game) ve canlı maçta takım arkadaşlarının ve rakiplerin güncel rankını, peak derecesini ve hesap seviyesini anlık gösterir.
   - Karşılaşma bozulsa (dodge) dahi oyuncuları hafızaya kaydeder.

2. **Takılı Skin Değeri Analizi:**
   - Maçtaki tüm oyuncuların takılı kaplamalarını tarar. Toplam lobi envanterinin VP ve yaklaşık TL değerini hesaplar.

3. **Karşılaşma Hafızası & Oyuncu Rehberi:**
   - Daha önce denk gelinen oyuncuları sayaçla (Dost / Rakip) listeler.
   - Oyunculara özel notlar ekleyebilir, cinsiyet etiketi (♀ / ♂) koyabilir veya toksik oyuncuları kara listeye alabilirsiniz.

4. **Günlük Mağaza & Gece Pazarı:**
   - Günlük vitrin rotasyonunu, öne çıkan bundle paketlerini ve Gece Pazarı kartlarını gösterir.
   - Silah seviyelerini, renk varyantlarını (chroma) ve resmi oynanış videolarını doğrudan uygulama içinde izleyebilirsiniz.

5. **Özelleştirilebilir Chat Şablonları:**
   - Maç başı, maç sonu ve liderlik mesajlarını tek tıkla panoya kopyalar.
   - Ayarlar menüsünden `{totalVp}`, `{totalTl}`, `{richest}`, `{blacklist}`, `{leaderboard}` gibi dinamik etiketlerle mesajlarınızı düzenleyip önizleyebilirsiniz.

6. **Sessiz Arka Plan Algılayıcısı (ValGridWatcher):**
   - Bilgisayarınızda Valorant başlatıldığında ValGrid'i otomatik olarak açan hafif arka plan servisi. Ayarlardan istendiğinde açılıp kapatılabilir. Kurulum sihirbazında seçilebilir.

---

### 💾 Kurulum & Otomatik Güncelleme

1. [**Releases**](https://github.com/Efeumut0/ValGrid/releases/latest) sayfasından en güncel kurulum dosyasını indirin.
2. Kurulum sihirbazını tamamlayın (arka plan izleyicisini kurmak isteğe bağlıdır).
3. Valorant çalışırken ValGrid'i açın; lobi verileri otomatik olarak ekrana gelecektir.
4. **Otomatik Güncelleme:** GitHub üzerinde yeni bir release yayınlandığında, ValGrid açılışta veya Ayarlar menüsünden güncellemeyi otomatik olarak algılar ve yeni kurulum paketini indirmenizi sağlar.

---

### 🔒 Güvenlik & Gizlilik

* **Hesap Şifresi İstemez:** Yalnızca bilgisayarınızda çalışan yerel Riot Client lockfile oturumu üzerinden güvenle veri okur.
* **Tamamen Yerel:** Oyuncu notları ve geçmiş karşılaşmalar `%LocalAppData%\ValGrid` altında yerel olarak tutulur.
* **Riot Kurallarına Uygun:** Oyuna DLL enjekte etmez veya oyun dosyalarını değiştirmez.

---

### 🌟 Orijinal Proje & Teşekkürler (Credits & Attribution)

ValGrid, Soneliem’in açık kaynaklı **[NOWT](https://github.com/Soneliem/NOWT)** projesini temel alır. Bu sürümde sohbet şablonları, Türkçe ve İngilizce arayüz, arka plan izleyicisi ve envanter değeri hesabı eklendi. Orijinal geliştiricilere teşekkür ederiz.

* **Orijinal Proje:** [https://github.com/Soneliem/NOWT](https://github.com/Soneliem/NOWT)
* **Lisans:** [MIT License](LICENSE)
