using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace NoDeadLineTelegramBot
{
    internal class TelegramFunctions
    {
        public static async Task<List<string>> GetUserProfilePhotosAsync(TelegramBotClient botClient, long userId)
        {
            List<string> photoUrls = new List<string>();

            try
            {
                // Get user profile photos
                UserProfilePhotos userProfilePhotos = await botClient.GetUserProfilePhotosAsync(userId);

                if (userProfilePhotos.TotalCount > 0)
                {
                    foreach (var photo in userProfilePhotos.Photos)
                    {
                        // Get the largest available size for each photo
                        var largestPhoto = photo.OrderByDescending(p => p.FileSize).FirstOrDefault();
                        if (largestPhoto != null)
                        {
                            // Get file information
                            var file = await botClient.GetFileAsync(largestPhoto.FileId);

                            // Construct the URL to access the photo
                            string fileUrl = $"https://api.telegram.org/file/bot{Paths.token}/{file.FilePath}";
                            photoUrls.Add(fileUrl);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddLog($"Exception: {ex.Message}");
            }

            return photoUrls;
        }
    }
}
