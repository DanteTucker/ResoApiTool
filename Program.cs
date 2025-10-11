using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ResoAPITool
{
    public class Program
    {

        static async Task Main(string[] args)
        {
            try
            {
                var auth = await ResoniteAuthService.CreateTokenAsync();
                
                await ShowMainMenuAsync(auth);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static async Task ShowMainMenuAsync(AuthResponse auth)
        {
            while (true)
            {
                Console.WriteLine("\n=== Resonite API Tool ===");
                Console.WriteLine("1. Manage Screens Records (RadiantDash)");
                Console.WriteLine("2. Review Message Items");
                Console.WriteLine("3. Search Records");
                Console.WriteLine("4. Display Auth Token");
                Console.WriteLine("5. Edit Profile");
                Console.WriteLine("6. Exit");
                Console.Write("\nSelect an option (1-6): ");

                var input = Console.ReadLine()?.Trim();
                
                switch (input)
                {
                    case "1":
                        await ManageScreensRecordsAsync(auth);
                        Console.WriteLine("\nPress any key to return to main menu...");
                        Console.ReadKey();
                        break;
                    case "2":
                        await ReviewMessageItemsAsync(auth);
                        Console.WriteLine("\nPress any key to return to main menu...");
                        Console.ReadKey();
                        break;
                    case "3":
                        await SearchRecordsAsync(auth);
                        Console.WriteLine("\nPress any key to return to main menu...");
                        Console.ReadKey();
                        break;
                    case "4":
                        DisplayAuthToken(auth);
                        Console.WriteLine("\nPress any key to return to main menu...");
                        Console.ReadKey();
                        break;
                    case "5":
                        await EditProfileAsync(auth);
                        Console.WriteLine("\nPress any key to return to main menu...");
                        Console.ReadKey();
                        break;
                    case "6":
                        Console.WriteLine("Goodbye!");
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please select 1-6.");
                        break;
                }
            }
        }

        static void DisplayAuthToken(AuthResponse auth)
        {
            Console.WriteLine("\n=== Authentication Token ===");
            Console.WriteLine($"User ID: {auth.UserId}");
            Console.WriteLine($"Token: {auth.Token}");
            Console.WriteLine($"\nAuthorization Header Format:");
            Console.WriteLine($"res {auth.UserId}:{auth.Token}");
            Console.WriteLine("\nYou can use this token with other API tools or for debugging purposes.");
        }

        static async Task EditProfileAsync(AuthResponse auth)
        {
            Console.WriteLine("\n=== Edit Profile ===");
            
            try
            {
                // Get current profile
                var currentProfile = await ResoniteRecordService.GetUserProfileAsync(auth);
                
                if (currentProfile == null)
                {
                    Console.WriteLine("Failed to retrieve current profile.");
                    return;
                }

                // Display current profile information
                Console.WriteLine("\nCurrent Profile:");
                Console.WriteLine($"Tagline: {currentProfile.Tagline ?? "(not set)"}");
                Console.WriteLine($"Description: {currentProfile.Description ?? "(not set)"}");
                
                // Get new values from user
                Console.WriteLine("\nEnter new values (press Enter to keep current value):");
                
                Console.Write($"New tagline [{currentProfile.Tagline ?? ""}]: ");
                var newTagline = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(newTagline))
                {
                    currentProfile.Tagline = newTagline;
                }
                
                Console.Write($"New description [{currentProfile.Description ?? ""}]: ");
                var newDescription = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(newDescription))
                {
                    currentProfile.Description = newDescription;
                }
                
                // Confirm changes
                Console.WriteLine("\nProfile changes:");
                Console.WriteLine($"Tagline: {currentProfile.Tagline ?? "(not set)"}");
                Console.WriteLine($"Description: {currentProfile.Description ?? "(not set)"}");
                
                Console.Write("\nSave these changes? (y/n): ");
                var confirm = Console.ReadLine()?.Trim().ToLower();
                
                if (confirm?.StartsWith("y") == true)
                {
                    await ResoniteRecordService.UpdateUserProfileAsync(auth, currentProfile);
                    Console.WriteLine("✓ Profile updated successfully!");
                }
                else
                {
                    Console.WriteLine("Profile update cancelled.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating profile: {ex.Message}");
            }
        }

        static async Task SearchRecordsAsync(AuthResponse auth)
        {
            Console.WriteLine("\n=== Search Records ===");
            Console.WriteLine("Search for records by name, tag, or both criteria.");
            Console.WriteLine("Leave fields empty to skip that criteria.\n");

            Console.Write("Enter record name (or press Enter to skip): ");
            var nameCriteria = Console.ReadLine()?.Trim();

            Console.Write("Enter tag name (or press Enter to skip): ");
            var tagCriteria = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(nameCriteria) && string.IsNullOrEmpty(tagCriteria))
            {
                Console.WriteLine("Please provide at least one search criteria (name or tag).");
                return;
            }

            Console.WriteLine("\nSearching for records...");

            List<Record> searchResults;

            try
            {

                if (!string.IsNullOrEmpty(nameCriteria) && !string.IsNullOrEmpty(tagCriteria))
                {
                    var nameResults = await ResoniteRecordService.GetRecordsByNameAsync(auth, nameCriteria);
                    var tagResults = await ResoniteRecordService.GetRecordsByTagAsync(auth, tagCriteria);

                    searchResults = nameResults.Where(r => tagResults.Any(tr => tr.Id == r.Id)).ToList();
                    
                    Console.WriteLine($"Found {searchResults.Count} records matching both name '{nameCriteria}' and tag '{tagCriteria}'");
                }
                else if (!string.IsNullOrEmpty(nameCriteria))
                {
                    searchResults = await ResoniteRecordService.GetRecordsByNameAsync(auth, nameCriteria);
                    Console.WriteLine($"Found {searchResults.Count} records with name '{nameCriteria}'");
                }
                else
                {
                    searchResults = await ResoniteRecordService.GetRecordsByTagAsync(auth, tagCriteria!);
                    Console.WriteLine($"Found {searchResults.Count} records with tag '{tagCriteria}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during search: {ex.Message}");
                return;
            }

            if (searchResults.Count == 0)
            {
                Console.WriteLine("No records found matching your search criteria.");
                return;
            }

            searchResults = searchResults.OrderByDescending(r => r.LastModificationTime).ToList();

            Console.WriteLine($"\nSearch Results Summary:");
            ResoniteRecordService.DisplayRecordSummary(searchResults);

            Console.Write("\nWould you like to review these records? (y/n): ");
            var reviewChoice = Console.ReadLine()?.Trim().ToLower();

            if (reviewChoice?.StartsWith("y") == true)
            {
                await InteractiveRecordReviewAsync(auth, searchResults);
            }
            else
            {
                Console.WriteLine("Search completed. Returning to main menu.");
            }
        }

        static async Task InteractiveRecordReviewAsync(AuthResponse auth, List<Record> records)
        {
            Console.WriteLine($"\n=== Interactive Review ===");
            Console.WriteLine($"Reviewing {records.Count} records...\n");

            int currentIndex = 0;
            int deletedCount = 0;
            int skippedCount = 0;

            while (currentIndex < records.Count)
            {
                var record = records[currentIndex];
                currentIndex++;

                Console.WriteLine($"\n--- Record {currentIndex} of {records.Count} ---");
                Console.WriteLine($"Name: {record.Name}");
                Console.WriteLine($"ID: {record.Id}");
                Console.WriteLine($"Path: {record.Path}");
                Console.WriteLine($"Last Modified: {record.LastModificationTime:yyyy-MM-dd HH:mm:ss}");
                if (record.Tags.Count > 0)
                {
                    Console.WriteLine($"Tags: {string.Join(", ", record.Tags)}");
                }

                Console.WriteLine($"\nProgress: {currentIndex}/{records.Count} | Deleted: {deletedCount} | Skipped: {skippedCount}");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  [D] Delete this record");
                Console.WriteLine("  [S] Skip this record");
                Console.WriteLine("  [E] Exit to main menu");
                Console.Write("Choose an option (D/S/E): ");

                var input = Console.ReadLine()?.Trim().ToUpper();

                switch (input)
                {
                    case "D":
                        try
                        {
                            await ResoniteRecordService.DeleteRecordAsync(auth, record);
                            Console.WriteLine($"✓ Successfully deleted record: {record.Name}");
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"✗ Failed to delete record: {ex.Message}");
                        }
                        break;
                    case "S":
                        Console.WriteLine($"→ Skipped record: {record.Name}");
                        skippedCount++;
                        break;
                    case "E":
                        Console.WriteLine("\nExiting to main menu...");
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please choose D, S, or E.");
                        currentIndex--; // Stay on current record
                        break;
                }
            }

            Console.WriteLine($"\n=== Review Complete ===");
            Console.WriteLine($"Total records reviewed: {records.Count}");
            Console.WriteLine($"Records deleted: {deletedCount}");
            Console.WriteLine($"Records skipped: {skippedCount}");
        }

        static async Task ReviewMessageItemsAsync(AuthResponse auth)
        {
            Console.WriteLine("\n=== Review Message Items ===");
            Console.WriteLine("Searching for records with 'message_item' tag (excluding voice+message combinations)...");

            var messageItemRecords = await ResoniteRecordService.GetMessageItemRecordsAsync(auth);

            if (messageItemRecords.Count == 0)
            {
                Console.WriteLine("No message item records found.");
                return;
            }

            Console.WriteLine($"Found {messageItemRecords.Count} message item records to review.\n");

            int currentIndex = 0;
            int deletedCount = 0;
            int skippedCount = 0;

            while (currentIndex < messageItemRecords.Count)
            {
                var record = messageItemRecords[currentIndex];
                currentIndex++;

                Console.WriteLine($"\n--- Record {currentIndex} of {messageItemRecords.Count} ---");
                Console.WriteLine($"Name: {record.Name}");
                Console.WriteLine($"ID: {record.Id}");
                Console.WriteLine($"Path: {record.Path}");
                Console.WriteLine($"Last Modified: {record.LastModificationTime:yyyy-MM-dd HH:mm:ss}");
                if (record.Tags.Count > 0)
                {
                    Console.WriteLine($"Tags: {string.Join(", ", record.Tags)}");
                }

                Console.WriteLine($"\nProgress: {currentIndex}/{messageItemRecords.Count} | Deleted: {deletedCount} | Skipped: {skippedCount}");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  [D] Delete this record");
                Console.WriteLine("  [S] Skip this record");
                Console.WriteLine("  [E] Exit to main menu");
                Console.Write("Choose an option (D/S/E): ");

                var input = Console.ReadLine()?.Trim().ToUpper();

                switch (input)
                {
                    case "D":
                        try
                        {
                            await ResoniteRecordService.DeleteRecordAsync(auth, record);
                            Console.WriteLine($"✓ Successfully deleted record: {record.Name}");
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"✗ Failed to delete record: {ex.Message}");
                        }
                        break;
                    case "S":
                        Console.WriteLine($"→ Skipped record: {record.Name}");
                        skippedCount++;
                        break;
                    case "E":
                        Console.WriteLine("\nExiting to main menu...");
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please choose D, S, or E.");
                        currentIndex--; // Stay on current record
                        break;
                }
            }

            Console.WriteLine($"\n=== Review Complete ===");
            Console.WriteLine($"Total records reviewed: {messageItemRecords.Count}");
            Console.WriteLine($"Records deleted: {deletedCount}");
            Console.WriteLine($"Records skipped: {skippedCount}");
        }

        static async Task ManageScreensRecordsAsync(AuthResponse auth)
        {
            var screensRecords = await ResoniteRecordService.GetRecordsByNameAndPathAsync(
                auth, 
                "Screens", 
                "Workspaces\\Private\\RadiantDash"
            );

            if (screensRecords.Count == 0)
            {
                Console.WriteLine("No Screens records found in RadiantDash path");
                return;
            }

            var sortedRecords = screensRecords
                .OrderBy(record => record.LastModificationTime)
                .ToList();

            Console.WriteLine($"Found {sortedRecords.Count} Screens records in RadiantDash");
            
            ResoniteRecordService.DisplayRecords(sortedRecords);

            var recordsToDelete = sortedRecords.Take(sortedRecords.Count - 1).ToList();

            if (recordsToDelete.Count > 0)
            {
                Console.WriteLine($"\nRecords to delete (excluding most recent):");
                ResoniteRecordService.DisplayRecordSummary(recordsToDelete);
                
                Console.Write($"\nDelete {recordsToDelete.Count} oldest Screens records? (y/n): ");
                var input = Console.ReadLine()?.Trim().ToLower();
                
                if (input?.StartsWith("y") == true)
                {
                    await ResoniteRecordService.DeleteRecordsAsync(auth, recordsToDelete);
                }
                else
                {
                    Console.WriteLine("Deletion cancelled");
                }
            }
            else
            {
                Console.WriteLine("No records to delete (only one record found)");
            }
        }
}
}
