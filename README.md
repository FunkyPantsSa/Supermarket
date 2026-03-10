# Supermarket

基于 `.NET 8 + WinForms + SQLite` 的超市人员与营业管理系统。

项目当前支持：

- 管理员登录
- 营业员登录
- 收银结算
- 商品管理
- 订单管理
- 统计分析
- 收银流水查询与导出
- 营业员账号自动同步与管理员重置密码

这份文档面向接手项目的研发，目标是让任何一个开发者拿到代码后可以快速跑通、理解结构并继续迭代。

## 1. 项目概览

系统是一个本地 WinForms 桌面应用，数据存储在 SQLite 文件中。程序启动后先进入登录页，根据账号角色决定主界面的可见页面和可操作范围。

当前角色模型：

- `Admin`：管理员，可查看和操作全部页面
- `Cashier`：营业员，只能使用收银台和查看/操作自己相关的订单，不能查看统计、流水、商品管理和人员管理

## 2. 技术栈

- 运行时：`.NET 8`
- UI：`Windows Forms`
- 数据库：`SQLite`，通过 `winsqlite3` 的 P/Invoke 直接访问
- 语言：`C#`
- 项目类型：单体桌面应用

项目文件见 [Supermarket.csproj](/C:/Users/volin/Desktop/Supermarket/Supermarket.csproj)。

## 3. 快速启动

### 3.1 环境要求

- Windows
- 已安装 `.NET SDK 8.0`

### 3.2 本地运行

在项目根目录执行：

```powershell
dotnet build
dotnet run
```

默认数据库路径：

`bin\Debug\net8.0-windows\db\supermarket.db`

首次运行时，程序会自动初始化数据库、表结构、默认商品、默认营业员和默认账号。

## 4. 默认账号

### 4.1 管理员

- 用户名：`admin`
- 密码：`123456`

### 4.2 营业员

营业员账号会根据 `Cashiers` 表自动同步生成：

- 用户名：营业员编号的小写形式，例如 `c001`
- 默认密码：`123456`

示例：

- `c001 / 123456`
- `c002 / 123456`

管理员可以在“人员管理”页面重置营业员密码。

## 5. 权限说明

### 5.1 管理员权限

- 可访问收银台
- 可访问商品管理
- 可访问订单管理
- 可访问收银流水
- 可访问统计页面
- 可访问人员管理
- 可为任意营业员结算
- 可查看全部订单
- 可导出流水
- 可作废订单

### 5.2 营业员权限

- 只能访问收银台
- 只能访问自己的订单页面
- 只能为自己结算
- 只能查看和操作自己的订单
- 不能访问统计页面
- 不能访问收银流水
- 不能访问商品管理
- 不能访问人员管理

权限主入口位于：

- [Program.cs](/C:/Users/volin/Desktop/Supermarket/Program.cs)
- [LoginForm.cs](/C:/Users/volin/Desktop/Supermarket/LoginForm.cs)
- [Form1.cs](/C:/Users/volin/Desktop/Supermarket/Form1.cs)
- [Form1.RoleUi.cs](/C:/Users/volin/Desktop/Supermarket/Form1.RoleUi.cs)
- [Services/AuthService.cs](/C:/Users/volin/Desktop/Supermarket/Services/AuthService.cs)

## 6. 主要功能说明

### 6.1 登录

- 启动后弹出登录页
- 通过 `AuthService` 校验账号和密码
- 登录成功后生成当前用户上下文
- 主窗体按角色应用权限

核心文件：

- [Program.cs](/C:/Users/volin/Desktop/Supermarket/Program.cs)
- [LoginForm.cs](/C:/Users/volin/Desktop/Supermarket/LoginForm.cs)
- [Entities/AppUser.cs](/C:/Users/volin/Desktop/Supermarket/Entities/AppUser.cs)
- [Services/AuthService.cs](/C:/Users/volin/Desktop/Supermarket/Services/AuthService.cs)

### 6.2 收银台

- 按商品 ID 添加到购物车
- 支持修改数量、折扣、实收、找零
- 生成订单、扣减库存、写入流水
- 自动生成本地小票文件 `receipt.txt`

核心文件：

- [Form1.cs](/C:/Users/volin/Desktop/Supermarket/Form1.cs)
- [BLL/OrderBLL.cs](/C:/Users/volin/Desktop/Supermarket/BLL/OrderBLL.cs)
- [BLL/ProductBLL.cs](/C:/Users/volin/Desktop/Supermarket/BLL/ProductBLL.cs)
- [Services/InventoryService.cs](/C:/Users/volin/Desktop/Supermarket/Services/InventoryService.cs)
- [Entities/OrderRecord.cs](/C:/Users/volin/Desktop/Supermarket/Entities/OrderRecord.cs)

### 6.3 商品管理

- 新增商品
- 修改价格
- 删除商品（逻辑删除）
- 按品类或供货商筛选
- 库存管理

核心文件：

- [Form1.cs](/C:/Users/volin/Desktop/Supermarket/Form1.cs)
- [Services/InventoryService.cs](/C:/Users/volin/Desktop/Supermarket/Services/InventoryService.cs)
- [BLL/ProductBLL.cs](/C:/Users/volin/Desktop/Supermarket/BLL/ProductBLL.cs)
- [DAL/ProductDAL.cs](/C:/Users/volin/Desktop/Supermarket/DAL/ProductDAL.cs)

### 6.4 订单管理

- 查询订单
- 快速筛选今日、昨日、本月订单
- 按订单号、营业员、时间区间筛选
- 查看订单明细
- 打印订单
- 作废订单并回补库存

营业员登录后，该页自动切换为“我的订单”视角。

核心文件：

- [Form1.cs](/C:/Users/volin/Desktop/Supermarket/Form1.cs)
- [Form1.RoleUi.cs](/C:/Users/volin/Desktop/Supermarket/Form1.RoleUi.cs)
- [BLL/OrderBLL.cs](/C:/Users/volin/Desktop/Supermarket/BLL/OrderBLL.cs)
- [DAL/OrderDAL.cs](/C:/Users/volin/Desktop/Supermarket/DAL/OrderDAL.cs)

### 6.5 统计页面

- 时间区间筛选
- 营业额汇总
- 订单数和客单价汇总
- 按营业员聚合统计

只对管理员开放。

核心文件：

- [Form1.cs](/C:/Users/volin/Desktop/Supermarket/Form1.cs)
- [BLL/StatisticsBLL.cs](/C:/Users/volin/Desktop/Supermarket/BLL/StatisticsBLL.cs)
- [UI/StatisticsPanel.cs](/C:/Users/volin/Desktop/Supermarket/UI/StatisticsPanel.cs)

### 6.6 收银流水

- 查询销售与退款流水
- 按时间区间筛选
- 导出 CSV

只对管理员开放。

核心文件：

- [UI/TransactionLogPanel.cs](/C:/Users/volin/Desktop/Supermarket/UI/TransactionLogPanel.cs)
- [BLL/TransactionLogBLL.cs](/C:/Users/volin/Desktop/Supermarket/BLL/TransactionLogBLL.cs)
- [DAL/TransactionLogDAL.cs](/C:/Users/volin/Desktop/Supermarket/DAL/TransactionLogDAL.cs)

### 6.7 人员管理

- 展示当前账号列表
- 区分管理员与营业员
- 管理员可将营业员密码重置为 `123456`

核心文件：

- [UI/StaffManagementPanel.cs](/C:/Users/volin/Desktop/Supermarket/UI/StaffManagementPanel.cs)
- [Services/AuthService.cs](/C:/Users/volin/Desktop/Supermarket/Services/AuthService.cs)
- [Services/SqliteDb.cs](/C:/Users/volin/Desktop/Supermarket/Services/SqliteDb.cs)

## 7. 项目结构

```text
Supermarket/
├─ BLL/                 业务逻辑层
├─ DAL/                 数据访问层
├─ Entities/            实体模型
├─ Services/            基础服务、SQLite 初始化、认证、库存、订单等
├─ UI/                  独立 UI 面板、主题、辅助组件
├─ Form1.cs             主窗体业务逻辑
├─ Form1.Designer.cs    主窗体布局定义
├─ Form1.RoleUi.cs      角色权限与页面可见性控制
├─ LoginForm.cs         登录页与修改密码弹窗
├─ Program.cs           应用启动入口
└─ README.md            项目文档与变更记录
```

### 7.1 分层职责

#### `Entities`

定义系统使用的数据模型，例如：

- `Product`
- `OrderRecord`
- `OrderItem`
- `Cashier`
- `TransactionLog`
- `AppUser`

#### `DAL`

负责直接读写数据库，例如：

- `ProductDAL`
- `OrderDAL`
- `TransactionLogDAL`

#### `BLL`

负责封装业务逻辑，例如：

- 库存校验
- 下单
- 退单
- 统计

#### `Services`

更偏基础设施与通用服务，例如：

- 数据库初始化与 Schema 维护
- 登录认证
- 收银员加载
- 订单列表缓存与导出

#### `UI`

承载可复用 UI 组件，例如：

- 主题样式
- 收银流水面板
- 人员管理面板

## 8. 核心代码入口

新人接手建议按下面顺序阅读：

1. [Program.cs](/C:/Users/volin/Desktop/Supermarket/Program.cs)
2. [LoginForm.cs](/C:/Users/volin/Desktop/Supermarket/LoginForm.cs)
3. [Form1.cs](/C:/Users/volin/Desktop/Supermarket/Form1.cs)
4. [Form1.RoleUi.cs](/C:/Users/volin/Desktop/Supermarket/Form1.RoleUi.cs)
5. [Services/SqliteDb.cs](/C:/Users/volin/Desktop/Supermarket/Services/SqliteDb.cs)
6. [Services/AuthService.cs](/C:/Users/volin/Desktop/Supermarket/Services/AuthService.cs)
7. [BLL/OrderBLL.cs](/C:/Users/volin/Desktop/Supermarket/BLL/OrderBLL.cs)
8. [DAL/OrderDAL.cs](/C:/Users/volin/Desktop/Supermarket/DAL/OrderDAL.cs)

## 9. 数据库设计

数据库由 [Services/SqliteDb.cs](/C:/Users/volin/Desktop/Supermarket/Services/SqliteDb.cs) 自动初始化。

### 9.1 `Products`

商品表。

关键字段：

- `ID`
- `Name`
- `Supplier`
- `Category`
- `Price`
- `StockCount`
- `LowStockThreshold`
- `IsDeleted`

### 9.2 `Cashiers`

营业员表。

关键字段：

- `CashierId`
- `CashierName`

### 9.3 `Users`

登录账号表。

关键字段：

- `UserName`
- `PasswordHash`
- `DisplayName`
- `Role`
- `CashierId`
- `IsActive`
- `UpdatedAt`

说明：

- `Admin` 账号不强制绑定 `CashierId`
- `Cashier` 账号通常绑定一个营业员
- 运行时会自动根据 `Cashiers` 表同步缺失账号

### 9.4 `Orders`

订单主表。

关键字段：

- `OrderId`
- `CashierId`
- `CashierName`
- `PayType`
- `DiscountAmount`
- `ReceivedAmount`
- `ChangeAmount`
- `Status`
- `CreatedAt`
- `CreatedTimestamp`
- `TotalAmount`

### 9.5 `OrderItems`

订单明细表。

关键字段：

- `ItemID`
- `OrderID`
- `ProductID`
- `Quantity`
- `UnitPriceAtTime`
- `ProductName`
- `Supplier`
- `Category`

### 9.6 `TransactionLogs`

交易流水表。

关键字段：

- `LogID`
- `Type`
- `Amount`
- `Detail`
- `Timestamp`

## 10. 关键运行机制

### 10.1 启动流程

1. 程序启动
2. 初始化 SQLite
3. 检查并补齐表结构
4. 写入默认数据
5. 同步营业员账号
6. 显示登录页
7. 登录成功后进入主窗体
8. 根据角色应用页面权限

### 10.2 订单提交流程

1. 从购物车读取商品
2. 校验库存
3. 生成 `OrderRecord`
4. 扣减库存
5. 写入订单主表和明细表
6. 写入销售流水
7. 生成 `receipt.txt`
8. 刷新商品、订单、统计页面

### 10.3 订单作废流程

1. 选择订单
2. 校验订单状态
3. 回补库存
4. 更新订单为 `Voided`
5. 写入退款流水
6. 刷新界面

## 11. UI 说明

最近一次改造后，项目已经引入统一主题文件：

- [UI/AppTheme.cs](/C:/Users/volin/Desktop/Supermarket/UI/AppTheme.cs)

它主要负责：

- 按钮风格统一
- 表格风格统一
- 表单与卡片背景统一
- 登录页与主界面视觉一致

如果后续要继续美化页面，建议优先扩展 `AppTheme`，避免每个页面各自写颜色和字体。

## 12. 常见开发任务

### 12.1 新增一个管理员页面

建议步骤：

1. 新建一个 `UserControl` 到 `UI/`
2. 在 `Form1` 中创建新的 `TabPage`
3. 在 [Form1.RoleUi.cs](/C:/Users/volin/Desktop/Supermarket/Form1.RoleUi.cs) 中加入导航按钮和管理员可见控制

### 12.2 新增一个角色字段或账号属性

建议修改点：

- [Entities/AppUser.cs](/C:/Users/volin/Desktop/Supermarket/Entities/AppUser.cs)
- [Services/SqliteDb.cs](/C:/Users/volin/Desktop/Supermarket/Services/SqliteDb.cs)
- [Services/AuthService.cs](/C:/Users/volin/Desktop/Supermarket/Services/AuthService.cs)
- [LoginForm.cs](/C:/Users/volin/Desktop/Supermarket/LoginForm.cs)

### 12.3 修改订单筛选逻辑

主要入口：

- [Form1.cs](/C:/Users/volin/Desktop/Supermarket/Form1.cs)
  - `RefreshOrderGrid`
  - `ApplyOrderAdvancedFilter`
  - `ApplyOrderQuickRange`

### 12.4 修改数据库初始化或默认数据

主要入口：

- [Services/SqliteDb.cs](/C:/Users/volin/Desktop/Supermarket/Services/SqliteDb.cs)

## 13. 测试与验证建议

当前项目没有自动化测试，建议每次改动后至少手工验证以下流程：

### 13.1 管理员验证

- 能否使用 `admin / 123456` 登录
- 是否可以看到全部页面
- 是否可以查看所有订单
- 是否可以进入人员管理
- 是否可以重置营业员密码

### 13.2 营业员验证

- 能否使用 `c001 / 123456` 登录
- 是否只能看到收银台和订单页
- 订单页是否只显示自己的订单
- 是否不能打开统计、流水、商品管理、人员管理

### 13.3 订单验证

- 下单是否扣减库存
- 作废是否回补库存
- 订单是否写入 `Orders` 与 `OrderItems`
- 是否生成交易流水

### 13.4 导出验证

- CSV 导出是否成功
- 时间筛选是否生效

## 14. 已知注意事项

- 项目当前直接使用 SQL 字符串拼接，虽然对文本做了基础转义，但后续如有更复杂输入，建议逐步改造成参数化查询
- 项目是 WinForms 单体结构，业务逻辑主要集中在 `Form1.cs`，后续功能继续增长时，建议逐步拆分更多独立面板或服务
- 目前以手工测试为主，建议后续补充基础单元测试或至少加入关键流程回归清单

## 15. 文档维护约定

从现在开始，所有功能改动都应该同步更新本 `README.md`，至少更新以下两部分：

1. 对应功能章节
2. 变更记录

推荐规则：

- 新增功能：补充“主要功能说明”与“项目结构”
- 权限改动：补充“权限说明”
- 数据库变更：补充“数据库设计”
- 大版本 UI 调整：补充“UI 说明”
- 每次提交后：追加“变更记录”

## 16. 变更记录

### 2026-03-10

本次完成管理员/营业员双角色能力与整套界面优化。

新增：

- 新增 `AppUser` 用户实体
- 新增统一主题文件 `UI/AppTheme.cs`
- 新增人员管理页面 `UI/StaffManagementPanel.cs`
- 新增角色控制文件 `Form1.RoleUi.cs`
- 新增 `README.md`

改动：

- 登录改为返回完整当前用户，而不是只返回用户名
- `Users` 表新增 `DisplayName`、`Role`、`CashierId`、`IsActive`
- 程序启动时自动同步营业员账号
- 管理员可重置营业员密码为 `123456`
- 营业员登录后只能查看和操作自己的订单
- 营业员不可查看统计页面、收银流水、商品管理、人员管理
- 管理员可查看全部页面
- 登录页与主界面统一做了视觉优化
- 收银流水页样式升级

验证：

- 已执行 `dotnet build`
- 构建通过，无报错

### 2026-03-10（第二次更新）

本次主要修复了界面可用性问题，并继续完善订单生成、打印与流水页面。

改动：

- 登录页改为自适应布局，避免窗口尺寸变化时内容显示不全
- 订单生成页增加关闭入口
- 订单生成页打开后增加稳定导航入口，切换到其他页面后仍可重新进入
- 收银台右侧区域加宽，总金额与找零显示改为更完整的文本格式
- 订单管理勾选列标题由“选”调整为“选择”，并优化“全选订单 / 已选 X 单”提示
- 打印按钮改为“打印所选订单”
- 未勾选订单时，打印按钮不可点击
- 收银流水页面新增“今日流水”和“本月流水”快捷筛选
- 收银流水列表增加“收银员”字段
- 流水导出按钮从订单管理移动到收银流水页面
- “营业管理”统一更名为“人员管理”

数据库与数据模型：

- `TransactionLogs` 表新增 `CashierName` 字段
- 交易流水实体增加 `CashierName` 属性
- 销售和退款流水写入时同步记录收银员姓名

验证：

- 已执行 `dotnet build`
- 构建通过，无报错

### 2026-03-10（第三次更新）

本次继续完善页面交互与人员维护能力。

改动：

- 修复部分页面布局问题，重点优化收银流水页顶部区域的自适应布局
- 人员管理新增“修改姓名”
- 修改姓名时同步更新账号显示名，并同步营业员主数据与订单收银员名称
- 统计页面新增“订单详情”入口，可直接跳转到订单管理
- 收银流水页面新增“订单详情”入口，可直接跳转到订单管理
- 从统计页或流水页跳转到订单管理后，会自动定位并选中对应订单
- 订单管理中的选中订单高亮对比度进一步增强

验证：

- 已执行 `dotnet build`
- 构建通过，无报错

### 2026-03-10（第四次更新）

本次继续修复细节体验问题。

改动：

- 修复“订单生成”页面关闭后左侧导航仍然保留入口的问题
- 优化收银流水页面时间筛选区域，避免全屏时开始和结束时间输入框被拉得过长
- 进一步增强订单管理中选中订单的高亮颜色，提高可识别性

验证：

- 已执行 `dotnet build /p:UseAppHost=false`
- 构建通过，无代码错误

### 2026-03-10（第五次更新）

本次继续优化收银台和隐藏页面交互。

改动：

- 所有主要数据表格统一使用更醒目的选中高亮样式
- 修复“订单生成”页面关闭后点击标题一次就重新出现的问题，现已恢复为需要多次点击才再次解锁
- 收银台新增商品名模糊搜索区域
- 支持按商品名称关键字筛选匹配商品并加入购物车
- 商品名称加入购物车与商品编码加入购物车共用数量选择，页面布局同步优化

验证：

- 已执行 `dotnet build /p:UseAppHost=false`
- 构建通过，无代码错误

### 2026-03-10（第六次更新）

本次继续优化收银台与账号提示。

改动：

- 缩小收银台“匹配商品”区域占用高度
- 修复匹配商品列表显示为类型名而不是商品信息的问题
- 购物车支持删除单个商品
- 购物车支持直接修改单个商品数量
- 进一步收紧收银流水页开始/结束时间控件宽度
- 登录页不再显示默认密码说明
- 人员管理页不再常驻显示默认密码说明
- 重置密码成功后，弹窗中明确提示默认密码为 `123456`

验证：

- 已执行 `dotnet build /p:UseAppHost=false`
- 构建通过，无代码错误
