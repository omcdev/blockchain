/*
SQLyog Community v13.1.2 (64 bit)
MySQL - 5.7.25-log : Database - fiiipool_main
*********************************************************************
*/

/*!40101 SET NAMES utf8 */;

/*!40101 SET SQL_MODE=''*/;

/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

-- CREATE DATABASE /*!32312 IF NOT EXISTS*/`fiiipool_main` /*!40100 DEFAULT CHARACTER SET latin1 */;

-- USE `fiiipool_main`;

/*Table structure for table `backupreward` */

DROP TABLE IF EXISTS `backupreward`;

CREATE TABLE `backupreward` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Id',
  `MinerAddress` varchar(64) NOT NULL DEFAULT '0' COMMENT '矿工地址',
  `StartDate` bigint(20) NOT NULL DEFAULT '0' COMMENT '备份表开始时间',
  `EndDate` bigint(20) NOT NULL DEFAULT '0' COMMENT '备份表结束时间',
  `OriginalReward` bigint(20) NOT NULL DEFAULT '0' COMMENT '原始奖励',
  `ActualReward` bigint(20) NOT NULL DEFAULT '0' COMMENT '实际奖励',
  PRIMARY KEY (`Id`),
  KEY `MinerAddress` (`MinerAddress`)
) ENGINE=InnoDB AUTO_INCREMENT=2550747 DEFAULT CHARSET=utf8 COMMENT='奖励备份表';

/*Table structure for table `blockrates` */

DROP TABLE IF EXISTS `blockrates`;

CREATE TABLE `blockrates` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Id',
  `Time` bigint(20) NOT NULL DEFAULT '0' COMMENT '时间戳',
  `Blocks` bigint(20) NOT NULL DEFAULT '0' COMMENT '区块个数',
  `Difficulty` bigint(20) NOT NULL DEFAULT '0' COMMENT '困难度',
  PRIMARY KEY (`Id`),
  KEY `Timestamp` (`Time`)
) ENGINE=InnoDB AUTO_INCREMENT=7431 DEFAULT CHARSET=utf8 COMMENT='区块增长率';

/*Table structure for table `blocks` */

DROP TABLE IF EXISTS `blocks`;

CREATE TABLE `blocks` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Id',
  `Hash` varchar(64) NOT NULL COMMENT '区块Hash',
  `Height` bigint(20) NOT NULL DEFAULT '0' COMMENT '区块高度',
  `Timstamp` bigint(20) NOT NULL DEFAULT '0' COMMENT '区块生成时间',
  `Generator` varchar(64) NOT NULL COMMENT '生成区块矿工钱包地址',
  `Nonce` bigint(20) NOT NULL DEFAULT '0' COMMENT '生成区块随机数',
  `TotalReward` bigint(20) NOT NULL DEFAULT '0' COMMENT '总的奖励',
  `TotalHashes` bigint(20) NOT NULL DEFAULT '0' COMMENT '总的Hash数，总的工作量',
  `Confirmed` int(11) NOT NULL DEFAULT '0' COMMENT '是否确认，检查当前区块是否有效',
  `IsDiscarded` int(11) NOT NULL DEFAULT '0' COMMENT '区块是否作废 0：正常， 1：已作废',
  `IsRewardSend` int(11) NOT NULL DEFAULT '0' COMMENT '区块奖励是否发放 0：未发放， 1：已发放',
  PRIMARY KEY (`Id`),
  KEY `Hash` (`Hash`),
  KEY `Height` (`Height`),
  KEY `Confirmed_IsDiscarded` (`Confirmed`,`IsDiscarded`),
  KEY `IsRewardSend` (`IsRewardSend`),
  KEY `Confirmed` (`Confirmed`)
) ENGINE=InnoDB AUTO_INCREMENT=365711 DEFAULT CHARSET=utf8 COMMENT='区块表';

/*Table structure for table `hashrates` */

DROP TABLE IF EXISTS `hashrates`;

CREATE TABLE `hashrates` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Id',
  `Time` bigint(20) NOT NULL DEFAULT '0' COMMENT '时间戳',
  `Hashes` bigint(20) NOT NULL DEFAULT '0' COMMENT '哈希个数',
  PRIMARY KEY (`Id`),
  KEY `Timestamp` (`Time`)
) ENGINE=InnoDB AUTO_INCREMENT=3093 DEFAULT CHARSET=utf8 COMMENT='哈希增长率';

/*Table structure for table `miners` */

DROP TABLE IF EXISTS `miners`;

CREATE TABLE `miners` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Id',
  `Address` varchar(64) NOT NULL COMMENT '钱包地址',
  `Account` varchar(64) NOT NULL COMMENT '姓名',
  `Type` int(11) NOT NULL DEFAULT '0' COMMENT '类型 0：POS，1：手机',
  `SN` varchar(64) NOT NULL COMMENT '设备序列号',
  `Status` int(11) NOT NULL DEFAULT '0' COMMENT '状态 0：enable，1：disable',
  `Timstamp` bigint(20) NOT NULL DEFAULT '0' COMMENT '写入时间戳',
  `LastLoginTime` bigint(20) NOT NULL DEFAULT '0' COMMENT '最后登陆时间时间戳',
  `PaidReward` bigint(20) NOT NULL DEFAULT '0' COMMENT '已经发放的奖励总额',
  `UnpaidReward` bigint(20) NOT NULL DEFAULT '0' COMMENT '没有发放的奖励总额',
  PRIMARY KEY (`Id`),
  KEY `Address` (`Address`),
  KEY `SN` (`SN`),
  KEY `SN_Account` (`Account`,`SN`),
  KEY `Status` (`Status`)
) ENGINE=InnoDB AUTO_INCREMENT=14898 DEFAULT CHARSET=utf8 COMMENT='矿工表';

/*Table structure for table `rewardlist20190916` */

DROP TABLE IF EXISTS `rewardlist20190916`;

CREATE TABLE `rewardlist20190916` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Id',
  `BlockHash` varchar(64) NOT NULL COMMENT '区块Hash',
  `MinerAddress` varchar(64) NOT NULL COMMENT '钱包地址',
  `Hashes` bigint(20) NOT NULL DEFAULT '0' COMMENT 'Hash个数',
  `OriginalReward` bigint(20) NOT NULL DEFAULT '0' COMMENT '原始奖励',
  `ActualReward` bigint(20) NOT NULL DEFAULT '0' COMMENT '实际奖励',
  `Paid` int(11) NOT NULL DEFAULT '0' COMMENT '是否支付 0：未支付，1已支付',
  `GenerateTime` bigint(20) NOT NULL DEFAULT '0' COMMENT '生成时间时间戳',
  `PaidTime` bigint(20) NOT NULL DEFAULT '0' COMMENT '支付时间时间戳',
  `TransactionHash` varchar(64) NOT NULL COMMENT '交易Hash',
  `Commission` bigint(20) NOT NULL DEFAULT '0' COMMENT '提成奖励',
  `IsCommissionProcessed` int(11) NOT NULL DEFAULT '0' COMMENT '提成是否发放',
  `CommissionProcessedTime` bigint(20) DEFAULT NULL COMMENT '提成发放时间',
  PRIMARY KEY (`Id`),
  KEY `BlockHash` (`BlockHash`),
  KEY `MinerAddress` (`MinerAddress`),
  KEY `Paid` (`Paid`),
  KEY `TransactionHash` (`TransactionHash`)
) ENGINE=InnoDB AUTO_INCREMENT=12089160 DEFAULT CHARSET=utf8 COMMENT='奖励信息表';

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;
