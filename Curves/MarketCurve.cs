using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SolverFoundation.Services;

namespace GreekAnalysis.Curves
{
    public class MarketCurve
    {
        private SortedDictionary<double, double> marketCurve;
        public int CouponFrequency { get; }

        public MarketCurve(SortedDictionary<double, double> marketCurve, int couponFrequency)
        {
            this.marketCurve = marketCurve;
            this.CouponFrequency = couponFrequency;
        }

        public SortedDictionary<double, double> ToSortedDictionary()
        {
            return marketCurve;
        }

        public ZeroCouponCurve BuildZeroCouponCurve()
        {
            // 1. Linear interpolate the market curve
            double gap = 1.0 / (double)CouponFrequency;
            SortedDictionary<double, double> interpMarketCurve = new SortedDictionary<double, double>();
            List<double> xs = marketCurve.Keys.ToList<double>();
            List<double> ys = marketCurve.Values.ToList<double>();
            for (double x = gap; x <= 50; x += gap)
            {
                interpMarketCurve.Add(x, LinearInterpolation(x, xs, ys));
            }

            // 2. Find discount factors with a bootstrap method
            List<double> discountFactors = new List<double>();
            List<double> couponRates = interpMarketCurve.Values.Select(x => x / (double)CouponFrequency).ToList<double>();
            for (int i = 0; i < interpMarketCurve.Count; i++)
            {
                double couponRate = couponRates[i];
                Func<Decision, Term> error = (df) =>
                {
                    Term guess = 0;
                    for (int j = 0; j < i; j++)
                    {
                        guess += couponRate * discountFactors[j];
                    }
                    guess += (1 + couponRate) * df;

                    return (guess - 1) * (guess - 1);
                };

                // Create solver context and model
                SolverContext context = SolverContext.GetContext();
                Model model = context.CreateModel();

                Decision df = new Decision(Domain.RealNonnegative, "df");
                df.SetInitialValue(1.0);
                model.AddDecision(df);

                model.AddGoal("error", GoalKind.Minimize, error(df));

                Solution solution = context.Solve();

                discountFactors.Add(df.GetDouble());
                context.ClearModel();
            }

            // 3. Calculate a zero coupon curve by discount factors
            SortedDictionary<double, double> zeroCouponCurve = new SortedDictionary<double, double>();
            List<double> yearFracs = interpMarketCurve.Keys.ToList<double>();
            for (int i = 0; i < interpMarketCurve.Count; i++)
            {
                double discountFactor = discountFactors[i];
                double yearFrac = yearFracs[i];
                double zeroCouponRate = Math.Pow(1 / discountFactor, 1 / yearFrac) - 1;
                zeroCouponCurve.Add(yearFrac, zeroCouponRate);
            }

            return new ZeroCouponCurve(zeroCouponCurve);
        }

        private double LinearInterpolation(double x, List<double> xs, List<double> ys)
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
                y = y0 + (x - x0) * (y1 - y0) / (x1 - x0);
            }

            return y;
        }
    }
}
