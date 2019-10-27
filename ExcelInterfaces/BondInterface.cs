using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDna.Integration;

using GreekAnalysis.Products;

namespace GreekAnalysis.ExcelInterfaces
{
    public static class BondInterface
    {
        [ExcelFunction(Name = "BondDelta", Description = "My first .NET function")]
        public static double BondDelta(
            DateTime evaluationDate, double tenor,
            DateTime issueDate, DateTime maturityDate, double couponRate, int couponFrequency, bool isForward,
            double m3, double m6, double m9, double m12, double m18, double m24, double m30,
            double y3, double y4, double y5, double y7, double y10, double y15, double y20
        )
        {
            SortedDictionary<double, double> marketCurve = new SortedDictionary<double, double>();
            marketCurve.Add(0.25, m3);
            marketCurve.Add(0.5, m6);
            marketCurve.Add(0.75, m9);
            marketCurve.Add(1, m12);
            marketCurve.Add(1.5, m18);
            marketCurve.Add(2, m24);
            marketCurve.Add(2.5, m30);
            marketCurve.Add(3, y3);
            marketCurve.Add(4, y4);
            marketCurve.Add(5, y5);
            marketCurve.Add(7, y7);
            marketCurve.Add(10, y10);
            marketCurve.Add(15, y15);
            marketCurve.Add(20, y20);


            Bond bond = new Bond(issueDate, maturityDate, couponRate, couponFrequency, isForward, marketCurve);
            double delta = bond.Delta(evaluationDate, tenor);

            return delta;
        }
    }
}
