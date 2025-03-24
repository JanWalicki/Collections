using CollectionManagement.Models;
using System.Diagnostics;


namespace CollectionManagement.Services
{
    public class FileService
    {
        private string CollectionsFile = Path.Combine(FileSystem.AppDataDirectory, "collections.txt");

        public FileService()
        {
            InitializeFiles();
        }

        private void InitializeFiles()
        {
            if (!File.Exists(CollectionsFile)) File.Create(CollectionsFile).Close();
            Debug.WriteLine($"APLICATION DATA IS STORED IN: {FileSystem.AppDataDirectory}");
        }

        public void AddCollection(Collection collection)
        {
            if (collection.Id == 0)
            {
                collection.Id = GetNextCollectionId();
            }
            File.AppendAllText(CollectionsFile, $"C|{collection.Id}|{collection.Name}\n");
            foreach (var item in collection.Items)
            {
                AddItem(item, collection.Id);
            }
        }

        public void AddItem(Item item, int collectionId)
        {
            if (item.Id == 0)
            {
                item.Id = GetNextItemId();
            }

            string imagePath = CopyImageToPhotos(item.ImagePath, collectionId);

            var customFieldsString = string.Join(";", item.CustomFields.Select(cf => $"{cf.Key}:{cf.Value}"));
            File.AppendAllText(CollectionsFile, $"I|{item.Id}|{collectionId}|{item.Title}|{item.Price}|{item.Status}|{item.Rating}|{item.Comment}|{imagePath}|{customFieldsString}\n");
        }

        public List<Collection> GetAllCollections()
        {
            var collections = new Dictionary<int, Collection>();
            var items = new List<Item>();

            foreach (var line in File.ReadAllLines(CollectionsFile))
            {
                var parts = line.Split('|');
                if (parts[0] == "C")
                {
                    var collection = new Collection(parts[2])
                    {
                        Id = int.Parse(parts[1])
                    };
                    collections[collection.Id] = collection;
                }
                else if (parts[0] == "I")
                {
                    var item = new Item(parts[3], decimal.Parse(parts[4]), (ItemStatus)Enum.Parse(typeof(ItemStatus), parts[5]), int.Parse(parts[6]), parts[7], parts[8])
                    {
                        Id = int.Parse(parts[1]),
                        CollectionId = int.Parse(parts[2]),
                        CustomFields = ParseCustomFields(parts[9])
                    };
                    items.Add(item);
                }
            }

            foreach (var item in items)
            {
                if (collections.TryGetValue(item.CollectionId, out var collection))
                {
                    collection.Items.Add(item);
                }
            }

            return collections.Values.ToList();
        }

        private Dictionary<string, CustomField> ParseCustomFields(string customFieldsString)
        {
            var customFields = new Dictionary<string, CustomField>();
            if (string.IsNullOrEmpty(customFieldsString)) return customFields;

            var fields = customFieldsString.Split(';');
            foreach (var field in fields)
            {
                var keyValue = field.Split(':');
                if (keyValue.Length == 2)
                {
                    var name = keyValue[0];
                    var value = keyValue[1];
                    customFields[name] = new CustomField(name, FieldType.Text, value);
                }
            }
            return customFields;
        }

        private int GetNextCollectionId()
        {
            var collections = GetAllCollections();
            return collections.Count > 0 ? collections.Max(c => c.Id) + 1 : 1;
        }

        private int GetNextItemId()
        {
            var items = GetAllCollections()
                .SelectMany(c => c.Items)
                .ToList();
            return items.Count > 0 ? items.Max(i => i.Id) + 1 : 1;
        }

        public void UpdateItem(int oldId, Item updatedItem)
        {
            var lines = File.ReadAllLines(CollectionsFile).ToList();
            bool itemUpdated = false;

            for (int i = 0; i < lines.Count; i++)
            {
                var parts = lines[i].Split('|');
                if (parts[0] == "I" && int.Parse(parts[1]) == oldId)
                {

                    string newImagePath = updatedItem.ImagePath == parts[8] ? parts[8] : ReplaceImage(parts[8],updatedItem.ImagePath, updatedItem.CollectionId);



                    lines[i] = $"I|{updatedItem.Id}|{updatedItem.CollectionId}|{updatedItem.Title}|{updatedItem.Price}|{updatedItem.Status}|{updatedItem.Rating}|{updatedItem.Comment}|{newImagePath}|{string.Join(";", updatedItem.CustomFields.Select(cf => $"{cf.Key}:{cf.Value}"))}";
                    itemUpdated = true;
                    Debug.WriteLine($"Item with ID '{oldId}' updated successfully.");
                    break;
                }
            }

            if (!itemUpdated)
            {
                Debug.WriteLine($"Item with ID '{oldId}' not found.");
            }

            File.WriteAllLines(CollectionsFile, lines);
        }

        public void UpdateCollection(int oldId, Collection updatedCollection)
        {
            var lines = File.ReadAllLines(CollectionsFile).ToList();
            bool collectionUpdated = false;

            for (int i = 0; i < lines.Count; i++)
            {
                var parts = lines[i].Split('|');
                if (parts[0] == "C" && int.Parse(parts[1]) == oldId)
                {

                    lines[i] = $"C|{updatedCollection.Id}|{updatedCollection.Name}";
                    collectionUpdated = true;
                    Debug.WriteLine($"Collection with ID '{oldId}' updated successfully.");
                    break;
                }
            }

            File.WriteAllLines(CollectionsFile, lines);
        }

        public async void ExportCollection(Collection collection, string exportFilePath)
        {
            using (var writer = new StreamWriter(exportFilePath))
            {
                writer.WriteLine($"C|{collection.Id}|{collection.Name}");

                foreach (var item in collection.Items)
                {
                    var customFieldsString = string.Join(";", item.CustomFields.Select(cf => $"{cf.Key}:{cf.Value}"));
                    string imageData = ConvertImageToBase64(item.ImagePath);
                    writer.WriteLine($"I|{item.Id}|{item.CollectionId}|{item.Title}|{item.Price}|{item.Status}|{item.Rating}|{item.Comment}|{imageData}|{customFieldsString}");
                }
            }

            await Application.Current.MainPage.DisplayAlert("Gotowe", $"Export udany do pliku: {exportFilePath}", "OK");
        }

        public void ImportCollection(string importFilePath, bool addDuplicates)
        {
            if (!File.Exists(importFilePath))
            {
                throw new FileNotFoundException("Import file not found.", importFilePath);
            }

            var lines = File.ReadAllLines(importFilePath);
            Collection importedCollection = null;

            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length < 3)
                {
                    Debug.WriteLine($"Invalid line format: {line}");
                    continue;
                }

                try
                {
                    if (parts[0] == "C")
                    {
                        importedCollection = GetAllCollections().FirstOrDefault(c => c.Name.Equals(parts[2], StringComparison.OrdinalIgnoreCase));
                        if (importedCollection == null)
                        {
                            importedCollection = new Collection(parts[2]) { Id = GetNextCollectionId() };
                            AddCollection(importedCollection);
                        }
                    }
                    else if (parts[0] == "I" && importedCollection != null)
                    {
                        var existingItem = importedCollection.Items.FirstOrDefault(i => i.Title.Equals(parts[3], StringComparison.OrdinalIgnoreCase));
                        if (existingItem == null || addDuplicates)
                        {
                            string imagePath = SaveImageFromBase64(parts[8], importedCollection.Id);
                            var newItem = new Item(parts[3], decimal.Parse(parts[4]), (ItemStatus)Enum.Parse(typeof(ItemStatus), parts[5]), int.Parse(parts[6]), parts[7], imagePath)
                            {
                                Id = GetNextItemId(),
                                CollectionId = importedCollection.Id,
                                CustomFields = ParseCustomFields(parts[9])
                            };

                            var customFieldsString = string.Join(";", newItem.CustomFields.Select(cf => $"{cf.Key}:{cf.Value}"));
                            File.AppendAllText(CollectionsFile, $"I|{newItem.Id}|{newItem.CollectionId}|{newItem.Title}|{newItem.Price}|{newItem.Status}|{newItem.Rating}|{newItem.Comment}|{imagePath}|{customFieldsString}\n");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing line: {line}. Exception: {ex.Message}");
                }
            }
        }

        public void ImportItems(int collectionId, string importFilePath, bool addDuplicates)
        {
            if (!File.Exists(importFilePath))
            {
                throw new FileNotFoundException("Import file not found.", importFilePath);
            }

            var lines = File.ReadAllLines(importFilePath);
            Collection targetCollection = GetAllCollections().FirstOrDefault(c => c.Id == collectionId);

            if (targetCollection == null)
            {
                Debug.WriteLine($"Collection with ID '{collectionId}' not found.");
                return;
            }

            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length < 9 || parts[0] != "I")
                {
                    Debug.WriteLine($"Invalid line format or not an item: {line}");
                    continue;
                }

                try
                {
                    var existingItem = targetCollection.Items.FirstOrDefault(i => i.Title.Equals(parts[3], StringComparison.OrdinalIgnoreCase));
                    if (existingItem != null)
                    {
                        if (!addDuplicates)
                        {
                            Debug.WriteLine($"Item '{existingItem.Title}' already exists. Skipping import.");
                            continue;
                        }
                    }

                    string imagePath = SaveImageFromBase64(parts[8], collectionId);
                    var newItem = new Item(parts[3], decimal.Parse(parts[4]), (ItemStatus)Enum.Parse(typeof(ItemStatus), parts[5]), int.Parse(parts[6]), parts[7], imagePath)
                    {
                        Id = GetNextItemId(),
                        CollectionId = collectionId,
                        CustomFields = ParseCustomFields(parts[9])
                    };

                    var customFieldsString = string.Join(";", newItem.CustomFields.Select(cf => $"{cf.Key}:{cf.Value}"));
                    File.AppendAllText(CollectionsFile, $"I|{newItem.Id}|{newItem.CollectionId}|{newItem.Title}|{newItem.Price}|{newItem.Status}|{newItem.Rating}|{newItem.Comment}|{imagePath}|{customFieldsString}\n");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing line: {line}. Exception: {ex.Message}");
                }
            }
        }

        public void DeleteCollection(int collectionId)
        {
            var collections = GetAllCollections();
            var collectionToDelete = collections.FirstOrDefault(c => c.Id == collectionId);

            if (collectionToDelete == null)
            {
                Debug.WriteLine($"Kolekcja o ID '{collectionId}' nie została znaleziona.");
                return;
            }

            foreach (var item in collectionToDelete.Items)
            {
                DeleteItem(item.Id); 
            }

            var updatedLines = new List<string>();
            foreach (var line in File.ReadAllLines(CollectionsFile))
            {
                var parts = line.Split('|');
                if (parts[0] == "C" && int.Parse(parts[1]) == collectionId)
                {
                    continue;
                }
                updatedLines.Add(line);
            }

            File.WriteAllLines(CollectionsFile, updatedLines);
            Debug.WriteLine($"Kolekcja o ID '{collectionId}' i jej przedmioty zostały pomyślnie usunięte.");
        }

        public void DeleteItem(int itemId)
        {
            var collections = GetAllCollections();
            var itemToDelete = collections.SelectMany(c => c.Items).FirstOrDefault(i => i.Id == itemId);

            if (itemToDelete == null)
            {
                Debug.WriteLine($"Item with ID '{itemId}' not found.");
                return;
            }

            string imagePath = itemToDelete.ImagePath;

            var updatedLines = new List<string>();
            foreach (var line in File.ReadAllLines(CollectionsFile))
            {
                var parts = line.Split('|');
                if (parts[0] == "I" && int.Parse(parts[1]) == itemId)
                {
                    if (File.Exists(imagePath))
                    {
                        try
                        {
                            File.Delete(imagePath);
                            Debug.WriteLine($"Image '{imagePath}' deleted successfully.");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error deleting image '{imagePath}': {ex.Message}");
                        }
                    }
                    continue;
                }
                updatedLines.Add(line);
            }

            File.WriteAllLines(CollectionsFile, updatedLines);
            Debug.WriteLine($"Item with ID '{itemId}' deleted successfully.");
        }

        private string SaveImageFromBase64(string base64Image, int collectionId)
        {
            if (string.IsNullOrEmpty(base64Image))
            {
                return string.Empty;
            }

            byte[] imageBytes = Convert.FromBase64String(base64Image);

            string directoryPath = Path.Combine(FileSystem.AppDataDirectory, "photos", collectionId.ToString());
            Directory.CreateDirectory(directoryPath);

            int fileIndex = 1;
            string imagePath;

            do
            {
                imagePath = Path.Combine(directoryPath, $"{fileIndex}.png");
                fileIndex++;
            } while (File.Exists(imagePath));

            File.WriteAllBytes(imagePath, imageBytes);
            return imagePath; 
        }

        private string ConvertImageToBase64(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return string.Empty;
            }

            byte[] imageBytes = File.ReadAllBytes(imagePath);
            return Convert.ToBase64String(imageBytes);
        }

        private string CopyImageToPhotos(string originalImagePath, int collectionId)
        {
            if (string.IsNullOrEmpty(originalImagePath) || !File.Exists(originalImagePath))
            {
                return string.Empty; 
            }


            string directoryPath = Path.Combine(FileSystem.AppDataDirectory, "photos", collectionId.ToString());
            Directory.CreateDirectory(directoryPath);


            int fileIndex = 1;
            string newImagePath;

            do
            {
                newImagePath = Path.Combine(directoryPath, $"{fileIndex}.png");
                fileIndex++;
            } while (File.Exists(newImagePath));


            File.Copy(originalImagePath, newImagePath);
            return newImagePath; 
        }

        private string ReplaceImage(string originalImagePath, string newImagePath, int collectionId)
        {
            File.Delete(originalImagePath);
            return CopyImageToPhotos(newImagePath, collectionId);
        }
    }
}