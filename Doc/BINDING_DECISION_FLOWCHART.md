# 绑定决策流程图

## 决策树：是否需要使用绑定？

```
┌─────────────────────────────────────────────────┐
│   这个属性/字段的值会改变吗？                     │
└──────────────────┬──────────────────────────────┘
                   │
        ┌──────────┴──────────┐
        │NO                   │YES
        ▼                     ▼
    ❌ 不需要绑定         需要继续判断...
    (用 const 或          
    private field)      ┌─────────────────────────────────────────────┐
                        │ 这个值可以从其他属性推导出来吗？            │
                        └────────────┬────────────────────────────────┘
                                     │
                            ┌────────┴────────┐
                            │YES              │NO
                            ▼                 ▼
                        用计算属性          需要继续判断...
                        (public bool     
                        MyProp =>       ┌────────────────────────────────────┐
                        SomeOtherProp)  │ 这个值需要在 UI 中显示或被用户输入？ │
                                        └────────┬───────────────────────────┘
                                                 │
                                        ┌────────┴────────┐
                                        │YES              │NO
                                        ▼                 ▼
                                    ✅ 用               ❌ 不需要绑定
                                  ObservableProperty   (private field
                                  ([ObservableProperty] 或常量)
                                   private string 
                                   myValue = "")
```

## 应用到你的项目

### 示例 1: `temperature`（温度）

```
温度值会改变吗？ → YES (从设备读取新值)
    ↓
可以从其他属性推导吗？ → NO (独立的设备数据)
    ↓
需要显示在 UI 中吗？ → YES (显示给用户)
    ↓
结论：✅ 使用 [ObservableProperty]

[ObservableProperty]
private string temperature = "-- °C";
```

### 示例 2: `portTextBoxEnabled`（端口输入框启用状态）

```
按钮启用状态会改变吗？ → YES (连接时禁用，断开时启用)
    ↓
可以从其他属性推导吗？ → YES (!IsConnected)
    ↓
结论：✅ 使用计算属性

public bool PortTextBoxEnabled => !IsConnected;
```

### 示例 3: `_tempBuffer`（临时缓冲区）

```
值会改变吗？ → YES
    ↓
可以从其他属性推导吗？ → NO
    ↓
需要显示在 UI 或被用户输入？ → NO (仅内部使用)
    ↓
结论：❌ 不需要绑定

private string _tempBuffer;  // 普通私有字段
```

---

## 四象限决策矩阵

```
                    ┌─────────────────────────────────────────┐
                    │  是否需要在 UI 中显示或修改？           │
                    │         YES              NO             │
┌─────────────────┬─────────────────────────────────────────┐
│                 │                                         │
│是否可以          │  ObservableProperty                   │不需要绑定
│从其他属性       │                                         │
│推导？           │  例: Temperature (显示)                │
│                 │  例: Message (消息提示)                │  例: _tempBuffer
│YES              │  例: Host (用户输入)                   │  例: _internalState
│                 │                                         │
├─────────────────┼─────────────────────────────────────────┤
│                 │                                         │
│NO               │  计算属性                               │PrivateField
│                 │  (公开推导属性)                         │(私有字段)
│                 │                                         │
│                 │  例: ConnectButtonEnabled              │例: _modbusClient
│                 │  => !IsConnected                       │例: _cacheBuffer
│                 │                                         │
│                 │  例: ReadButtonEnabled                 │
│                 │  => IsConnected                        │
│                 │                                         │
└─────────────────┴─────────────────────────────────────────┘
```

---

## 时间轴：当值改变时会发生什么

### ❌ 使用 5 个独立属性（优化前）

```
时间  →  →  →  →  →  →

用户点击连接按钮
  │
  ├─ IsConnected = true           (第 1 个改变 ✓)
  ├─ ConnectButtonEnabled = false  (第 2 个改变 ✓)
  ├─ DisconnectButtonEnabled = true(第 3 个改变 ✓)
  ├─ ReadButtonEnabled = true      (第 4 个改变 ✓)
  ├─ HostTextBoxEnabled = false    (第 5 个改变 ✓)
  └─ PortTextBoxEnabled = false    (第 6 个改变 ✓)
  
潜在问题：
⚠️ 6 个同步操作，任何一个遗漏都会导致 UI 不一致
```

### ✅ 使用计算属性（优化后）

```
时间  →

用户点击连接按钮
  │
  └─ IsConnected = true (仅 1 个改变 ✓)
       │
       ├─ PropertyChanged("IsConnected") 通知
       │   └─ ConnectButtonEnabled 自动重新计算 ✓
       │   └─ DisconnectButtonEnabled 自动重新计算 ✓
       │   └─ ReadButtonEnabled 自动重新计算 ✓
       │   └─ HostTextBoxEnabled 自动重新计算 ✓
       │   └─ PortTextBoxEnabled 自动重新计算 ✓
       │
       └─ UI 全部自动更新 ✓

优点：
✨ 只需要管理 1 个值
✨ 无同步错误的可能
✨ 自动更新所有相关计算
```

---

## 代码示例对比表

| 需求 | ❌ 不推荐的做法 | ✅ 推荐的做法 |
|------|---------|----------|
| 显示传感器数据 | `private string _temp;`<br/>手动更新 UI | `[ObservableProperty]`<br/>`private string temperature;` |
| 按钮启用/禁用 | `[ObservableProperty]`<br/>`private bool buttonEnabled = true;`<br/>手动同步 | `public bool ButtonEnabled`<br/>`=> !IsConnected;` |
| 内部缓存 | `[ObservableProperty]`<br/>`private string _buffer;` | `private string _buffer;` |
| 计数总和 | `[ObservableProperty]`<br/>`private int _total;`<br/>每次改变时更新 | `public int Total`<br/>`=> Items.Sum(x => x.Value);` |
| 错误状态 | `[ObservableProperty]`<br/>`private bool hasError;`<br/>手动检查和更新 | `public bool HasError`<br/>`=> Errors.Count > 0;` |

---

## 快速检查表

在添加一个新属性前，问自己：

- [ ] 这个值会改变吗？
  - NO → 使用 const
  - YES → 继续

- [ ] 这个值可以从其他属性推导吗？
  - YES → 使用计算属性
  - NO → 继续

- [ ] 这个值需要在 UI 中显示或被修改吗？
  - YES → 使用 `[ObservableProperty]`
  - NO → 使用私有字段

---

## 你的应用中的实际应用

### ✅ 正确的绑定（当前实现）

```csharp
// 核心状态 - 用 ObservableProperty
[ObservableProperty]
private bool isConnected = false;

// 数据显示 - 用 ObservableProperty
[ObservableProperty]
private string temperature = "-- °C";

// 推导状态 - 用计算属性 ✨
public bool ConnectButtonEnabled => !IsConnected;
public bool ReadButtonEnabled => IsConnected;
public bool HostTextBoxEnabled => !IsConnected;

// 内部字段 - 不绑定
private ModbusTcpClient _modbusClient;
```

### 结果对比

| 指标 | 优化前 | 优化后 |
|------|--------|--------|
| ObservableProperty 数 | 13 | 8 |
| 计算属性数 | 0 | 5 |
| 私有字段数 | 1 | 1 |
| 状态不同步风险 | ⚠️ 高 | ✅ 无 |
| 代码可维护性 | 低 | 高 |

---

## 关键要点

1. **不是所有值都需要绑定** - 只有影响 UI 的值才需要
2. **计算属性 > 独立属性** - 可推导的状态用计算属性
3. **单一数据源** - 同一个业务概念只维护一个值
4. **自动更新** - 依赖计算属性会自动更新
5. **更少的代码** - 结果是代码更简洁、更可靠

🎯 **最终目标：最小化需要手动维护的状态**
