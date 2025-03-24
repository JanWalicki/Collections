using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CollectionManagement.Models;
using CollectionManagement.Services;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace CollectionManagement.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private readonly FileService _fileService;

        [ObservableProperty]
        public ObservableCollection<Collection> collections;

        [ObservableProperty]
        private string newCollectionName;

        [ObservableProperty]
        private string editedCollectionName;

        [ObservableProperty]
        private bool isEditing;

        private Collection collectionToEdit;

        public MainPageViewModel()
        {
            _fileService = new FileService();
            LoadCollections();
        }

        private void LoadCollections()
        {
            var loadedCollections = _fileService.GetAllCollections();
            Collections = new ObservableCollection<Collection>(loadedCollections);
        }

        [RelayCommand]
        public async void AddCollection()
        {
            if (IsValidCollectionName(NewCollectionName))
            {
                if (Collections.Any(c => c.Name.Equals(NewCollectionName, StringComparison.OrdinalIgnoreCase)))
                {
                    await Application.Current.MainPage.DisplayAlert("Błąd", "Kolekcja o tej nazwie już istnieje.", "OK");
                    return;
                }

                var newCollection = new Collection(NewCollectionName);
                Collections.Add(newCollection);
                _fileService.AddCollection(newCollection);
                NewCollectionName = string.Empty;
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Błąd", "Nazwa kolekcji jest nieprawidłowa. Użyj tylko liter, cyfr i spacji.", "OK");
            }
        }

        private bool IsValidCollectionName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && Regex.IsMatch(name, @"^[\p{L}\p{N}\s]+$");
        }

        [RelayCommand]
        public async void OpenCollection(Collection collection)
        {
            Shell.Current.Items.Clear();
            Application.Current!.MainPage = new AppShell();
            await Shell.Current.GoToAsync($"///CollectionPage?CollectionId={collection.Id}");
        }

        [RelayCommand]
        public void EditCollection(Collection collection)
        {
            if (collection != null)
            {
                collectionToEdit = collection;
                EditedCollectionName = collection.Name;
                IsEditing = true;
            }
        }

        [RelayCommand]
        public async void SaveChanges()
        {
            if (IsValidCollectionName(EditedCollectionName))
            {
                if (Collections.Any(c => c.Name.Equals(EditedCollectionName, StringComparison.OrdinalIgnoreCase) && c != collectionToEdit))
                {
                    await Application.Current.MainPage.DisplayAlert("Błąd", "Kolekcja o tej nazwie już istnieje.", "OK");
                    return;
                }

                collectionToEdit.Name = EditedCollectionName;
                _fileService.UpdateCollection(collectionToEdit.Id,collectionToEdit); 
                IsEditing = false;
                EditedCollectionName = string.Empty;
                LoadCollections();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Błąd", "Nazwa kolekcji jest nieprawidłowa. Użyj tylko liter, cyfr i spacji.", "OK");
            }
        }

        [RelayCommand]
        public void CancelEdit()
        {
            IsEditing = false;
            EditedCollectionName = string.Empty;
        }

        [RelayCommand]
        public async void DeleteCollection(Collection collection)
        {
            if (collection != null)
            {
                bool confirm = await Application.Current.MainPage.DisplayAlert("Potwierdzenie",
                    "Czy na pewno chcesz usunąć tę kolekcję?", "Tak", "Nie");

                if (confirm)
                {
                    _fileService.DeleteCollection(collection.Id);
                    LoadCollections();
                }
            }
        }

        [RelayCommand]
        public async Task ImportCollection()
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Pick a Text File",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".txt" } },
                        { DevicePlatform.Android, new[] { ".txt" } },
                        { DevicePlatform.iOS, new[] { ".txt" } },
                        { DevicePlatform.macOS, new[] { ".txt" } }
                    })
                });

                if (result != null)
                {
                    bool addDuplicates = await Application.Current.MainPage.DisplayAlert(
                        "Confirmation",
                        "Do you want to add duplicates?",
                        "Yes",
                        "No");

                    _fileService.ImportCollection(result.FullPath, addDuplicates);
                }

                LoadCollections();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Błąd", "Wystąpił problem podczas importu", "OK");
            }
        }
    }
}