# ScriptableObject 数据管理系统

[English](README_EN.md) | 简体中文

一个功能强大的 Unity ScriptableObject 数据管理工具，提供可视化编辑器界面，帮助你高效管理项目中的所有 ScriptableObject 资产。
支持增量更新和批量操作。

## 功能特性

### 核心功能

- **自动扫描注册** - 自动扫描并注册 ScriptableObject 类型
- **通过 `[ManagedData]` 特性标记** - 通过添加特性标记为其增加分类信息
- **分类管理** - 支持按 Category 分组显示和管理资产
- **快速创建** - 可视化界面快速创建新的 ScriptableObject 实例

### 查询与搜索

- **实时搜索** - 支持按名称、类型进行实时搜索过滤
- **高级搜索** - 多条件组合查询，快速定位目标资产
- **类型过滤** - 按类型或分类筛选资产

### 批量操作

- **多选模式** - 支持同时选择多个资产进行操作
- **批量编辑** - 对选中的多个资产进行批量属性修改
- **路径导出** - 一键导出所有资产路径列表

### 依赖分析

- **引用查找** - 查找指定资产被哪些对象引用
- **依赖查看** - 可视化查看资产的依赖关系图
- **孤立检测** - 找出未被任何对象引用的孤立资产

### SOHelper 快捷编辑

- **快速编辑窗口** - 独立窗口快速编辑资产属性
- **Inspector 集成** - 与原生 Inspector 无缝集成
- **历史导航** - 支持在 SO 引用之间前进/后退跳转
- **快捷按钮** - 所有 ScriptableObject 引用字段自带快捷打开按钮 (🔍)

## 安装方法

1. 将本项目克隆到 Unity 项目的 `Assets/` 目录下：
   ```
   Assets/
   ```

2. 或使用 Unity Package Manager 的 Git URL 功能：

3. 已经打包好的 Unity Package 文件，可以直接导入使用。

## 使用方法

### 标记可管理类型

在你需要管理的 ScriptableObject 类上添加 `[ManagedData]` 特性：

```csharp
using UnityEngine;

[ManagedData("角色配置")]
[CreateAssetMenu(fileName = "New Character", menuName = "Game/Character")]
public class CharacterSO : ScriptableObject
{
    public string displayName;
    public int maxHealth;
    // ...
}
```

### 打开管理窗口

在 Unity 编辑器中：
- 菜单路径：`Tools/SO Data Manager`
- 快捷键：`Ctrl + Shift + M`

### 窗口功能说明

| 功能按钮 | 说明 |
|---------|------|
| Scan | 重新扫描项目中的所有资产 |
| Create + | 创建新的 ScriptableObject 实例 |
| Export Paths | 导出所有资产路径到文本文件 |
| Find References | 查找选中资产的引用 |
| Dependencies | 查看资产的依赖关系图 |
| Orphans | 查看孤立资产 |
| Batch Edit | 批量编辑选中的资产 |

## 项目结构

```
ScriptObjectManagerSystem/
├── Runtime/
│   └── ManagedDataAttribute.cs    # 管理数据标记特性（运行时）
├── Editor/
│   ├── DataManagement/
│   │   ├── Core/                  # 核心数据结构
│   │   ├── Services/              # 业务逻辑服务
│   │   ├── UI/                    # 编辑器窗口
│   │   └── SOQuickEditWindow.cs   # 快速编辑窗口
│   └── SOHelper/                  # 辅助工具
│       ├── GenericSOWindow.cs     # 带历史导航的 SO 编辑器
│       └── SOPopupDrawer.cs       # SO 引用快捷按钮绘制器
├── ScriptableObjectManager.asmdef       # Runtime 程序集定义
└── Editor/ScriptableObjectManager.Editor.asmdef  # Editor 程序集定义
```

### SOHelper 模块说明

SOHelper 提供了增强的 ScriptableObject 编辑体验：

- **GenericSOWindow** - 通用的 SO 编辑窗口，支持历史导航功能，可以在多个 SO 引用之间快速跳转和返回
- **SOPopupDrawer** - 为所有 ScriptableObject 类型的引用字段自动添加快捷打开按钮 (🔍)，点击即可在编辑窗口中打开对应的资产

## 依赖关系

- Unity 2020.3 或更高版本
- 无外部第三方依赖

## 贡献

欢迎提交 Issue 和 Pull Request！

## 许可证

MIT License

## 作者

whatevertogo
讨厌八股文的人
