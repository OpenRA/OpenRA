## gamesave-loading.yaml
label-gamesave-loading-screen-title = 加载保存的游戏
label-gamesave-loading-screen-desc = 按下Escape取消加载并返回主菜单

## ingame-observer.yaml
button-observer-widgets-pause-tooltip = 暂停
button-observer-widgets-play-tooltip = 播放

button-observer-widgets-slow =
   .tooltip = 慢速
   .label = 50%

button-observer-widgets-regular =
   .tooltip = 常规速度
   .label = 100%

button-observer-widgets-fast =
   .tooltip = 快速
   .label = 200%

button-observer-widgets-maximum =
   .tooltip = 最大速度
   .label = MAX

label-basic-stats-player-header = 玩家
label-basic-stats-cash-header = 资金
label-basic-stats-power-header = 电力
label-basic-stats-kills-header = 击杀
label-basic-stats-deaths-header = 死亡
label-basic-stats-assets-destroyed-header = 摧毁
label-basic-stats-assets-lost-header = 损失
label-basic-stats-experience-header = 分数
label-basic-stats-actions-min-header = APM
label-economy-stats-player-header = 玩家
label-economy-stats-cash-header = 资金
label-economy-stats-income-header = 收入
label-economy-stats-assets-header = 资产
label-economy-stats-earned-header = 赚取
label-economy-stats-spent-header = 花费
label-economy-stats-harvesters-header = 采矿车
label-economy-stats-derricks-header = 油井
label-production-stats-player-header = 玩家
label-production-stats-header = 生产
label-support-powers-player-header = 玩家
label-support-powers-header = 支援能力
label-army-player-header = 玩家
label-army-header = 军队
label-combat-stats-player-header = 玩家
label-combat-stats-assets-destroyed-header = 摧毁
label-combat-stats-assets-lost-header = 损失
label-combat-stats-units-killed-header = 单位击杀
label-combat-stats-units-dead-header = 单位损失
label-combat-stats-buildings-killed-header = 建筑击杀
label-combat-stats-buildings-dead-header = 建筑损失
label-combat-stats-army-value-header = 军队价值
label-combat-stats-vision-header = 视野

## ingame-observer.yaml, ingame-player.yaml
label-mute-indicator = 音频静音
button-top-buttons-options-tooltip = 选项

## ingame-player.yaml
supportpowers-support-powers-palette =
   .ready = 准备就绪
   .hold = 暂停

button-command-bar-attack-move =
   .tooltip = 攻击移动
   .tooltipdesc = 选中的单位将移动到目标位置并在途中攻击遇到的敌人。
    按住<(Ctrl)>进行突击移动，攻击途中遇到的任何单位或结构。
    左键点击图标，然后右键点击目标位置。

button-command-bar-force-move =
   .tooltip = 强制移动
   .tooltipdesc = 选中的单位将移动到目标位置
      - 默认活动被抑制
      - 车辆将尝试在目标位置碾压敌人
      - 直升机将在目标位置降落
      - 时空坦克将向目标位置传送

    左键点击图标，然后右键点击目标。
    按住<(Alt)>在指挥单位时临时激活。

button-command-bar-force-attack =
   .tooltip = 强制攻击
   .tooltipdesc = 选中的单位将攻击目标单位或位置
      - 默认活动被抑制
      - 允许攻击己方或盟友单位
      - 远程炮兵单位将始终瞄准位置，忽略单位和建筑

    左键点击图标，然后右键点击目标。
    按住<(Ctrl)>在指挥单位时临时激活。

button-command-bar-guard =
   .tooltip = 护卫
   .tooltipdesc = 选中的单位将跟随目标单位。
    左键点击图标，然后右键点击目标单位。

button-command-bar-deploy =
   .tooltip = 部署
   .tooltipdesc = 选中的单位将执行默认部署活动
      - MCV将展开为建造厂
      - 建造厂将重新打包为MCV
      - 运输工具将卸载乘客
      - 自爆卡车和疯狂坦克将自毁
      - 布雷车将部署地雷
      - 飞机将返回基地
    立即对选中的单位生效。

button-command-bar-scatter =
   .tooltip = 散开
   .tooltipdesc = 选中的单位将停止当前活动并移动到附近位置。
    立即对选中的单位生效。

button-command-bar-stop =
   .tooltip = 停止
   .tooltipdesc = 选中的单位将停止当前活动。选中的建筑将重置集结点。
   立即对选中的目标生效。

button-command-bar-queue-orders =
   .tooltip = 路径点模式
   .tooltipdesc = 使用路径点模式为选中的单位下达多个链接命令。单位将在收到命令后立即执行。
   左键点击图标，然后在游戏世界中下达命令。
   按住<(Shift)>在指挥单位时临时激活。

button-stance-bar-attackanything =
   .tooltip = 攻击任何事物姿态
   .tooltipdesc = 将选中的单位设置为攻击任何事物姿态：
   - 单位将在看到敌人单位和建筑时攻击
   - 单位将追击战场上的攻击者

button-stance-bar-defend =
   .tooltip = 防御姿态
   .tooltipdesc = 将选中的单位设置为防御姿态：
   - 单位将在看到敌人单位时攻击
   - 单位不会移动或追击敌人

button-stance-bar-returnfire =
   .tooltip = 还击姿态
   .tooltipdesc = 将选中的单位设置为还击姿态：
   - 单位将对攻击它们的敌人进行还击
   - 单位不会移动或追击敌人

button-stance-bar-holdfire =
   .tooltip = 停火姿态
   .tooltipdesc = 将选中的单位设置为停火姿态：
   - 单位不会对敌人开火
   - 单位不会移动或追击敌人

button-top-buttons-beacon-tooltip = 放置信标
button-top-buttons-sell-tooltip = 出售
button-top-buttons-power-tooltip = 断电
button-top-buttons-repair-tooltip = 修理

productionpalette-sidebar-production-palette =
   .ready = 准备就绪
   .hold = 暂停

button-production-types-building-tooltip = 建筑
button-production-types-defense-tooltip = 防御
button-production-types-infantry-tooltip = 步兵
button-production-types-vehicle-tooltip = 车辆
button-production-types-aircraft-tooltip = 飞机
button-production-types-naval-tooltip = 海军
button-production-types-scroll-up-tooltip = 向上滚动
button-production-types-scroll-down-tooltip = 向下滚动
