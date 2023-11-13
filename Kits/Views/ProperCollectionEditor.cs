using System.Collections;
using System.Windows;
namespace Kits.Views;

public partial class ProperCollectionEditor : Window
{
    public ProperCollectionEditor()
    {
        InitializeComponent();
    }

    private void ButtonAdd_OnClick(object sender, RoutedEventArgs e)
    {
        ((IList)DataContext).Add(Activator.CreateInstance(DataContext.GetType().GenericTypeArguments[0]));
    }
    private void ButtonDelete_OnClick(object sender, RoutedEventArgs e)
    {
        if (ElementsGrid.SelectedItem is { } item)
            ((IList)DataContext).Remove(item);
    }
}
