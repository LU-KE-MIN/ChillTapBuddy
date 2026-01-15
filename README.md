# 🐕 Chill Tap Buddy | 癒し系デスクトップバディ & ポモドーロタイマー

![Unity](https://img.shields.io/badge/Unity-2022.3%20LTS-black?logo=unity)
![C#](https://img.shields.io/badge/Language-C%23-239120?logo=c-sharp&logoColor=white)
![Architecture](https://img.shields.io/badge/Architecture-MVC%20%2F%20Event--Driven-blueviolet)
![License](https://img.shields.io/badge/License-MIT-blue)

## 📖 プロジェクト概要 (Overview)
「生産性向上」と「癒し」を融合させたデスクトップ常駐型アプリケーション。
ポモドーロ・テクニックに基づく集中タイマー機能と、インタラクティブなバーチャルペット育成要素を組み合わせ、ユーザーの作業継続を技術的にサポートします。

**【開発の目的】**
単なるツール開発に留まらず、**「Unityにおけるソフトウェアデザインパターンの実践」**（MVC、Observer、Singleton）をテーマに、保守性と拡張性の高いコード設計を目指しました。

---

## ✨ 主な機能 (Features)
- **ポモドーロタイマー**：25分間の集中セッション管理（ステートマシンによる制御）
- **インタラクティブバディ**：クリック等の入力に反応するアニメーションシステム
- **報酬システム**：集中時間に応じたポイント計算アルゴリズム
- **データ永続化**：進捗状況とストリーク（継続日数）のローカル保存
- **UI/UX**：作業を妨げないミニマルなデザインと直感的な操作性

---

## 🛠️ 技術構成 (Technical Architecture)

本プロジェクトでは、各クラスの責務（Responsibility）を明確にするため、以下のアーキテクチャを採用しています。

### 1. デザインパターンの適用
| Pattern | Implementation & Benefit |
| :--- | :--- |
| **MVC Architecture** | データ(`FocusTimer`)、表示(`UIController`)、制御(`GameManager`)を分離し、仕様変更に強い構造を実現。 |
| **Observer Pattern** | C#の `Action/Event` を活用。タイマー完了通知などを疎結合（Loose Coupling）で実装し、スパゲッティコードを回避。 |
| **Singleton** | `GameManager` 等のライフサイクル管理に使用し、グローバルな状態管理を一元化。 |

### 2. システム構成
```text
├── Core
│   ├── GameManager.cs      # アプリケーション全体のライフサイクル管理
│   └── SaveService.cs      # JSONシリアライズを用いたデータ永続化
├── Logic (Model)
│   ├── FocusTimer.cs       # タイマーロジック（ステート管理）
│   ├── RewardSystem.cs     # ポイント算出計算
│   └── InputTracker.cs     # アクティビティ計測
└── Presentation (View/Controller)
    ├── UIController.cs     # UIイベントハンドリング
    └── BuddyController.cs  # Animator制御とインタラクション
