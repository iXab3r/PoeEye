using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Microsoft.SqlServer.Server;
using PoeBud.Models;

namespace PoeBud.Converters
{
    internal sealed class PoeSolutionToHumanReadableFormat : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var solutionsToProcess = new List<IPoeTradeSolution>();

            if (value is IPoeTradeSolution single)
            {
                solutionsToProcess.Add(single);
            } else if (value is IPoeTradeSolution[] array)
            {
                solutionsToProcess.AddRange(array);
            } else if (value is IEnumerable<IPoeTradeSolution> enumerable)
            {
                solutionsToProcess.AddRange(enumerable);
            }

            if (solutionsToProcess.Count == 1)
            {
                return $"{solutionsToProcess[0].Items.Length} items";
            }
            else if (solutionsToProcess.Count > 0)
            {
                var total = solutionsToProcess.Select(x => x.Items.Length).Sum();
                var itemsInSet = solutionsToProcess.Select(x => x.Items.Length).Average();
                return $"{total} items, {solutionsToProcess.Count} sets, ~{itemsInSet:F0} items in each set";
            }
            else
            {
                return "No matching items";
            }
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}