CREATE TABLE [dbo].[HashRates]
(
	[Id] BIGINT PRIMARY KEY IDENTITY(1,1),						--
	[Time] BIGINT NOT NULL,										--时间戳
	[Hashes] BIGINT NOT NULL									--hash个数
)