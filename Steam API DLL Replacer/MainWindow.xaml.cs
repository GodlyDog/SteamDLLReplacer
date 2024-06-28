using System;
using System.Collections.Generic;
using System.IO;
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
using Steam_API_DLL_Replacer.Properties;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Diagnostics;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

namespace Steam_API_DLL_Replacer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            
            InitializeComponent();
            DataContext = this;
            PopulateListBox("C:\\Program Files (x86)\\Steam\\steamapps\\libraryfolders.vdf");
        }

        List<string> VdfParse(string path)
        {
            VProperty vdf_obj = VdfConvert.Deserialize(File.ReadAllText(path)); // let user choose path lol
            List<string> ret = new List<string>();
            foreach (dynamic obj in vdf_obj.Value)
            {
                var val = obj.Value.path.Value;
                ret.Add(val + "\\steamapps\\common");
            }
            return ret;
        }

        private void PopulateListBox(string selectedPath)
        {
            List<string> library_paths = VdfParse(selectedPath);
            List<FolderItem> folders = new List<FolderItem>();
            string thing = LoadSelectedFolders();
            List<string> selected = JsonConvert.DeserializeObject<List<string>>(thing);
            if (selected == null)
            {
                selected = new List<string>();
            }
            foreach (string library_path in library_paths)
            {
                folders = folders.Concat(Directory.GetDirectories(library_path)
                                   .Select(folderPath => new FolderItem { Path = folderPath, IsSelected = selected.Contains(folderPath) })).ToList();
            }

            FoldersListBox.ItemsSource = folders;
        }
        
        public List<string> GetSelectedFolders()
        {
            var selectedFolders = ((List<FolderItem>)FoldersListBox.ItemsSource)
                                  .Where(item => item.IsSelected)
                                  .Select(item => item.Path)
                                  .ToList();

            return selectedFolders;
        }

        private string LoadSelectedFolders()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderPath = System.IO.Path.Combine(appDataPath, "SteamDLLReplacer");
            string filePath = System.IO.Path.Combine(folderPath, "selected.json");

            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            return "";
        }

        private void SaveSelectedFolders()
        {
            var selectedFolders = GetSelectedFolders();
            string selectedFoldersJson = JsonConvert.SerializeObject(selectedFolders);
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderPath = System.IO.Path.Combine(appDataPath, "SteamDLLReplacer");
            Directory.CreateDirectory(folderPath);

            string filePath = System.IO.Path.Combine(folderPath, "selected.json");
            File.WriteAllText(filePath, selectedFoldersJson);
        }

        private void CheckBoxChecked(object sender, RoutedEventArgs e)
        {
            SaveSelectedFolders();
        }
        private void CheckBoxUnchecked(object sender, RoutedEventArgs e)
        {
            SaveSelectedFolders();
        }

        private bool ReplaceRecursive(string directoryPath)
        {
                string[] files = Directory.GetFiles(directoryPath);
                foreach (string file in files)
                {
                    string fileName = System.IO.Path.GetFileName(file);
                    if (fileName.Equals("steam_api.dll"))
                    {
                        string newFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(file), "steam_api_o.dll");
                        File.Move(file, newFilePath);
                        File.Copy(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "Replacement\\steam_api.dll"), System.IO.Path.Combine(System.IO.Path.GetDirectoryName(file), "steam_api.dll"), false);
                        return true;
                    }
                    else if (fileName.Equals("steam_api64.dll"))
                    {
                        string newFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(file), "steam_api64_o.dll");
                        File.Move(file, newFilePath);
                        File.Copy(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "Replacement\\steam_api64.dll"), System.IO.Path.Combine(System.IO.Path.GetDirectoryName(file), "steam_api64.dll"), false);
                        return true;
                    }
                }
                string[] subdirectories = Directory.GetDirectories(directoryPath);
                foreach (string subdirectory in subdirectories)
                {
                    if (ReplaceRecursive(subdirectory))
                    {
                        return true;
                    }
                }
                return false;
        }

        private void ReplaceDLLs(object sender, RoutedEventArgs e)
        {
            List<string> selected = GetSelectedFolders();
            foreach (string folder in selected)
            {
                ReplaceRecursive(folder);
            }
        }
    }

    public class FolderItem
    {
        public string Path { get; set; }
        public bool IsSelected { get; set; }
    }
}
