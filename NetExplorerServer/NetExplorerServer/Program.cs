﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetExplorerServer
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            FolderBrowserDialog chooseBrowserDialog = new FolderBrowserDialog
            {
                Description = @"Выберите корневую папку",
                RootFolder = Environment.SpecialFolder.MyComputer
            };
            if (chooseBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedRoot = chooseBrowserDialog.SelectedPath;
                Console.WriteLine(@"Ваш корневой каталог - {0}",selectedRoot);
                ServerStart server = new ServerStart(selectedRoot);
                server.Start();
            }
        }
    }
}
