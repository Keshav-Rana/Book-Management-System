-- MySQL dump 10.13  Distrib 9.0.1, for macos14.7 (arm64)
--
-- Host: localhost    Database: BMS
-- ------------------------------------------------------
-- Server version	9.0.1

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Current Database: `BMS`
--

CREATE DATABASE /*!32312 IF NOT EXISTS*/ `BMS` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;

USE `BMS`;

--
-- Table structure for table `Book`
--

DROP TABLE IF EXISTS `Book`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Book` (
  `BookId` char(16) NOT NULL,
  `Title` varchar(30) NOT NULL,
  `Author` varchar(30) NOT NULL,
  `Publisher` varchar(40) NOT NULL,
  `ISBN` char(13) NOT NULL,
  `Genre` varchar(15) NOT NULL,
  `PublishedDate` date NOT NULL,
  `Edition` int NOT NULL,
  `Description` varchar(100) DEFAULT NULL,
  `Price` decimal(10,2) NOT NULL,
  `Rating` decimal(10,2) DEFAULT NULL,
  PRIMARY KEY (`BookId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `BorrowedBook`
--

DROP TABLE IF EXISTS `BorrowedBook`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `BorrowedBook` (
  `BorrowId` char(16) NOT NULL,
  `BookId` char(16) DEFAULT NULL,
  `UserId` char(16) DEFAULT NULL,
  `BorrowDate` date DEFAULT NULL,
  `ReturnDate` date DEFAULT NULL,
  `ActualReturnDate` date DEFAULT NULL,
  `Status` varchar(10) DEFAULT NULL,
  `FineAmount` decimal(10,2) DEFAULT NULL,
  PRIMARY KEY (`BorrowId`),
  UNIQUE KEY `UniqueBookUserCombo` (`BookId`,`UserId`),
  KEY `UserId` (`UserId`),
  CONSTRAINT `borrowedbook_ibfk_1` FOREIGN KEY (`BookId`) REFERENCES `Book` (`BookId`) ON DELETE CASCADE,
  CONSTRAINT `borrowedbook_ibfk_2` FOREIGN KEY (`UserId`) REFERENCES `User` (`UserId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Review`
--

DROP TABLE IF EXISTS `Review`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Review` (
  `ReviewId` char(16) NOT NULL,
  `BookId` char(16) DEFAULT NULL,
  `UserId` char(16) DEFAULT NULL,
  `Description` varchar(70) DEFAULT NULL,
  `Rating` int DEFAULT NULL,
  PRIMARY KEY (`ReviewId`),
  KEY `BookId` (`BookId`),
  KEY `UserId` (`UserId`),
  CONSTRAINT `review_ibfk_1` FOREIGN KEY (`BookId`) REFERENCES `Book` (`BookId`),
  CONSTRAINT `review_ibfk_2` FOREIGN KEY (`UserId`) REFERENCES `User` (`UserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `User`
--

DROP TABLE IF EXISTS `User`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `User` (
  `UserId` char(16) NOT NULL,
  `UserName` char(20) DEFAULT NULL,
  `FirstName` varchar(20) DEFAULT NULL,
  `LastName` varchar(20) DEFAULT NULL,
  `Email` varchar(50) DEFAULT NULL,
  `Password` varchar(255) DEFAULT NULL,
  `DateOfBirth` date DEFAULT NULL,
  `Role` varchar(10) DEFAULT NULL,
  `Status` varchar(15) DEFAULT NULL,
  `CreatedAt` timestamp NULL DEFAULT NULL,
  `LastModifiedAt` timestamp NULL DEFAULT NULL,
  `LastLogin` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`UserId`),
  UNIQUE KEY `UserName` (`UserName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-03-15 18:52:01
