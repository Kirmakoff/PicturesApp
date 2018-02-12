using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace PicturesApp
{
    public class Config
    {
        public Configuration Conf;
        public Config()
        {
            Conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }

        public string GetRootFolder()
        {
            return Properties.Settings.Default.rootFolder;
            //return Conf.GetSection("rootFolder").ToString();
        }

        public string GetNewFolderName()
        {
            return Properties.Settings.Default.newFolderName;
            //return Conf.GetSection("newFolderName").ToString();
        }
    }

    public sealed class SimpleSection : ConfigurationSection
    {

    }

}
