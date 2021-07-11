-- --------------------------------------------------------
-- Хост:                         127.0.0.1
-- Версия сервера:               5.6.48-log - MySQL Community Server (GPL)
-- Операционная система:         Win64
-- HeidiSQL Версия:              11.2.0.6213
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;


-- Дамп структуры базы данных ktvcss
CREATE DATABASE IF NOT EXISTS `ktvcss` /*!40100 DEFAULT CHARACTER SET utf8 COLLATE utf8_unicode_ci */;
USE `ktvcss`;

-- Дамп структуры для таблица ktvcss.battlecup_list
CREATE TABLE IF NOT EXISTS `battlecup_list` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` text COLLATE utf8_unicode_ci,
  `Bracket` int(11) DEFAULT NULL,
  `Status` int(11) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Id` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=34 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица ktvcss.battlecup_matches
CREATE TABLE IF NOT EXISTS `battlecup_matches` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `MatchId` int(11) NOT NULL DEFAULT '0',
  `MatchPlayed` int(11) DEFAULT '0',
  `MatchWinnerName` text COLLATE utf8_unicode_ci,
  `MatchFormat` int(11) DEFAULT '1',
  `TeamAName` text COLLATE utf8_unicode_ci,
  `TeamBName` text COLLATE utf8_unicode_ci,
  `TournamentId` int(11) DEFAULT NULL,
  `ServerId` int(11) DEFAULT NULL,
  `DateTimeStart` datetime DEFAULT NULL,
  `DateTimeEnd` datetime DEFAULT NULL,
  `BracketItem` text COLLATE utf8_unicode_ci,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `MatchId` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица ktvcss.battlecup_recovery
CREATE TABLE IF NOT EXISTS `battlecup_recovery` (
  `RegBoardId` text COLLATE utf8_unicode_ci,
  `bCupId` text COLLATE utf8_unicode_ci,
  `bTeams` text COLLATE utf8_unicode_ci,
  `Teams` text COLLATE utf8_unicode_ci,
  `postId` text COLLATE utf8_unicode_ci,
  `password` text COLLATE utf8_unicode_ci
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица ktvcss.ccw_matches
CREATE TABLE IF NOT EXISTS `ccw_matches` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `TeamAName` text COLLATE utf8_unicode_ci,
  `TeamBName` text COLLATE utf8_unicode_ci,
  `ServerId` int(11) DEFAULT NULL,
  `DateTimeStart` datetime DEFAULT NULL,
  `DateTimeEnd` datetime DEFAULT NULL,
  `Playing` int(11) DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `MatchId` (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица ktvcss.ccw_teams
CREATE TABLE IF NOT EXISTS `ccw_teams` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `TNAME` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `TSTUFF` text COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`ID`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица ktvcss.cups_bracket
CREATE TABLE IF NOT EXISTS `cups_bracket` (
  `POSITION` int(11) DEFAULT NULL,
  `TEXT` text COLLATE utf8mb4_unicode_ci,
  `X` int(11) DEFAULT NULL,
  `Y` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица ktvcss.cups_list
CREATE TABLE IF NOT EXISTS `cups_list` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `NAME` text COLLATE utf8mb4_unicode_ci,
  `DESCRIPTION` text COLLATE utf8mb4_unicode_ci,
  `BRACKET` text COLLATE utf8mb4_unicode_ci,
  `STARTDATE` datetime DEFAULT NULL,
  `TEAMSCOUNT` int(11) DEFAULT '16',
  `STATUS` int(11) NOT NULL DEFAULT '0',
  `ISFINISHED` int(11) NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица ktvcss.cups_matches
CREATE TABLE IF NOT EXISTS `cups_matches` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `MatchPlayed` int(11) DEFAULT '0',
  `MatchWinnerName` text COLLATE utf8_unicode_ci,
  `MatchLooserName` text COLLATE utf8_unicode_ci,
  `MatchFormat` text COLLATE utf8_unicode_ci,
  `TeamAName` text COLLATE utf8_unicode_ci,
  `TeamAScore` int(11) NOT NULL DEFAULT '0',
  `TeamBName` text COLLATE utf8_unicode_ci,
  `TeamBScore` int(11) DEFAULT '0',
  `TournamentId` int(11) DEFAULT NULL,
  `ServerId` int(11) DEFAULT NULL,
  `DateTimeStart` datetime DEFAULT NULL,
  `DateTimeEnd` datetime DEFAULT NULL,
  `BracketItem` text COLLATE utf8_unicode_ci,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `MatchId` (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица ktvcss.cups_matchesplayed
CREATE TABLE IF NOT EXISTS `cups_matchesplayed` (
  `ID` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица ktvcss.cups_recovery
CREATE TABLE IF NOT EXISTS `cups_recovery` (
  `CupId` int(11) DEFAULT NULL,
  `CupName` text COLLATE utf8mb4_unicode_ci,
  `Teams` text COLLATE utf8mb4_unicode_ci,
  `RegBoardId` int(11) DEFAULT NULL,
  `password` int(11) DEFAULT NULL,
  `postId` tinytext COLLATE utf8mb4_unicode_ci
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица ktvcss.cups_teams
CREATE TABLE IF NOT EXISTS `cups_teams` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `CID` int(11) NOT NULL,
  `CNAME` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `TNAME` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `TSTUFF` text COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для процедура ktvcss.DelPlayers
DELIMITER //
CREATE PROCEDURE `DelPlayers`()
BEGIN

DELETE FROM ktvcss.players WHERE DAYOFYEAR(SYSDATE()) - DAYOFYEAR(LASTMATCH) >= 30 AND YEAR(SYSDATE()) = YEAR(LASTMATCH);

END//
DELIMITER ;

-- Дамп структуры для процедура ktvcss.findmix
DELIMITER //
CREATE PROCEDURE `findmix`(
	IN `steam` TEXT
)
BEGIN

SET @this = (SELECT rankpts FROM players WHERE steamid = steam);
SET @difp = @this - 1000;
SET @difm = @this + 1000;
SELECT * FROM players WHERE rankpts > @difp AND rankpts < @difm;

END//
DELIMITER ;

-- Дамп структуры для таблица ktvcss.matches
CREATE TABLE IF NOT EXISTS `matches` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `TeamA` text COLLATE utf8_unicode_ci NOT NULL,
  `TeamAScore` int(11) NOT NULL DEFAULT '0',
  `TeamB` text COLLATE utf8_unicode_ci NOT NULL,
  `TeamBScore` int(11) NOT NULL DEFAULT '0',
  `MatchDate` datetime NOT NULL,
  `Map` text COLLATE utf8_unicode_ci,
  `ServerId` int(11) DEFAULT NULL,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `ID` (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=19252 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица ktvcss.matchresults
CREATE TABLE IF NOT EXISTS `matchresults` (
  `MatchId` int(11) DEFAULT NULL,
  `TeamName` text COLLATE utf8_unicode_ci,
  `NickName` text COLLATE utf8_unicode_ci,
  `Kills` text COLLATE utf8_unicode_ci,
  `Deaths` text COLLATE utf8_unicode_ci,
  `KD` text COLLATE utf8_unicode_ci,
  `HK` text COLLATE utf8_unicode_ci
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица ktvcss.players
CREATE TABLE IF NOT EXISTS `players` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `Name` text NOT NULL,
  `SteamID` text NOT NULL,
  `Kills` int(11) NOT NULL DEFAULT '0',
  `Deaths` int(11) NOT NULL DEFAULT '0',
  `Headshots` int(11) NOT NULL DEFAULT '0',
  `KD` text NOT NULL,
  `HK` text NOT NULL,
  `RankPTS` int(11) NOT NULL DEFAULT '0',
  `RankName` text NOT NULL,
  `MatchesPlayed` int(11) NOT NULL DEFAULT '1',
  `MatchesVictories` int(11) NOT NULL DEFAULT '0',
  `MatchesDefeats` int(11) NOT NULL DEFAULT '0',
  `IsCalibration` int(11) NOT NULL DEFAULT '1',
  `LastMatch` datetime DEFAULT NULL,
  `VkID` text NOT NULL,
  `SendStatistics` int(11) NOT NULL DEFAULT '0',
  `WinRate` text,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `ID` (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=6379 DEFAULT CHARSET=utf8mb4;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица ktvcss.ranks
CREATE TABLE IF NOT EXISTS `ranks` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `Name` text COLLATE utf8_unicode_ci,
  `MinPTS` int(11) NOT NULL,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `ID` (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=42 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица ktvcss.servers
CREATE TABLE IF NOT EXISTS `servers` (
  `Id` int(11) DEFAULT NULL,
  `ServerName` text COLLATE utf8_unicode_ci,
  `Host` text COLLATE utf8_unicode_ci,
  `UserName` text COLLATE utf8_unicode_ci,
  `UserPassword` text COLLATE utf8_unicode_ci,
  `Port` text COLLATE utf8_unicode_ci,
  `LogsDir` text COLLATE utf8_unicode_ci,
  `GamePort` text COLLATE utf8_unicode_ci,
  `RconPassword` text COLLATE utf8_unicode_ci,
  `Enabled` tinyint(4) DEFAULT '0',
  `Busy` tinyint(4) DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица ktvcss.statsettings
CREATE TABLE IF NOT EXISTS `statsettings` (
  `MainGroupId` int(11) DEFAULT NULL,
  `StatGroupId` int(11) DEFAULT NULL,
  `AdminUserId` int(11) DEFAULT NULL,
  `BattleCupEnabled` tinyint(4) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Экспортируемые данные не выделены.

-- Дамп структуры для таблица ktvcss.teams
CREATE TABLE IF NOT EXISTS `teams` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `Name` text NOT NULL,
  `RankPTS` int(11) NOT NULL DEFAULT '0',
  `MatchesPlayed` int(11) NOT NULL DEFAULT '0',
  `MatchesVictories` int(11) NOT NULL DEFAULT '0',
  `MatchesDefeats` int(11) NOT NULL DEFAULT '0',
  `IsCalibration` int(11) NOT NULL DEFAULT '1',
  `LastMatch` datetime DEFAULT NULL,
  `Staff` text NOT NULL,
  `CapID` text,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `ID` (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=541 DEFAULT CHARSET=utf8mb4;

-- Экспортируемые данные не выделены.

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
