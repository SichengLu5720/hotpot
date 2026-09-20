# Fish Sort：不加机制的三个高频内容骨架

A/B/C 是本次分析命名，不是原开发者的类名或资源名。鱼种字母在每份骨架内独立编号，不能跨骨架认定同一个字母对应同一种原始鱼。

这里的骨架是：有序的气泡配置列表 + 每个气泡的 BubbleType + 有序的鱼种同类关系。它不是已还原的场景坐标图，也不包含冻结、锁、隐藏、特殊鱼、目标策略或计时等机制。

编号对应配置数组顺序；所选三个代表文件中也恰好等于 BubbleID。不能把编号直接当成玩家点击顺序、物理前后排或出现时间。以下均为开局运行时变换前的静态数据。

## 总览

| 指标 | 骨架 A | 骨架 B | 骨架 C |
|---|---:|---:|---:|
| 代表基础关号 | 51 | 52 | 54 |
| 气泡配置数 | 33 | 44 | 50 |
| 鱼条目总数 | 123 | 162 | 183 |
| 鱼种数 | 15 | 15 | 16 |
| 单种鱼数量均可被 3 整除 | 是 | 是 | 是 |
| 仅按数量折算的三鱼组数 | 41 | 54 | 61 |
| 含至少三条同种鱼的气泡数 | 4 | 1 | 1 |
| 混合鱼种气泡数 | 31 | 43 | 48 |
| 基础 1300 关中匹配数 | 278 | 508 | 255 |

“三鱼组数”只是逐鱼种除以三后的数量统计，不是已经恢复的目标总数或解题步数。

## 每泡鱼数分布

| 每泡鱼条目数 | A 的气泡数 | B 的气泡数 | C 的气泡数 |
|---|---:|---:|---:|
| 1 | 0 | 1 | 1 |
| 2 | 5 | 6 | 7 |
| 3 | 9 | 12 | 14 |
| 4 | 9 | 12 | 14 |
| 5 | 10 | 13 | 14 |

这三个代表配置中 BubbleType 数值都等于该泡 FishStr 条目数；这不是对所有 BubbleType 的全局语义断言。

## 如何读

`01: ABCDD` 表示第 1 条气泡配置里有 5 条鱼，前 3 条互不同种，后 2 条属于同一个 D 鱼种。另一个气泡中出现 D，仍指本骨架中相同鱼种。

`ABCDD / AFD` 和 `12344 / 164` 只改变鱼种代号，骨架相同；把 `ABCDD` 改成 `ABCDE` 则改变同类关系，是不同骨架。

## 骨架 A：33 泡、123 条、15 种

来源：原归档中的 `FishSort_LevelPreparation/configs/BubbleConfig_51.json`。完整机器可读数据见 [skeleton_A.json](skeleton_A.json)。

### 完整装鱼序列

```text
01: ABCDD  BubbleType=5
02: EE     BubbleType=2
03: AF     BubbleType=2
04: AF     BubbleType=2
05: CCD    BubbleType=3
06: FGEE   BubbleType=4
07: AAF    BubbleType=3
08: DAAF   BubbleType=4
09: HIFJI  BubbleType=5
10: KFAA   BubbleType=4
11: KKG    BubbleType=3
12: LF     BubbleType=2
13: AAEEF  BubbleType=5
14: MMGF   BubbleType=4
15: GGHDA  BubbleType=5
16: BBGHE  BubbleType=5
17: JJJF   BubbleType=4
18: CCCBF  BubbleType=5
19: FFF    BubbleType=3
20: FFIDG  BubbleType=5
21: KKBJN  BubbleType=5
22: ADKEO  BubbleType=5
23: GNK    BubbleType=3
24: LFA    BubbleType=3
25: FFFKJ  BubbleType=5
26: FDE    BubbleType=3
27: AC     BubbleType=2
28: DDO    BubbleType=3
29: EADO   BubbleType=4
30: AAMC   BubbleType=4
31: BKCN   BubbleType=4
32: GFE    BubbleType=3
33: ELFD   BubbleType=4
```

### 每种鱼的配额与分布

| 代号 | 原始鱼种 ID | 数量 | 出现的配置序号 |
|---|---:|---:|---|
| A | 25 | 18 | 1, 3, 4, 7×2, 8×2, 10×2, 13×2, 15, 22, 24, 27, 29, 30×2 |
| B | 60 | 6 | 1, 16×2, 18, 21, 31 |
| C | 63 | 9 | 1, 5×2, 18×3, 27, 30, 31 |
| D | 2 | 12 | 1×2, 5, 8, 15, 20, 22, 26, 28×2, 29, 33 |
| E | 43 | 12 | 2×2, 6×2, 13×2, 16, 22, 26, 29, 32, 33 |
| F | 27 | 24 | 3, 4, 6, 7, 8, 9, 10, 12, 13, 14, 17, 18, 19×3, 20×2, 24, 25×3, 26, 32, 33 |
| G | 51 | 9 | 6, 11, 14, 15×2, 16, 20, 23, 32 |
| H | 11 | 3 | 9, 15, 16 |
| I | 32 | 3 | 9×2, 20 |
| J | 59 | 6 | 9, 17×3, 21, 25 |
| K | 10 | 9 | 10, 11×2, 21×2, 22, 23, 25, 31 |
| L | 29 | 3 | 12, 24, 33 |
| M | 71 | 3 | 14×2, 30 |
| N | 68 | 3 | 21, 23, 31 |
| O | 28 | 3 | 22, 28, 29 |

含三条同种鱼的气泡：17: JJJF；18: CCCBF；19: FFF；25: FFFKJ。

## 骨架 B：44 泡、162 条、15 种

来源：原归档中的 `FishSort_LevelPreparation/configs/BubbleConfig_52.json`。完整机器可读数据见 [skeleton_B.json](skeleton_B.json)。

### 完整装鱼序列

```text
01: ABCD   BubbleType=4
02: EBFG   BubbleType=4
03: CAAHI  BubbleType=5
04: EEJCG  BubbleType=5
05: AEG    BubbleType=3
06: BA     BubbleType=2
07: JJJFF  BubbleType=5
08: JIG    BubbleType=3
09: GGBAH  BubbleType=5
10: IIJAG  BubbleType=5
11: AAHB   BubbleType=4
12: JIK    BubbleType=3
13: KJHD   BubbleType=4
14: KHB    BubbleType=3
15: IDKG   BubbleType=4
16: KI     BubbleType=2
17: I      BubbleType=1
18: JJEGD  BubbleType=5
19: BCJH   BubbleType=4
20: BEJ    BubbleType=3
21: BBH    BubbleType=3
22: HK     BubbleType=2
23: FLCJ   BubbleType=4
24: FKH    BubbleType=3
25: JJLLF  BubbleType=5
26: BF     BubbleType=2
27: JJHLL  BubbleType=5
28: IL     BubbleType=2
29: IKF    BubbleType=3
30: IIL    BubbleType=3
31: AGF    BubbleType=3
32: HHJ    BubbleType=3
33: BDCAJ  BubbleType=5
34: CBGA   BubbleType=4
35: CCIH   BubbleType=4
36: CDBFG  BubbleType=5
37: CCJKB  BubbleType=5
38: FLHM   BubbleType=4
39: JD     BubbleType=2
40: DINLC  BubbleType=5
41: FCJN   BubbleType=4
42: CHOOM  BubbleType=5
43: JJIO   BubbleType=4
44: MND    BubbleType=3
```

### 每种鱼的配额与分布

| 代号 | 原始鱼种 ID | 数量 | 出现的配置序号 |
|---|---:|---:|---|
| A | 25 | 12 | 1, 3×2, 5, 6, 9, 10, 11×2, 31, 33, 34 |
| B | 43 | 15 | 1, 2, 6, 9, 11, 14, 19, 20, 21×2, 26, 33, 34, 36, 37 |
| C | 10 | 15 | 1, 3, 4, 19, 23, 33, 34, 35×2, 36, 37×2, 40, 41, 42 |
| D | 9 | 9 | 1, 13, 15, 18, 33, 36, 39, 40, 44 |
| E | 63 | 6 | 2, 4×2, 5, 18, 20 |
| F | 32 | 12 | 2, 7×2, 23, 24, 25, 26, 29, 31, 36, 38, 41 |
| G | 3 | 12 | 2, 4, 5, 8, 9×2, 10, 15, 18, 31, 34, 36 |
| H | 71 | 15 | 3, 9, 11, 13, 14, 19, 21, 22, 24, 27, 32×2, 35, 38, 42 |
| I | 5 | 15 | 3, 8, 10×2, 12, 15, 16, 17, 28, 29, 30×2, 35, 40, 43 |
| J | 2 | 24 | 4, 7×3, 8, 10, 12, 13, 18×2, 19, 20, 23, 25×2, 27×2, 32, 33, 37, 39, 41, 43×2 |
| K | 8 | 9 | 12, 13, 14, 15, 16, 22, 24, 29, 37 |
| L | 12 | 9 | 23, 25×2, 27×2, 28, 30, 38, 40 |
| M | 49 | 3 | 38, 42, 44 |
| N | 14 | 3 | 40, 41, 44 |
| O | 68 | 3 | 42×2, 43 |

含三条同种鱼的气泡：07: JJJFF。

## 骨架 C：50 泡、183 条、16 种

来源：原归档中的 `FishSort_LevelPreparation/configs/BubbleConfig_54.json`。完整机器可读数据见 [skeleton_C.json](skeleton_C.json)。

### 完整装鱼序列

```text
01: ABCDE  BubbleType=5
02: BDFGG  BubbleType=5
03: EHICC  BubbleType=5
04: EJD    BubbleType=3
05: AAECH  BubbleType=5
06: HHB    BubbleType=3
07: BGHCC  BubbleType=5
08: CJFD   BubbleType=4
09: KHBC   BubbleType=4
10: HGB    BubbleType=3
11: EEBC   BubbleType=4
12: DDJF   BubbleType=4
13: DJIL   BubbleType=4
14: JELCI  BubbleType=5
15: JDICC  BubbleType=5
16: FEKL   BubbleType=4
17: CJHIG  BubbleType=5
18: FGJA   BubbleType=4
19: DBBG   BubbleType=4
20: HHI    BubbleType=3
21: KJHGC  BubbleType=5
22: JJJHG  BubbleType=5
23: CK     BubbleType=2
24: MMC    BubbleType=3
25: CF     BubbleType=2
26: KFJN   BubbleType=4
27: KHC    BubbleType=3
28: EAHI   BubbleType=4
29: LOIC   BubbleType=4
30: ENI    BubbleType=3
31: HEI    BubbleType=3
32: PKECG  BubbleType=5
33: BHM    BubbleType=3
34: II     BubbleType=2
35: LJEP   BubbleType=4
36: DAFI   BubbleType=4
37: DJP    BubbleType=3
38: PC     BubbleType=2
39: NPO    BubbleType=3
40: NNC    BubbleType=3
41: FOM    BubbleType=3
42: EAMH   BubbleType=4
43: CCA    BubbleType=3
44: P      BubbleType=1
45: JN     BubbleType=2
46: MH     BubbleType=2
47: CB     BubbleType=2
48: CCEAI  BubbleType=5
49: KLGJJ  BubbleType=5
50: GDIKB  BubbleType=5
```

### 每种鱼的配额与分布

| 代号 | 原始鱼种 ID | 数量 | 出现的配置序号 |
|---|---:|---:|---|
| A | 9 | 9 | 1, 5×2, 18, 28, 36, 42, 43, 48 |
| B | 21 | 12 | 1, 2, 6, 7, 9, 10, 11, 19×2, 33, 47, 50 |
| C | 25 | 27 | 1, 3×2, 5, 7×2, 8, 9, 11, 14, 15×2, 17, 21, 23, 24, 25, 27, 29, 32, 38, 40, 43×2, 47, 48×2 |
| D | 52 | 12 | 1, 2, 4, 8, 12×2, 13, 15, 19, 36, 37, 50 |
| E | 43 | 15 | 1, 3, 4, 5, 11×2, 14, 16, 28, 30, 31, 32, 35, 42, 48 |
| F | 45 | 9 | 2, 8, 12, 16, 18, 25, 26, 36, 41 |
| G | 63 | 12 | 2×2, 7, 10, 17, 18, 19, 21, 22, 32, 49, 50 |
| H | 27 | 18 | 3, 5, 6×2, 7, 9, 10, 17, 20×2, 21, 22, 27, 28, 31, 33, 42, 46 |
| I | 10 | 15 | 3, 13, 14, 15, 17, 20, 28, 29, 30, 31, 34×2, 36, 48, 50 |
| J | 71 | 18 | 4, 8, 12, 13, 14, 15, 17, 18, 21, 22×3, 26, 35, 37, 45, 49×2 |
| K | 11 | 9 | 9, 16, 21, 23, 26, 27, 32, 49, 50 |
| L | 68 | 6 | 13, 14, 16, 29, 35, 49 |
| M | 67 | 6 | 24×2, 33, 41, 42, 46 |
| N | 16 | 6 | 26, 30, 39, 40×2, 45 |
| O | 47 | 3 | 29, 39, 41 |
| P | 14 | 6 | 32, 35, 37, 38, 39, 44 |

含三条同种鱼的气泡：22: JJJHG。

## 能说明什么，不能说明什么

同一骨架不仅锁定总鱼数，还锁定每种鱼的配额、每泡混合方式、同类鱼跨泡分布，以及配置中的顺序。只随机放够 123/162/183 条鱼，不能复现这些骨架。

A 有 4 处一泡含三条同种鱼；B、C 各有 1 处。这是静态装鱼分布差异，不能直接推导真实难度、消除顺序、玩家所需步数或是否可解。

B 的 M/N/O 各有 3 条，首次配置序号为 38/40/42：可以证明稀有鱼种在配置序列中的位置固定，不能据此断言它们必然在游戏后期出现。

三份骨架不是简单在同一清单末尾加泡：例如第 1 泡分别为 ABCDD、ABCD、ABCDE，已存在鱼种关系和装鱼数差别。它们最初是否由同一离线算法生成，仍未知。

基础表的 237 个骨架中，这 3 个匹配 1041 关。其他 234 个未在本文件逐一展开，不能把游戏全部关卡等同于这三份。

## 文件说明

`skeleton_A/B/C.json`：抽象字母、原始 ID 对照、逐泡清单、配额、跨泡位置与所有匹配基础关号。

`BubbleConfig_A/B/C_content_only.json`：从代表关提取的 BubbleID/BubbleType/FishStr 三个字段，保留原始鱼种 ID。不保证删除其他字段后可直接导入原游戏。

`verification.json`：全量重新统计与本次数据检查；没有运行游戏或验证可解。

复核命令（Python 3.10+，标准库）：

```bash
python extract_skeletons.py "FishSort_关卡模板与生成逻辑.zip" --output fishsort_skeletons
```
