CREATE TABLE [dbo].[Blocks]
(
	[Id] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),				--Id
	[Hash] VARCHAR(64) NOT NULL,								--区块Hash
	[Height] BIGINT NOT NULL,									--区块高度
	[Timstamp] BIGINT NOT NULL,									--区块生成时间
	[Generator] VARCHAR(64) NOT NULL,							--生成区块矿工钱包地址
	[Nonce] BIGINT NOT NULL,									--生成区块随机数
	[TotalReward] BIGINT NOT NULL,								--总的奖励
	[TotalHashes] BIGINT NOT NULL,								--总的Hash数，总的工作量
	[Confirmed] INT NOT NULL,									--是否确认，检查当前区块是否有效
	[IsDiscarded] INT NOT NULL									--区块是否作废 0：正常， 1：已作废
)