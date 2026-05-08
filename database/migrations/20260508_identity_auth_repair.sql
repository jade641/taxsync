SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";

USE `TaxsyncDB`;

UPDATE `users`
SET
  `normalized_username` = UPPER(TRIM(`username`)),
  `normalized_email` = UPPER(TRIM(`email`))
WHERE `normalized_username` IS NULL
   OR `normalized_username` = ''
   OR `normalized_email` IS NULL
   OR `normalized_email` = '';

ALTER TABLE `users`
  MODIFY `username` varchar(256) NOT NULL,
  MODIFY `email` varchar(256) NOT NULL,
  MODIFY `normalized_username` varchar(256) NOT NULL,
  MODIFY `normalized_email` varchar(256) NOT NULL,
  MODIFY `password_hash` varchar(512) NOT NULL,
  MODIFY `phone` varchar(50) DEFAULT NULL,
  MODIFY `phone_confirmed` tinyint(1) NOT NULL DEFAULT 0,
  MODIFY `two_factor_enabled` tinyint(1) NOT NULL DEFAULT 0,
  MODIFY `lockout_enabled` tinyint(1) NOT NULL DEFAULT 0,
  MODIFY `role` enum('Admin','Accountant','Auditor','Staff','Taxpayer','TaxOfficer') NOT NULL DEFAULT 'Taxpayer',
  MODIFY `status` enum('Active','Inactive','Suspended','Pending') NOT NULL DEFAULT 'Pending',
  MODIFY `email_verified` tinyint(1) NOT NULL DEFAULT 0;

DELIMITER $$

DROP PROCEDURE IF EXISTS `repair_identity_indexes` $$
CREATE PROCEDURE `repair_identity_indexes`()
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM information_schema.statistics
    WHERE table_schema = DATABASE()
      AND table_name = 'users'
      AND index_name = 'ux_users_normalized_username'
  ) THEN
    ALTER TABLE `users` ADD UNIQUE KEY `ux_users_normalized_username` (`normalized_username`);
  END IF;

  IF NOT EXISTS (
    SELECT 1
    FROM information_schema.statistics
    WHERE table_schema = DATABASE()
      AND table_name = 'users'
      AND index_name = 'ix_users_normalized_email'
  ) THEN
    ALTER TABLE `users` ADD KEY `ix_users_normalized_email` (`normalized_email`);
  END IF;
END $$

CALL `repair_identity_indexes`() $$
DROP PROCEDURE `repair_identity_indexes` $$

DELIMITER ;

CREATE TABLE IF NOT EXISTS `user_claims` (
  `claim_id` int(11) NOT NULL AUTO_INCREMENT,
  `user_id` int(11) NOT NULL,
  `claim_type` varchar(256) DEFAULT NULL,
  `claim_value` text DEFAULT NULL,
  PRIMARY KEY (`claim_id`),
  KEY `idx_user_claims_user_id` (`user_id`),
  CONSTRAINT `fk_user_claims_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `user_logins` (
  `login_provider` varchar(128) NOT NULL,
  `provider_key` varchar(128) NOT NULL,
  `provider_display_name` varchar(256) DEFAULT NULL,
  `user_id` int(11) NOT NULL,
  PRIMARY KEY (`login_provider`,`provider_key`),
  KEY `idx_user_logins_user_id` (`user_id`),
  CONSTRAINT `fk_user_logins_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `user_tokens` (
  `user_id` int(11) NOT NULL,
  `login_provider` varchar(128) NOT NULL,
  `token_name` varchar(128) NOT NULL,
  `token_value` text DEFAULT NULL,
  PRIMARY KEY (`user_id`,`login_provider`,`token_name`),
  CONSTRAINT `fk_user_tokens_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SET @admin_email = 'jcanlubopaye@gmail.com';
SET @admin_username = 'admin';
SET @admin_normalized_email = UPPER(@admin_email);
SET @admin_normalized_username = UPPER(@admin_username);
SET @admin_hash = 'AQAAAAIAAYagAAAAENxsa99p3HzKJwGyFQ1kVLjajcG5uT1PYDUjv5EeY/bYWw/4L8av1Ap9B+/nuTGDvg==';
SET @admin_security_stamp = 'd5ce197c-9d51-4b9d-9a0a-8ae3ee2c59ae';
SET @admin_concurrency_stamp = 'd9a2a9e8-a49b-4fdd-bf7b-75dce9bb5ed8';

INSERT INTO `users` (
  `username`,
  `email`,
  `normalized_username`,
  `normalized_email`,
  `password_hash`,
  `security_stamp`,
  `concurrency_stamp`,
  `first_name`,
  `last_name`,
  `lockout_enabled`,
  `access_failed_count`,
  `role`,
  `status`,
  `email_verified`
) VALUES (
  @admin_username,
  @admin_email,
  @admin_normalized_username,
  @admin_normalized_email,
  @admin_hash,
  @admin_security_stamp,
  @admin_concurrency_stamp,
  'System',
  'Administrator',
  1,
  0,
  'Admin',
  'Active',
  1
)
ON DUPLICATE KEY UPDATE
  `username` = VALUES(`username`),
  `email` = VALUES(`email`),
  `normalized_username` = VALUES(`normalized_username`),
  `normalized_email` = VALUES(`normalized_email`),
  `password_hash` = VALUES(`password_hash`),
  `security_stamp` = VALUES(`security_stamp`),
  `concurrency_stamp` = VALUES(`concurrency_stamp`),
  `first_name` = VALUES(`first_name`),
  `last_name` = VALUES(`last_name`),
  `lockout_enabled` = VALUES(`lockout_enabled`),
  `access_failed_count` = VALUES(`access_failed_count`),
  `role` = VALUES(`role`),
  `status` = VALUES(`status`),
  `email_verified` = VALUES(`email_verified`),
  `updated_at` = CURRENT_TIMESTAMP,
  `last_login` = NULL;

COMMIT;