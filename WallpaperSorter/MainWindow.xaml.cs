using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WallpaperSorter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Regex tagRegex = new Regex(@"\d+ - ([^\.]+)");

        public MainWindow()
        {
            InitializeComponent();
            sourceText.Text = Properties.Settings.Default.SourceDir;
            destinationText.Text = Properties.Settings.Default.DesinationDir;
        }

        private void sourceBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = Properties.Settings.Default.SourceDir;
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.Cancel)
                Properties.Settings.Default.SourceDir = dialog.SelectedPath;
            sourceText.Text = Properties.Settings.Default.SourceDir;
            Properties.Settings.Default.Save();
        }

        private void destinationBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = Properties.Settings.Default.DesinationDir;
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.Cancel)
                Properties.Settings.Default.DesinationDir = dialog.SelectedPath;
            destinationText.Text = Properties.Settings.Default.DesinationDir;
            Properties.Settings.Default.Save();
        }

        private void goButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                process();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private String findArtistTag(String[] tags)
        {
            const String ARTIST_TAG_PREFIX = "artist_";
            const String ARTIST_TAG_PREFIX_2 = "artist-";
            const String ARTIST_TAG_SUFFIX_PARENTHETICAL = "_(artist)";
            foreach (String tag in tags)
            {
                if (tag.ToLower().StartsWith(ARTIST_TAG_PREFIX))
                {
                    return tag.Substring(ARTIST_TAG_PREFIX.Length);
                }
                if (tag.ToLower().StartsWith(ARTIST_TAG_PREFIX_2))
                {
                    return tag.Substring(ARTIST_TAG_PREFIX_2.Length);
                }
                if (tag.ToLower().EndsWith(ARTIST_TAG_SUFFIX_PARENTHETICAL))
                {
                    return tag.Substring(0, tag.Length - ARTIST_TAG_SUFFIX_PARENTHETICAL.Length);
                }
            }
            return String.Empty;
        }

        private void process()
        {
            Dictionary<string, Dictionary<String, DirectoryInfo>> directoriesNamesAndArtists = new Dictionary<string, Dictionary<String, DirectoryInfo>>();
            List<string> skippedArtists = new List<string>();

            DirectoryInfo destinationDir = new DirectoryInfo(Properties.Settings.Default.DesinationDir);


            DirectoryInfo[] categoryDirs = destinationDir.GetDirectories();
            foreach (DirectoryInfo categoryDir in categoryDirs)
            {
                string category = categoryDir.Name.ToLower();
                directoriesNamesAndArtists.Add(category, new Dictionary<String, DirectoryInfo>());

                DirectoryInfo[] artistDirs = categoryDir.GetDirectories();
                foreach (DirectoryInfo artistDir in artistDirs)
                {
                    String name = artistDir.Name.ToLower();
                    name = name.Replace(categoryDir.Name.ToLower(), String.Empty).Trim();

                    directoriesNamesAndArtists[category].Add(name,artistDir);
                    processArtistDir(name, artistDir);
                }
            }

            DirectoryInfo sourceDir = new DirectoryInfo(Properties.Settings.Default.SourceDir);
            FileInfo[] remainingFiles = sourceDir.GetFiles();

            foreach (FileInfo f in remainingFiles)
            {
                f.Refresh();
                if (!f.Exists)
                    continue;
                String name = correctFileName(f.Name);
                String[] tags = getTags(name);
                String artistTag = findArtistTag(tags);

                if (String.IsNullOrWhiteSpace(artistTag))
                {
                    List<FileInfo> tmp = new List<FileInfo>();

                    tmp.Add(f);
                    TagPicker tagPicker = new TagPicker(tags, tmp);
                    tagPicker.Owner = this;
                    if (tagPicker.ShowDialog().Value && !String.IsNullOrWhiteSpace(tagPicker.SelectedTag))
                    {
                        artistTag = tagPicker.SelectedTag.Trim().ToLower();
                    }
                }

                if (String.IsNullOrWhiteSpace(artistTag))
                {
                    continue;
                }

                artistTag = artistTag.ToLower().Trim();

                String artistCategory = String.Empty;

                foreach(String category in directoriesNamesAndArtists.Keys)
                {
                    if(directoriesNamesAndArtists[category].ContainsKey(artistTag))
                    {
                        artistCategory = category;
                        break;
                    }
                }

                if(String.IsNullOrEmpty(artistCategory))
                {
                    List<FileInfo> artistImages = getOtherArtistImages(artistTag);
                    
                    TagPicker tagPicker = new TagPicker(directoriesNamesAndArtists.Keys.ToArray<String>(), artistImages);
                    tagPicker.Owner = this;
                    if (tagPicker.ShowDialog().Value && !String.IsNullOrWhiteSpace(tagPicker.SelectedTag))
                    {
                        artistCategory = tagPicker.SelectedTag;
                    }
                }

                if (String.IsNullOrWhiteSpace(artistCategory))
                {
                    continue;
                }

                if (!directoriesNamesAndArtists.ContainsKey(artistCategory))
                    directoriesNamesAndArtists.Add(artistCategory, new Dictionary<string, DirectoryInfo>());

                String newArtistDir = System.IO.Path.Combine(destinationDir.FullName, System.IO.Path.Combine(artistCategory, artistCategory + " " + artistTag));
                DirectoryInfo newArtistDirInfo = new DirectoryInfo(newArtistDir);
                if (!newArtistDirInfo.Exists)
                    newArtistDirInfo.Create();

                if (!directoriesNamesAndArtists[artistCategory].ContainsKey(artistTag))
                    directoriesNamesAndArtists[artistCategory].Add(artistTag, newArtistDirInfo);

                processArtistDir(artistTag, newArtistDirInfo);
            }
        }


        private String[] getTags(String name)
        {
            if (!tagRegex.IsMatch(name))
                throw new Exception("No regex match: " + name);

            String[] tags = tagRegex.Match(name).Groups[1].Value.Split(' ');
            for(int i = 0; i<tags.Length; i++)
            {
                tags[i] = tags[i].Trim().Trim('-').ToLower();
            }
            return tags;
        }

        private List<FileInfo> getOtherArtistImages(String artistName)
        {
            List<FileInfo> output = new List<FileInfo>();
            artistName = artistName.ToLower();
            DirectoryInfo sourceDir = new DirectoryInfo(Properties.Settings.Default.SourceDir);
            FileInfo[] matchingFiles = sourceDir.GetFiles();
            foreach (FileInfo f in matchingFiles)
            {
                String name = correctFileName(f.Name);
                String[] tags = getTags(name);
                if (!tags.Contains(artistName) && findArtistTag(tags) != artistName)
                    continue;
                output.Add(f);
            }
            return output;
        }

        private void processArtistDir(String artist, DirectoryInfo artistDir)
        {
            List<FileInfo> matchingFiles = getOtherArtistImages(artist);

            foreach (FileInfo f in matchingFiles)
            {
                String name = correctFileName(f.Name);

                String destination = System.IO.Path.Combine(artistDir.FullName, name);

                if (File.Exists(destination))
                    throw new Exception("File already exists: " + destination);

                f.MoveTo(destination);
            }
        }

        private String correctFileName(String input)
        {
            return System.Web.HttpUtility.UrlDecode(input).Replace(":", "_");
        }
    }
}
