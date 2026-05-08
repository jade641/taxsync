SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";

-- Database: TaxsyncDB
CREATE DATABASE IF NOT EXISTS `TaxsyncDB` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `TaxsyncDB`;


CREATE TABLE `users` (
  `user_id` int(11) NOT NULL AUTO_INCREMENT,
  `username` varchar(256) NOT NULL,
  `email` varchar(256) NOT NULL,
  `normalized_username` varchar(256) NOT NULL,
  `normalized_email` varchar(256) NOT NULL,
  `password_hash` varchar(512) NOT NULL,
  `security_stamp` varchar(255) DEFAULT NULL,
  `concurrency_stamp` varchar(255) DEFAULT NULL,
  `first_name` varchar(50) NOT NULL,
  `last_name` varchar(50) NOT NULL,
  `phone` varchar(50) DEFAULT NULL,
  `phone_confirmed` tinyint(1) NOT NULL DEFAULT 0,
  `two_factor_enabled` tinyint(1) NOT NULL DEFAULT 0,
  `lockout_end` datetime(6) DEFAULT NULL,
  `lockout_enabled` tinyint(1) NOT NULL DEFAULT 0,
  `access_failed_count` int(11) NOT NULL DEFAULT 0,
  `role` enum('Admin','Accountant','Auditor','Staff','Taxpayer','TaxOfficer') NOT NULL DEFAULT 'Taxpayer',
  `status` enum('Active','Inactive','Suspended','Pending') NOT NULL DEFAULT 'Pending',
  `email_verified` tinyint(1) NOT NULL DEFAULT 0,
  `profile_image` varchar(255) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `last_login` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `ux_users_username` (`username`),
  UNIQUE KEY `ux_users_email` (`email`),
  UNIQUE KEY `ux_users_normalized_username` (`normalized_username`),
  KEY `ix_users_normalized_email` (`normalized_email`),
  KEY `idx_role` (`role`),
  KEY `idx_status` (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `user_claims` (
  `claim_id` int(11) NOT NULL AUTO_INCREMENT,
  `user_id` int(11) NOT NULL,
  `claim_type` varchar(256) DEFAULT NULL,
  `claim_value` text DEFAULT NULL,
  PRIMARY KEY (`claim_id`),
  KEY `idx_user_claims_user_id` (`user_id`),
  CONSTRAINT `fk_user_claims_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `user_logins` (
  `login_provider` varchar(128) NOT NULL,
  `provider_key` varchar(128) NOT NULL,
  `provider_display_name` varchar(256) DEFAULT NULL,
  `user_id` int(11) NOT NULL,
  PRIMARY KEY (`login_provider`,`provider_key`),
  KEY `idx_user_logins_user_id` (`user_id`),
  CONSTRAINT `fk_user_logins_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `user_tokens` (
  `user_id` int(11) NOT NULL,
  `login_provider` varchar(128) NOT NULL,
  `token_name` varchar(128) NOT NULL,
  `token_value` text DEFAULT NULL,
  PRIMARY KEY (`user_id`,`login_provider`,`token_name`),
  CONSTRAINT `fk_user_tokens_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ASP.NET Identity PBKDF2 hash for TaxSyncAdmin#2026
INSERT INTO `users` (`username`, `email`, `normalized_username`, `normalized_email`, `password_hash`, `security_stamp`, `concurrency_stamp`, `first_name`, `last_name`, `lockout_enabled`, `access_failed_count`, `role`, `status`, `email_verified`) VALUES
('admin', 'jcanlubopaye@gmail.com', 'ADMIN', 'JCANLUBOPAYE@GMAIL.COM', 'AQAAAAIAAYagAAAAENxsa99p3HzKJwGyFQ1kVLjajcG5uT1PYDUjv5EeY/bYWw/4L8av1Ap9B+/nuTGDvg==', 'd5ce197c-9d51-4b9d-9a0a-8ae3ee2c59ae', 'd9a2a9e8-a49b-4fdd-bf7b-75dce9bb5ed8', 'System', 'Administrator', 1, 0, 'Admin', 'Active', 1);

COMMIT;

-- ============================================
-- 3. GEOGRAPHIC HIERARCHY MODULE
-- ============================================

CREATE TABLE `regions` (
  `region_id` int(11) NOT NULL AUTO_INCREMENT,
  `region_code` varchar(20) NOT NULL,
  `region_name` varchar(100) NOT NULL,
  `description` text DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`region_id`),
  UNIQUE KEY `region_code` (`region_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `regions` (`region_code`, `region_name`, `description`) VALUES
('XI', 'Davao Region', 'Region XI - Davao Region');

CREATE TABLE `provinces` (
  `province_id` int(11) NOT NULL AUTO_INCREMENT,
  `region_id` int(11) NOT NULL,
  `province_code` varchar(20) NOT NULL,
  `province_name` varchar(100) NOT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`province_id`),
  UNIQUE KEY `province_code` (`province_code`),
  KEY `idx_region` (`region_id`),
  CONSTRAINT `fk_province_region` FOREIGN KEY (`region_id`) REFERENCES `regions` (`region_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `provinces` (`region_id`, `province_code`, `province_name`) VALUES
(1, 'DAV-SUR', 'Davao del Sur'),
(1, 'DAV-NOR', 'Davao del Norte'),
(1, 'DAV-ORO', 'Davao de Oro'),
(1, 'DAV-ORI', 'Davao Oriental'),
(1, 'DAV-OCC', 'Davao Occidental');

CREATE TABLE `cities` (
  `city_id` int(11) NOT NULL AUTO_INCREMENT,
  `province_id` int(11) NOT NULL,
  `city_code` varchar(20) NOT NULL,
  `city_name` varchar(100) NOT NULL,
  `city_type` enum('City','Municipality') NOT NULL DEFAULT 'Municipality',
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`city_id`),
  UNIQUE KEY `city_code` (`city_code`),
  KEY `idx_province` (`province_id`),
  CONSTRAINT `fk_city_province` FOREIGN KEY (`province_id`) REFERENCES `provinces` (`province_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `cities` (`province_id`, `city_code`, `city_name`, `city_type`) VALUES
-- Davao del Sur
(1, 'DIGOS', 'Digos City', 'City'),
(1, 'BANSALAN', 'Bansalan', 'Municipality'),
(1, 'HAGONOY', 'Hagonoy', 'Municipality'),
(1, 'KIBLAWAN', 'Kiblawan', 'Municipality'),
(1, 'MAGSAYSAY', 'Magsaysay', 'Municipality'),
(1, 'MALALAG', 'Malalag', 'Municipality'),
(1, 'MATANAO', 'Matanao', 'Municipality'),
(1, 'PADADA', 'Padada', 'Municipality'),
(1, 'SANTA-CRUZ', 'Santa Cruz', 'Municipality'),
(1, 'SULOP', 'Sulop', 'Municipality'),
-- Davao del Norte
(2, 'TAGUM', 'Tagum City', 'City'),
(2, 'PANABO', 'Panabo City', 'City'),
(2, 'SAMAL', 'Island Garden City of Samal', 'City'),
(2, 'ASUNCION', 'Asuncion', 'Municipality'),
(2, 'BRAULIO', 'Braulio E. Dujali', 'Municipality'),
(2, 'CARMEN', 'Carmen', 'Municipality'),
(2, 'KAPALONG', 'Kapalong', 'Municipality'),
(2, 'NEW-CORELLA', 'New Corella', 'Municipality'),
(2, 'SAN-ISIDRO', 'San Isidro', 'Municipality'),
(2, 'SANTO-TOMAS', 'Santo Tomas', 'Municipality'),
(2, 'TALAINGOD', 'Talaingod', 'Municipality'),
-- Davao de Oro
(3, 'NABUNTURAN', 'Nabunturan', 'Municipality'),
(3, 'COMPOSTELA', 'Compostela', 'Municipality'),
(3, 'LAAK', 'Laak', 'Municipality'),
(3, 'MABINI', 'Mabini', 'Municipality'),
(3, 'MACO', 'Maco', 'Municipality'),
(3, 'MARAGUSAN', 'Maragusan', 'Municipality'),
(3, 'MAWAB', 'Mawab', 'Municipality'),
(3, 'MONKAYO', 'Monkayo', 'Municipality'),
(3, 'MONTEVISTA', 'Montevista', 'Municipality'),
(3, 'NEW-BATAAN', 'New Bataan', 'Municipality'),
(3, 'PANTUKAN', 'Pantukan', 'Municipality'),
-- Davao Oriental
(4, 'MATI', 'Mati City', 'City'),
(4, 'BAGANGA', 'Baganga', 'Municipality'),
(4, 'BANAYBANAY', 'Banaybanay', 'Municipality'),
(4, 'BOSTON', 'Boston', 'Municipality'),
(4, 'CARAGA', 'Caraga', 'Municipality'),
(4, 'CATEEL', 'Cateel', 'Municipality'),
(4, 'GOVERNOR', 'Governor Generoso', 'Municipality'),
(4, 'LUPON', 'Lupon', 'Municipality'),
(4, 'MANAY', 'Manay', 'Municipality'),
(4, 'SAN-ISIDRO-ORI', 'San Isidro', 'Municipality'),
(4, 'TARRAGONA', 'Tarragona', 'Municipality'),
-- Davao Occidental
(5, 'MALITA', 'Malita', 'Municipality'),
(5, 'SANTA-MARIA', 'Santa Maria', 'Municipality'),
(5, 'DON-MARCELINO', 'Don Marcelino', 'Municipality'),
(5, 'JOSE-ABAD', 'Jose Abad Santos', 'Municipality'),
(5, 'SARANGANI', 'Sarangani', 'Municipality');

-- Davao City (Highly Urbanized City - separate from provinces)
INSERT INTO `cities` (`province_id`, `city_code`, `city_name`, `city_type`) VALUES
(2, 'DAVAO-CITY', 'Davao City', 'City');

CREATE TABLE `barangays` (
  `barangay_id` int(11) NOT NULL AUTO_INCREMENT,
  `city_id` int(11) NOT NULL,
  `barangay_code` varchar(20) NOT NULL,
  `barangay_name` varchar(100) NOT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`barangay_id`),
  UNIQUE KEY `barangay_code` (`barangay_code`),
  KEY `idx_city` (`city_id`),
  CONSTRAINT `fk_barangay_city` FOREIGN KEY (`city_id`) REFERENCES `cities` (`city_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Sample barangays for Davao City
INSERT INTO `barangays` (`city_id`, `barangay_code`, `barangay_name`) VALUES
(32, 'DC-POBLACION', 'Poblacion District'),
(32, 'DC-AGDAO', 'Agdao'),
(32, 'DC-BUHANGIN', 'Buhangin'),
(32, 'DC-BUNAWAN', 'Bunawan'),
(32, 'DC-CALINAN', 'Calinan'),
(32, 'DC-MARILOG', 'Marilog'),
(32, 'DC-PAQUIBATO', 'Paquibato'),
(32, 'DC-TALOMO', 'Talomo'),
(32, 'DC-TORIL', 'Toril'),
(32, 'DC-TUGBOK', 'Tugbok'),
(32, 'DC-BAGUIO', 'Baguio District');

-- Sample barangays for Tagum City
INSERT INTO `barangays` (`city_id`, `barangay_code`, `barangay_name`) VALUES
(11, 'TAG-POBLACION', 'Poblacion'),
(11, 'TAG-APOKON', 'Apokon'),
(11, 'TAG-BINCUNGAN', 'Bincungan'),
(11, 'TAG-CUAMBOGAN', 'Cuambogan'),
(11, 'TAG-LA-FILIPINA', 'La Filipina'),
(11, 'TAG-LIBOGANON', 'Liboganon'),
(11, 'TAG-MADAUM', 'Madaum'),
(11, 'TAG-MAGUGPO', 'Magugpo'),
(11, 'TAG-MANKILAM', 'Mankilam'),
(11, 'TAG-NEW-BALAMBAN', 'New Balamban'),
(11, 'TAG-PAGSABANGAN', 'Pagsabangan'),
(11, 'TAG-SAN-AGUSTIN', 'San Agustin'),
(11, 'TAG-SAN-MIGUEL', 'San Miguel'),
(11, 'TAG-VISAYAN-VILLAGE', 'Visayan Village');

-- ============================================
-- 3. PROPERTY REGISTRATION MODULE
-- ============================================

CREATE TABLE `properties` (
  `property_id` int(11) NOT NULL AUTO_INCREMENT,
  `owner_id` int(11) NOT NULL,
  `property_type` enum('Residential','Commercial','Industrial','Agricultural','MixedUse') NOT NULL,
  `property_number` varchar(50) DEFAULT NULL,
  `title_number` varchar(100) DEFAULT NULL,
  `address_line1` varchar(255) NOT NULL,
  `address_line2` varchar(255) DEFAULT NULL,
  `region_id` int(11) NOT NULL,
  `province_id` int(11) NOT NULL,
  `city_id` int(11) NOT NULL,
  `barangay_id` int(11) NOT NULL,
  `postal_code` varchar(10) DEFAULT NULL,
  `lot_area` decimal(12,2) DEFAULT NULL,
  `floor_area` decimal(12,2) DEFAULT NULL,
  `market_value` decimal(15,2) DEFAULT NULL,
  `assessed_value` decimal(15,2) DEFAULT NULL,
  `year_acquired` year(4) DEFAULT NULL,
  `registration_date` date DEFAULT NULL,
  `status` enum('Active','Inactive','Pending','Archived') DEFAULT 'Pending',
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`property_id`),
  UNIQUE KEY `property_number` (`property_number`),
  KEY `idx_owner` (`owner_id`),
  KEY `idx_property_number` (`property_number`),
  KEY `idx_status` (`status`),
  KEY `idx_region` (`region_id`),
  KEY `idx_province` (`province_id`),
  KEY `idx_city` (`city_id`),
  KEY `idx_barangay` (`barangay_id`),
  CONSTRAINT `fk_property_owner` FOREIGN KEY (`owner_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_property_region` FOREIGN KEY (`region_id`) REFERENCES `regions` (`region_id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_property_province` FOREIGN KEY (`province_id`) REFERENCES `provinces` (`province_id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_property_city` FOREIGN KEY (`city_id`) REFERENCES `cities` (`city_id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_property_barangay` FOREIGN KEY (`barangay_id`) REFERENCES `barangays` (`barangay_id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `property_documents` (
  `document_id` int(11) NOT NULL AUTO_INCREMENT,
  `property_id` int(11) NOT NULL,
  `document_type` enum('Title','TaxDeclaration','DeedOfSale','SurveyPlan','Other') NOT NULL,
  `document_name` varchar(255) NOT NULL,
  `file_path` varchar(255) NOT NULL,
  `file_size` int(11) DEFAULT NULL,
  `uploaded_by` int(11) DEFAULT NULL,
  `uploaded_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`document_id`),
  KEY `idx_property` (`property_id`),
  CONSTRAINT `fk_doc_property` FOREIGN KEY (`property_id`) REFERENCES `properties` (`property_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_doc_uploader` FOREIGN KEY (`uploaded_by`) REFERENCES `users` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================
-- 4. TAX CALCULATION MODULE
-- ============================================

CREATE TABLE `tax_rates` (
  `rate_id` int(11) NOT NULL AUTO_INCREMENT,
  `property_type` enum('Residential','Commercial','Industrial','Agricultural','MixedUse') NOT NULL,
  `rate_percentage` decimal(5,4) NOT NULL,
  `effective_from` date NOT NULL,
  `effective_to` date DEFAULT NULL,
  `description` text DEFAULT NULL,
  `created_by` int(11) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`rate_id`),
  KEY `idx_property_type` (`property_type`),
  KEY `idx_effective_dates` (`effective_from`,`effective_to`),
  CONSTRAINT `fk_rate_creator` FOREIGN KEY (`created_by`) REFERENCES `users` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `tax_rates` (`property_type`, `rate_percentage`, `effective_from`, `description`) VALUES
('Residential', 0.0200, '2024-01-01', 'Standard residential property tax rate'),
('Commercial', 0.0300, '2024-01-01', 'Standard commercial property tax rate'),
('Industrial', 0.0350, '2024-01-01', 'Standard industrial property tax rate'),
('Agricultural', 0.0150, '2024-01-01', 'Standard agricultural property tax rate'),
('MixedUse', 0.0250, '2024-01-01', 'Standard mixed-use property tax rate');

CREATE TABLE `tax_assessments` (
  `assessment_id` int(11) NOT NULL AUTO_INCREMENT,
  `property_id` int(11) NOT NULL,
  `tax_year` year(4) NOT NULL,
  `quarter` tinyint(1) DEFAULT NULL CHECK (`quarter` between 1 and 4),
  `assessed_value` decimal(15,2) NOT NULL,
  `tax_rate` decimal(5,4) NOT NULL,
  `basic_tax` decimal(15,2) NOT NULL,
  `sef_tax` decimal(15,2) DEFAULT 0.00,
  `penalties` decimal(15,2) DEFAULT 0.00,
  `discounts` decimal(15,2) DEFAULT 0.00,
  `total_amount` decimal(15,2) NOT NULL,
  `due_date` date NOT NULL,
  `status` enum('Pending','Approved','Paid','Overdue','Cancelled') DEFAULT 'Pending',
  `assessed_by` int(11) DEFAULT NULL,
  `approved_by` int(11) DEFAULT NULL,
  `notes` text DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`assessment_id`),
  UNIQUE KEY `unique_assessment` (`property_id`,`tax_year`,`quarter`),
  KEY `idx_property` (`property_id`),
  KEY `idx_tax_year` (`tax_year`),
  KEY `idx_status` (`status`),
  CONSTRAINT `fk_assessment_property` FOREIGN KEY (`property_id`) REFERENCES `properties` (`property_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_assessment_assessor` FOREIGN KEY (`assessed_by`) REFERENCES `users` (`user_id`) ON DELETE SET NULL,
  CONSTRAINT `fk_assessment_approver` FOREIGN KEY (`approved_by`) REFERENCES `users` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================
-- 5. PAYMENT MANAGEMENT MODULE
-- ============================================

CREATE TABLE `payments` (
  `payment_id` int(11) NOT NULL AUTO_INCREMENT,
  `assessment_id` int(11) NOT NULL,
  `payer_id` int(11) NOT NULL,
  `payment_reference` varchar(100) NOT NULL,
  `payment_method` enum('Cash','Check','BankTransfer','CreditCard','DebitCard','Gcash','Paymaya','Online') NOT NULL,
  `amount_paid` decimal(15,2) NOT NULL,
  `payment_date` timestamp NOT NULL DEFAULT current_timestamp(),
  `transaction_id` varchar(100) DEFAULT NULL,
  `bank_name` varchar(100) DEFAULT NULL,
  `check_number` varchar(50) DEFAULT NULL,
  `status` enum('Pending','Completed','Failed','Refunded','Cancelled') DEFAULT 'Pending',
  `receipt_number` varchar(100) DEFAULT NULL,
  `processed_by` int(11) DEFAULT NULL,
  `notes` text DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`payment_id`),
  UNIQUE KEY `payment_reference` (`payment_reference`),
  UNIQUE KEY `receipt_number` (`receipt_number`),
  KEY `idx_assessment` (`assessment_id`),
  KEY `idx_payer` (`payer_id`),
  KEY `idx_reference` (`payment_reference`),
  KEY `idx_status` (`status`),
  CONSTRAINT `fk_payment_assessment` FOREIGN KEY (`assessment_id`) REFERENCES `tax_assessments` (`assessment_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_payment_payer` FOREIGN KEY (`payer_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_payment_processor` FOREIGN KEY (`processed_by`) REFERENCES `users` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `activity_logs` (
  `log_id` int(11) NOT NULL AUTO_INCREMENT,
  `user_id` int(11) DEFAULT NULL,
  `action` varchar(100) NOT NULL,
  `module` varchar(50) NOT NULL,
  `severity` enum('Info','Warning','Critical') DEFAULT 'Info',
  `description` text DEFAULT NULL,
  `ip_address` varchar(45) DEFAULT NULL,
  `user_agent` text DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`log_id`),
  KEY `idx_user` (`user_id`),
  KEY `idx_module` (`module`),
  KEY `idx_severity` (`severity`),
  KEY `idx_created_at` (`created_at`),
  CONSTRAINT `fk_log_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================
-- VIEWS FOR REPORTING
-- ============================================

CREATE VIEW `view_property_tax_summary` AS
SELECT 
    p.property_id,
    p.property_number,
    p.property_type,
    CONCAT(u.first_name, ' ', u.last_name) AS owner_name,
    p.assessed_value,
    ta.tax_year,
    ta.quarter,
    ta.total_amount AS tax_amount,
    ta.status AS payment_status,
    COALESCE(SUM(pay.amount_paid), 0) AS amount_paid,
    (ta.total_amount - COALESCE(SUM(pay.amount_paid), 0)) AS balance
FROM properties p
JOIN users u ON p.owner_id = u.user_id
LEFT JOIN tax_assessments ta ON p.property_id = ta.property_id
LEFT JOIN payments pay ON ta.assessment_id = pay.assessment_id AND pay.status = 'Completed'
GROUP BY p.property_id, ta.assessment_id;

CREATE VIEW `view_collection_summary` AS
SELECT 
    DATE_FORMAT(payment_date, '%Y-%m') AS month,
    COUNT(*) AS total_transactions,
    SUM(amount_paid) AS total_collected,
    payment_method,
    status
FROM payments
GROUP BY DATE_FORMAT(payment_date, '%Y-%m'), payment_method, status;

COMMIT;

-- ============================================
-- END OF TAXSYNC DATABASE
-- Database: TaxsyncDB
-- Version: 1.0
-- Ready for phpMyAdmin Import
-- ============================================
