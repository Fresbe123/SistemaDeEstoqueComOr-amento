using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GestãoEstoque.ClassesOrganização
{
    public class FavoritoToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Se o item for favorito (value = true), retorna cor roxa CLARA
            if (value is bool isFavorito && isFavorito)
            {
                // Cor roxa clara com transparência (RGB: 123, 31, 162 com 30% opacidade)
                return new SolidColorBrush(Color.FromArgb(30, 123, 31, 162));
            }

            // Se não for favorito, retorna transparente (sem cor)
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter 2: Transforma true/false em COR DE TEXTO
    public class FavoritoToTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Se o item for favorito (value = true), retorna cor roxa ESCURA
            if (value is bool isFavorito && isFavorito)
            {
                // Cor roxa escura (RGB: 103, 58, 183)
                return new SolidColorBrush(Color.FromRgb(103, 58, 183));
            }

            // Se não for favorito, retorna preto
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter 3: Transforma true/false em VISIBILIDADE (para as estrelas)
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Se tiver "inverse" no parâmetro, inverte a lógica
                if (parameter as string == "inverse")
                    return boolValue ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

                // Normal: true = Visível, false = Escondido
                return boolValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
            return System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
