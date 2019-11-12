CREATE TABLE [dbo].[BlockRates]
(
	[Id] BIGINT PRIMARY KEY IDENTITY(1,1),					--
	[Time] BIGINT NOT NULL,									--时间戳
	[Blocks] BIGINT NOT NULL,								--区块个数
	[Difficulty] BIGINT NOT NULL							--难度
)