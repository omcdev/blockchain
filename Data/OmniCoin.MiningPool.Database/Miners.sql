CREATE TABLE [dbo].[Miners]
(
	[Id] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),				--Id
	[Address] VARCHAR(64) NOT NULL,								--钱包地址
	[Account] NVARCHAR(64) NOT NULL,							--姓名
	[Type] INT NOT NULL,										--类型 0：POS，1：手机
	[SN] VARCHAR(64) NOT NULL,									--设备序列号
	[Status] INT NOT NULL,										--状态 0：enable，1：disable
	[Timstamp] BIGINT NOT NULL,									--写入时间戳?
	[LastLoginTime] BIGINT NOT NULL,							--最后登陆时间时间戳
)