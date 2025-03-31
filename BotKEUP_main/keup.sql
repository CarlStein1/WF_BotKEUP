-- phpMyAdmin SQL Dump
-- version 5.1.1
-- https://www.phpmyadmin.net/
--
-- Хост: 127.0.0.1:3306
-- Время создания: Мар 14 2025 г., 12:00
-- Версия сервера: 5.6.51
-- Версия PHP: 7.1.33

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- База данных: `keup`
--

-- --------------------------------------------------------

--
-- Структура таблицы `applications`
--

CREATE TABLE `applications` (
  `chat_id` bigint(10) NOT NULL,
  `Surname` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Patronymic` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ApplicationDate` datetime DEFAULT CURRENT_TIMESTAMP,
  `Position` enum('Преподаватель','Староста') COLLATE utf8mb4_unicode_ci NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Дамп данных таблицы `applications`
--

INSERT INTO `applications` (`chat_id`, `Surname`, `Name`, `Patronymic`, `ApplicationDate`, `Position`) VALUES
(1001754356, 'Иванов', 'Алексей', 'Петрович', '2025-02-14 10:00:00', 'Староста'),
(1003218643, 'Кузнецов', 'Дмитрий', 'Олегович', '2025-02-14 11:00:00', 'Преподаватель'),
(1928456729, 'Валибеков', 'Руслан', 'Казбекович', '2025-03-13 18:21:23', 'Староста');

-- --------------------------------------------------------

--
-- Структура таблицы `groups`
--

CREATE TABLE `groups` (
  `GroupName` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Specialization` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Дамп данных таблицы `groups`
--

INSERT INTO `groups` (`GroupName`, `Specialization`) VALUES
('РВМП-41', 'РВМП'),
('РВМП-42', 'РВМП'),
('СИС-31', 'СИС'),
('СИС-32', 'СИС'),
('ЮР-21', 'ЮР'),
('ЮР-22', 'ЮР');

-- --------------------------------------------------------

--
-- Структура таблицы `specializations`
--

CREATE TABLE `specializations` (
  `SpecializationName` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `MaxCourse` tinyint(4) NOT NULL,
  `Code` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Дамп данных таблицы `specializations`
--

INSERT INTO `specializations` (`SpecializationName`, `MaxCourse`, `Code`) VALUES
('РВМП', 4, '10.03.01'),
('СИС', 4, '09.02.07'),
('ЮР', 3, '40.02.01');

-- --------------------------------------------------------

--
-- Структура таблицы `students`
--

CREATE TABLE `students` (
  `chat_id` bigint(10) NOT NULL,
  `Surname` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Patronymic` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `GroupName` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `IsLeader` tinyint(1) DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Дамп данных таблицы `students`
--

INSERT INTO `students` (`chat_id`, `Surname`, `Name`, `Patronymic`, `GroupName`, `IsLeader`) VALUES
(1001754356, 'Иванов', 'Алексей', 'Петрович', 'СИС-31', 0),
(1002960534, 'Смирнова', 'Екатерина', 'Андреевна', 'СИС-32', 1),
(1003745935, 'Кузнецов', 'Дмитрий', 'Олегович', 'РВМП-41', 0),
(1004804563, 'Васильева', 'Мария', 'Сергеевна', 'ЮР-22', 1);

-- --------------------------------------------------------

--
-- Структура таблицы `teachers`
--

CREATE TABLE `teachers` (
  `chat_id` bigint(10) NOT NULL,
  `Surname` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Patronymic` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Дамп данных таблицы `teachers`
--

INSERT INTO `teachers` (`chat_id`, `Surname`, `Name`, `Patronymic`) VALUES
(7412345834, 'Шинакова', 'Светлана', 'Викторовна');

--
-- Индексы сохранённых таблиц
--

--
-- Индексы таблицы `applications`
--
ALTER TABLE `applications`
  ADD PRIMARY KEY (`chat_id`);

--
-- Индексы таблицы `groups`
--
ALTER TABLE `groups`
  ADD PRIMARY KEY (`GroupName`),
  ADD KEY `idx_Specialization` (`Specialization`);

--
-- Индексы таблицы `specializations`
--
ALTER TABLE `specializations`
  ADD PRIMARY KEY (`SpecializationName`),
  ADD UNIQUE KEY `Code` (`Code`);

--
-- Индексы таблицы `students`
--
ALTER TABLE `students`
  ADD PRIMARY KEY (`chat_id`),
  ADD KEY `idx_GroupName` (`GroupName`);

--
-- Индексы таблицы `teachers`
--
ALTER TABLE `teachers`
  ADD PRIMARY KEY (`chat_id`);

--
-- Ограничения внешнего ключа сохраненных таблиц
--

--
-- Ограничения внешнего ключа таблицы `groups`
--
ALTER TABLE `groups`
  ADD CONSTRAINT `groups_ibfk_1` FOREIGN KEY (`Specialization`) REFERENCES `specializations` (`SpecializationName`) ON DELETE CASCADE;

--
-- Ограничения внешнего ключа таблицы `students`
--
ALTER TABLE `students`
  ADD CONSTRAINT `students_ibfk_1` FOREIGN KEY (`GroupName`) REFERENCES `groups` (`GroupName`) ON DELETE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
