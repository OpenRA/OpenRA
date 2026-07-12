## player.yaml
options-tech-level =
    .low = 低
    .medium = 中等
    .no-powers = 无超级能力
    .unrestricted = 无限制

checkbox-automatic-concrete =
    .label = 自动混凝土
    .description = 建筑物下自动铺设混凝土基础

notification-insufficient-funds = 资金不足。
notification-new-construction-options = 新的建造选项。
notification-cannot-deploy-here = 无法在此处部署。
notification-low-power = 电力不足。
notification-base-under-attack = 基地正受到攻击。
notification-ally-under-attack = 我们的同盟正受到攻击。
notification-harvester-under-attack = 收割者正受到攻击。
notification-silos-needed = 需要筒仓。
notification-no-room-for-new-unit = 没有空间建造新单位。
notification-cannot-build-here = 无法在此处建造。
notification-one-of-our-buildings-has-been-captured = 我们的某个建筑已被占领。

## world.yaml
notification-game-saved = 游戏已保存。

dropdown-map-worms =
    .label = 蠕虫
    .description = 蠕虫在地图上游荡，吞噬准备不足的部队

options-starting-units =
    .mcv-only = 仅MCV
    .light-support = 轻型支援
    .heavy-support = 重型支援
    .carryall = MCV + 运输机

resource-spice = 香料

faction-random =
    .name = 任意
    .description = 随机家族
    随机选择一个家族作为游戏开始

faction-atreides =
    .name = 阿特里德斯
    .description = 阿特里德斯家族
    崇高的阿特里德斯家族，来自水世界卡兰丹，
    依赖他们的飞翼来确保空中优势。
    他们与弗里曼，即可怕的Dune本地战士联盟，
    他们能够在战斗中不留痕迹地移动。

    家族变体：
        - 战斗坦克在速度和耐久性方面平衡

    特殊单位：
        - 手榴弹兵
        - 弗里曼
        - 音波坦克

    超级武器：
        - 空袭

faction-harkonnen =
    .name = 哈肯纳森
    .description = 哈肯纳森家族
    坏的哈肯纳森会为了获得香料控制权而不惜一切。
    他们依靠蛮力和原子弹武器实现他们的目标：
    财富，以及毁灭阿特里德斯家族。

    家族变体：
        - 战斗坦克更耐用但移动速度较慢

    特殊单位：
        - 萨达卡
        - 毁灭者

    超级武器：
        - 死亡之手导弹

faction-ordos =
    .name = 奥多斯
    .description = 奥多斯家族
    来自冰世界西格玛德克尼西IV，恶毒的奥多斯以
    财富、贪婪和背信弃义而闻名。他们经常求助于雇佣兵、破坏活动，
    以及被禁止的伊克技术来获得优势。

    家族变体：
        - 三轮摩托车被突袭三轮摩托所取代
        - 战斗坦克更快但耐久性较差

    特殊单位：
        - 突袭三轮摩托
        - 隐形突袭三轮摩托
        - 破坏者
        - 偏转者

faction-corrino =
    .name = 柯里诺

faction-mercenaries =
    .name = 雇佣兵

faction-smugglers =
    .name = 走私者

faction-fremen =
    .name = 弗里曼

map-generator-d2k = 地图生成器
map-generator-clear = 清除地形

## defaults.yaml
notification-unit-lost = 单位损失。
notification-unit-promoted = 单位晋升。
notification-enemy-building-captured = 敌方建筑被占领。
notification-primary-building-selected = 已选择主要建筑。

## aircraft.yaml
actor-carryall-reinforce =
    .name = 运输机
    .description =
    大型翼状、行星上的飞船
    自动将收割者运输到香料田和精炼厂之间。
    当被命令时，将载具运输到修理站。

actor-carryall-encyclopedia =
    自动在香料田和精炼厂之间运输收割者。当被命令时，它们也可以将单位运送到修理站。

    运输机是一种装甲较轻的运输飞机。它容易受到导弹攻击，只能被防空武器击中。

actor-frigate-name = 航空母舰

actor-ornithopter =
    .name = 飞翼战机
    .encyclopedia =
    Dune上最快飞机，装甲较轻，能投下500磅炸弹。对步兵和轻装甲目标非常有效，能够对其他装甲类型造成伤害。

actor-ornithopter-husk-name = 飞翼战机
actor-carryall-husk-name = 运输机
actor-carryall-huskvtol-name = 运输机

## arrakis.yaml
notification-worm-attack = 蠕虫攻击。
notification-worm-sign = 蠕虫迹象。

actor-spicebloom-spawnpoint-name = 香料盛开生成点
actor-spicebloom-name = 香料盛开
actor-sandworm-name = 沙虫
actor-sietch-name = 弗里曼根据地

## defaults.yaml
meta-vehicle-generic-name = 单位
meta-husk-generic-name = 已损毁的单位
meta-aircrafthusk-generic-name = 单位
meta-infantry-generic-name = 单位
meta-plane-generic-name = 单位
meta-building-generic-name = 建筑
## husks.yaml
actor-mcv-husk-name = 移动建造车（已损毁）
actor-harvester-husk-name = 香料收割者（已损毁）
actor-siege-tank-husk-name = 攻城坦克（已损毁）
actor-missile-tank-husk-name = 导弹坦克（已损毁）
actor-sonic-tank-husk-name = 音波坦克（已损毁）
actor-devastator-husk-name = 毁灭者（已损毁）
actor-deviator-husk-name = 偏转者（已损毁）
meta-combat-tank-husk-name = 战斗坦克（已损毁）

## infantry.yaml
actor-light-inf =
    .name = 轻型步兵
    .description =
    通用步兵。
      强于步兵
      弱于载具和火炮
    .encyclopedia =
    装备9mm RP突击步枪的轻甲步兵。他们对步兵和轻装甲载具非常有效。

    轻型步兵对导弹和大口径武器有抵抗力，但对高爆弹、火和小型武器非常脆弱。

    总结：

        - 爆炸半径：小
        - 视野：非常小
        - 强于：轻型步兵、突袭兵、导弹坦克、偏转者
        - 弱于：战斗坦克、攻城坦克、手榴弹兵、音波坦克

actor-engineer =
    .name = 工程兵
    .description =
    伪装并占领敌方
    建筑。
      强于建筑
      弱于所有东西
      修复被损坏的悬崖
    .encyclopedia =
    可用来占领敌方建筑。

    工程兵能抵抗反坦克武器，但对高爆弹、火和小型武器非常脆弱。

    工程兵可以重新激活已损毁的残骸至勉强可运行状态。这允许将残骸发送到最近的维修台进行完全修复。

actor-trooper =
    .name = 突袭兵
    .description =
    反坦克步兵。
      强于坦克
      弱于步兵和火炮
    .encyclopedia =
    装备着便携式、穿甲导弹的突袭兵，对载具和建筑非常有效，但在步兵方面显得力不从心。

    突袭兵能抵抗反坦克武器，但很容易受到高爆弹、火和子弹武器的攻击。

    总结：

        - 爆炸半径：中等
        - 视野：小
        - 强于：战斗坦克、导弹坦克、四轮摩托车、三轮摩托车、偏转者、建筑、防御设施
        - 弱于：攻城坦克、轻型步兵、手榴弹兵、音波坦克

actor-thumper =
    .name = 鼓兵步兵
    .description =
    部署时吸引附近的蠕虫。
      无武装
    .encyclopedia =
    部署一个响亮的锤击装置，将沙虫吸引到该区域。

actor-fremen =
    .name = 弗里曼
    .description =
    拥有突袭步枪和火箭的精英步兵单位。
      强于步兵和载具
      弱于火炮
      特殊能力：隐形
    .encyclopedia =
    来自Dune的本地沙漠战士，装备10mm突袭步枪和火箭。他们的火力对步兵和载具同样有效。

    弗里曼单位对高爆弹和子弹武器非常脆弱。

    总结：

        - 爆炸半径：中等
        - 视野：小
        - 强于：四轮摩托车、三轮摩托车、导弹坦克、战斗坦克、毁灭者、建筑、防御设施
        - 弱于：攻城坦克、轻型步兵、手榴弹兵、音波坦克

actor-grenadier =
    .name = 手榴弹兵
    .description =
    带手榴弹的步兵。
      强于建筑和步兵
      弱于载具
    .encyclopedia =
    一种用于破坏建筑的步兵火炮单位。它们在被杀死时有爆炸的可能，因此不应该集中在一起。

    总结：

        - 爆炸半径：大
        - 视野：小
        - 强于：轻型步兵、三轮摩托车、导弹坦克、战斗坦克、建筑、防御设施
        - 弱于：攻城坦克、战斗坦克、音波坦克、毁灭者

actor-sardaukar =
    .name = 萨达卡
    .description =
    精英柯里诺突袭步兵。
      强于步兵和载具
      弱于火炮
    .encyclopedia =
    强大的重装步兵，配备机枪有效对抗步兵，及火箭发射器针对载具。当被压住单位爆炸并损坏上方的载具。

    总结：

        - 爆炸半径：大
        - 视野：小
        - 强于：三轮摩托车、四轮摩托车、导弹坦克、战斗坦克、建筑、防御设施
        - 弱于：攻城坦克、音波坦克、手榴弹兵、坦克压碎

actor-mpsardaukar-description =
    精英哈肯纳森突袭步兵。
      强于步兵和载具
      弱于火炮

actor-saboteur =
    .name = 破坏者
    .description =
    神秘的步兵，带有爆炸物。
    临时隐形。
      强于建筑
      弱于所有东西
      特殊能力：破坏建筑
    .encyclopedia =
    房屋奥多斯的特制军事单位，能够摧毁敌方建筑和载具，但由于爆炸会死亡。它能启动自爆并伤害附近的敌方单位。

    破坏者能抵抗反坦克武器，但对高爆弹、火和子弹武器很脆弱。

actor-nsfremen-description =
    精英步兵单位，配备突袭步枪和火箭。
      强于步兵和载具
      弱于火炮

## misc.yaml
actor-crate-name = 箱子
actor-mpspawn-name = （多人游戏开始点）
actor-waypoint-name = （脚本行为路径点）
actor-camera-name = （向所有者显示区域）
actor-wormspawner-name = （蠕虫生成位置）

actor-upgrade-conyard =
    .name = 建造区升级
    .description =
    解锁更多建造选择：
    - 大混凝土板
    - 火箭炮塔

actor-upgrade-barracks =
    .name = 兵营升级
    .description =
    解锁更多步兵：
    - 突袭兵
    - 工程兵
    - 鼓兵步兵

    需要解锁家族特定步兵：
    - 阿特里德斯：手榴弹兵
    - 哈肯纳森：萨达卡

actor-upgrade-light =
    .name = 轻型工厂升级
    .description =
    解锁更多轻型单位：
    - 火箭四轮车

    需要解锁家族特定的轻型单位：
    - 奥多斯：隐形突袭三轮摩托车

actor-upgrade-heavy =
    .name = 重型工厂升级
    .description =
    解锁更多建造选项：
    - 伊克斯研究中心
    - 模拟器

    解锁更多重型单位：
    - 攻城坦克
    - 导弹坦克
    - MCV

actor-upgrade-hightech =
    .name = 高科技工厂升级
    .description =
    解锁阿特里德斯空袭超级武器。

actor-deathhand =
    .name = 死亡之手
    .encyclopedia =
    装备核集群弹药，攻击它在目标上方引爆，对大范围造成严重伤害。

## structures.yaml
notification-construction-complete = 建造完成。
notification-unit-ready = 单位准备就绪。
notification-repairing = 正在修理。
notification-unit-repaired = 单位已修理。
notification-unit-sold = 单位已出售。
notification-ion-cannon-ready = 离子加农炮已准备。
notification-select-target = 选择目标。
notification-cluster-missile-ready = 集束导弹已准备。
notification-missile-launch-detected = 检测到导弹发射。
notification-emp-cannon-ready = 电磁加农炮已准备。

## Defaults
notification-unit-lost = 单位被消灭。
notification-unit-promoted = 单位升级。
notification-primary-building-selected = 已选择主要建筑。

## Infantry
notification-building-infiltrated = 建筑被渗透。
notification-building-captured = 建筑被占领。
notification-bridge-repaired = 桥已修复。

## aircraft.yaml
actor-dpod-name = 降落舱
actor-dpod2-name = 降落舱
actor-dshp-name = 运输舰

actor-orca =
    .name = 战斗机
    .description =
    快速突袭攻击机，携带
    双重导弹发射器。
       强于建筑和载具
       弱于步兵和飞机

actor-orcab =
    .name = 轰炸机
    .description =
    重型轰炸机。
       强于建筑和载具
       弱于步兵和飞机

actor-orcatran-name = 运输机

actor-trnsport =
    .name = 运输机
    .description =
    能够升降
    和运输载具的垂直起降飞机。
      无武装

actor-scrin =
    .name = 幻影战斗机
    .description =
    先进的战斗机轰炸机
    携带有双等离子加农炮。
       强于建筑和载具
       弱于步兵和飞机

actor-apache =
    .name = 猎鹰
    .description =
    反人员支援攻击直升机
    携带有双链式机枪。
       强于步兵、轻型装甲和飞机
       弱于载具

actor-hunter-name = 猎人-追踪者机器人

## bridges.yaml
actor-cabhut-name = 桥梁维修小屋
meta-lowbridgeramp-name = 桥梁
actor-lobrdg-d-name = 坏掉的桥梁
actor-lobrdg-r-name = 桥梁坡道
meta-elevatedbridgeplaceholder-name = 桥梁

## civilian-infantry.yaml
actor-weedguy-name = 化学喷射步兵
actor-umagon-name = 乌马贡
actor-chamspy-disguisetooltip-name = 变色龙间谍
actor-mutant-name = 突变体
actor-mwmn-name = 突变体士兵
actor-mutant3-name = 突变体中士
actor-tratos-name = 特拉托斯
actor-oxanna-name = 奥克萨娜
actor-slav-name = 斯拉文

## civilian-structures.yaml
actor-aban01-name = WS 伐木公司
actor-aban02-name = 潘努洛农场
actor-aban03-name = 被遗弃的工厂
actor-aban04-name = 市政厅
actor-aban05-name = 猎人小屋
actor-aban06-name = 当地旅馆和住宿
actor-aban07-name = 教堂
actor-aban08-name = 被遗弃的仓库
actor-aban09-name = 塔的住宅
actor-aban10-name = 丹齐尔的最后机会汽车旅馆
actor-aban11-name = 米尔家居
actor-aban12-name = 凯特勒的场所
actor-aban13-name = 长的住宅
actor-aban14-name = 当地商店
actor-aban15-name = 亚当的房子
actor-aban16-name = 加油站
actor-aban17-name = 加油泵
actor-aban18-name = 加油站标志
actor-ammocrat-name = 弹药箱
actor-bboard01-name = 在瑞德的乡村酒店用餐
actor-bboard02-name = 喝YEO-CA可乐！
actor-bboard03-name = 汉堡包99美分
actor-bboard04-name = 游览景色拉斯维加斯
actor-bboard05-name = 房间29美元一夜
actor-bboard06-name = 卡斯帕姆的钛菌仓库
actor-bboard07-name = 碱性电池超级商店
actor-bboard08-name = 亚历克斯-鳄鱼宠物店就在前方！
actor-bboard09-name = 战术X游戏很精彩！
actor-bboard10-name = WW 海滩和肉排正好合适！
actor-bboard11-name = 只有11英里到泽德科咖啡馆！
actor-bboard12-name = 无法离开阿彻精神病院！
actor-bboard13-name = 在赫威特的美发沙龙报名
actor-bboard14-name = 比利·鲍勃的收割者学校
actor-bboard15-name = 潘努洛农场好棒
actor-bboard16-name = 加入GDI：我们拯救生命。
actor-ca0001-name = 瑞德的乡村酒店
actor-ca0002-name = 桑德伯格和儿子的
actor-ca0003-name = 临时住宿
actor-ca0004-name = 中转站
actor-ca0005-name = 费比的4销售
actor-ca0006-name = 豪华住宿
actor-ca0007-name = 场地发生器
actor-ca0008-name = 地下居住
actor-ca0009-name = 地下居住
actor-ca0010-name = 利里旅行者旅馆
actor-ca0011-name = 水箱
actor-ca0012-name = 温室
actor-ca0013-name = 水净化器
actor-ca0014-name = 观察塔
actor-ca0015-name = 便携式小屋
actor-ca0016-name = 便携式小屋豪华版
actor-ca0017-name = 能量转换器
actor-ca0018-name = 太阳能板
actor-ca0019-name = 太阳能板
actor-ca0020-name = 太阳能板
actor-ca0021-name = 太阳能板
actor-caaray-name = 民用阵列
actor-caarmr-name = 民用军械库
actor-cacrsh01-name = 坠毁现场
actor-capyr01-name = 金字塔
actor-capyr02-name = 金字塔
actor-capyr03-name = 金字塔
actor-city01-name = 康内利公寓
actor-city02-name = 莱纳的豪华套房
actor-city03-name = 办公楼
actor-city04-name = 西伍德股票交易所
actor-city05-name = 每日太阳时报
actor-city06-name = YEO-CA 可乐公司
actor-city07-name = 城市住宅
actor-city08-name = 美发用品店
actor-city09-name = 被遗弃的仓库
actor-city10-name = 城市店面
actor-city11-name = 安布罗斯餐厅
actor-city12-name = 波斯特塔
actor-city13-name = 赫威特美发沙龙
actor-city14-name = 商务办公室
actor-city15-name = 第二国家银行
actor-city16-name = 高层酒店
actor-city17-name = 项目
actor-city18-name = 阿彻精神病院
actor-city19-name = 加油站
actor-city20-name = 加油泵
actor-city21-name = 加油站标志
actor-city22-name = 教堂
actor-ctdam-name = 水力发电站
actor-ctvega-name = 维加金字塔
actor-gakodk-name = GDI 科迪亚克
actor-gaoldcc1-name = 旧建造区
actor-gaoldcc2-name = 旧寺庙
actor-gaoldcc3-name = 旧武器工厂
actor-gaoldcc4-name = 旧精炼厂
actor-gaoldcc5-name = 旧高级发电厂
actor-gaoldcc6-name = 旧筒仓

actor-gasand =
    .name = 沙袋
    .description =
    阻止步兵和轻型载具。
    可被坦克压毁。

actor-gaspot-name = 灯塔
actor-galite-name = 灯柱
actor-ingalite-name = （隐形灯柱）
actor-neglamp-name = （隐形负灯柱）
actor-redlamp-name = 红灯柱
actor-negred-name = 负红灯柱
actor-grenlamp-name = 绿灯柱
actor-bluelamp-name = 蓝灯柱
actor-yelwlamp-name = 黄灯柱
actor-inyelwlamp-name = （隐形黄灯柱）
actor-purplamp-name = 紫灯柱
actor-inpurplamp-name = （隐形紫灯柱）
actor-inoranlamp-name = （隐形橙灯柱）
actor-ingrnlmp-name = （隐形绿灯柱）
actor-inredlmp-name = （隐形红灯柱）
actor-inblulmp-name = （隐形蓝灯柱）
actor-gaicbm-name = 部署的导弹
actor-namntk-name = Nod 蒙塔尤克
actor-ntpyra-name = Nod 金字塔
actor-ufo-name = Scrin 船

## civilian-vehicles.yaml
actor-4tnk-name = 巨兽坦克
meta-truck-name = 卡车
actor-icbm-name = 弹道导弹发射器
actor-bus-name = 校车
actor-pick-name = 货车
actor-car-name = 汽车
actor-wini-name = 娱乐车
actor-locomotive-name = 火车
actor-traincar-name = 客车
actor-cargocar-name = 货车

## critters.yaml
actor-doggie-name = 钛菌恶魔
actor-visc-sml-name = 婴儿液态生物
actor-visc-lrg-name = 成年液态生物
actor-jfish-name = 钛菌漂浮者

## defaults.yaml
meta-crate-name = 箱子
meta-civilianinfantry-name = 民兵
meta-aircrafthusk-generic-name = 已损毁的飞机
meta-blossomtree-name = 花树
meta-tree-name = 树
meta-rock-name = 岩石
meta-box-name = 箱子
meta-drum-name = 鼓
meta-palette-name = 货盘
meta-railway-name = 铁路
meta-gate-description = 自动门，当盟友的单位通过时会开启。

## gdi-infantry.yaml
actor-e2 =
    .name = 飞盘投掷手
    .description =
    带有特殊爆炸飞盘的步兵。
       强于建筑和步兵
       弱于载具和飞机

actor-medic =
    .name = 医兵
    .description =
    治疗附近的步兵。
       无武装

actor-jumpjet =
    .name = 跳跃喷气步兵
    .description =
    空中士兵。
       强于步兵和飞机
       弱于载具

actor-jumpjet-husk-name = 跳跃喷气步兵

actor-ghost =
    .name = 幽灵潜行者
    .description =
    精锐特种兵步兵，配备线性枪和C4。
    同一时间只能训练一个。
       强于步兵和建筑
       弱于载具和飞机
       特殊能力：使用C4摧毁建筑

## gdi-structures.yaml
actor-gapowr =
    .name = GDI 发电厂
    .description =
    为其他建筑提供电力。

actor-gapowr-socket-name = GDI 发电厂插槽

actor-gapowrup =
    .name = 发电机组
    .description =
    提供额外的发电量。

actor-gapile =
    .name = GDI 兵营
    .description =
    生产步兵。

actor-gaweap =
    .name = GDI 战工厂
    .description =
    生产载具。

actor-gahpad =
    .name = 直升机停机坪
    .description =
    生产、重新武装和
    修理直升机。

actor-gadept =
    .name = 服务库
    .description =
    修理或出售载具和飞机。

actor-garadr =
    .name = GDI 雷达
    .description =
    提供战场概况。
    能够检测隐形单位。
    需要电力运行。

actor-gatech =
    .name = GDI 科技中心
    .description =
    提供对先进技术的访问权限。

actor-gaplug =
    .name = GDI 升级中心
    .description =
    可以升级以获得额外的技术。
    .ioncannonpower-name = 离子加农炮
    .ioncannonpower-description = 启动离子加农炮打击。
    对一小片区域施加即时伤害。
    .droppodspower-name = 降落舱
    .droppodspower-description = 降落舱增援。
    一组精英士兵从轨道上降落到目标地点。
    .produceactorpower-name = 寻找者
    .produceactorpower-description = 寻找并摧毁敌方目标的无人机。

actor-gafire =
    .name = 火风暴发生器
    .description =
    建筑物可获取火风暴装置。

actor-gaplug-socket-ioncannon-name = GDI 升级中心插槽
actor-gaplug-socket-hunterseeker-name = GDI 升级中心插槽

actor-gaplug2 =
    .name = 寻找者控制
    .description =
    解锁寻找者机器人。

actor-gaplug3 =
    .name = 离子加农炮联机
    .description =
    解锁离子加农炮。

actor-gaplug4 =
    .name = 降落舱节点
    .description =
    解锁降落舱增援。

## gdi-support.yaml
actor-gawall =
    .name = 混凝土墙
    .description =
    阻止步兵并阻挡敌方火炮
    不可被坦克压毁。

actor-gagate-a-name = GDI 大门
actor-gagate-b-name = GDI 大门

actor-gactwr =
    .name = 组件塔
    .description =
    用于基地防御的模块化塔。

actor-gactwr-socket-name = 组件塔（未升级）
actor-gavulc =
    .name = 火神塔
    .description =
    基础基地防御。
    不需要电力运行。
       强于步兵和轻型装甲
       弱于飞机

actor-garock =
    .name = RPG 升级
    .description =
    GDI 高级基地防御。
    不需要电力运行。
       强于装甲地面单位
       弱于飞机

actor-gacsam =
    .name = SAM 升级
    .description =
    GDI 反飞机基地防御。
    不需要电力运行。
       强于飞机
       弱于地面单位

## gdi-vehicles.yaml
actor-apc =
    .name = 两栖运兵车
    .description =
    防护步兵运输车。
    可在水上移动。
       无武装

actor-hvr =
    .name = 悬浮MLRS
    .description =
    悬浮车载有
    长距离导弹。
       强于载具和飞机
       弱于步兵

actor-smech =
    .name = 狼獾
    .description =
    反人员步行机。
       强于步兵和轻装甲
       弱于载具和飞机

actor-mmch =
    .name = 坦克
    .description =
    通用装甲徒步机。
       强于载具
       弱于步兵和飞机

actor-hmec =
    .name = 巨兽Mk.II
    .description =
    慢速，重装甲步行机。
    最多只能建造一个。
    装备双线性加农炮和火箭发射器。
       强于步兵、载具、飞机和建筑
       弱于任何东西

actor-sonic =
    .name = 扰乱者
    .description =
    装甲高科技载具带
    长距离和声波武器。
       强于步兵、载具和建筑
       弱于飞机

actor-jugg =
    .name = 巨人
    .deployed-name = 巨人（部署）
    .description =
    移动炮兵机械。
    必须部署才能射击。
       强于地面单位
       弱于飞机

actor-mobilemp =
    .name = 移动电磁加农炮
    .description =
    发射脉冲爆破，使该区域的所有机械单位失效。

## husks.yaml
actor-dshp-husk-name = 运输舰
actor-orca-husk-name = 战斗机
actor-orcab-husk-name = 轰炸机
actor-orcatran-husk-name = 运输机
actor-trnsport-husk-name = 运输机
actor-scrin-husk-name = 幻影战斗机
actor-apache-husk-name = 猎鹰

## misc.yaml
actor-mpspawn-name = （多人游戏开始点）
actor-waypoint-name = （脚本行为路径点）
actor-camera-name = （向所有者显示区域）

## nod-infantry.yaml
actor-e3 =
    .name = 火箭步兵
    .description =
    反坦克步兵。
       强于载具、飞机和建筑
       弱于步兵

actor-cyborg =
    .name = 赛博格步兵
    .description =
    赛博格步兵单位。
       强于步兵和轻装甲
       弱于载具和飞机

actor-cyc2 =
    .name = 赛博格特种兵
    .description =
    精锐赛博格步兵单位。
    最多只能建造一个。
       强于步兵、载具和建筑
       弱于飞机

actor-mhijack =
    .name = 突变体劫持者
    .description =
    劫持敌方载具。
       无武装

## nod-structures.yaml
actor-napowr =
    .name = Nod 发电厂
    .description =
    为其他建筑提供电力。

actor-naapwr =
    .name = Nod 高级发电厂
    .description =
    产生两倍于发电厂的电力。

actor-nahand =
    .name = Nod 手
    .description =
    生产步兵。

actor-naweap =
    .name = Nod 战工厂
    .description =
    生产载具。

actor-nahpad =
    .name = 直升机停机坪
    .description =
    生产、重新武装和
    修理直升机。

actor-naradr =
    .name = Nod 雷达
    .description =
    提供战场概况。
    检测隐形单位。
    需要电力运行。

actor-natech =
    .name = Nod 科技中心
    .description =
    提供对先进技术的访问权限。

actor-nastlh =
    .name = 隐形发生器
    .description =
    生成一个隐形场
    来隐藏你的部队免受敌人。
    
actor-natmpl =
    .name = Nod 神庙
    .description =
    提供对先进技术的访问权限。
    .produceactorpower-name = 寻找者
    .produceactorpower-description = 释放一架无人机，寻找并摧毁敌人目标。

actor-namisl =
    .nukepower-name = 集束导弹
    .description =
    在目标位置发射毁灭性导弹。
    需要电力运行。
    最多只能建造一个。
    .name = Nod 导弹筒
    .nukepower-description = 在目标位置发射爆炸性集束弹头

actor-nawast =
    .name = 废料精炼厂
    .description =
    处理Vein
    成可用资源。
    最多只能建造一个。

## nod-support.yaml
actor-nawall =
    .name = 混凝土墙
    .description =
    阻止步兵并阻挡敌方火炮。
    不可被坦克压毁。

actor-nagate-a-name = Nod 大门
actor-nagate-b-name = Nod 大门

actor-napost =
    .name = 激光围栏
    .description =
    阻止步兵并阻挡敌方火炮。
    不可被坦克压毁。

actor-nafnce-name = 激光围栏

actor-nalasr =
    .name = 激光炮塔
    .description =
    基础基地防御。
    需要电力运行。
       强于地面单位
       弱于飞机

actor-naobel =
    .name = 光之方尖碑
    .description =
    高级基地防御。
    需要电力运行。
       强于地面单位
       弱于飞机

actor-nasam =
    .name = S.A.M. 站
    .description =
    Nod 反飞机基地防御。
    需要电力运行。
       强于飞机
       弱于地面单位

## nod-vehicles.yaml
actor-bggy =
    .name = 攻击吉普车
    .description =
    快速侦察和反步兵载具。
       强于步兵和轻装甲
       弱于载具和飞机

actor-bike =
    .name = 攻击自行车
    .description =
    快速侦察载具，带火箭。
       强于载具
       弱于步兵和飞机

actor-ttnk =
    .name = 蚀刻坦克
    .deployed-name = 蚀刻坦克（部署）
    .description =
    Nod 主战坦克。
    需要部署来获得更多保护。
       强于载具
       弱于步兵和飞机

actor-art2 =
    .name = 火炮
    .deployed-name = 火炮（部署）
    .description =
    可移动大炮。
    需要部署才能射击。
       强于地面单位
       弱于飞机

actor-repair =
    .name = 移动维修载具
    .description =
    维修附近的载具。
       无武装

actor-weed =
    .name = 杂草收割机
    .description =
    收集钛菌Vein进行加工。
       无武装

actor-sapc =
    .name = 地下运兵车
    .description =
    能够地下移动以避免被发现的载具运输。
       无武装

actor-subtank =
    .name = 魔鬼的舌头
    .description =
    潜行火焰坦克
    具有在地下移动的能力。
       强于步兵和建筑
       弱于坦克和飞机

actor-stnk =
    .name = 隐形坦克
    .description =
    轻装甲坦克装备个人
    隐形生成器。装备导弹。
    在近距离内被步兵发现。
       强于载具和飞机
       弱于步兵

actor-sgen =
    .name = 移动隐形生成器
    .deployed-name = 移动隐形生成器（部署）
    .description =
    部署后能够隐形单位。
       无武装

## shared-infantry.yaml
actor-e1 =
    .name = 轻型步兵
    .description =
    通用步兵。
       强于步兵
       弱于载具和飞机

actor-engineer =
    .name = 工程兵
    .description =
    渗透并占领敌方结构。
       无武装

## shared-structures.yaml
actor-gacnst =
    .name = 建造区
    .description =
    建造基地结构。

actor-proc =
    .name = 钛菌精炼厂
    .description =
    处理原始钛菌
    成可用资源。

actor-gasilo =
    .name = 筒仓
    .description =
    储存多余的钛菌。

actor-anypower-name = 发电
actor-barracks-name = 步兵生产
actor-factory-name = 载具生产
actor-radar-name = 雷达
actor-tech-name = 科技中心

## shared-support.yaml
actor-napuls =
    .name = 电磁加农炮
    .description =
    使一个区域内的机械单位失效。
    需要电力运行。
    .attackorderpower-name = 电磁
    .attackorderpower-description = 发射脉冲爆破，使该区域内的所有机械单位失效。

## shared-vehicles.yaml
actor-mcv =
    .name = 移动建造车
    .description =
    部署为建造区。
      无武装

actor-harv =
    .name = 收割机
    .description =
    收集钛菌进行加工。
       无武装

actor-lpst =
    .name = 移动传感器阵列
    .deployed-name = 移动传感器阵列（部署）
    .description =
    部署后检测隐形和地下
    单位。
       无武装

## trees.yaml
actor-bigblue-name = 大蓝色钛菌晶体
actor-veinhole-name = Veinhole
meta-tibflora-name = 钛菌植物群

## 民间科技
actor-cahosp =
    .name = 民间医院
    .captured-desc = 为步兵提供自愈。
    .capturable-desc = 夺取以启用步兵自愈。

## ai.yaml
bot-test-ai =
    .name = 测试AI

## map-generators.yaml
label-random-map = 随机地图
label-clear-map-generator-option-tile = 图块
label-clear-map-generator-choice-tile-clear =
   .label = 清除
label-clear-map-generator-choice-tile-snow =
   .label = 雪地
label-clear-map-generator-choice-tile-blank =
   .label = 空白
label-clear-map-generator-choice-tile-rough =
   .label = 粗糙
label-clear-map-generator-choice-tile-water =
   .label = 水
label-clear-map-generator-choice-tile-ground01 =
   .label = 地面01
label-clear-map-generator-choice-tile-sand =
   .label = 沙地
label-clear-map-generator-choice-tile-green =
   .label = 绿色
label-clear-map-generator-choice-tile-pavement =
   .label = 水泥地
label-clear-map-generator-choice-tile-crystal =
   .label = 水晶
label-clear-map-generator-choice-tile-swamp =
   .label = 沼泽
label-clear-map-generator-choice-tile-rock =
   .label = 岩石
label-clear-map-generator-choice-tile-bluemold =
   .label = 蓝色霉菌
label-clear-map-generator-choice-tile-grey =
   .label = 灰色

label-ts-map-generator-option-seed = 种子

label-ts-map-generator-option-terrain-type = 地形类型
label-ts-map-generator-choice-terrain-type-lakes =
   .label = 湖泊
   .description = 开放空间中具有中等大小湖
label-ts-map-generator-choice-terrain-type-puddles =
   .label = 水坑
   .description = 开放空间中有小水池
label-ts-map-generator-choice-terrain-type-gardens =
   .label = 花园
   .description = 功能丰富的地形，有池塘、峭壁和森林
label-ts-map-generator-choice-terrain-type-plots =
   .label = 地块
   .description = 稀疏地形，有池塘、峭壁和森林
label-ts-map-generator-choice-terrain-type-plains =
   .label = 平原
   .description = 开放空间稀疏树木和峭壁
label-ts-map-generator-choice-terrain-type-parks =
   .label = 公园
   .description = 开放空间轻度森林和偶尔峭壁
label-ts-map-generator-choice-terrain-type-woodlands =
   .label = 森林
   .description = 中度森林，偶尔有峭壁
label-ts-map-generator-choice-terrain-type-overgrown =
   .label = 荒野
   .description = 狭窄通道，密集森林和中度峭壁
label-ts-map-generator-choice-terrain-type-rocky =
   .label = 岩石
   .description = 中度峭壁，轻度森林
label-ts-map-generator-choice-terrain-type-mountains =
   .label = 山脉
   .description = 许多长峭壁
label-ts-map-generator-choice-terrain-type-mountain-lakes =
   .label = 山地湖
   .description = 湖泊和许多长峭壁

label-ts-map-generator-option-symmetry = 对称
label-ts-map-generator-choice-mirror-none =
   .label = 无
label-ts-map-generator-choice-symmetry-mirror-horizontal =
   .label = 水平镜像
label-ts-map-generator-choice-symmetry-mirror-vertical =
   .label = 垂直镜像
label-ts-map-generator-choice-symmetry-mirror-diagonal-tl =
   .label = 对角镜像（左上）
label-ts-map-generator-choice-symmetry-mirror-diagonal-tr =
   .label = 对角镜像（右上）
label-ts-map-generator-choice-symmetry-mirror-2-rotations =
   .label = 2次旋转
label-ts-map-generator-choice-symmetry-mirror-3-rotations =
   .label = 3次旋转
label-ts-map-generator-choice-symmetry-mirror-4-rotations =
   .label = 4次旋转
label-ts-map-generator-choice-symmetry-mirror-5-rotations =
   .label = 5次旋转
label-ts-map-generator-choice-symmetry-mirror-6-rotations =
   .label = 6次旋转
label-ts-map-generator-choice-symmetry-mirror-7-rotations =
   .label = 7次旋转
label-ts-map-generator-choice-symmetry-mirror-8-rotations =
   .label = 8次旋转

label-ts-map-generator-option-players = 玩家

label-ts-map-generator-option-resources = 资源
label-ts-map-generator-choice-resources-none =
   .label = 无
label-ts-map-generator-choice-resources-low =
   .label = 低
label-ts-map-generator-choice-resources-medium =
   .label = 中等
label-ts-map-generator-choice-resources-high =
   .label = 高
label-ts-map-generator-choice-resources-very-high =
   .label = 非常高
label-ts-map-generator-choice-resources-full =
   .label = 矿藏丰富
label-ts-map-generator-choice-resources-oreful =
   .label = 矿藏丰富

label-ts-map-generator-option-buildings = Veinholes
label-ts-map-generator-choice-buildings-none =
   .label = 无
   .description = 无veinholes
label-ts-map-generator-choice-buildings-veinholes =
   .label = Veinholes
   .description = 标准数量veinholes
label-ts-map-generator-choice-buildings-more-veinholes =
   .label = 更多Veinholes
   .description = Veinholes数量翻倍

label-ts-map-generator-option-density = 扩张机遇
label-ts-map-generator-choice-density-players =
   .label = 根据玩家数调整
label-ts-map-generator-choice-density-area-and-players =
   .label = 根据大小和玩家数调整
label-ts-map-generator-choice-density-area-very-low =
   .label = 非常低
label-ts-map-generator-choice-density-area-low =
   .label = 低
label-ts-map-generator-choice-density-area-medium =
   .label = 中等
label-ts-map-generator-choice-density-area-high =
   .label = 高
label-ts-map-generator-choice-density-area-very-high =
   .label = 非常高

label-ts-map-generator-option-civilian-density = 民兵密度
label-ts-map-generator-choice-civilian-density-default =
   .label = 默认
label-ts-map-generator-choice-civilian-density-none =
   .label = 无
label-ts-map-generator-choice-civilian-density-low =
   .label = 低
label-ts-map-generator-choice-civilian-density-medium =
   .label = 中等
label-ts-map-generator-choice-civilian-density-high =
   .label = 高
label-ts-map-generator-choice-civilian-density-very-high =
   .label = 非常高
label-ts-map-generator-choice-civilian-density-max =
   .label = 最大

label-ts-map-generator-option-coastlines = 海岸线
label-ts-map-generator-choice-coastlines-beaches =
   .label = 海滩
label-ts-map-generator-choice-coastlines-sunken-beaches =
   .label = 沉没海滩
label-ts-map-generator-choice-coastlines-cliffs =
   .label = 峭壁
label-ts-map-generator-choice-coastlines-mixed =
   .label = 混合
label-ts-map-generator-choice-coastlines-mixed =
   .label = 混合

label-ts-map-generator-option-deny-walled-areas = 阻挡有墙的区域