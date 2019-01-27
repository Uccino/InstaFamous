using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramFamous.Classes
{
    static class SettingsManager
    {
        /// <summary>
        /// Loads settings from a .JSON file and puts it in Config.settings
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool LoadSettings(string filePath)
        {
            try
            {
                // Read the JSON file
                dynamic jsonSettings = ReadJsonFile(filePath);

                // Grab each part of the JSON and set the settings.
                var setRedditSettings = jsonSettings.Reddit;
                var setInstagramSettings = jsonSettings.Instagram;
                var setDatabaseSettings = jsonSettings.Database;

                // Get and set Reddit settings
                Properties.Config.Default.RedditSubreddit = setRedditSettings.Subreddit.ToString();
                Properties.Config.Default.RedditHeaders = setRedditSettings.Headers.ToString();
                Properties.Config.Default.RedditUpvoteRequirement = setRedditSettings.UpvoteThreshold.ToString();
                Properties.Config.Default.FileDirectory = setRedditSettings.Subreddit.ToString();

                // Get and set the instagram settings
                Properties.Config.Default.InstagramUsername = setInstagramSettings.InstagramUsername;
                Properties.Config.Default.InstagramPassword = setInstagramSettings.InstagramPassword;
                Properties.Config.Default.InstagramTags = setInstagramSettings.InstagramTags;

                Properties.Config.Default.Save();

                return true;
            }
            catch (FileNotFoundException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occured! " + ex.ToString());
            }        
        }

        /// <summary>
        /// Reads the json file specified
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static dynamic ReadJsonFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                // Read the JSON
                using (StreamReader jsonReader = new StreamReader(filePath))
                {
                    // Deserialize it into a dynamic
                    string jsonFile         = jsonReader.ReadToEnd();
                    dynamic jsonSettings    = JsonConvert.DeserializeObject(jsonFile);

                    return jsonSettings;
                }
            }
            else
            {
                throw new FileNotFoundException("Warning: unable to find the settings.JSON");
            }
        }
    }
}
