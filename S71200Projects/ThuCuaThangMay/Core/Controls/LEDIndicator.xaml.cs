using System;
using System.Collections.Generic;
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

namespace ThuCuaThangMay.Core.Controls
{
    /// <summary>
    /// Interaction logic for LEDIndicator.xaml
    /// </summary>
    public partial class LEDIndicator : UserControl
    {
        #region TitleProperty
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            name: "Title",
            propertyType: typeof(string),
            ownerType: typeof(LEDIndicator),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: null));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        #endregion

        #region OnColorProperty
        public static readonly DependencyProperty OnColorProperty = DependencyProperty.Register(
            name: "OnColor",
            propertyType: typeof(Brush),
            ownerType: typeof(LEDIndicator),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: Brushes.Red));

        public Brush OnColor
        {
            get => (Brush)GetValue(OnColorProperty);
            set => SetValue(OnColorProperty, value);
        }
        #endregion

        #region IsOnProperty
        public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(
            name: "IsOn",
            propertyType: typeof(bool),
            ownerType: typeof(LEDIndicator),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: false));

        public bool IsOn
        {
            get => (bool)GetValue(IsOnProperty);
            set => SetValue(IsOnProperty, value);
        }
        #endregion

        public LEDIndicator()
        {
            InitializeComponent();

            DataContext = this;
        }
    }
}
