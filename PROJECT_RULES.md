# PROJECT ARCHITECTURE & CORE CONSTITUTION
# (PROJE MİMARİSİ VE KALICI TEMEL KURALLAR)

Bu belge, **Mobil Zombi Hayatta Kalma ve İnteraktif Hikaye Oyunu** projesinin mimari anayasasıdır. Gelecekteki tüm geliştirme adımları ve yapay zeka oturumları bu kurallara harfiyen uymak zorundadır.

---

## 1. DİL VE İSİMLENDİRME STANDARTLARI (LANGUAGE & NAMING CONVENTIONS)
- **Kod ve Varlıklar (Assets):** Tüm klasör isimleri, C# scriptleri, sınıflar, değişkenler, metodlar, enumlar, arayüzler ve sahne hiyerarşisindeki GameObject isimleri **İngilizce** olmalıdır.
  - Sınıflar, Structlar, Metodlar, Property'ler ve Enumlar: `PascalCase` (örn. `PlayerController`, `TakeDamage`, `CurrentHealth`).
  - Değişkenler ve parametreler: `camelCase` (örn. `moveSpeed`, `targetZone`).
  - Private serialize edilen alanlar: `[SerializeField] private float moveSpeed;` (camelCase).
  - Event ve Action isimleri: `On` ön eki veya geçmiş zaman (örn. `OnPlayerDeath`, `OnItemCollected`).
- **Kullanıcı İletişimi ve Hikaye İçeriği:** Durum raporları, test yönergeleri, geliştirici notları ve oyun içi hikaye JSON dosyaları (`dialogueText`, `choiceText`, vb.) **Türkçe** olmalıdır.

---

## 2. MODEL DEĞİŞTİRİLEBİLİR PREFAB MİMARİSİ (SWAPPABLE MESH ARCHITECTURE)
İlerleyen aşamalarda Tripo AI, Meshy ve Mixamo gibi araçlardan üretilecek 3D modeller ve animasyonlar sisteme giydirilecektir. Bu süreçte oyun mantığının ve fizik yapısının bozulmaması için:
- **Tek Parça Obje Yasağı:** Oyuncu, Zombi, NPC, Eşya ve Sandıklar asla doğrudan mesh içeren tek parça bir obje olarak yapılandırılmaz.
- **Kök Obje (`_Root`):** Tüm fizik bileşenleri (`Rigidbody`, `CharacterController`, `Collider`), `NavMeshAgent` ve oyun mantığını yürüten C# scriptleri kök objede yer alır (örn. `Player_Root`, `Zombie_Standard_Root`, `LootContainer_Root`).
- **Görsel Model Alt Objesi (`Visual_Model`):** MeshFilter, MeshRenderer, SkinnedMeshRenderer ve Animator bileşenleri sadece kök objenin altındaki **`Visual_Model`** isimli child objede bulunur.
- **Sonuç:** 3D model değiştirilirken sadece `Visual_Model` altındaki mesh/prefab güncellenir; hiçbir kod, collider, sensor veya NavMesh referansı bozulmaz.

---

## 3. VERİ VE KOD AYRIMI (DATA-DRIVEN DESIGN)
- **ScriptableObject Kullanımı:** Oyun dengesi parametreleri (karakter hızları, zombi hasarları, açlık/susuzluk tüketim hızları, eşya envanter özellikleri) asla C# kodunun içine sabit değer (hardcoded) olarak yazılmaz. `ScriptableObject` mimarisi ile `Assets/_Project/Data/` altında yapılandırılır.
- **JSON Tabanlı Hikaye ve Görev Motoru:** Tüm dallanan hikayeler, diyaloglar, seçimler, seçimlerin şartları ve tetiklediği harita görevleri `Assets/_Project/Resources/Stories/` altındaki dış JSON dosyalarından dinamik olarak okunur. Kod sadece bu veriyi yorumlayıp oynatan bir motordur.

---

## 4. BAĞIMSIZ SİSTEMLER VE EVENT MİMARİSİ (DECOUPLED ARCHITECTURE)
- Sistemler birbirine doğrudan bağımlı (tightly coupled) olmamalıdır. Örneğin `SurvivalStats`, `HUDController` referansını doğrudan tutmaz.
- İletişim `GameEvents` statik C# Action / event kanalları üzerinden gerçekleştirilir.
- Örnek: Sağlık değiştiğinde `GameEvents.TriggerHealthChanged(current, max)` çağrılır; UI, ses veya kamera efektleri bu event'e bağımsızca abone olur.

---

## 5. MOBİL PERFORMANS VE URP OPTİMİZASYONU (MOBILE PERFORMANCE)
- **Object Pooling:** Çok sayıda üretilen/yok edilen nesneler (Zombiler, haritadaki ganimetler/loot, fırlatılan taşlar/mermiler, hasar/ses görsel halkaları) kesinlikle `Instantiate` ve `Destroy` ile yönetilmez. Merkezi `ObjectPooler` sistemi üzerinden havuza alınıp geri dönüştürülür.
- **Responsive Mobil Canvas:** Tüm UI sistemleri `Canvas Scaler (Scale With Screen Size - Reference Resolution: 1920x1080 Landscape, Match: 0.5)` standardında kurulur; tüm akıllı telefon ekran oranlarına (16:9, 19.5:9, 20:9) hatasız uyum sağlar.
- **URP Uyumlu Aydınlatma ve Gölgelendirme:** Düşük mobil pil tüketimi ve yüksek kare hızı (60 FPS hedefi) için URP ayarları ve hafif gölge çözünürlükleri gözetilir.

---

## 6. KLASÖR HİYERARŞİSİ (DIRECTORY STRUCTURE)
Tüm proje dosyaları sadece `Assets/_Project/` dizini altında izole edilmiş halde tutulur:
- `Assets/_Project/Scripts/Core` -> GameManager, GameEvents, SaveSystem, ObjectPooler
- `Assets/_Project/Scripts/Player` -> PlayerController, StealthSystem, NoiseEmitter, CameraFollow
- `Assets/_Project/Scripts/Survival` -> SurvivalStats, InventorySystem, ItemData, LootContainer
- `Assets/_Project/Scripts/AI` -> ZombieAI, ZombieSensor, ZombieSpawner
- `Assets/_Project/Scripts/Narrative` -> StoryManager, DialogueUI, StoryModels, ChoiceProcessor
- `Assets/_Project/Scripts/Missions` -> MissionManager, DynamicObjective, MissionZone
- `Assets/_Project/Scripts/UI` -> MobileJoystick, HUDController, InteractionPrompt
- `Assets/_Project/Scripts/Editor` -> SceneBuilder, Automation araçları
- `Assets/_Project/Data/Items` -> ScriptableObject eşya verileri
- `Assets/_Project/Data/Configs` -> Oyun ve Zombi ayar dosyaları
- `Assets/_Project/Resources/Stories` -> JSON hikaye dosyaları
- `Assets/_Project/Prefabs/` -> Player, Zombies, Environment, UI
- `Assets/_Project/Scenes/` -> Main_Prototype.unity

---

## 7. TRIPO AI 3D MODEL ÜRETİM VE OTOMATİK ENTEGRASYON HATTI (AUTOMATED ASSET PIPELINE)
Kullanıcı yeni bir 3D obje, karakter, zombi veya bina istediğinde `Tools/TripoPipeline/tripo_studio.py` aracını kullanarak Tripo AI üzerinden üret, `Assets/_Project/Models/TripoGenerated/` altına indir ve Unity MCP ile doğrudan sahneye/prefaba entegre et. Modeller daima `Visual_Model` mimarisiyle giydirilir, fizik ve script hiyerarşisi korunur.

