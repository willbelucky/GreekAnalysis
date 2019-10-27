using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreekAnalysis.Curves
{
    public class ZeroCouponCurve
    {
        private SortedDictionary<double, double> zeroCouponCurve;

        public ZeroCouponCurve(SortedDictionary<double, double> zeroCouponCurve)
        {
            this.zeroCouponCurve = zeroCouponCurve;
        }

        public SortedDictionary<double, double> ToSortedDictionary()
        {
            return zeroCouponCurve;
        }

        public double DiscountFactor(double yearFrac)
        {
            List<double> xs = zeroCouponCurve.Keys.ToList();
            List<double> ys = zeroCouponCurve.Values.ToList();
            double zeroCouponRate = RawInterpolation(yearFrac, xs, ys);
            double discountFactor = 1 / Math.Pow(1 + zeroCouponRate, yearFrac);

            return discountFactor;
        }

        private double RawInterpolation(double x, List<double> xs, List<double> ys)
        {
            double y;
            if (x <= xs.Min())
            {
                y = ys.First();
            }
            else if (x >= xs.Max())
            {
                y = ys.Last();
            }
            else
            {
                int i = xs.FindIndex(e => x < e);
                double x0 = xs[i - 1];
                double x1 = xs[i];
                double y0 = ys[i - 1];
                double y1 = ys[i];
                double xy = x0 * y0 + (x - x0) * (x1 * y1 - x0 * y0) / (x1 - x0);
                y = xy / x;
            }

            return y;
        }
    }
}
