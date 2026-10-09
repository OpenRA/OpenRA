## player.yaml
options-tech-level =
    .low = 低
    .medium = 中等
    .no-powers = 无超级武器
    .unrestricted = 无限制

checkbox-automatic-concrete =
    .label = 自动铺设混凝土
    .description = 建筑物下方自动铺设混凝土地基

notification-insufficient-funds = 资金不足。
notification-new-construction-options = 新的建筑选项。
notification-cannot-deploy-here = 无法在此部署。
notification-low-power = 电力不足。
notification-base-under-attack = 基地受到攻击。
notification-ally-under-attack = 我们的盟友正在受到攻击。
notification-harvester-under-attack = 采集器正受到攻击。
notification-silos-needed = 需要粮仓。
notification-no-room-for-new-unit = 没有空间建造新单位。
notification-cannot-build-here = 无法在此建造。
notification-one-of-our-buildings-has-been-captured = 我们的建筑之一已被占领。

## world.yaml
notification-game-saved = 游戏已保存。

dropdown-map-worms =
    .label = 蛆虫
    .description = 蛆虫在地图上游荡，吞噬毫无准备的部队

options-starting-units =
    .mcv-only = 仅MCV
    .light-support = 轻型支援
    .heavy-support = 重型支援
    .carryall = MCV + 运输机

resource-spice = 香料

faction-random =
    .name = 任意
    .description = 随机家族
    游戏开始时随机选择一个家族

faction-atreides =
    .name = 奥特瑞德斯
    .description = 奥特瑞德斯家族
    奥特瑞德斯家族，来自富水世界卡兰丹，
    依靠他们的飞翼机确保制空权。
    他们与弗雷曼人结盟，这些可怕的
    地中海战士可以在战役中无声无息地移动。

    家族变种：
        - 战斗坦克在速度和耐久性方面平衡

    特殊单位：
        - 霰弹兵
        - 弗雷曼战士
        - 音波坦克

    超级武器：
        - 空袭

faction-harkonnen =
    .name = 哈康南
    .description = 哈康南家族
    恶毒的哈康南不会为了获得香料控制权而不择手段。
    他们依靠蛮力和原子武器来实现他们的目标：
    财富，以及摧毁奥特瑞德斯家族。

    家族变种：
        - 战斗坦克更加耐用但移动速度较慢

    特殊单位：
        - 沙杜卡尔
        - 毁灭者

    超级武器：
        - 死亡之手导弹

faction-ordos =
    .name = 奥多斯
    .description = 奥多斯家族
    来自冰冻世界Sigma Draconis IV的阴险奥多斯以
    财富、贪婪和背叛而闻名。他们经常求助于雇佣兵、破坏活动，
    和禁忌的伊克西亚科技来获得优势。

    家族变种：
        - 三轮摩托车被突袭三轮摩托车取代
        - 战斗坦克更快但耐久性较差

    特殊单位：
        - 突袭三轮摩托车
        - 隐形突袭三轮摩托车
        - 破坏者
        - 变形者

faction-corrino =
    .name = 科林诺

faction-mercenaries =
    .name = 雇佣兵

faction-smugglers =
    .name = 走私者

faction-fremen =
    .name = 弗雷曼人

map-generator-d2k = 地图生成器
map-generator-clear = 平坦地形

## defaults.yaml
notification-unit-lost = 单位损失。
notification-unit-promoted = 单位晋升。
notification-enemy-building-captured = 敌方建筑被占领。
notification-primary-building-selected = 主要建筑被选中。

## aircraft.yaml
actor-carryall-reinforce =
    .name = 运输机
    .description =
    有大型翅膀、只能在星球上航行的飞船
    自动将采集器从香料田运送到精炼厂。
    当指令时将车辆运送到维修平台。

actor-carryall-encyclopedia =
    自动运输采集器在香料田和精炼厂之间。当指挥时，他们还能拾取单位并将其运送到维修平台。

    运输机是一种轻装甲的运输飞机。它易受导弹攻击，只能被防空武器击中。

actor-frigate-name = 驱逐舰

actor-ornithopter =
    .name = 飞翼机
    .encyclopedia =
    诺顿星球最快的飞机，它有轻装甲，能投掷500磅炸弹。对步兵和轻装甲目标效果极佳，能损害其他装甲类型。

actor-ornithopter-husk-name = 飞翼机
actor-carryall-husk-name = 运输机
actor-carryall-huskvtol-name = 运输机

## arrakis.yaml
notification-worm-attack = 蛆虫攻击。
notification-worm-sign = 蛆虫踪迹。

actor-spicebloom-spawnpoint-name = 香料盛开生成点
actor-spicebloom-name = 香料盛开
actor-sandworm-name = 沙虫
actor-sietch-name = 弗雷曼定居点

## defaults.yaml
meta-vehicle-generic-name = 单位
meta-husk-generic-name = 被摧毁的单位
meta-aircrafthusk-generic-name = 单位
meta-infantry-generic-name = 单位
meta-plane-generic-name = 单位
meta-building-generic-name = 建筑

## husks.yaml
actor-mcv-husk-name = 移动建设车辆 (已摧毁)
actor-harvester-husk-name = 香料采集器 (已摧毁)
actor-siege-tank-husk-name = 攻城坦克 (已摧毁)
actor-missile-tank-husk-name = 导弹坦克 (已摧毁)
actor-sonic-tank-husk-name = 音波坦克 (已摧毁)
actor-devastator-husk-name = 毁灭者 (已摧毁)
actor-deviator-husk-name = 变形者 (已摧毁)
meta-combat-tank-husk-name = 战斗坦克 (已摧毁)

## infantry.yaml
actor-light-inf =
    .name = 轻装步兵
    .description =
    通用型步兵。
      对步兵强
      对载具和火炮弱
    .encyclopedia =
    轻装甲的步兵，配备9毫米RP突击步枪。他们对步兵和轻装甲载具有效果。

    轻装步兵对导弹和大口径火炮有抗性，但对高爆、火和小口径武器非常脆弱。

    概要：

        - 爆炸半径：小
        - 视野：非常小
        - 对轻装步兵、步兵、导弹坦克、变形者强
        - 对战斗坦克、攻城坦克、霰弹兵、三轮摩托、音波坦克弱

actor-engineer =
    .name = 工程师
    .description =
    渗透并占领敌方
    建筑。
      对建筑强
      对一切弱
      修复损坏的悬崖
    .encyclopedia =
    可用于占领敌方建筑。

    工程师对反坦克武器有抗性，但对高爆、火和小口径武器非常脆弱。

    工程师可以重新激活被摧毁的残骸到几乎功能状态。这允许向最近的维修平台发送残骸以取得完全修复。

actor-trooper =
    .name = 步兵
    .description =
    反坦克步兵。
      对坦克强
      对步兵和火炮弱
    .encyclopedia =
    武装着线导、穿甲导弹战斗部，步兵对载具和建筑效果极好，但在对抗步兵方面遇到困难。

    步兵对反坦克武器有抗性，但非常脆弱于高爆、火和子弹武器。

    概要：

        - 爆炸半径：中等
        - 视野：小
        - 对战斗坦克、导弹坦克、四轮摩托、三轮摩托、变形者、建筑、防御强
        - 对攻城坦克、轻装步兵、霰弹兵、音波坦克弱

actor-thumper =
    .name = 振动步兵
    .description =
    部署时吸引附近的蠕虫。
      无武器
    .encyclopedia =
    部署一个响亮的锤击装置，吸引沙虫到该区域。

actor-fremen =
    .name = 弗雷曼战士
    .description =
    用突击步枪和火箭的精英步兵单位。
      对步兵和载具强
      对火炮弱
      特殊能力：隐形
    .encyclopedia =
    诺顿星球的本土沙漠战士，配备10毫米突击步枪和火箭。他们的火力对步兵和载具同样有效。

    弗雷曼战士单位对高爆和子弹武器非常脆弱。

    概要：

        - 爆炸半径：中等
        - 视野：小
        - 对四轮摩托、三轮摩托、导弹坦克、战斗坦克、毁灭者、建筑、防御强
        - 对攻城坦克、轻装步兵、霰弹兵、音波坦克弱

actor-grenadier =
    .name = 霰弹兵
    .description =
    装有手榴弹的步兵。
      对建筑和步兵强
      对载具弱
    .encyclopedia =
    一种对抗建筑的步兵炮兵单位。它们被杀死时有爆炸的可能性，因此不应将它们聚集在一起。

    概要：

        - 爆炸半径：大
        - 视野：小
        - 对轻装步兵、三轮摩托、导弹坦克、战斗坦克、建筑、防御强
        - 对攻城坦克、战斗坦克、音波坦克、毁灭者弱

actor-sardaukar =
    .name = 沙杜卡尔
    .description =
    精英科林诺突击步兵。
      对步兵和载具强
      对火炮弱
    .encyclopedia =
    强大的重装步兵配备机枪，对步兵有效，火箭发射器用于攻击载具。当被压住时，单位爆炸并损害上方的载具。

    概要：

        - 爆炸半径：大
        - 视野：小
        - 对三轮摩托、四轮摩托、导弹坦克、战斗坦克、建筑、防御强
        - 对攻城坦克、音波坦克、霰弹兵、坦克压碎弱

actor-mpsardaukar-description =
    精英哈康南突击步兵。
      对步兵和载具强
      对火炮弱

actor-saboteur =
    .name = 破坏者
    .description =
    带爆炸物的狡猾步兵。
    暂时隐形。
      对建筑强
      对一切弱
      特殊能力：摧毁建筑
    .encyclopedia =
    奥多斯家族的专门军事单位，能够破坏敌方建筑和载具进入时，但在随之而来的爆炸中死亡。它可以激活自爆并损害附近的敌方单位。

    破坏者对反坦克武器有抗性，但对高爆、火和子弹武器非常脆弱。

actor-nsfremen-description =
    用突击步枪和火箭的精英步兵单位。
      对步兵和载具强
      对火炮弱

## misc.yaml
actor-crate-name = 箱子
actor-mpspawn-name = （多人游戏起始点）
actor-waypoint-name = （脚本行为路径点）
actor-camera-name = （揭示区域给所有者）
actor-wormspawner-name = （蠕虫生成地点）

actor-upgrade-conyard =
    .name = 建设中心升级
    .description =
    解锁更多建造选项：
    - 大型混凝土板
    - 火箭炮塔

actor-upgrade-barracks =
    .name = 兵营升级
    .description =
    解锁更多步兵：
    - 步兵
    - 工程师
    - 振动步兵

    解锁家族特定步兵：
    - 奥特瑞德斯：霰弹兵
    - 哈康南：沙杜卡尔

actor-upgrade-light =
    .name = 轻型工厂升级
    .description =
    解锁更多轻型单位：
    - 导弹四轮摩托

    解锁一个家族特定的轻型单位：
    - 奥多斯：隐形突袭三轮摩托

actor-upgrade-heavy =
    .name = 重型工厂升级
    .description =
    解锁更多建造选项：
    - IX研究中心

    解锁更多重型单位：
    - 攻城坦克
    - 导弹坦克
    - MCV

actor-upgrade-hightech =
    .name = 高科技工厂升级
    .description =
    解锁奥特瑞德斯空袭超级武器。

actor-deathhand =
    .name = 死亡之手
    .encyclopedia =
    装备原子集束弹药，以上方为目标，造成大面积巨大损害。

## structures.yaml
notification-construction-complete = 建造完成。
notification-unit-ready = 单位准备就绪。
notification-repairing = 正在修理。
notification-unit-repaired = 单位已完成修理。
notification-select-target = 选择目标。
notification-missile-launch-detected = 检测到导弹发射。
notification-airstrike-ready = 空袭准备就绪。
notification-building-lost = 建筑损失。
notification-reinforcements-have-arrived = 救援到达。
notification-death-hand-missile-prepping = 死亡之手导弹准备中。
notification-death-hand-missile-ready = 死亡之手导弹准备就绪。
notification-fremen-ready = 弗雷曼准备就绪。
notification-saboteur-ready = 破坏者准备就绪。

meta-concrete =
    .generic-name = 建筑
    .description =
    提供一个坚固的基础，
    抗地形损害。

actor-concrete-a =
    .name = 混凝土板
    .encyclopedia =
    在混凝土板上建造的建筑不会受到诺顿恶劣沙漠环境的持续损害。虽然可以修理，但将结构建在混凝土上可防止持续的天气侵蚀。

    混凝土对大多数武器都脆弱，一旦损坏就无法修复。

actor-concrete-b-name = 大型混凝土板

actor-construction-yard =
    .name = 建设中心
    .description = 生产建筑。
    .encyclopedia =
    充当任何建立在阿瑞克萨斯上的基地的基础，建设中心生产少量电力并允许新建建筑。保护此建筑！它对基地的成功至关重要。

    建设中心相当坚固，但对各种武器都脆弱。

actor-wind-trap =
    .name = 风力塔
    .description =
    为其他建筑提供电力。
    
    .encyclopedia =
    为您的基地生产电力和水。大型、地面的管道将风流引导到地下，进入巨大的涡轮机，驱动电力发生器和湿度提取器。

    风力塔对大多数武器都有脆弱。

actor-barracks =
    .name = 兵营
    .description = 训练步兵。
    .encyclopedia =
    生产和训练轻型步兵单位所必需，后期任务中可以升级以训练更高级的步兵。

    兵营对大多数武器都有脆弱。

actor-refinery =
    .name = 香料精炼厂
    .description =
    采集器在这里卸下香料
    用于加工。
    .encyclopedia =
    诺顿上所有香料生产的基石。采集器将开采的香料运送到精炼厂，在那里将其转换为信用点。精炼香料自动分配到储藏库和精炼厂存储。每个精炼厂都能存储香料。一旦建造精炼厂，采集器就会由运输机运送。

    精炼厂对大多数武器都有脆弱。

actor-silo =
    .name = 储藏库
    .description = 存储多余的采集香料。
    .encyclopedia =
    存储开采的香料。任何来自精炼厂的盈余都平均分配给所有可用的储藏库。如果存储容量已满，多余的香料会丢失。被摧毁或被占领的储藏库会重新分配其内容，只要空间足够。

    香料储藏库对大多数武器都有脆弱。

actor-light-factory =
    .name = 轻型工厂
    .description = 生产轻型载具。
    .encyclopedia =
    生产小型、轻装甲战斗载具所必需。在后期任务中可以升级以制造更先进的轻型载具。

    轻型工厂对大多数武器都有脆弱。

actor-heavy-factory =
    .name = 重型工厂
    .description = 生产重型载具。
    .encyclopedia =
    允许建造重型载具，如采集器和战斗坦克。经过升级后，它可以解锁高级载具，但有些可能需要额外的建筑。

    重型工厂对大多数武器都有脆弱。

actor-outpost =
    .name = 前哨站
    .description =
    提供战场的雷达地图。
    需要电力来操作。
    能检测隐形单位。
    .encyclopedia =
    当有足够的电力后，雷达前哨站会激活，提供雷达地图。

    雷达前哨站对大多数武器都有脆弱。

actor-starport =
    .name = 星港
    .description = 快速支援的着陆区，代价昂贵。
    .encyclopedia =
    解锁与CHOAM商人公会的星际贸易，在那里可以以不同价格购买载具和飞行单位。此设施对于从公会获取单位至关重要。

    即使有重型装甲，星港对大多数武器都有脆弱。

actor-wall =
    .name = 混凝土墙
    .generic-name = 建筑
    .description = 阻挡单位和阻挡敌方火力。
    .encyclopedia =
    诺顿上最有效的防御障碍，阻挡坦克火力并阻碍单位移动。

    墙只能被爆炸武器、导弹和炮弹损坏。和混凝土板一样，一旦损坏就无法修复。

actor-medium-gun-turret =
    .name = 火炮炮塔
    .description =
    防御建筑。能检测隐形单位。
      对轻型载具强
      对步兵中等
      对坦克和飞机弱
    .encyclopedia =
    中等射程的武器，能对所有类型的载具有效，特别是重装甲载具。它会在其范围内自动开火任何敌方单位，并需要电力操作。

    火炮炮塔对小口径武器和爆炸武器有抗性，但对导弹和高爆武器脆弱。

actor-large-gun-turret =
    .name = 火箭炮塔
    .description =
    防御建筑。能检测隐形单位。
    需要电力操作。
      对坦克、飞机、移动目标强
      对步兵弱
    .encyclopedia =
    改进的防御建筑，射程更长，射击速度比火炮炮塔快。其先进的瞄准系统需要电力操作。

    火箭炮塔对枪械和爆炸武器有抗性，但对导弹和大口径火炮脆弱。

actor-repair-pad =
    .name = 维修平台
    .description =
    修理载具。
    允许建造MCV。
    .encyclopedia =
    以生产成本的一小部分修理单位。

    维修平台对大多数武器都有脆弱。

actor-high-tech-factory =
    .name = 高科技工厂
    .description = 解锁先进技术。
    .airstrikepower-name = 空袭
    .airstrikepower-description = 飞翼机轰炸目标。
    .encyclopedia =
    生产空中单位，并需要建造运输机。奥特瑞德斯家族可以在后期任务中升级此设施以建造飞翼机进行空袭。

    高科技工厂对大多数武器都有脆弱。

actor-research-centre =
    .name = IX研究中心
    .description = 解锁高级坦克。
    .encyclopedia =
    为建筑和载具提供技术升级。该设施需要开发高级特殊武器和原型。

    IX研究中心对大多数武器都有脆弱。

actor-palace =
    .name = 宫殿
    .description = 解锁精英步兵和武器。
    .encyclopedia =
    一旦建造，就作为指挥中心发挥作用，提供额外选项和特殊武器。

    即使有重型装甲，宫殿对大多数武器都有脆弱。
    .nukepower-name = 死亡之手
    .nukepower-description = 向目标地点发射原子导弹。
    .produceactorpower-fremen-name = 征募弗雷曼
    .produceactorpower-fremen-description = 用突击步枪和火箭的精英步兵单位。
      对步兵和载具强
      对火炮弱
      特殊能力：隐形
    .produceactorpower-saboteur-name = 征募破坏者
    .produceactorpower-s sabotur-description = 带着爆炸物的狡猾步兵。
    可以部署成为隐形一段时间。
      对建筑强
      对一切弱
      特殊能力：摧毁建筑

## vehicles.yaml
actor-mcv =
    .name = 移动建设车辆
    .description =
    部署为建设中心。
      无武器
    .encyclopedia =
    必须驾驶到一个可以部署的区域。在找到合适的岩石表面后，MCV可以转换为建设中心。

    MCV对子弹和轻爆炸物有抗性。它们对导弹和大口径火炮脆弱。

actor-harvester =
    .name = 香料采集器
    .description =
    收集香料用于加工。
      无武器
    .encyclopedia =
    抗子弹和一定程度的高爆炸药。它们对导弹和大口径火炮脆弱。

    采集器随精炼厂一起提供。

actor-trike =
    .name = 三轮摩托
    .description =
    快速侦察。
      对步兵强
      对坦克弱
    .encyclopedia =
    轻装甲、三轮载具，配备重机枪，对步兵和轻装甲载具有效果。

    三轮摩托对大多数武器都有脆弱，大口径火炮对其稍不那么有效。

    概要：

        - 爆炸半径：小
        - 视野：中等
        - 对轻装步兵、步兵、导弹坦克、变形者强
        - 对战斗坦克、攻城坦克、霰弹兵、沙杜卡尔弱

    提示：三轮摩托对轻装步兵有0.5射程优势。当轻装步兵过于接近时立即将其移开。

actor-quad =
    .name = 导弹四轮摩托
    .description =
    导弹侦察。
      对载具强
      对步兵弱
    .encyclopedia =
    在装甲和火力方面优于三轮摩托，四轮载具发射穿甲火箭。它对大多数载具有效。

    四轮摩托对子弹，以及一定程度的爆炸有抗性。它们对导弹和大口径火炮脆弱。

    概要：

        - 爆炸半径：中等
        - 视野：中等
        - 对攻城坦克、音波坦克、建筑强
        - 对战斗坦克、步兵、导弹坦克弱

    提示：四轮摩托对移动目标有较大误差。尽可能接近目标以获得最大伤害。

actor-siege-tank =
    .name = 攻城坦克
    .description =
    攻城火炮。
      对步兵和建筑强
      对坦克弱
    .encyclopedia =
    对步兵和轻装甲载具效果极佳，但对重装甲目标表现差。它有很长的射程。

    攻城坦克对子弹和某种程度的爆炸有抗性。它们对导弹和大口径火炮脆弱。

    大量高爆炸药造成了车辆被摧毁后的巨大爆炸

    概要：

        - 爆炸半径：大
        - 视野：大
        - 对任何步兵、三轮摩托、变形者强
        - 对战斗坦克、四轮摩托、导弹坦克弱

    提示：攻城坦克可以超越它们的视野射程射击。

actor-missile-tank =
    .name = 导弹坦克
    .description =
    火箭火炮。
      对载具、建筑和飞机强
      对步兵弱
    .encyclopedia =
    防空并能有效对付大多数目标，除了步兵。

    导弹坦克对大多数武器都脆弱，大口径火炮对其稍不那么有效。

    概要：

        - 爆炸半径：中等
        - 视野：大
        - 对战斗坦克、毁灭者、四轮摩托强
        - 对任何步兵类型、三轮摩托、隐形突袭三轮摩托车弱

    提示：导弹坦克可以超越其视野射程射击。


actor-sonic-tank =
    .name = 音波坦克
    .description =
    发射声波。
      对步兵和载具强
      对火炮弱
    .encyclopedia =
    对步兵和轻装甲载具最有效，但对装甲目标效果差。

    它的声波损害路径上的所有单位。

    抗子弹和小爆炸，但对导弹和大口径火炮脆弱。

    概要：

        - 爆炸半径：极大
        - 视野：中等
        - 强对：任何步兵、攻城坦克、导弹坦克、变形者
        - 弱对：战斗坦克、四轮摩托、毁灭者

    提示：声波强度随射程增加。尝试在最大射程射击以获得最大伤害。

actor-devastator =
    .name = 毁灭者
    .description =
    超重型坦克。
      对坦克强
      对火炮弱
    .encyclopedia =
    诺顿最强大的坦克，行动缓慢但能有效对付大多数单位。它发射双等离子电荷，可以接受指令自爆，损害附近的单位和建筑。

    抗子弹和高爆炸药，但对导弹和大口径火炮脆弱。

    概要：

        - 爆炸半径：大
        - 视野：中等
        - 强对：战斗坦克、攻城坦克、轻型步兵、音波坦克
        - 弱对：步兵、导弹坦克、变形者

    提示：毁灭者对大量步兵脆弱。改用自爆。

actor-raider =
    .name = 突袭三轮摩托车
    .description =
    改进的侦察。
      对步兵和轻载具强
      对坦克弱
    .encyclopedia =
    奥多斯家族升级的突袭三轮摩托，火力、速度和装甲都增强。装备双20毫米加农炮，对步兵和轻装甲载具有效果。

    突袭者对大多数武器脆弱，但大口径火炮（战斗坦克）对其稍不那么有效。

    概要：

        - 爆炸半径：小
        - 视野：小
        - 强对：轻装步兵、步兵、导弹坦克、变形者、三轮摩托
        - 弱对：战斗坦克、攻城坦克、霰弹兵、沙杜卡尔、四轮摩托

actor-stealth-raider =
    .name = 隐形突袭三轮摩托车
    .description =
    隐形突袭三轮摩托车。
      对步兵和轻载具强
      对坦克弱
    .encyclopedia =
    突袭者的隐形版本，适合潜行攻击。射击时会解除隐形。

    概要：

        - 爆炸半径：小
        - 视野：小
        - 强对：轻装步兵、步兵、导弹坦克、变形者、三轮摩托
        - 弱对：战斗坦克、攻城坦克、霰弹兵、沙杜卡尔

    提示：攻城和导弹坦克可以超越其视野射程。使用隐形突袭者扩大其视野，以便能从最大射程射击。

actor-deviator =
    .name = 变形者
    .description =
    发射改变
    敌方载具忠诚度的弹头。
    .encyclopedia =
    发射释放硅云的导弹，临时改变目标载具的忠诚度。人员只受云影响较小。

    变形者对大多数武器脆弱，大口径火炮对其稍不那么有效。

    概要：

        - 爆炸半径：小
        - 视野：大
        - 强对：战斗坦克、四轮摩托、毁灭者、导弹坦克
        - 弱对：任何步兵、导弹坦克、音波坦克、三轮摩托

    提示：变形者重新装载时间非常长。在混合中保持一些变形者，以便在机会出现时始终准备有导弹。


meta-combat-tank-description =
    主战坦克。
      对坦克强
      对步兵弱

actor-combat-tank-a =
    .name = 奥特瑞德斯战斗坦克
    .encyclopedia =
    对大多数载具有效果但不太适合对付轻装甲目标。

    抗子弹和重爆炸，但对导弹和大口径火炮脆弱。奥特瑞德斯战斗坦克是机动性和装甲的良好的折中，略微有射程优势。

    概要：

        - 爆炸半径：中等
        - 视野：中等
        - 对战斗坦克、攻城坦克、四轮摩托、音波坦克强
        - 对步兵、导弹坦克、毁灭者、变形者弱

    奥特瑞德斯坦克加成：更好的射程

actor-combat-tank-h =
    .name = 哈康南战斗坦克
    .encyclopedia =
    对大多数载具有效果但不太适合对付轻装甲目标。

    比其同类更强大，但移动速度慢，射速较低。

    概要：

        - 爆炸半径：中等
        - 视野：中等
        - 对战斗坦克、攻城坦克、四轮摩托、音波坦克强
        - 对步兵、导弹坦克、毁灭者、变形者弱

    哈康南坦克加成：更强大的装甲

actor-combat-tank-o =
    .name = 奥多斯战斗坦克
    .encyclopedia =
    对大多数载具有效果但不太适合对付轻装甲目标。

    最快的战斗坦克变种，但也是最弱的。比其同类有更好的射速。

    概要：

        - 爆炸半径：中等
        - 视野：中等
        - 对战斗坦克、攻城坦克、四轮摩托、音波坦克强
        - 对步兵、导弹坦克、毁灭者、变形者弱

    奥多斯坦克加成：射速

meta-destroyabletile =
    .generic-name = 通道（可摧毁）
    .name = 通道（可摧毁）

meta-destroyedtile =
    .generic-name = 通道（可修复）
    .name = 通道（可修复）

## ai.yaml
bot-omnius =
    .name = 罗摩

bot-vidius =
    .name = 维迪乌斯

bot-gladius =
    .name = 格拉迪乌斯

## map-generators.yaml
label-random-map = 随机地图
label-clear-map-generator-option-tile = 格子
label-clear-map-generator-choice-tile-sand =
   .label = 沙子
label-clear-map-generator-choice-tile-concrete =
   .label = 混凝土
label-clear-map-generator-choice-tile-dune =
   .label = 沙丘
label-clear-map-generator-choice-tile-rock =
   .label = 岩石
label-clear-map-generator-choice-tile-platform =
   .label = 平台

label-d2k-map-generator-option-seed = 种子
label-d2k-map-generator-option-terrain-type = 地形类型
label-d2k-map-generator-choice-terrain-type-rocky =
   .label = 岩石
label-d2k-map-generator-choice-terrain-type-rough =
   .label = 粗糙
label-d2k-map-generator-choice-terrain-type-flat =
   .label = 平坦
label-d2k-map-generator-choice-terrain-type-pockets =
   .label = 零碎
label-d2k-map-generator-option-players = 玩家

label-d2k-map-generator-option-symmetry = 对称
label-d2k-map-generator-choice-mirror-none =
   .label = 无
label-d2k-map-generator-choice-symmetry-mirror-horizontal =
   .label = 水平镜像
label-d2k-map-generator-choice-symmetry-mirror-vertical =
   .label = 垂直镜像
label-d2k-map-generator-choice-symmetry-mirror-diagonal-tl =
   .label = 对角线镜像（左上）
label-d2k-map-generator-choice-symmetry-mirror-diagonal-tr =
   .label = 对角线镜像（右上）
label-d2k-map-generator-choice-symmetry-mirror-2-rotations =
   .label = 2个旋转
label-d2k-map-generator-choice-symmetry-mirror-3-rotations =
   .label = 3个旋转
label-d2k-map-generator-choice-symmetry-mirror-4-rotations =
   .label = 4个旋转
label-d2k-map-generator-choice-symmetry-mirror-5-rotations =
   .label = 5个旋转
label-d2k-map-generator-choice-symmetry-mirror-6-rotations =
   .label = 6个旋转
label-d2k-map-generator-choice-symmetry-mirror-7-rotations =
   .label = 7个旋转
label-d2k-map-generator-choice-symmetry-mirror-8-rotations =
   .label = 8个旋转

label-d2k-map-generator-option-resources = 资源
label-d2k-map-generator-choice-resources-none =
   .label = 无
label-d2k-map-generator-choice-resources-low =
   .label = 低
label-d2k-map-generator-choice-resources-medium =
   .label = 中等
label-d2k-map-generator-choice-resources-high =
   .label = 高
label-d2k-map-generator-choice-resources-very-high =
   .label = 很高
label-d2k-map-generator-choice-resources-full =
   .label = 全部

label-d2k-map-generator-option-worms = 蛆虫
label-d2k-map-generator-choice-worms-none =
   .label = 无
label-d2k-map-generator-choice-worms-low =
   .label = 低
label-d2k-map-generator-choice-worms-medium =
   .label = 中等
label-d2k-map-generator-choice-worms-high =
   .label = 高

label-d2k-map-generator-option-density = 密度
label-d2k-map-generator-choice-density-players =
   .label = 随玩家数量调整
label-d2k-map-generator-choice-density-area-and-players =
   .label = 随大小和玩家数量调整
label-d2k-map-generator-choice-density-area-very-low =
   .label = 非常低
label-d2k-map-generator-choice-density-area-low =
   .label = 低
label-d2k-map-generator-choice-density-area-medium =
   .label = 中等
label-d2k-map-generator-choice-density-area-high =
   .label = 高
label-d2k-map-generator-choice-density-area-very-high =
   ..label = 非常高
