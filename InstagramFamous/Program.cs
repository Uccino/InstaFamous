using InstagramFamous.Classes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace InstagramFamous
{
    class Program
    {
        static void Main()
        {
            // Attempt to load the settings before continueing
            SendMessage("Loading settings.json");
            if (SettingsManager.LoadSettings("settings.json"))
            {
                SendMessage("Successfully loaded the settings");

                // Attempting to get the latest reddit posts of given subreddit.

                FileManager fileClient = new FileManager();
                InstagramManager instaClient = new InstagramManager();
                RedditManager redditClient = new RedditManager();

                while (true)
                {
                    // Get all the reddit posts
                    dynamic redditPosts;
                    try
                    {
                        redditPosts = redditClient.GetPosts();
                        redditPosts = redditPosts["data"]["children"];
                    }
                    catch (Exception ex)
                    {
                        SendMessage("There was a fatal error, writing it to the error log.");
                        break;
                    }

                    // Filter all of the posts before downloading.
                    List<Dictionary<string, string>> lstRedditApproved = redditClient.FilterPosts(redditPosts);
                    SendMessage($"Got {lstRedditApproved.Count} approved posts!");
                    SendMessage("Attempting to download posts");

                    // Download all of the posts                    
                    foreach (var Post in lstRedditApproved)
                    {
                        fileClient.DownloadPost(Post);
                    }

                    // Change post picture format
                    string[] filePaths = Directory.GetFiles(Properties.Config.Default.FileDirectory);
                    // Get all of the files that are in PNG format
                    string[] pngList = Array.FindAll(filePaths, item => item.Contains(".png"));
                    string imageDirectory = Properties.Config.Default.FileDirectory;

                    // Add padding to each image
                    SendMessage("Changing picture formats.");
                    foreach (string filePath in pngList)
                    {
                        fileClient.ChangePictureFormat(filePath);
                    }
                    // Add padding to posts
                    SendMessage("Resizing images");
                    foreach (string filePath in Directory.EnumerateFiles(imageDirectory))
                    {
                        fileClient.AddPaddingToPicture(filePath);
                        using (Image img = Image.FromFile(filePath))
                        {
                            if (img.Width > 1080)
                            {
                                img.Dispose();
                                fileClient.ResizeImage(filePath);
                            }
                        }
                        fileClient.AddPaddingToPicture(filePath);
                    }
                    SendMessage("Removing metadata");
                    // Remove EXIF in posts
                    foreach (string filePath in Directory.EnumerateFiles(imageDirectory))
                    {
                        fileClient.RemoveExif(filePath);
                    }
                    SendMessage("Uploading images to instagram.");
                    // Upload to instagram.
                    int timeToWait = GetWaitTime(imageDirectory);
                    SendMessage("Attempting login.");
                    if (instaClient.Login())
                    {
                        SendMessage("Logged in to instagram.");
                        foreach (string filePath in Directory.EnumerateFiles(imageDirectory))
                        {
                            if (instaClient.UploadPicture(filePath))
                            {
                                SendMessage("Succesfully uploaded picture.");
                            }
                            else
                            {
                                SendMessage("Unable to upload the picture to instagram.");
                            }

                            SendMessage("Waiting untill we can upload the next picture.");
                            System.Threading.Thread.Sleep(timeToWait);
                        }

                        instaClient.Logout();
                    }
                    else
                    {
                        SendMessage("Unable to login to instagram!");
                    }
                }
            }
        }

        /// <summary>
        /// This function Formats text and writes it to the console.
        /// </summary>
        /// <param name="message">Message that you want to display</param>
        /// <param name="WriteLine">Set to true to use Console.WriteLine, set to false to use Console.Write </param>
        internal static void SendMessage(string message, bool WriteLine = true)
        {
            string Message = ($"[{DateTime.Now.ToShortTimeString()}] {message} ");
            if (WriteLine)
            {
                Console.WriteLine(Message);
            }
            else
            {
                Console.Write(Message);
            }

            // TODO
            // Write all the entries to a database for logging purposes
        }

        /// <summary>
        /// This function calculates the amount of time you need to wait between posting
        /// </summary>
        /// <param name="DirectoryName"></param>
        /// <returns></returns>
        internal static int GetWaitTime(string DirectoryName)
        {
            // Get amount of items in the image folder
            int fileCount = Directory.GetFiles(DirectoryName).Length;
            var minutesToWait = (24.0 / fileCount) * 60;
            int milisecondsToWait = Convert.ToInt32(minutesToWait * 60000);

            return milisecondsToWait;
        }
    }
}
