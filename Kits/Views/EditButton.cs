using System.Windows;
using System.Windows.Controls;
namespace Kits.Views;

public partial class EditButton : UserControl
{
    public EditButton()
    {
        InitializeComponent();
    }
    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        new ProperCollectionEditor
        {
            DataContext = DataContext,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this)
        }.ShowDialog();
    }
}
