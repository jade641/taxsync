SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";

-- Database: TaxsyncDB
CREATE DATABASE IF NOT EXISTS `TaxsyncDB` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `TaxsyncDB`;


CREATE TABLE `users` (
  `user_id` int(11) NOT NULL AUTO_INCREMENT,
  `username` varchar(50) NOT NULL,
  `email` varchar(100) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `first_name` varchar(50) NOT NULL,
  `last_name` varchar(50) NOT NULL,
  `phone` varchar(20) DEFAULT NULL,
  `role` enum('admin','accountant','auditor','staff','taxpayer','tax_officer') NOT NULL DEFAULT 'taxpayer',
  `status` enum('active','inactive','suspended','pending') NOT NULL DEFAULT 'pending',
  `email_verified` tinyint(1) DEFAULT 0,
  `profile_image` varchar(255) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `last_login` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `username` (`username`),
  UNIQUE KEY `email` (`email`),
  KEY `idx_email` (`email`),
  KEY `idx_role` (`role`),
  KEY `idx_status` (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `role`, `status`, `email_verified`) VALUES
('admin', 'admin@taxsync.com', '\\\.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'System', 'Administrator', 'admin', 'active', 1),
('accountant1', 'accountant@taxsync.com', '\\\.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Maria', 'Santos', 'accountant', 'active', 1),
('auditor1', 'auditor@taxsync.com', '\\\.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Juan', 'Dela Cruz', 'auditor', 'active', 1),
('staff1', 'staff@taxsync.com', '\\\.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Pedro', 'Reyes', 'staff', 'active', 1);

COMMIT;

-- ============================================
-- 2. USER SESSIONS & AUTHENTICATION
-- ============================================

CREATE TABLE `user_sessions` (
  `session_id` int(11) NOT NULL AUTO_INCREMENT,
  `user_id` int(11) NOT NULL,
  `token` varchar(255) NOT NULL,
  `ip_address` varchar(45) DEFAULT NULL,
  `user_agent` text DEFAULT NULL,
  `expires_at` timestamp NOT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`session_id`),
  UNIQUE KEY `token` (`token`),
  KEY `idx_token` (`token`),
  KEY `idx_user_id` (`user_id`),
  CONSTRAINT `fk_sessions_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `password_reset_tokens` (
  `token_id` int(11) NOT NULL AUTO_INCREMENT,
  `user_id` int(11) NOT NULL,
  `token` varchar(255) NOT NULL,
  `expires_at` timestamp NOT NULL,
  `used` tinyint(1) DEFAULT 0,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`token_id`),
  UNIQUE KEY `token` (`token`),
  KEY `idx_token` (`token`),
  CONSTRAINT `fk_reset_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================
-- 3. PROPERTY REGISTRATION MODULE
-- ============================================

CREATE TABLE `properties` (
  `property_id` int(11) NOT NULL AUTO_INCREMENT,
  `owner_id` int(11) NOT NULL,
  `property_type` enum('residential','commercial','industrial','agricultural','mixed_use') NOT NULL,
  `property_number` varchar(50) DEFAULT NULL,
  `title_number` varchar(100) DEFAULT NULL,
  `address_line1` varchar(255) NOT NULL,
  `address_line2` varchar(255) DEFAULT NULL,
  `barangay` varchar(100) NOT NULL,
  `city` varchar(100) NOT NULL,
  `province` varchar(100) NOT NULL,
  `postal_code` varchar(10) DEFAULT NULL,
  `lot_area` decimal(12,2) DEFAULT NULL,
  `floor_area` decimal(12,2) DEFAULT NULL,
  `market_value` decimal(15,2) DEFAULT NULL,
  `assessed_value` decimal(15,2) DEFAULT NULL,
  `year_acquired` year(4) DEFAULT NULL,
  `registration_date` date DEFAULT NULL,
  `status` enum('active','inactive','pending','archived') DEFAULT 'pending',
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`property_id`),
  UNIQUE KEY `property_number` (`property_number`),
  KEY `idx_owner` (`owner_id`),
  KEY `idx_property_number` (`property_number`),
  KEY `idx_status` (`status`),
  CONSTRAINT `fk_property_owner` FOREIGN KEY (`owner_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `property_documents` (
  `document_id` int(11) NOT NULL AUTO_INCREMENT,
  `property_id` int(11) NOT NULL,
  `document_type` enum('title','tax_declaration','deed_of_sale','survey_plan','other') NOT NULL,
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
  `property_type` enum('residential','commercial','industrial','agricultural','mixed_use') NOT NULL,
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
('residential', 0.0200, '2024-01-01', 'Standard residential property tax rate'),
('commercial', 0.0300, '2024-01-01', 'Standard commercial property tax rate'),
('industrial', 0.0350, '2024-01-01', 'Standard industrial property tax rate'),
('agricultural', 0.0150, '2024-01-01', 'Standard agricultural property tax rate'),
('mixed_use', 0.0250, '2024-01-01', 'Standard mixed-use property tax rate');

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
  `status` enum('pending','approved','paid','overdue','cancelled') DEFAULT 'pending',
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
  `payment_method` enum('cash','check','bank_transfer','credit_card','debit_card','gcash','paymaya','online') NOT NULL,
  `amount_paid` decimal(15,2) NOT NULL,
  `payment_date` timestamp NOT NULL DEFAULT current_timestamp(),
  `transaction_id` varchar(100) DEFAULT NULL,
  `bank_name` varchar(100) DEFAULT NULL,
  `check_number` varchar(50) DEFAULT NULL,
  `status` enum('pending','completed','failed','refunded','cancelled') DEFAULT 'pending',
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

CREATE TABLE `payment_receipts` (
  `receipt_id` int(11) NOT NULL AUTO_INCREMENT,
  `payment_id` int(11) NOT NULL,
  `receipt_file` varchar(255) DEFAULT NULL,
  `generated_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`receipt_id`),
  CONSTRAINT `fk_receipt_payment` FOREIGN KEY (`payment_id`) REFERENCES `payments` (`payment_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================
-- 6. FILING MODULE
-- ============================================

CREATE TABLE `tax_filings` (
  `filing_id` int(11) NOT NULL AUTO_INCREMENT,
  `property_id` int(11) NOT NULL,
  `taxpayer_id` int(11) NOT NULL,
  `filing_type` enum('annual','quarterly','amended','final') NOT NULL,
  `tax_year` year(4) NOT NULL,
  `quarter` tinyint(1) DEFAULT NULL CHECK (`quarter` between 1 and 4),
  `filing_date` date NOT NULL,
  `due_date` date NOT NULL,
  `status` enum('draft','submitted','under_review','approved','rejected','amended') DEFAULT 'draft',
  `total_income` decimal(15,2) DEFAULT NULL,
  `total_deductions` decimal(15,2) DEFAULT NULL,
  `taxable_amount` decimal(15,2) DEFAULT NULL,
  `tax_due` decimal(15,2) DEFAULT NULL,
  `submitted_by` int(11) DEFAULT NULL,
  `reviewed_by` int(11) DEFAULT NULL,
  `approved_by` int(11) DEFAULT NULL,
  `rejection_reason` text DEFAULT NULL,
  `notes` text DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`filing_id`),
  KEY `idx_property` (`property_id`),
  KEY `idx_taxpayer` (`taxpayer_id`),
  KEY `idx_status` (`status`),
  CONSTRAINT `fk_filing_property` FOREIGN KEY (`property_id`) REFERENCES `properties` (`property_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_filing_taxpayer` FOREIGN KEY (`taxpayer_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_filing_submitter` FOREIGN KEY (`submitted_by`) REFERENCES `users` (`user_id`) ON DELETE SET NULL,
  CONSTRAINT `fk_filing_reviewer` FOREIGN KEY (`reviewed_by`) REFERENCES `users` (`user_id`) ON DELETE SET NULL,
  CONSTRAINT `fk_filing_approver` FOREIGN KEY (`approved_by`) REFERENCES `users` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `filing_attachments` (
  `attachment_id` int(11) NOT NULL AUTO_INCREMENT,
  `filing_id` int(11) NOT NULL,
  `file_name` varchar(255) NOT NULL,
  `file_path` varchar(255) NOT NULL,
  `file_type` varchar(50) DEFAULT NULL,
  `file_size` int(11) DEFAULT NULL,
  `uploaded_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`attachment_id`),
  CONSTRAINT `fk_attachment_filing` FOREIGN KEY (`filing_id`) REFERENCES `tax_filings` (`filing_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================
-- 7. COMPLIANCE MODULE
-- ============================================

CREATE TABLE `compliance_requirements` (
  `requirement_id` int(11) NOT NULL AUTO_INCREMENT,
  `requirement_name` varchar(255) NOT NULL,
  `description` text DEFAULT NULL,
  `frequency` enum('annual','quarterly','monthly','one_time') NOT NULL,
  `applicable_to` enum('all','residential','commercial','industrial','agricultural') DEFAULT 'all',
  `is_mandatory` tinyint(1) DEFAULT 1,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`requirement_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `compliance_requirements` (`requirement_name`, `description`, `frequency`, `applicable_to`, `is_mandatory`) VALUES
('Annual Tax Declaration', 'Submit annual tax declaration for property', 'annual', 'all', 1),
('Quarterly Payment', 'Pay quarterly property tax', 'quarterly', 'all', 1),
('Property Valuation Update', 'Update property market valuation', 'annual', 'all', 0),
('Business Permit Renewal', 'Renew business permit for commercial properties', 'annual', 'commercial', 1);

CREATE TABLE `compliance_records` (
  `record_id` int(11) NOT NULL AUTO_INCREMENT,
  `property_id` int(11) NOT NULL,
  `requirement_id` int(11) NOT NULL,
  `due_date` date NOT NULL,
  `completion_date` date DEFAULT NULL,
  `status` enum('pending','in_progress','completed','overdue','waived') DEFAULT 'pending',
  `compliance_score` decimal(5,2) DEFAULT NULL,
  `notes` text DEFAULT NULL,
  `verified_by` int(11) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`record_id`),
  KEY `idx_property` (`property_id`),
  KEY `idx_status` (`status`),
  CONSTRAINT `fk_compliance_property` FOREIGN KEY (`property_id`) REFERENCES `properties` (`property_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_compliance_requirement` FOREIGN KEY (`requirement_id`) REFERENCES `compliance_requirements` (`requirement_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_compliance_verifier` FOREIGN KEY (`verified_by`) REFERENCES `users` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================
-- 8. AUDIT MODULE
-- ============================================

CREATE TABLE `audit_cases` (
  `audit_id` int(11) NOT NULL AUTO_INCREMENT,
  `case_number` varchar(50) NOT NULL,
  `property_id` int(11) NOT NULL,
  `taxpayer_id` int(11) NOT NULL,
  `audit_type` enum('routine','random','targeted','complaint_based') NOT NULL,
  `audit_period_from` date NOT NULL,
  `audit_period_to` date NOT NULL,
  `status` enum('scheduled','in_progress','completed','suspended','closed') DEFAULT 'scheduled',
  `priority` enum('low','medium','high','critical') DEFAULT 'medium',
  `assigned_to` int(11) DEFAULT NULL,
  `start_date` date DEFAULT NULL,
  `end_date` date DEFAULT NULL,
  `findings` text DEFAULT NULL,
  `recommendations` text DEFAULT NULL,
  `amount_disputed` decimal(15,2) DEFAULT NULL,
  `amount_recovered` decimal(15,2) DEFAULT NULL,
  `created_by` int(11) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`audit_id`),
  UNIQUE KEY `case_number` (`case_number`),
  KEY `idx_case_number` (`case_number`),
  KEY `idx_status` (`status`),
  CONSTRAINT `fk_audit_property` FOREIGN KEY (`property_id`) REFERENCES `properties` (`property_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_audit_taxpayer` FOREIGN KEY (`taxpayer_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_audit_assignee` FOREIGN KEY (`assigned_to`) REFERENCES `users` (`user_id`) ON DELETE SET NULL,
  CONSTRAINT `fk_audit_creator` FOREIGN KEY (`created_by`) REFERENCES `users` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `audit_findings` (
  `finding_id` int(11) NOT NULL AUTO_INCREMENT,
  `audit_id` int(11) NOT NULL,
  `finding_type` enum('discrepancy','non_compliance','fraud','error','other') NOT NULL,
  `severity` enum('minor','moderate','major','critical') NOT NULL,
  `description` text NOT NULL,
  `amount_involved` decimal(15,2) DEFAULT NULL,
  `recommendation` text DEFAULT NULL,
  `status` enum('open','resolved','disputed','closed') DEFAULT 'open',
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`finding_id`),
  CONSTRAINT `fk_finding_audit` FOREIGN KEY (`audit_id`) REFERENCES `audit_cases` (`audit_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================
-- 9. REPORTING MODULE
-- ============================================

CREATE TABLE `report_templates` (
  `template_id` int(11) NOT NULL AUTO_INCREMENT,
  `template_name` varchar(255) NOT NULL,
  `report_type` enum('financial','compliance','audit','collection','property','custom') NOT NULL,
  `description` text DEFAULT NULL,
  `parameters` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL CHECK (json_valid(`parameters`)),
  `created_by` int(11) DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT 1,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`template_id`),
  CONSTRAINT `fk_template_creator` FOREIGN KEY (`created_by`) REFERENCES `users` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `generated_reports` (
  `report_id` int(11) NOT NULL AUTO_INCREMENT,
  `template_id` int(11) DEFAULT NULL,
  `report_name` varchar(255) NOT NULL,
  `report_type` enum('financial','compliance','audit','collection','property','custom') NOT NULL,
  `period_from` date DEFAULT NULL,
  `period_to` date DEFAULT NULL,
  `file_path` varchar(255) DEFAULT NULL,
  `file_format` enum('pdf','excel','csv','html') DEFAULT 'pdf',
  `generated_by` int(11) DEFAULT NULL,
  `generated_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`report_id`),
  CONSTRAINT `fk_report_template` FOREIGN KEY (`template_id`) REFERENCES `report_templates` (`template_id`) ON DELETE SET NULL,
  CONSTRAINT `fk_report_generator` FOREIGN KEY (`generated_by`) REFERENCES `users` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================
-- 10. NOTIFICATIONS MODULE
-- ============================================

CREATE TABLE `notifications` (
  `notification_id` int(11) NOT NULL AUTO_INCREMENT,
  `user_id` int(11) NOT NULL,
  `type` enum('payment_due','payment_received','assessment_ready','filing_due','audit_scheduled','compliance_alert','system','other') NOT NULL,
  `title` varchar(255) NOT NULL,
  `message` text NOT NULL,
  `link` varchar(255) DEFAULT NULL,
  `is_read` tinyint(1) DEFAULT 0,
  `priority` enum('low','normal','high','urgent') DEFAULT 'normal',
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `read_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`notification_id`),
  KEY `idx_user` (`user_id`),
  KEY `idx_is_read` (`is_read`),
  CONSTRAINT `fk_notification_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================
-- 11. SYSTEM ADMINISTRATION MODULE
-- ============================================

CREATE TABLE `system_settings` (
  `setting_id` int(11) NOT NULL AUTO_INCREMENT,
  `setting_key` varchar(100) NOT NULL,
  `setting_value` text DEFAULT NULL,
  `setting_type` enum('string','number','boolean','json') DEFAULT 'string',
  `description` text DEFAULT NULL,
  `updated_by` int(11) DEFAULT NULL,
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`setting_id`),
  UNIQUE KEY `setting_key` (`setting_key`),
  CONSTRAINT `fk_setting_updater` FOREIGN KEY (`updated_by`) REFERENCES `users` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `system_settings` (`setting_key`, `setting_value`, `setting_type`, `description`) VALUES
('system_name', 'TaxSync', 'string', 'System name'),
('tax_year', '2024', 'number', 'Current tax year'),
('early_payment_discount', '10', 'number', 'Early payment discount percentage'),
('late_payment_penalty', '2', 'number', 'Late payment penalty percentage per month'),
('payment_deadline_q1', '03-31', 'string', 'Q1 payment deadline (MM-DD)'),
('payment_deadline_q2', '06-30', 'string', 'Q2 payment deadline (MM-DD)'),
('payment_deadline_q3', '09-30', 'string', 'Q3 payment deadline (MM-DD)'),
('payment_deadline_q4', '12-31', 'string', 'Q4 payment deadline (MM-DD)');

CREATE TABLE `activity_logs` (
  `log_id` int(11) NOT NULL AUTO_INCREMENT,
  `user_id` int(11) DEFAULT NULL,
  `action` varchar(100) NOT NULL,
  `module` varchar(50) NOT NULL,
  `severity` enum('info','warning','critical') DEFAULT 'info',
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

CREATE TABLE `announcements` (
  `announcement_id` int(11) NOT NULL AUTO_INCREMENT,
  `title` varchar(255) NOT NULL,
  `content` text NOT NULL,
  `type` enum('info','warning','alert','maintenance') DEFAULT 'info',
  `target_audience` enum('all','taxpayers','officers','admins') DEFAULT 'all',
  `is_active` tinyint(1) DEFAULT 1,
  `start_date` timestamp NOT NULL DEFAULT current_timestamp(),
  `end_date` timestamp NULL DEFAULT NULL,
  `created_by` int(11) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`announcement_id`),
  CONSTRAINT `fk_announcement_creator` FOREIGN KEY (`created_by`) REFERENCES `users` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================
-- 12. ANALYTICS & DASHBOARD MODULE
-- ============================================

CREATE TABLE `dashboard_metrics` (
  `metric_id` int(11) NOT NULL AUTO_INCREMENT,
  `metric_name` varchar(100) NOT NULL,
  `metric_value` decimal(20,2) DEFAULT NULL,
  `metric_data` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL CHECK (json_valid(`metric_data`)),
  `period` varchar(50) DEFAULT NULL,
  `calculated_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`metric_id`),
  KEY `idx_metric_name` (`metric_name`)
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
LEFT JOIN payments pay ON ta.assessment_id = pay.assessment_id AND pay.status = 'completed'
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

CREATE VIEW `view_compliance_dashboard` AS
SELECT 
    p.property_id,
    p.property_number,
    COUNT(cr.record_id) AS total_requirements,
    SUM(CASE WHEN cr.status = 'completed' THEN 1 ELSE 0 END) AS completed,
    SUM(CASE WHEN cr.status = 'overdue' THEN 1 ELSE 0 END) AS overdue,
    AVG(cr.compliance_score) AS avg_compliance_score
FROM properties p
LEFT JOIN compliance_records cr ON p.property_id = cr.property_id
GROUP BY p.property_id;

COMMIT;

-- ============================================
-- END OF TAXSYNC DATABASE
-- Database: TaxsyncDB
-- Version: 1.0
-- Ready for phpMyAdmin Import
-- ============================================
