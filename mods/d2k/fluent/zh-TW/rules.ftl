## player.yaml
options-tech-level =
    .low = 低
    .medium = 中
    .no-powers = 無超級武器
    .unrestricted = 無限制

checkbox-automatic-concrete =
    .label = 自動混凝土
    .description = 建築下方會自動鋪設混凝土地基

notification-insufficient-funds = 資金不足。
notification-new-construction-options = 有新的建造選項。
notification-cannot-deploy-here = 無法在這裡部署。
notification-low-power = 電力不足。
notification-base-under-attack = 基地正遭受攻擊。
notification-ally-under-attack = 我們的盟友正遭受攻擊。
notification-harvester-under-attack = 收割機正遭受攻擊。
notification-silos-needed = 需要儲存槽。
notification-no-room-for-new-unit = 沒有空間容納新單位。
notification-cannot-build-here = 無法在這裡建造。
notification-one-of-our-buildings-has-been-captured = 我們的一座建築已被佔領。

## world.yaml
notification-game-saved = 遊戲已儲存。

dropdown-map-worms =
    .label = 沙蟲
    .description = 沙蟲在地圖上游走，吞噬毫無防備的部隊

options-starting-units =
    .mcv-only = 僅機動建造車
    .light-support = 輕型支援
    .heavy-support = 重型支援
    .carryall = 機動建造車 + 運輸機

resource-spice = 香料

faction-random =
    .name = 任意
    .description = 隨機家族 遊戲開始時會隨機選出一個家族

faction-atreides =
    .name = 亞崔迪
    .description = 亞崔迪家族 來自水世界卡拉丹的高貴亞崔迪， 依靠撲翼機確保制空權。 他們與弗雷曼結盟。這些可怕的 沙丘原住民戰士能在戰鬥中隱匿行動。 陣營差異： - 戰鬥坦克在速度與耐久上取得平衡 特殊單位： - 擲彈兵 - 弗雷曼 - 音波坦克 超級武器： - 空襲

faction-harkonnen =
    .name = 哈肯尼
    .description = 哈肯尼家族 邪惡的哈肯尼為了控制香料不擇手段。 他們依靠蠻力與原子武器達成目標： 財富，以及毀滅亞崔迪家族。 陣營差異： - 戰鬥坦克更耐打，但移動較慢 特殊單位： - 薩督卡 - 毀滅者 超級武器： - 死亡之手飛彈

faction-ordos =
    .name = 奧多斯
    .description = 奧多斯家族 來自冰封世界天龍座西格瑪四號。陰險的奧多斯以 財富、貪婪與背叛聞名。他們往往藉助傭兵、破壞， 以及禁忌的伊克斯科技佔得上風。 陣營差異： - 三輪車改由襲擊三輪車取代 - 戰鬥坦克更快，但較不耐打 特殊單位： - 襲擊三輪車 - 隱形襲擊三輪車 - 破壞者 - 偏導車

faction-corrino =
    .name = 科瑞諾

faction-mercenaries =
    .name = 傭兵

faction-smugglers =
    .name = 走私者

faction-fremen =
    .name = 弗雷曼

map-generator-d2k = 地圖產生器
map-generator-clear = 清空地形

## defaults.yaml
notification-unit-lost = 單位損失。
notification-unit-promoted = 單位晉升。
notification-enemy-building-captured = 已佔領敵方建築。
notification-primary-building-selected = 已選取主要建築。

## aircraft.yaml
actor-carryall-reinforce =
    .name = 運輸機
    .description = 大型有翼的行星運輸船 自動將收割機運往香料田並載回。 受命時會把載具送到維修坪。

actor-carryall-encyclopedia = 自動在香料田與精煉廠之間運送收割機。受命時也能吊起單位，送到維修坪。 運輸機是輕裝甲運輸飛行器。它易受飛彈損傷，而且只有防空武器打得到。

actor-frigate-name = 護衛艦

actor-ornithopter =
    .name = 撲翼機
    .encyclopedia = 沙丘上最快的飛行器。裝甲輕薄，能投擲 500 磅炸彈。對步兵與輕裝甲目標非常有效，也能損傷其他類型的裝甲。

actor-ornithopter-husk-name = 撲翼機
actor-carryall-husk-name = 運輸機
actor-carryall-huskvtol-name = 運輸機

## arrakis.yaml
notification-worm-attack = 沙蟲來襲。
notification-worm-sign = 出現沙蟲跡象。

actor-spicebloom-spawnpoint-name = 香料噴發生成點
actor-spicebloom-name = 香料噴發
actor-sandworm-name = 沙蟲
actor-sietch-name = 弗雷曼穴居

## defaults.yaml
meta-vehicle-generic-name = 單位
meta-husk-generic-name = 已摧毀單位
meta-aircrafthusk-generic-name = 單位
meta-infantry-generic-name = 單位
meta-plane-generic-name = 單位
meta-building-generic-name = 建築

## husks.yaml
actor-mcv-husk-name = 機動建造車（已摧毀）
actor-harvester-husk-name = 香料收割機（已摧毀）
actor-siege-tank-husk-name = 攻城坦克（已摧毀）
actor-missile-tank-husk-name = 飛彈坦克（已摧毀）
actor-sonic-tank-husk-name = 音波坦克（已摧毀）
actor-devastator-husk-name = 毀滅者（已摧毀）
actor-deviator-husk-name = 偏導車（已摧毀）
meta-combat-tank-husk-name = 戰鬥坦克（已摧毀）

## infantry.yaml
actor-light-inf =
    .name = 輕步兵
    .description = 泛用步兵。 強對步兵 弱對坦克與砲兵
    .encyclopedia = 輕裝甲步兵，配備 9mm RP 突擊步槍。對步兵與輕裝甲載具有效。 輕步兵能抵抗飛彈與大口徑砲，但非常容易被高爆炸藥、火焰與輕武器損傷。 摘要： - 爆炸半徑：小 - 視野：非常小 - 強對輕步兵、裝甲兵、飛彈坦克、偏導車 - 弱對戰鬥坦克、攻城坦克、擲彈兵、三輪車、音波坦克

actor-engineer =
    .name = 工程師
    .description = 滲入並佔領敵方 建築。 強對建築 弱對一切 可修復受損峭壁
    .encyclopedia = 可用來佔領敵方建築。 工程師能抵抗反坦克武器，但非常容易被高爆炸藥、火焰與輕武器損傷。 工程師能把已摧毀的殘骸重新啟動到勉強能運作的狀態。這樣就能把殘骸送到最近的維修坪做完全修復。

actor-trooper =
    .name = 裝甲兵
    .description = 反坦克步兵。 強對坦克 弱對步兵與砲兵
    .encyclopedia = 配備線導穿甲飛彈彈頭。裝甲兵對載具與建築非常有效，但難以對付步兵。 裝甲兵能抵抗反坦克武器，但非常容易被高爆炸藥、火焰與子彈武器損傷。 摘要： - 爆炸半徑：中 - 視野：小 - 強對戰鬥坦克、飛彈坦克、飛彈四輪車、三輪車、偏導車、建築、防禦工事 - 弱對攻城坦克、輕步兵、擲彈兵、音波坦克

actor-thumper =
    .name = 擊砂兵
    .description = 部署後吸引附近沙蟲。 無武裝
    .encyclopedia = 部署會發出巨大敲擊聲的裝置，把沙蟲引到該處。

actor-fremen =
    .name = 弗雷曼
    .description = 配備突擊步槍與火箭的精銳步兵。 強對步兵與載具 弱對砲兵 特殊能力：隱形
    .encyclopedia = 沙丘的沙漠原住民戰士，配備 10mm 突擊步槍與火箭。火力對步兵和載具同樣有效。 弗雷曼單位非常容易被高爆炸藥與子彈武器損傷。 摘要： - 爆炸半徑：中 - 視野：小 - 強對飛彈四輪車、三輪車、飛彈坦克、戰鬥坦克、毀滅者、建築、防禦工事 - 弱對攻城坦克、輕步兵、擲彈兵、音波坦克

actor-grenadier =
    .name = 擲彈兵
    .description = 投擲手榴彈的步兵。 強對建築與步兵 弱對載具
    .encyclopedia = 擅長對付建築的步兵砲兵單位。死亡時有機率爆炸，因此不應聚集在一起。 摘要： - 爆炸半徑：大 - 視野：小 - 強對輕步兵、三輪車、飛彈坦克、戰鬥坦克、建築、防禦工事 - 弱對攻城坦克、戰鬥坦克、擲彈兵、音波坦克、毀滅者

actor-sardaukar =
    .name = 薩督卡
    .description = 科瑞諾精銳突擊步兵。 強對步兵與載具 弱對砲兵
    .encyclopedia = 強大的重裝步兵，配備對步兵有效的機槍，以及瞄準載具的火箭發射器。被碾壓時會爆炸，並傷害上方的載具。 摘要： - 爆炸半徑：大 - 視野：小 - 強對三輪車、飛彈四輪車、飛彈坦克、戰鬥坦克、建築、防禦工事 - 弱對攻城坦克、音波坦克、擲彈兵、坦克碾壓

actor-mpsardaukar-description = 哈肯尼精銳突擊步兵。 強對步兵與載具 弱對砲兵

actor-saboteur =
    .name = 破壞者
    .description = 攜帶炸藥的潛行步兵。 可在有限時間內隱形。 強對建築、載具 弱對一切 特殊能力：摧毀建築與載具
    .encyclopedia = 奧多斯家族的特種單位。進入後能炸毀敵方建築與載具，但自己也會死於爆炸。可啟動自毀，傷及附近的敵方單位。 破壞者能抵抗反坦克武器，但非常容易被高爆炸藥、火焰與子彈武器損傷。

actor-nsfremen-description = 配備突擊步槍與火箭的精銳步兵。 強對步兵與載具 弱對砲兵

## misc.yaml
actor-crate-name = 補給箱
actor-mpspawn-name = （多人連線出生點）
actor-waypoint-name = （腳本用路徑點）
actor-camera-name = （向擁有者揭示區域）
actor-wormspawner-name = （沙蟲生成位置）

actor-upgrade-conyard =
    .name = 建造廠升級
    .description = 解鎖額外建造選項： - 大型混凝土板 - 火箭塔

actor-upgrade-barracks =
    .name = 兵營升級
    .description = 解鎖額外步兵： - 裝甲兵 - 工程師 - 擊砂兵 解鎖家族專屬步兵所需： - 亞崔迪：擲彈兵 - 哈肯尼：薩督卡

actor-upgrade-light =
    .name = 輕型工廠升級
    .description = 解鎖額外輕型單位： - 飛彈四輪車 解鎖家族專屬輕型單位所需： - 奧多斯：隱形襲擊三輪車

actor-upgrade-heavy =
    .name = 重型工廠升級
    .description = 解鎖額外建造選項： - IX 研究中心 解鎖額外重型單位： - 攻城坦克 - 飛彈坦克 - 機動建造車

actor-upgrade-hightech =
    .name = 高科技工廠升級
    .description = 解鎖亞崔迪的空襲超級武器。

actor-deathhand =
    .name = 死亡之手
    .encyclopedia = 搭載原子集束彈藥，在目標上方引爆，對廣大範圍造成重大傷害。

## structures.yaml
notification-construction-complete = 建造完成。
notification-unit-ready = 單位就緒。
notification-repairing = 修復中。
notification-unit-repaired = 單位已修復。
notification-select-target = 選取目標。
notification-missile-launch-detected = 偵測到飛彈發射。
notification-airstrike-ready = 空襲就緒。
notification-building-lost = 建築損失。
notification-reinforcements-have-arrived = 援軍已抵達。
notification-death-hand-missile-prepping = 死亡之手飛彈準備中。
notification-death-hand-missile-ready = 死亡之手飛彈就緒。
notification-fremen-ready = 弗雷曼就緒。
notification-saboteur-ready = 破壞者就緒。

meta-concrete =
    .generic-name = 建築
    .description = 提供堅固地基， 防範地形損傷。

actor-concrete-a =
    .name = 混凝土板
    .encyclopedia = 沒有建在混凝土板上的建築，會因為沙丘嚴酷的沙漠環境持續受損。雖然可以修復，但把建築放在混凝土上，就能避免持續風化。 混凝土易受多數武器損傷，而且受損後無法修復。

actor-concrete-b-name = 大型混凝土板

actor-construction-yard =
    .name = 建造廠
    .description = 生產建築。
    .encyclopedia = 任何建在阿拉基斯上的基地，都以建造廠為根基。建造廠會產生少量電力，並讓你能建造新建築。請保護這座建築！它關係著基地的成敗。 建造廠相當堅固，但對各種武器都有不同程度的弱點。

actor-wind-trap =
    .name = 捕風器
    .description = 為其他建築供應 電力。
    .encyclopedia = 為基地產生電力與水。地面上的大型導管把風灌進地下巨大渦輪，藉此驅動發電機與濕氣萃取器。 捕風器易受多數武器損傷。

actor-barracks =
    .name = 兵營
    .description = 訓練步兵。
    .encyclopedia = 生產並訓練輕步兵的必要建築。後續任務中可升級，以訓練進階步兵。 兵營易受多數武器損傷。

actor-refinery =
    .name = 香料精煉廠
    .description = 收割機在此卸下香料 進行加工。
    .encyclopedia = 沙丘上一切香料生產的基礎。收割機把採得的香料運到精煉廠，轉換成資金。精製後的香料會自動分配到各儲存槽與精煉廠存放。每座精煉廠都能儲存香料。精煉廠建造完成後，運輸機會送來一台香料收割機。 精煉廠易受多數武器損傷。

actor-silo =
    .name = 儲存槽
    .description = 儲存多餘的採集香料。
    .encyclopedia = 存放採得的香料。精煉廠超出的部分會平均分配到所有可用的儲存槽。若容量不足，多餘的香料就會流失。儲存槽被摧毀或佔領時，只要還有足夠空間，所存香料就會重新分配。 香料儲存槽易受多數武器損傷。

actor-light-factory =
    .name = 輕型工廠
    .description = 生產輕型載具。
    .encyclopedia = 生產小型輕裝甲戰鬥載具的必要建築。後續任務中可升級，以製造更先進的輕型載具。 輕型工廠易受多數武器損傷。

actor-heavy-factory =
    .name = 重型工廠
    .description = 生產重型載具。
    .encyclopedia = 可建造收割機、戰鬥坦克等重型載具。升級後能解鎖進階載具，但其中一部分還需要其他建築。 重型工廠易受多數武器損傷。

actor-outpost =
    .name = 前哨站
    .description = 提供戰場的雷達地圖。 運作需要電力。 偵測隱形單位。
    .encyclopedia = 電力足夠時，前哨站會啟動並提供雷達地圖。 前哨站易受多數武器損傷。

actor-starport =
    .name = 星港
    .description = 以資金換取快速援軍的降落區。
    .encyclopedia = 開啟與喬安姆商人公會的星際貿易，能以不同價格購買載具與空中單位。要向公會取得單位，這座設施不可或缺。 星港即使有重裝甲，仍易受多數武器損傷。

actor-wall =
    .name = 混凝土牆
    .generic-name = 建築
    .description = 阻擋單位並擋住敵火。
    .encyclopedia = 沙丘上最有效的防禦屏障，能擋住坦克砲火，並阻礙單位移動。 牆只能被爆裂武器、飛彈與砲彈損傷。和混凝土板一樣，受損後無法修復。

actor-medium-gun-turret =
    .name = 機槍塔
    .description = 防禦建築。偵測隱形單位。 強對輕型載具 對步兵中等 弱對坦克與飛行器
    .encyclopedia = 中射程武器，對各類載具都有效，尤其是重裝甲載具。會自動射擊射程內的任何敵方單位，運作需要電力。 機槍塔能抵抗輕武器與爆裂武器，但易受飛彈與重爆裂物損傷。

actor-large-gun-turret =
    .name = 火箭塔
    .description = 防禦建築。偵測隱形單位。 運作需要電力。 強對坦克、飛行器、移動目標 弱對步兵
    .encyclopedia = 比機槍塔射程更遠、射速更快的強化防禦建築。其先進瞄準系統運作時需要電力。 火箭塔能抵抗槍械與爆裂武器，但易受飛彈與大口徑砲損傷。

actor-repair-pad =
    .name = 維修坪
    .description = 修復載具。 可建造機動建造車。
    .encyclopedia = 以生產成本的一小部分修復單位。 維修坪易受多數武器損傷。

actor-high-tech-factory =
    .name = 高科技工廠
    .description = 解鎖先進科技。
    .airstrikepower-name = 空襲
    .airstrikepower-description = 撲翼機轟炸目標。
    .encyclopedia = 生產空中單位，也是建造運輸機的必要建築。亞崔迪家族可在後續任務升級這座設施，建造能進行空襲的撲翼機。 高科技工廠易受多數武器損傷。

actor-research-centre =
    .name = IX 研究中心
    .description = 解鎖進階坦克。
    .encyclopedia = 為建築與載具提供科技升級。要開發先進的特殊武器與原型單位，都需要這座設施。 IX 研究中心易受多數武器損傷。

actor-palace =
    .name = 宮殿
    .description = 解鎖精銳步兵與武器。
    .encyclopedia = 建成後即成為指揮中心，提供額外選項與特殊武器。 宮殿即使有重裝甲，仍易受多數武器損傷。
    .nukepower-name = 死亡之手
    .nukepower-description = 對目標位置發射原子飛彈。
    .produceactorpower-fremen-name = 招募弗雷曼
    .produceactorpower-fremen-description = 配備突擊步槍與火箭的精銳步兵。 強對步兵與載具 弱對砲兵 特殊能力：隱形
    .produceactorpower-saboteur-name = 招募破壞者
    .produceactorpower-saboteur-description = 攜帶炸藥的潛行步兵。 可部署，在有限時間內變成隱形。 強對建築 弱對一切 特殊能力：摧毀建築

## vehicles.yaml
actor-mcv =
    .name = 機動建造車
    .description = 部署成建造廠。 無武裝
    .encyclopedia = 必須開到能夠部署的區域。找到合適的岩石地面後，機動建造車能變形成建造廠。 機動建造車能抵抗子彈與輕爆裂物，但易受飛彈與大口徑砲損傷。

actor-harvester =
    .name = 香料收割機
    .description = 採集香料以供加工。 無武裝
    .encyclopedia = 能抵抗子彈，對高爆炸藥也有一定抵抗力。易受飛彈與大口徑砲損傷。 精煉廠會附贈一台收割機。

actor-trike =
    .name = 三輪車
    .description = 快速偵察單位。 強對步兵 弱對坦克
    .encyclopedia = 輕裝甲的三輪載具，配備重機槍，對步兵與輕裝甲載具有效。 三輪車易受多數武器損傷，大口徑砲的效果稍差。 摘要： - 爆炸半徑：小 - 視野：大 - 強對輕步兵、裝甲兵、飛彈坦克、偏導車 - 弱對戰鬥坦克、攻城坦克、擲彈兵、薩督卡 提示：三輪車對輕步兵有 0.5 的射程優勢。輕步兵一靠太近，就立刻把三輪車往後移。

actor-quad =
    .name = 飛彈四輪車
    .description = 飛彈偵察車。 強對載具 弱對步兵、坦克
    .encyclopedia = 裝甲與火力都優於三輪車。這種四輪車發射穿甲火箭，對多數載具有效。 四輪車能抵抗子彈，對爆裂物也有一定抵抗力，但易受飛彈與大口徑砲損傷。 摘要： - 爆炸半徑：中 - 視野：中 - 強對攻城坦克、音波坦克、建築 - 弱對戰鬥坦克、裝甲兵、飛彈坦克 提示：飛彈四輪車對移動目標的命中誤差很大。盡量靠近目標，以打出最大傷害。

actor-siege-tank =
    .name = 攻城坦克
    .description = 攻城砲兵。 強對步兵與建築 弱對坦克
    .encyclopedia = 對步兵與輕裝甲載具極為有效，但難以對付重裝甲目標。射程很長。 攻城坦克能抵抗子彈，對爆裂物也有一定抵抗力，但易受飛彈與大口徑砲損傷。 大量高爆砲彈是載具被摧毀後發生大爆炸的原因。 摘要： - 爆炸半徑：大 - 視野：大 - 強對任何步兵、三輪車、偏導車 - 弱對戰鬥坦克、飛彈四輪車、飛彈坦克 提示：攻城坦克可以射到視野範圍之外。

actor-missile-tank =
    .name = 飛彈坦克
    .description = 火箭砲兵。 強對載具、建築與飛行器 弱對步兵
    .encyclopedia = 能擊落飛行器，除了步兵以外對多數目標都有效。 飛彈坦克易受多數武器損傷，大口徑砲的效果稍差。 摘要： - 爆炸半徑：中 - 視野：大 - 強對戰鬥坦克、毀滅者、飛彈四輪車 - 弱對任何步兵類型、三輪車、隱形襲擊三輪車 提示：飛彈坦克可以射到視野範圍之外。


actor-sonic-tank =
    .name = 音波坦克
    .description = 發射音波衝擊。 強對步兵與載具 弱對坦克、火箭砲兵
    .encyclopedia = 對步兵與輕裝甲載具最有效，但對裝甲目標較弱。 音波會傷害路徑上的所有單位。 能抵抗子彈與小型爆裂物，但易受飛彈與大口徑砲損傷。 摘要： - 爆炸半徑：非常大 - 視野：中 - 強對任何步兵、攻城坦克、飛彈坦克、偏導車 - 弱對戰鬥坦克、飛彈四輪車、毀滅者 提示：音波強度會隨射程增加。試著以最大射程射擊，才能打出最大傷害。

actor-devastator =
    .name = 毀滅者
    .description = 超重型坦克。 強對坦克 弱對火箭砲兵
    .encyclopedia = 毀滅者是沙丘上威力最強的坦克。速度雖慢，但對多數單位非常有效。它發射雙重電漿彈，也能奉命自毀，傷及附近的單位與建築。 能抵抗子彈與高爆炸藥，但易受飛彈與大口徑砲損傷。 摘要： - 爆炸半徑：大 - 視野：中 - 強對戰鬥坦克、攻城坦克、輕步兵、音波坦克 - 弱對裝甲兵、飛彈坦克、偏導車 提示：毀滅者面對大量裝甲兵時很脆弱。改用自毀。

actor-raider =
    .name = 襲擊三輪車
    .description = 改良偵察車。 強對步兵與輕型載具 弱對坦克
    .encyclopedia = 奧多斯家族改良的襲擊三輪車，火力、速度與裝甲都更強。配備雙聯 20mm 機砲，對步兵與輕裝甲載具很有效。 襲擊三輪車易受多數武器損傷，不過大口徑砲（戰鬥坦克）的效果稍差。 摘要： - 爆炸半徑：小 - 視野：大 - 強對輕步兵、裝甲兵、飛彈坦克、偏導車、三輪車 - 弱對戰鬥坦克、攻城坦克、擲彈兵、薩督卡、飛彈四輪車

actor-stealth-raider =
    .name = 隱形襲擊三輪車
    .description = 會隱形的襲擊三輪車。 強對步兵與輕型載具 弱對坦克
    .encyclopedia = 襲擊三輪車的隱形版本，適合偷襲。機槍開火時會現形。 摘要： - 爆炸半徑：小 - 視野：大 - 強對輕步兵、裝甲兵、飛彈坦克、偏導車、三輪車 - 弱對戰鬥坦克、攻城坦克、擲彈兵、薩督卡 提示：攻城坦克與飛彈坦克可以射到視野之外。用隱形襲擊三輪車延伸它們的視野，就能以最大射程射擊。

actor-deviator =
    .name = 偏導車
    .description = 發射能改變敵方載具 陣營歸屬的彈頭。
    .encyclopedia = 發射飛彈並釋放矽雲，暫時改變目標載具的陣營。人員只會受到矽雲輕微影響。 偏導車易受多數武器損傷，大口徑砲的效果稍差。 摘要： - 爆炸半徑：小 - 視野：中 - 強對戰鬥坦克、飛彈四輪車、毀滅者、飛彈坦克 - 弱對任何步兵、飛彈坦克、音波坦克、三輪車 提示：偏導車的重新裝填時間很長。在你的混編中讓部分偏導車停火，以便機會出現時總有飛彈可用。


meta-combat-tank-description = 主力戰鬥坦克。 強對坦克 弱對步兵

actor-combat-tank-a =
    .name = 亞崔迪戰鬥坦克
    .encyclopedia = 對多數載具有效，但較不適合對付輕裝甲目標。 能抵抗子彈與重爆裂物，但易受飛彈與大口徑砲損傷。亞崔迪戰鬥坦克在機動與裝甲之間取得不錯的平衡，並有些微射程優勢。 摘要： - 爆炸半徑：中 - 視野：中 - 強對戰鬥坦克、攻城坦克、飛彈四輪車、音波坦克 - 弱對裝甲兵、飛彈坦克、毀滅者、偏導車 亞崔迪坦克加成：射程較佳

actor-combat-tank-h =
    .name = 哈肯尼戰鬥坦克
    .encyclopedia = 對多數載具有效，但較不適合對付輕裝甲目標。 比其他型號更堅固，但也更慢，射速也更低。 摘要： - 爆炸半徑：中 - 視野：中 - 強對戰鬥坦克、攻城坦克、飛彈四輪車、音波坦克 - 弱對裝甲兵、飛彈坦克、毀滅者、偏導車 哈肯尼坦克加成：裝甲更強

actor-combat-tank-o =
    .name = 奧多斯戰鬥坦克
    .encyclopedia = 對多數載具有效，但較不適合對付輕裝甲目標。 戰鬥坦克中速度最快的型號，但也最脆弱。射速優於其他型號。 摘要： - 爆炸半徑：中 - 視野：中 - 強對戰鬥坦克、攻城坦克、飛彈四輪車、音波坦克 - 弱對裝甲兵、飛彈坦克、毀滅者、偏導車 奧多斯坦克加成：射速

meta-destroyabletile =
    .generic-name = 通道（可摧毀）
    .name = 通道（可摧毀）

meta-destroyedtile =
    .generic-name = 通道（可修復）
    .name = 通道（可修復）

## ai.yaml
bot-omnius =
    .name = 奧姆尼烏斯

bot-vidius =
    .name = 維迪烏斯

bot-gladius =
    .name = 格拉迪烏斯

## map-generators.yaml
label-random-map = 隨機地圖
label-clear-map-generator-option-tile = 圖塊
label-clear-map-generator-choice-tile-sand =
   .label = 沙地
label-clear-map-generator-choice-tile-concrete =
   .label = 混凝土
label-clear-map-generator-choice-tile-dune =
   .label = 沙丘
label-clear-map-generator-choice-tile-rock =
   .label = 岩石
label-clear-map-generator-choice-tile-platform =
   .label = 平台

label-d2k-map-generator-option-seed = 種子
label-d2k-map-generator-option-terrain-type = 地形類型
label-d2k-map-generator-choice-terrain-type-rocky =
   .label = 多岩
label-d2k-map-generator-choice-terrain-type-rough =
   .label = 崎嶇
label-d2k-map-generator-choice-terrain-type-flat =
   .label = 平坦
label-d2k-map-generator-choice-terrain-type-pockets =
   .label = 袋狀
label-d2k-map-generator-option-players = 玩家

label-d2k-map-generator-option-symmetry = 對稱
label-d2k-map-generator-choice-mirror-none =
   .label = 無
label-d2k-map-generator-choice-symmetry-mirror-horizontal =
   .label = 水平鏡像
label-d2k-map-generator-choice-symmetry-mirror-vertical =
   .label = 垂直鏡像
label-d2k-map-generator-choice-symmetry-mirror-diagonal-tl =
   .label = 對角鏡像（左上）
label-d2k-map-generator-choice-symmetry-mirror-diagonal-tr =
   .label = 對角鏡像（右上）
label-d2k-map-generator-choice-symmetry-mirror-2-rotations =
   .label = 2 次旋轉
label-d2k-map-generator-choice-symmetry-mirror-3-rotations =
   .label = 3 次旋轉
label-d2k-map-generator-choice-symmetry-mirror-4-rotations =
   .label = 4 次旋轉
label-d2k-map-generator-choice-symmetry-mirror-5-rotations =
   .label = 5 次旋轉
label-d2k-map-generator-choice-symmetry-mirror-6-rotations =
   .label = 6 次旋轉
label-d2k-map-generator-choice-symmetry-mirror-7-rotations =
   .label = 7 次旋轉
label-d2k-map-generator-choice-symmetry-mirror-8-rotations =
   .label = 8 次旋轉

label-d2k-map-generator-option-resources = 資源
label-d2k-map-generator-choice-resources-none =
   .label = 無
label-d2k-map-generator-choice-resources-low =
   .label = 低
label-d2k-map-generator-choice-resources-medium =
   .label = 中
label-d2k-map-generator-choice-resources-high =
   .label = 高
label-d2k-map-generator-choice-resources-very-high =
   .label = 非常高
label-d2k-map-generator-choice-resources-full =
   .label = 全滿

label-d2k-map-generator-option-worms = 沙蟲
label-d2k-map-generator-choice-worms-none =
   .label = 無
label-d2k-map-generator-choice-worms-low =
   .label = 低
label-d2k-map-generator-choice-worms-medium =
   .label = 中
label-d2k-map-generator-choice-worms-high =
   .label = 高

label-d2k-map-generator-option-density = 密度
label-d2k-map-generator-choice-density-players =
   .label = 隨玩家數調整
label-d2k-map-generator-choice-density-area-and-players =
   .label = 隨大小與玩家數調整
label-d2k-map-generator-choice-density-area-very-low =
   .label = 非常低
label-d2k-map-generator-choice-density-area-low =
   .label = 低
label-d2k-map-generator-choice-density-area-medium =
   .label = 中
label-d2k-map-generator-choice-density-area-high =
   .label = 高
label-d2k-map-generator-choice-density-area-very-high =
   .label = 非常高
