using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CollectionManagement.Models;
using CollectionManagement.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CollectionManagement.ViewModels
{
    [QueryProperty(nameof(CollectionId), nameof(CollectionId))]
    public partial class CollectionPageViewModel : ObservableObject
    {
        private readonly FileService _fileService;

        [ObservableProperty]
        private string collectionId;

        [ObservableProperty]
        private Collection selectedCollection;

        [ObservableProperty]
        private ObservableCollection<string> columns;

        [ObservableProperty]
        private string newItemTitle;

        [ObservableProperty]
        private string newItemPrice;

        [ObservableProperty]
        private string newItemRating;

        [ObservableProperty]
        private string newItemComment;

        [ObservableProperty]
        private string newItemImagePath;

        [ObservableProperty]
        private Item itemToEdit;

        [ObservableProperty]
        private bool isEditing;

        [ObservableProperty]
        private bool isEditingInverse = true;

        [ObservableProperty]
        private ItemStatus newItemStatus;

        public bool IsNewChecked
        {
            get => NewItemStatus == ItemStatus.New;
            set
            {
                if (value) NewItemStatus = ItemStatus.New;
                OnPropertyChanged();
            }
        }

        public bool IsUsedChecked
        {
            get => NewItemStatus == ItemStatus.Used;
            set
            {
                if (value) NewItemStatus = ItemStatus.Used;
                OnPropertyChanged();
            }
        }

        public bool IsForSaleChecked
        {
            get => NewItemStatus == ItemStatus.ForSale;
            set
            {
                if (value) NewItemStatus = ItemStatus.ForSale;
                OnPropertyChanged();
            }
        }

        public bool IsWantToBuyChecked
        {
            get => NewItemStatus == ItemStatus.WantToBuy;
            set
            {
                if (value) NewItemStatus = ItemStatus.WantToBuy;
                OnPropertyChanged();
            }
        }

        public bool IsSoldChecked
        {
            get => NewItemStatus == ItemStatus.Sold;
            set
            {
                if (value) NewItemStatus = ItemStatus.Sold;
                OnPropertyChanged();
            }
        }

        public CollectionPageViewModel()
        {
            _fileService = new FileService();
        }

        partial void OnCollectionIdChanged(string value)
        {
            LoadCollection(value);
        }

        private void LoadCollection(string collectionId)
        {
            var collections = _fileService.GetAllCollections();
            SelectedCollection = collections.FirstOrDefault(c => c.Id.ToString() == collectionId);

            if (SelectedCollection?.Items != null)
            {
                var sortedItems = SelectedCollection.Items
                    .OrderBy(item => item.IsSold)
                    .ToList();

                SelectedCollection.Items = new ObservableCollection<Item>(sortedItems);
            }
        }

        [RelayCommand]
        public async Task SelectImage()
        {
            try
            {
                var photo = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Wybierz zdjęcie"
                });

                if (photo != null)
                {
                    using (var stream = await photo.OpenReadAsync())
                    {
                        NewItemImagePath = photo.FullPath;
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Błąd", ex.Message, "OK");
            }
        }

        [RelayCommand]
        public async void AddItem()
        {
            if (string.IsNullOrWhiteSpace(NewItemTitle) ||
                string.IsNullOrWhiteSpace(NewItemPrice) ||
                string.IsNullOrWhiteSpace(NewItemRating))
            {
                await Application.Current.MainPage.DisplayAlert("Błąd", "Wszystkie pola muszą być wypełnione.", "OK");
                return;
            }

            if (NewItemComment == null)
                NewItemComment = string.Empty;


            var nameCommentPattern = @"^[\p{L}\p{N}\s]+$"; 
            if (!Regex.IsMatch(NewItemTitle, nameCommentPattern))
            {
                await Application.Current.MainPage.DisplayAlert("Nieprawidłowy tytuł", "Tytuł może zawierać tylko litery, cyfry i spacje.", "OK");
                return;
            }

            if (!Regex.IsMatch(NewItemComment, nameCommentPattern))
            {
                await Application.Current.MainPage.DisplayAlert("Nieprawidłowy komentarz", "Komentarz może zawierać tylko litery, cyfry i spacje.", "OK");
                return;
            }


            if (!int.TryParse(NewItemRating, out int rating) || rating < 0 || rating > 10)
            {
                await Application.Current.MainPage.DisplayAlert("Nieprawidłowy rating", "Rating musi być liczbą całkowitą między 0 a 10.", "OK");
                return;
            }


            if (!decimal.TryParse(NewItemPrice, out decimal price) || price < 0)
            {
                await Application.Current.MainPage.DisplayAlert("Nieprawidłowa cena", "Cena musi być liczbą dodatnią.", "OK");
                return;
            }

            var existingItem = SelectedCollection.Items
                     .FirstOrDefault(item => item.Title.Equals(NewItemTitle, StringComparison.OrdinalIgnoreCase));

            if (existingItem != null)
            {

                bool userConfirmed = await Application.Current.MainPage.DisplayAlert(
                    "Element już istnieje",
                    "Element o tej samej nazwie już istnieje. Czy chcesz dodać go ponownie?",
                    "Tak",
                    "Nie");

                if (!userConfirmed)
                {
                    return;
                }
            }


            var newItem = new Item(NewItemTitle, price, NewItemStatus, rating, NewItemComment, NewItemImagePath);
            SelectedCollection.Items.Add(newItem);
            _fileService.AddItem(newItem, SelectedCollection.Id);

            var sortedItems = SelectedCollection.Items
                .OrderBy(item => item.IsSold)
                .ToList();

            SelectedCollection.Items = new ObservableCollection<Item>(sortedItems);

            ClearInputFields();
        }

        [RelayCommand]
        public async void UpdateItem()
        {
            
            if (ItemToEdit == null)
            {
                await Application.Current.MainPage.DisplayAlert("Błąd", "Nie znaleziono elementu do edycji.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewItemTitle) ||
                string.IsNullOrWhiteSpace(NewItemPrice) ||
                string.IsNullOrWhiteSpace(NewItemRating))
            {
                await Application.Current.MainPage.DisplayAlert("Błąd", "Wszystkie pola muszą być wypełnione.", "OK");
                return;
            }

            if (NewItemComment == null)
                NewItemComment = string.Empty;

            
            var nameCommentPattern = @"^[\p{L}\p{N}\s]+$"; 
            if (!Regex.IsMatch(NewItemTitle, nameCommentPattern))
            {
                await Application.Current.MainPage.DisplayAlert("Nieprawidłowy tytuł", "Tytuł może zawierać tylko litery, cyfry i spacje.", "OK");
                return;
            }

            if (!Regex.IsMatch(NewItemComment, nameCommentPattern))
            {
                await Application.Current.MainPage.DisplayAlert("Nieprawidłowy komentarz", "Komentarz może zawierać tylko litery, cyfry i spacje.", "OK");
                return;
            }

            
            if (!int.TryParse(NewItemRating, out int rating) || rating < 0 || rating > 10)
            {
                await Application.Current.MainPage.DisplayAlert("Nieprawidłowy rating", "Rating musi być liczbą całkowitą między 0 a 10.", "OK");
                return;
            }

            
            if (!decimal.TryParse(NewItemPrice, out decimal price) || price < 0)
            {
                await Application.Current.MainPage.DisplayAlert("Nieprawidłowa cena", "Cena musi być liczbą dodatnią.", "OK");
                return;
            }

            var existingItem = SelectedCollection.Items
                     .FirstOrDefault(item => item.Title.Equals(NewItemTitle, StringComparison.OrdinalIgnoreCase));

            if (existingItem != null)
            {

                bool userConfirmed = await Application.Current.MainPage.DisplayAlert(
                    "Element już istnieje",
                    "Element o tej samej nazwie już istnieje. Czy chcesz dodać go ponownie?",
                    "Tak",
                    "Nie");

                if (!userConfirmed)
                {
                    return;
                }
            }


            ItemToEdit.Title = NewItemTitle;
            ItemToEdit.Price = price;
            ItemToEdit.Status = NewItemStatus;
            ItemToEdit.Rating = rating;
            ItemToEdit.Comment = NewItemComment;
            ItemToEdit.ImagePath = NewItemImagePath;

            
            _fileService.UpdateItem(ItemToEdit.Id, ItemToEdit);

            
            LoadCollection(CollectionId);

            ClearInputFields();
            IsEditing = !IsEditing; 
            IsEditingInverse = !IsEditingInverse; 
        }

        [RelayCommand]
        private async void ShowStats()
        {
            await Application.Current.MainPage.DisplayAlert("Statystyki", 
                $"Ilość Wszystkich przedmiotów w kolekcji: {SelectedCollection.Items.Count}\n" +
                $"Ilość posiadanych przedmiotów: {SelectedCollection.Items.Where(i => i.Status != ItemStatus.Sold && i.Status != ItemStatus.WantToBuy).Count()}\n" +
                $"Ilość przedmiotów do sprzedaży: {SelectedCollection.Items.Where(i => i.Status == ItemStatus.ForSale).Count()}\n" +
                $"Ilość sprzedanych przedmiotów: {SelectedCollection.Items.Where(i=>i.Status == ItemStatus.Sold).Count()}\n" +
                $"Wartkość kolekcji: {SelectedCollection.Items.Where(i=>i.IsSold==false).Sum(i=>i.Price)}zł\n",
                "OK");
        }

        [RelayCommand]
        public void SelectItemForEditing(Item item)
        {
            if (item != null)
            {

                ItemToEdit = item; 
                NewItemTitle = item.Title;
                NewItemPrice = item.Price.ToString();
                NewItemRating = item.Rating.ToString();
                NewItemComment = item.Comment;
                NewItemStatus = item.Status; 
                NewItemImagePath = item.ImagePath;

                IsEditing = !IsEditing; 
                IsEditingInverse = !IsEditingInverse; 
            }
        }


        [RelayCommand]
        public void CancelEdit()
        {
            IsEditing = !IsEditing;
            IsEditingInverse = !IsEditingInverse;
            ClearInputFields();
        }

        [RelayCommand]
        public void DeleteItem(Item item)
        {
            if (item != null)
            {
                SelectedCollection.Items.Remove(item);
                _fileService.DeleteItem(item.Id);

                if (IsEditing)
                {
                    IsEditing = !IsEditing;
                    IsEditingInverse = !IsEditingInverse;
                }
                ClearInputFields();
            }
        }

        private void ClearInputFields()
        {
            NewItemTitle = string.Empty;
            NewItemPrice = string.Empty;
            NewItemStatus = ItemStatus.New; 
            IsNewChecked = true;
            NewItemRating = string.Empty;
            NewItemComment = string.Empty;
            NewItemImagePath = string.Empty; 
        }

        [RelayCommand]
        public async Task ExportCollection()
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Wybierz plik tekstowy",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".txt" } },
                        { DevicePlatform.Android, new[] { ".txt" } },
                        { DevicePlatform.iOS, new[] { ".txt" } },
                        { DevicePlatform.macOS, new[] { ".txt" } }
                    })
                });

                if (result != null)
                    _fileService.ExportCollection(SelectedCollection, result.FullPath);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Błąd", "Wystąpił błąd podczas eksportu.", "OK");
            }
        }

        [RelayCommand]
        public async Task ImportItems()
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Wybierz plik tekstowy",
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
                        "Potwierdzenie",
                        "Czy chcesz dodać duplikaty?",
                        "Tak",
                        "Nie");

                    _fileService.ImportItems(SelectedCollection.Id, result.FullPath, addDuplicates);
                }

                LoadCollection(CollectionId);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Błąd", "Wystąpił błąd podczas importu.", "OK");
            }
        }

        [RelayCommand]
        public async Task Back()
        {
            Shell.Current.Items.Clear();
            Application.Current!.MainPage = new AppShell();
            await Shell.Current.GoToAsync($"///MainPage");
        }
    }
}