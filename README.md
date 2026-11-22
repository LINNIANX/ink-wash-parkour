# 水墨风跑酷

一款水墨风格的横版跑酷游戏，使用 Unity 2022.3 LTS 开发。玩家在不断向前延伸的场景中跳跃、滑铲、攻击敌人、收集金币，并挑战关底的 Boss。

## 玩法与操作

| 按键 | 操作 |
| --- | --- |
| `W` | 跳跃 |
| `S` | 滑铲 / 快速下落（按住） |
| `J` | 攻击 |
| `K` | 按住吸取金币 |
| `Space` | 按住释放远程技能（消耗能量） |

## 游戏内容

- **无尽跑酷**：地面与背景无限循环，速度随时间逐渐提升
- **敌人**：蝙蝠、蜘蛛陷阱、飞剑、Boss 及其弹幕
- **收集物**：金币、能量胶囊
- **技能系统**：能量积攒后可释放远程技能
- **骑乘系统**：可骑乘的坐骑
- **完整流程**：开始界面 → 游戏 → 结算界面，另有设置界面
- **水墨特效**：墨滴、能量墨迹等 UI 特效，视差滚动背景
- **音频**：背景音乐与音效管理

## 技术栈

- Unity **2022.3.62f3c1**（LTS）
- Universal Render Pipeline（URP）
- C#、ShaderLab / HLSL
- TextMeshPro、Unity UI
- 2D / 3D 混合渲染

## 项目结构

```
Assets/
├── AnimationClip/      # 角色与特效动画
├── Boss/               # Boss 相关美术与预制体
├── Prefabs/            # 敌人、收集物、障碍等预制体
├── Scenes/             # ksishi（开始）、SampleScene（游戏）、jiesuan（结算）、shezhi（设置）
├── Scripts/
│   ├── Player/         # 玩家控制、能量技能、骑乘系统
│   ├── Enemy/          # 敌人、Boss、弹幕、障碍物、收集物
│   ├── Manager/        # 游戏管理、场景生成、音频、输入、UI、摄像机
│   ├── UI/             # 水墨 UI 特效、结算显示、开始菜单
│   └── Utils/          # 工具类
├── TextMesh Pro/       # 文字资源
└── 场景搭建指南.md      # 场景搭建详细说明
```

## 运行方式

1. 用 Unity Hub 添加并打开本项目文件夹（需要 Unity 2022.3.x）
2. 首次打开会导入资源，等待完成
3. 打开 `Assets/Scenes/ksishi.unity` 进入开始界面，点击开始进入游戏
4. 或将 `ksishi`、`SampleScene`、`jiesuan`、`shezhi` 按顺序加入 Build Settings 后直接构建

> 仓库中不包含 `Library/`、`Logs/`、`UserSettings/` 等 Unity 自动生成的目录，首次打开需要重新导入。
