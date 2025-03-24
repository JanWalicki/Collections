using CollectionManagement.ViewModels;

namespace CollectionManagement.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new MainPageViewModel();
    }

}