﻿using MoreLinq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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

namespace QuickDictionary
{
    /// <summary>
    /// Interaction logic for WordLists.xaml
    /// </summary>
    public partial class WordLists : Window, INotifyPropertyChanged
    {
        private bool editLists = false;
        public bool EditLists
        {
            get
            {
                return editLists;
            }
            set
            {
                editLists = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditLists)));
            }
        }

        string renameListName;
        public string RenameListName
        {
            get
            {
                return renameListName;
            }
            set
            {
                renameListName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RenameListName)));
            }
        }

        private PathWordListPair selectedWordList;
        public PathWordListPair SelectedWordList
        {
            get
            {
                return selectedWordList;
            }
            set
            {
                selectedWordList = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedWordList)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private PathWordListPair renamingList = null;

        public WordLists()
        {
            InitializeComponent();
            Helper.HideBoundingBox(root);
            listWordLists.ItemsSource = WordListManager.WordLists;
            checkTopmost.IsSelected = MainWindow.Config.WordListManagerAlwaysOnTop;
        }

        private void btnFlipToBack_Click(object sender, RoutedEventArgs e)
        {
            SelectedWordList?.WordList?.Entries?.ForEach(x => x.FlashcardFlipped = true);
        }

        private void btnFlipToFront_Click(object sender, RoutedEventArgs e)
        {
            SelectedWordList?.WordList?.Entries?.ForEach(x => x.FlashcardFlipped = false);
        }

        private void WordCard_WordEdited(object sender, EventArgs e)
        {
            if (SelectedWordList != null)
                WordListManager.SaveList(SelectedWordList);
        }

        private void WordCard_DeleteWord(object sender, EventArgs e)
        {
            if (SelectedWordList == null) return;
            WordEntry entry = (sender as WordCard)?.DataContext as WordEntry;
            if (entry != null)
            {
                SelectedWordList.WordList?.Entries?.Remove(entry);
                WordListManager.SaveList(SelectedWordList);
            }
        }

        private void WordListsWindow_Closing(object sender, CancelEventArgs e)
        {
            if (SelectedWordList != null)
                WordListManager.SaveList(SelectedWordList);
        }

        private void WordListItem_DeleteList(object sender, EventArgs e)
        {
            PathWordListPair entry = (sender as WordListItem)?.DataContext as PathWordListPair;
            if (entry != null)
            {
                WordListManager.WordLists.Remove(entry);
                WordListManager.DeletedPaths.Add(entry.Path);
                snackbar.MessageQueue.Enqueue(
                    entry.WordList.Name + " deleted", 
                    "UNDO", 
                    (path) => { 
                        WordListManager.WordLists.Add(new PathWordListPair(path, WordListManager.LoadList(path)));
                        WordListManager.DeletedPaths.Remove(path);
                    }, 
                    entry.Path, 
                    false, 
                    true, 
                    TimeSpan.FromSeconds(5));
            }
        }

        private void WordListItem_RenameList(object sender, EventArgs e)
        {
            renamingList = (sender as WordListItem)?.DataContext as PathWordListPair;
            RenameListName = renamingList.WordList.Name;
            dialogHost.IsOpen = true;
        }

        private void btnRenameListSave_Click(object sender, RoutedEventArgs e)
        {
            if (renamingList == null) return;

            if (!WordlistNameValidationRule.ValidateWordlistName(txtRenameListName.Text, CultureInfo.InvariantCulture).IsValid)
            {
                txtRenameListName.Focus();
                Keyboard.Focus(txtRenameListName);
                txtRenameListName.SelectAll();
                return;
            }
            string newPath = Path.Combine(Path.GetDirectoryName(renamingList.Path), txtRenameListName.Text + ".xml");
            File.Move(renamingList.Path, newPath);
            renamingList.WordList.Name = txtRenameListName.Text;
            renamingList.Path = newPath;
            WordListManager.SaveList(renamingList);
            renamingList = null;
            snackbar.MessageQueue.Enqueue($"{txtRenameListName.Text} renamed");
            dialogHost.IsOpen = false;
        }

        private void btnRenameListCancel_Click(object sender, RoutedEventArgs e)
        {
            dialogHost.IsOpen = false;
            renamingList = null;
        }

        private void check_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MainWindow.Config.WordListManagerAlwaysOnTop = checkTopmost.IsSelected;
            MainWindow.Config.SaveConfig();
        }
    }
}
