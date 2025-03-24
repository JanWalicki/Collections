using CollectionManagement.Models;
using CollectionManagement.ViewModels;

namespace CollectionManagement.Views;

public partial class CollectionPage : ContentPage
{
    public CollectionPage()
    {
        InitializeComponent();
        BindingContext = new CollectionPageViewModel();

    }
}