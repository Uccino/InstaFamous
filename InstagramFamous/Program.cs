using InstagramFamous.Classes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace InstagramFamous
{
    class Program
    {
        internal enum LOGLEVEL { INFO, DEBUG, WARNING }

        static void Main()
        {
            // Attempt to load the settings before continueing
            SendMessage("Loading settings.json", LOGLEVEL.INFO);
            if (SettingsManager.LoadSettings("settings.json"))
            {
                SendMessage("Successfully loaded the settings", LOGLEVEL.INFO);

                // Attempting to get the latest reddit posts of given subreddit.

                FileManager fileClient = new FileManager();
                InstagramManager instaClient = new InstagramManager();
                RedditManager redditClient = new RedditManager();
                string imageDirectory = Properties.Config.Default.FileDirectory;

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
                        SendMessage("There was a fatal error, writing it to the error log.", LOGLEVEL.DEBUG);
                        SendMessage(ex.ToString(), LOGLEVEL.WARNING);
                        break;
                    }

                    // Filter all of the posts before downloading.
                    List<Dictionary<string, string>> lstRedditApproved = redditClient.FilterPosts(redditPosts);
                    SendMessage($"Got {lstRedditApproved.Count} approved posts!", LOGLEVEL.DEBUG);
                    SendMessage("Attempting to download posts", LOGLEVEL.INFO);

                    // Attempt to download all the approved posts                   
                    foreach (var Post in lstRedditApproved)
                    {
                        try
                        {
                            fileClient.DownloadPost(Post);
                            SendMessage($"Downloading {Post["Link"]}", LOGLEVEL.DEBUG);
                        }
                        catch (Exception ex)
                        {
                            SendMessage($"An error occured while downloading {Post["Link"]} {Environment.NewLine} {ex.ToString()}", LOGLEVEL.WARNING);
                            throw;
                        }

                    }

                    // Get all the files in our directory and change the format from .png to .jpg
                    string[] filePaths = Directory.GetFiles(Properties.Config.Default.FileDirectory);
                    string[] pngList = Array.FindAll(filePaths, item => item.Contains(".png"));

                    // Add padding to each image
                    SendMessage("Changing picture formats.", LOGLEVEL.INFO);
                    foreach (string filePath in pngList)
                    {
                        fileClient.ChangePictureFormat(filePath);
                    }
                    // Add padding to posts
                    SendMessage("Resizing images", LOGLEVEL.INFO);
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
                    }
                    SendMessage("Removing metadata", LOGLEVEL.INFO);
                    // Remove EXIF in posts using ImageMagick.NET
                    foreach (string filePath in Directory.EnumerateFiles(imageDirectory))
                    {
                        try
                        {
                            fileClient.RemoveExif(filePath);
                        }
                        catch (Exception e)
                        {
                            SendMessage($"An error occured while trying to remove EXIF data on {filePath} {Environment.NewLine} {e.ToString()}", LOGLEVEL.WARNING);
                            throw;
                        }

                    }
                    SendMessage("Uploading images to instagram.", LOGLEVEL.INFO);
                    // Upload to instagram.
                    int timeToWait = GetWaitTime(imageDirectory);
                    SendMessage("Attempting login.", LOGLEVEL.INFO);

                    if (instaClient.Login())
                    {
                        SendMessage("Logged in to instagram.", LOGLEVEL.INFO);
                        foreach (string filePath in Directory.EnumerateFiles(imageDirectory))
                        {
                            if (instaClient.UploadPicture(filePath))
                            {
                                SendMessage("Succesfully uploaded picture.", LOGLEVEL.INFO);
                            }
                            else
                            {
                                SendMessage("Unable to upload the picture to instagram.", LOGLEVEL.WARNING);
                            }

                            SendMessage("Waiting untill we can upload the next picture.", LOGLEVEL.INFO);
                            System.Threading.Thread.Sleep(timeToWait);
                        }
                    }
                    else
                    {
                        SendMessage("Unable to login to instagram, check the username / password combination",LOGLEVEL.WARNING);
                        Console.ReadKey();
                    }
                }
            }
        }

        /// <summary>
        /// This function Formats text and writes it to the console.
        /// </summary>
        /// <param name="message">Message that you want to display</param>
        /// <param name="WriteLine">Set to true to use Console.WriteLine, set to false to use Console.Write </param>
        internal static void SendMessage(string message, LOGLEVEL level)
        {
            string Message = ($"[{DateTime.Now.ToShortTimeString()}][{level.ToString()}] {message} ");

            switch (level)
            {
                case LOGLEVEL.INFO:
                    Console.WriteLine(Message);
                    break;
                case LOGLEVEL.DEBUG:
#if DEBUG
                    Console.WriteLine(Message);
#endif
                    break;
                case LOGLEVEL.WARNING:
#if DEBUG
                    Console.WriteLine(Message);
#endif
                    Console.WriteLine("An error occured, check the logfiles for more info.");
                    HandleError(Message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }

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

            SendMessage($"Time to wait: {minutesToWait} minutes",LOGLEVEL.DEBUG);
            return milisecondsToWait;
        }

        private static void HandleError(string Message)
        {
            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("Logs");
            }

            using (StreamWriter logWriter = new StreamWriter($"Logs/Logfile {DateTime.Now.ToFileTime()} .txt"))
            {
                logWriter.Write(DateTime.Now.ToLongTimeString());
                logWriter.Write(Environment.NewLine);
                logWriter.Write(Message);

                logWriter.Flush();
            }

        }
    }
}
