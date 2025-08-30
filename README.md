# ClickableObject - Usage Guide

## Demo
![ClickableObject](demo.gif)

## 概要: 

ClickableObject は、3Dオブジェクトにアタッチすることで「マウスオーバー」「クリック」などの直感的な操作とフィードバックをする Unity 用 MonoBehaviour です。

マウスカーソルをオブジェクトに合わせると、イベント発火 / アニメーション再生 / マテリアル切替 が可能

現在カーソルが乗っているオブジェクトは static に保存され、外部から参照可能

## 使い方: 

### 1. コンポーネントを追加
シーン内の任意の 3D オブジェクトに ClickableObject をアタッチします。

⚠ 必須: オブジェクトには Collider が必要です（Raycast 用）。

### 2. インスペクター設定

#### ビジュアルフィードバック

Enable Highlight: ハイライト表示の有効/無効

Highlight Material: ハイライト用マテリアル

Apply Highlight Child: 子オブジェクトにも適用

#### イベント
On Click: クリック時のイベント

On Mouse Enter: マウスが乗った瞬間のイベント

On Mouse Exit: マウスが外れた瞬間のイベント

#### アニメーション

Enable Animation: Animator 制御を有効化

IsMouseOver Parameter: マウスオーバー時に切り替える bool

IsClicked Parameter: クリック時にトリガーされる Trigger

### 3. 外部

現在ホバー中のオブジェクトを取得
```csharp
if (ClickableObject.AnyHovered)
{
    Debug.Log("Hovering: " + ClickableObject.All[0].name);
}
```

クリック処理をスクリプトから発火
```csharp
myClickableObject.TriggerClick();
```
## オプション: 

Show Debug GUI: デバッグ用のオンスクリーン情報を表示

Enable Debug Log: Console にマウスイベントのログを出力

SetClickablesActive(bool): 全ての ClickableObject の有効/無効を一括切替
