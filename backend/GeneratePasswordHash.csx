#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.AspNetCore.Identity.EntityFrameworkCore, 9.0.0"

using Microsoft.AspNetCore.Identity;

// Generate an ASP.NET Identity (PBKDF2) hash for the admin password
var password = "TaxSyncAdmin#2026";
var hasher = new PasswordHasher<IdentityUser>();
var user = new IdentityUser();

var hash = hasher.HashPassword(user, password);

Console.WriteLine($"Password: {password}");
Console.WriteLine($"Hash: {hash}");
Console.WriteLine();

// Verify it works
var isValid = hasher.VerifyHashedPassword(user, hash, password);
Console.WriteLine($"Verification: {isValid}");
Console.WriteLine();

// Optional: verify an existing Identity hash from the database
var existingHash = "REPLACE_WITH_IDENTITY_HASH";
if (!string.IsNullOrWhiteSpace(existingHash) && existingHash != "REPLACE_WITH_IDENTITY_HASH")
{
    var isExistingValid = hasher.VerifyHashedPassword(user, existingHash, password);
    Console.WriteLine($"Existing hash verification: {isExistingValid}");
}
