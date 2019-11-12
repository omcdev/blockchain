CREATE TABLE [dbo].[RewardList]
(
	[Id] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),			--Id
	[BlockHash] VARCHAR(64) NOT NULL,						--区块Hash
	[MinerAddress] VARCHAR(64) NOT NULL,					--钱包地址
	[Hashes] BIGINT NOT NULL,								--Hash个数?
	[OriginalReward] BIGINT NOT NULL,						--原始奖励
	[ActualReward] BIGINT NOT NULL,							--实际奖励
	[Paid] INT NOT NULL,									--是否支付 0：未支付，1已支付
	[GenerateTime] BIGINT NOT NULL,							--生成时间时间戳
	[PaidTime] BIGINT NOT NULL,								--支付时间时间戳
	[TransactionHash] VARCHAR(64) NOT NULL,					--交易Hash
	[Commission] BIGINT NOT NULL,							--提成奖励
	[IsCommissionProcessed] INT NOT NULL,					--提成是否发放
	[CommissionProcessedTime] BIGINT						--提成发放时间
)